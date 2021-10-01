using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using PX.SM;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

using PX.Objects.Common;
using PX.Objects.Extensions;
using PX.Objects.AR;
using PX.BarcodeProcessing;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.IN.WMS;

namespace PX.Objects.SO.WMS
{
	using WMSBase = WarehouseManagementSystem<PickPackShip, PickPackShip.Host>;

	public partial class PickPackShip : WMSBase
	{
		public sealed class PackMode : ScanMode
		{
			public const string Value = "PACK";
			public class value : BqlString.Constant<value> { public value() : base(PackMode.Value) { } }

			public override string Code => Value;
			public override string Description => Msg.Description;

			#region State Machine
			protected override IEnumerable<ScanState<PickPackShip>> CreateStates()
			{
				yield return new ShipmentState();
				yield return new LocationState()
					.Intercept.IsStateActive.ByConjoin(basis => !basis.HasPick && !basis.DefaultLocation)
					.Intercept.IsStateSkippable.ByDisjoin(basis => !basis.PromptLocationForEveryLine && basis.LocationID != null)
					.Intercept.Validate.ByAppend((basis, location) => basis.IsLocationMissing(Basis.Get<Logic>().PickedForPack, location, out var error) ? error : Validation.Ok);
				yield return new BoxState();
				yield return new InventoryItemState() { AlternateType = INPrimaryAlternateType.CPN, IsForIssue = true }
					.Intercept.Validate.ByAppend((basis, inventory) => basis.IsItemMissing(Basis.Get<Logic>().PickedForPack, inventory, out var error) ? error : Validation.Ok)
					.Intercept.HandleAbsence.ByAppend((basis, barcode) =>
					{
						if (basis.TryProcessBy<LocationState>(barcode, StateSubstitutionRule.KeepPositiveReports | StateSubstitutionRule.KeepApplication))
							return AbsenceHandling.Done;

						if (basis.Remove == false)
						{
							CSBox box = CSBox.PK.Find(basis, barcode);
							if (box != null)
							{
								if (!basis.Get<Logic>().AutoConfirmPackage(basis.Setup.Current.ConfirmEachPackageWeight == false))
									return AbsenceHandling.Done;

								if (basis.TryProcessBy<BoxState>(barcode, StateSubstitutionRule.KeepPositiveReports | StateSubstitutionRule.KeepApplication))
									return AbsenceHandling.Done;
							}
						}

						return AbsenceHandling.Skipped;
					});
				yield return new LotSerialState()
					.Intercept.IsStateActive.ByConjoin(basis => !basis.DefaultLotSerial || !basis.HasPick && basis.IsEnterableLotSerial(isForIssue: true))
					.Intercept.Validate.ByAppend((basis, lotSerialNbr) => basis.IsLotSerialMissing(Basis.Get<Logic>().PickedForPack, lotSerialNbr, out var error) ? error : Validation.Ok);
				yield return new ExpireDateState() { IsForIssue = true }
					.Intercept.IsStateActive.ByConjoin(basis =>
						!basis.HasPick &&
						basis.Get<Logic>().PickedForPack.SelectMain().Any(t => t.IsUnassigned == true || t.LotSerialNbr == basis.LotSerialNbr && t.PackedQty == 0));
				yield return new BoxWeightState();
				yield return new ConfirmState();
				yield return new CommandOrShipmentOnlyState();
			}

			protected override IEnumerable<ScanTransition<PickPackShip>> CreateTransitions()
			{
				return StateFlow(flow => flow
					.From<ShipmentState>()
					.NextTo<BoxState>()
					.NextTo<LocationState>()
					.NextTo<InventoryItemState>()
					.NextTo<LotSerialState>()
					.NextTo<ExpireDateState>());
			}

			protected override IEnumerable<ScanCommand<PickPackShip>> CreateCommands()
			{
				yield return new RemoveCommand();
				yield return new QtySupport.SetQtyCommand();
				yield return new ConfirmPackageCommand();
				yield return new ConfirmShipmentCommand();
			}

			protected override IEnumerable<ScanQuestion<PickPackShip>> CreateQuestions()
			{
				yield return new WeightSkipQuestion();
				yield return new ConfirmBoxQuestion(); // backward compatibility
			}

			protected override IEnumerable<ScanRedirect<PickPackShip>> CreateRedirects() => AllWMSRedirects.CreateFor<PickPackShip>();

			protected override void ResetMode(bool fullReset)
			{
				base.ResetMode(fullReset);

				Clear<ShipmentState>(when: fullReset && !Basis.IsWithinReset);
				Clear<BoxState>(when: fullReset);
				Clear<LocationState>(when: fullReset || Basis.PromptLocationForEveryLine);
				Clear<InventoryItemState>();
				Clear<LotSerialState>();
				Clear<ExpireDateState>();
				Clear<BoxWeightState>();

				if (fullReset)
					Get<Logic>().PackageLineNbrUI = null;
			}
			#endregion

			#region Logic
			public class Logic : ScanExtension
			{
				#region State
				public PackScanHeader PackHeader => Basis.Header.Get<PackScanHeader>() ?? new PackScanHeader();
				public ValueSetter<ScanHeader>.Ext<PackScanHeader> PackSetter => Basis.HeaderSetter.With<PackScanHeader>();

				#region PackageLineNbr
				public int? PackageLineNbr
				{
					get => PackHeader.PackageLineNbr;
					set => PackSetter.Set(h => h.PackageLineNbr, value);
				}
				#endregion
				#region PackageLineNbrUI
				public int? PackageLineNbrUI
				{
					get => PackHeader.PackageLineNbrUI;
					set => PackSetter.Set(h => h.PackageLineNbrUI, value);
				}
				#endregion
				#region Weight
				public decimal? Weight
				{
					get => PackHeader.Weight;
					set => PackSetter.Set(h => h.Weight, value);
				}
				#endregion
				#region LastWeighingTime
				public DateTime? LastWeighingTime
				{
					get => PackHeader.LastWeighingTime;
					set => PackSetter.Set(h => h.LastWeighingTime, value);
				}
				#endregion
				#endregion

				#region Views
				public
					SelectFrom<SOShipLineSplit>.
					InnerJoin<SOShipLine>.On<SOShipLineSplit.FK.ShipmentLine>.
					OrderBy<
						SOShipLineSplit.shipmentNbr.Asc,
						SOShipLineSplit.isUnassigned.Desc,
						SOShipLineSplit.lineNbr.Asc>.
					View PickedForPack;
				protected virtual IEnumerable pickedForPack()
				{
					var delegateResult = new PXDelegateResult { IsResultSorted = true };
					delegateResult.AddRange(Basis.GetSplits(Basis.RefNbr, includeUnassigned: true, s => s.PackedQty >= s.Qty));
					return delegateResult;
				}

				public SelectFrom<SOShipLineSplit>.View Packed;
				protected virtual IEnumerable packed()
				{
					return Basis.Header == null
						? Enumerable.Empty<PXResult<SOShipLineSplit, SOShipLine>>() :
						from link in Graph.PackageDetailExt.PackageDetailSplit.SelectMain(Basis.RefNbr, PackageLineNbrUI)
						from split in PickedForPack.Select().Cast<PXResult<SOShipLineSplit, SOShipLine>>()
						where
							((SOShipLineSplit)split).ShipmentNbr == link.ShipmentNbr &&
							((SOShipLineSplit)split).LineNbr == link.ShipmentLineNbr &&
							((SOShipLineSplit)split).SplitLineNbr == link.ShipmentSplitLineNbr
						select split;
				}

				public
					SelectFrom<SOPackageDetailEx>.
					Where<
						SOPackageDetailEx.shipmentNbr.IsEqual<WMSScanHeader.refNbr.FromCurrent>.
						And<SOPackageDetailEx.lineNbr.IsEqual<PackScanHeader.packageLineNbrUI.FromCurrent.NoDefault>>>.
					View ShownPackage;

				public PXSetupOptional<CommonSetup> CommonSetupUOM;
				#endregion

				#region Buttons
				public PXAction<ScanHeader> ReviewPack;
				[PXButton, PXUIField(DisplayName = "Review")]
				protected virtual IEnumerable reviewPack(PXAdapter adapter)
				{
					PackageLineNbrUI = null;
					return adapter.Get();
				}
				#endregion

				#region Event Handlers
				protected virtual void _(Events.RowSelected<ScanHeader> e)
				{
					if (e.Row == null)
						return;

					new[] {
						Base.Packages.Cache,
						Base.PackageDetailExt.PackageDetailSplit.Cache
					}
					.Modify(c => c.AllowInsert = c.AllowUpdate = c.AllowDelete = !Basis.DocumentIsConfirmed)
					.Consume();

					ReviewPack.SetVisible(Base.IsMobile && e.Row.Mode == PackMode.Value);
				}
				#endregion

				public virtual bool CanPack => Basis.HasPick
					? PickedForPack.SelectMain().Any(s => s.PackedQty < s.PickedQty)
					: PickedForPack.SelectMain().Any(s => s.PackedQty < s.Qty);

				public SOPackageDetailEx SelectedPackage => Base.Packages.SelectMain().FirstOrDefault(t => t.LineNbr == PackageLineNbr);

				public virtual bool HasUnconfirmedBoxes => Graph.Packages.SelectMain().Any(p => p.Confirmed == false);
				public virtual bool HasSingleAutoPackage(string shipmentNbr, out SOPackageDetailEx package)
				{
					if (PXAccess.FeatureInstalled<FeaturesSet.autoPackaging>())
					{
						var packages =
							SelectFrom<SOPackageDetailEx>.
							Where<SOPackageDetailEx.shipmentNbr.IsEqual<@P.AsString>>.
							View.Select(Basis, shipmentNbr)
							.RowCast<SOPackageDetailEx>()
							.ToArray();
						if (packages.Length == 1 && packages[0].PackageType == SOPackageType.Auto)
						{
							package = packages[0];
							return true;
						}
						else if (packages.Any(p => p.PackageType == SOPackageType.Auto))
							throw new PXInvalidOperationException(Msg.CannotBePacked, shipmentNbr);
					}

					package = null;
					return false;
				}

				public virtual void ConfirmPackage()
				{
					if (Basis.CurrentState is BoxWeightState)
					{
						SkipBoxWeightInput();
					}
					else if (TryConfirmPackedBox())
					{
						if (Basis.Setup.Current.ConfirmEachPackageWeight == false && SelectedPackage?.Confirmed == false)
							SkipBoxWeightInput();
					}
				}

				protected virtual bool TryConfirmPackedBox()
				{
					SOPackageDetailEx package = SelectedPackage;
					if (package == null)
						return false;

					Weight =
						package.Weight == 0
							? AutoCalculateBoxWeightBasedOnItems(package)
							: package.Weight.Value;

					if (UserSetup.For(Graph).UseScale != true)
						Basis.SetScanState<BoxWeightState>(Msg.BoxConfirm, package.BoxID, Weight, CommonSetupUOM.Current.WeightUOM);
					else if (ProcessScaleWeight(package) == false)
						Basis.SetScanState<BoxWeightState>();

					return true;
				}

				protected virtual decimal AutoCalculateBoxWeightBasedOnItems(SOPackageDetailEx package)
				{
					decimal calculatedWeight = CSBox.PK.Find(Basis, package.BoxID)?.BoxWeight ?? 0m;
					SOShipLineSplitPackage[] links = Graph.PackageDetailExt.PackageDetailSplit.SelectMain(package.ShipmentNbr, package.LineNbr);
					foreach (var link in links)
					{
						var inventory = InventoryItem.PK.Find(Graph, link.InventoryID);
						calculatedWeight += (inventory.BaseWeight ?? 0) * (link.BasePackedQty ?? 0);
					}

					return Math.Round(calculatedWeight, 4);
				}

				public virtual bool SkipBoxWeightInput()
				{
					if (Weight.IsIn(null, 0))
					{
						Basis.ReportError(Msg.BoxWeightNoSkip);
						return false;
					}
					else
					{
						try
						{
							SetPackageWeightAndConfirm(Weight.Value);
							return true;
						}
						catch (PXSetPropertyException outer) when (Basis.Setup.Current.ConfirmEachPackageWeight == false && outer.InnerException is PXSetPropertyException inner)
						{
							Basis.SetScanState<BoxWeightState>();
							Basis.ReportError(inner.MessageNoPrefix);
							return false;
						}
					}
				}

				public virtual bool AutoConfirmPackage(bool skipBoxWeightInput)
				{
					if (PackageLineNbr != null)
					{
						var package = SelectedPackage;
						if (package != null)
						{
							if (TryConfirmPackedBox())
							{
								if (skipBoxWeightInput && package.Confirmed == false)
									return SkipBoxWeightInput();
							}
							else
								return false;
						}
					}
					return true;
				}

				public virtual void SetPackageWeightAndConfirm(decimal value)
				{
					SOPackageDetailEx packageDetail = SelectedPackage;
					if (packageDetail != null)
					{
						packageDetail.Weight = Math.Round(value, 4);
						packageDetail.Confirmed = true;
						Base.Packages.Update(packageDetail);
						Basis.Save.Press();

						Basis.Clear<BoxState>();
						Basis.Reset(fullReset: false);

						Basis.SetScanState(
							Basis.CurrentMode.DefaultState.Code,
							Msg.BoxConfirmed,
							packageDetail.Weight, CommonSetupUOM.Current.WeightUOM);
					}
				}

				protected virtual bool? ProcessScaleWeight(SOPackageDetailEx package)
				{
					Guid? scaleDeviceID = UserSetup.For(Graph).ScaleDeviceID;

					Graph.Caches<SMScale>().ClearQueryCache();
					SMScale scale = SMScale.PK.Find(Basis, scaleDeviceID);

					if (scale == null)
					{
						Basis.ReportError(Msg.ScaleMissing, "");
						return false;
					}

					DateTime dbNow = GetServerTime();

					if (scale.LastModifiedDateTime.Value.AddHours(1) < dbNow)
					{
						Basis.ReportError(Msg.ScaleDisconnected, scale.ScaleID);
						return false;
					}
					else if (scale.LastWeight.GetValueOrDefault() == 0)
					{
						if (LastWeighingTime == scale.LastModifiedDateTime.Value)
						{
							SkipBoxWeightInput();
							return true;
						}
						else
						{
							Basis.ReportWarning(Msg.ScaleNoBox, scale.ScaleID);
							Basis.Prompt(Msg.ScaleSkipPrompt);
							LastWeighingTime = scale.LastModifiedDateTime.Value;
							return null;
						}
					}
					else if (scale.LastModifiedDateTime.Value.AddSeconds(ScaleWeightValiditySeconds) < dbNow)
					{
						Basis.ReportError(Msg.ScaleTimeout, scale.ScaleID, ScaleWeightValiditySeconds);
						return null;
					}
					else
					{
						decimal weight = ConvertKilogramToWeightUnit(scale.LastWeight.GetValueOrDefault(), CommonSetupUOM.Current.WeightUOM);
						SetPackageWeightAndConfirm(weight);
						return true;
					}
				}

				protected virtual decimal ConvertKilogramToWeightUnit(decimal weight, string weightUnit)
				{
					weightUnit = weightUnit.Trim().ToUpperInvariant();
					decimal conversionFactor =
						weightUnit == "KG" ? 1m :
						weightUnit == "LB" ? 0.453592m :
						throw new PXException(Msg.BoxWeightWrongUnit, weightUnit);

					return weight / conversionFactor;
				}

				protected virtual DateTime GetServerTime()
				{
					DateTime dbNow;
					PXDatabase.SelectDate(out DateTime _, out dbNow);
					dbNow = PXTimeZoneInfo.ConvertTimeFromUtc(dbNow, LocaleInfo.GetTimeZone());
					return dbNow;
				}

				public virtual double ScaleWeightValiditySeconds => 30;
			}
			#endregion

			#region States
			public new sealed class ShipmentState : PickPackShip.ShipmentState
			{
				protected override AbsenceHandling.Of<SOShipment> HandleAbsence(string barcode)
				{
					SOShipment shipmentInTote =
						SelectFrom<SOShipment>.
						InnerJoin<SOPickerToShipmentLink>.On<SOPickerToShipmentLink.FK.Shipment>.
						InnerJoin<INTote>.On<SOPickerToShipmentLink.FK.Tote>.
						InnerJoin<SOPicker>.On<SOPickerToShipmentLink.FK.Picker>.
						InnerJoin<SOPickingWorksheet>.On<SOPicker.FK.Worksheet>.
						Where<
							INTote.toteCD.IsEqual<@P.AsString>.
							And<SOPickingWorksheet.worksheetType.IsEqual<SOPickingWorksheet.worksheetType.wave>>.
							And<SOPicker.confirmed.IsEqual<True>>.
							And<SOShipment.picked.IsEqual<True>>.
							And<SOShipment.confirmed.IsEqual<False>>.
							And<Not<Exists<
								SelectFrom<SOShipLineSplit>.
								Where<
									SOShipLineSplit.shipmentNbr.IsEqual<SOShipment.shipmentNbr>.
									And<SOShipLineSplit.packedQty.IsNotEqual<decimal0>>>>>>>.
						View.Select(Basis, barcode).TopFirst;

					if (shipmentInTote != null)
						return AbsenceHandling.ReplaceWith(shipmentInTote);

					return base.HandleAbsence(barcode);
				}

				protected override Validation Validate(SOShipment shipment)
				{
					if (shipment.Operation != SOOperation.Issue)
						return Validation.Fail(Msg.InvalidOperation, shipment.ShipmentNbr, Basis.SightOf<SOShipment.operation>(shipment));

					if (shipment.Status != SOShipmentStatus.Open)
						return Validation.Fail(Msg.InvalidStatus, shipment.ShipmentNbr, Basis.SightOf<SOShipment.status>(shipment));

					if (Basis.HasPick && Basis.GetSplits(shipment.ShipmentNbr, includeUnassigned: true).RowCast<SOShipLineSplit>().All(s => s.PickedQty == 0))
						return Validation.Fail(Msg.ShouldBePickedFirst, shipment.ShipmentNbr);

					Get<PackMode.Logic>().HasSingleAutoPackage(shipment.ShipmentNbr, out var _);

					return Validation.Ok;
				}

				protected override void ReportSuccess(SOShipment shipment) => Basis.ReportInfo(Msg.Ready, shipment.ShipmentNbr);

				protected override void SetNextState()
				{
					var mode = Basis.Get<PackMode.Logic>();
					if (Basis.Remove == false && mode.CanPack == false && (!mode.HasUnconfirmedBoxes || mode.HasSingleAutoPackage(Basis.RefNbr, out var _)))
						Basis.SetScanState(BuiltinScanStates.Command, PackMode.Msg.Completed, Basis.RefNbr);
					else
						base.SetNextState();
				}

				#region Messages
				[PXLocalizable]
				public new abstract class Msg : PickPackShip.ShipmentState.Msg
				{
					public new const string Ready = "The {0} shipment is loaded and ready to be packed.";
					public const string InvalidStatus = "The {0} shipment cannot be packed because it has the {1} status.";
					public const string InvalidOperation = "The {0} shipment cannot be packed because it has the {1} operation.";
					public const string ShouldBePickedFirst = "The {0} shipment cannot be packed because the items have not been picked.";
				}
				#endregion
			}

			public sealed class BoxState : EntityState<CSBox>
			{
				public const string Value = "BOX";
				public class value : BqlString.Constant<value> { public value() : base(BoxState.Value) { } }

				public PackMode.Logic Mode => Get<PackMode.Logic>();

				public override string Code => Value;
				protected override string StatePrompt => Msg.Prompt;

				protected override bool IsStateSkippable() => base.IsStateSkippable() || Mode.PackageLineNbr != null;

				protected override void OnTakingOver()
				{
					if (Mode.HasSingleAutoPackage(Basis.RefNbr, out SOPackageDetailEx package)) // skip single auto package input
					{
						Mode.PackageLineNbr = package.LineNbr;
						Basis.Graph.Packages.Current = package;
						Basis.DispatchNext();
					}
				}

				protected override CSBox GetByBarcode(string barcode) => CSBox.PK.Find(Basis, barcode);

				protected override void Apply(CSBox box)
				{
					SOPackageDetailEx package = Basis.Graph.Packages.SelectMain().FirstOrDefault(p => string.Equals(p.BoxID.Trim(), box.BoxID.Trim(), StringComparison.OrdinalIgnoreCase) && p.Confirmed == false);
					if (package == null)
					{
						package = (SOPackageDetailEx)Basis.Graph.Packages.Cache.CreateInstance();
						package.BoxID = box.BoxID;
						package.ShipmentNbr = Basis.RefNbr;
						package = Basis.Graph.Packages.Insert(package);
						Basis.Save.Press();
					}

					Mode.PackageLineNbr = package.LineNbr;
					Basis.Graph.Packages.Current = package;

					Basis.Ask<ConfirmBoxQuestion>(); // for backward compatibility
				}

				protected override void ClearState()
				{
					Mode.PackageLineNbr = null;
					Basis.Graph.Packages.Current = null;
				}

				protected override void ReportSuccess(CSBox entity) => Basis.ReportInfo(Msg.Ready, entity.BoxID);
				protected override void ReportMissing(string barcode) => Basis.ReportError(Msg.Missing, barcode);

				#region Messages
				[PXLocalizable]
				public abstract class Msg
				{
					public const string Prompt = "Scan the box barcode.";
					public const string Ready = "The {0} box is selected.";
					public const string Missing = "The {0} box is not found in the database.";
				}
				#endregion
			}

			public sealed class BoxWeightState : EntityState<decimal?>
			{
				public const string Value = "BWGT";
				public class value : BqlString.Constant<value> { public value() : base(BoxWeightState.Value) { } }

				public override string Code => Value;
				protected override string StatePrompt => Msg.Prompt;

				protected override void OnTakingOver()
				{
					var mode = Get<PackMode.Logic>();
					if (mode.HasSingleAutoPackage(Basis.RefNbr, out var _)) // package is already weighed
						Basis.SetScanState(BuiltinScanStates.Command, PackMode.Msg.Completed, Basis.RefNbr);
					else
						Basis.Ask<WeightSkipQuestion>();
				}

				protected override decimal? GetByBarcode(string barcode) => decimal.TryParse(barcode, out decimal value) ? value : (decimal?)null;
				protected override void ReportMissing(string barcode) => Basis.ReportError(Msg.BadFormat);
				protected override void Apply(decimal? value) => Get<PackMode.Logic>().SetPackageWeightAndConfirm(value.Value);

				protected override void ClearState()
				{
					var logic = Get<PackMode.Logic>();
					logic.Weight = null;
					logic.LastWeighingTime = null;
					Basis.RevokeQuestion<WeightSkipQuestion>();
				}

				// both are done by the SetPackageWeightAndConfirm method
				protected override void ReportSuccess(decimal? entity) { }
				protected override void SetNextState() { }

				#region Messages
				[PXLocalizable]
				public abstract class Msg
				{
					public const string Prompt = "Enter the actual total weight of the package. To skip weighting, click OK.";
					public const string BadFormat = "The quantity format does not fit the locale settings.";
				}
				#endregion
			}

			public sealed class ConfirmState : ConfirmationState
			{
				public sealed override string Prompt => Basis.Localize(Msg.Prompt, Basis.SightOf<WMSScanHeader.inventoryID>(), Basis.Qty, Basis.UOM);

				protected sealed override FlowStatus PerformConfirmation() => Get<Logic>().Confirm();

				#region Logic
				public class Logic : ScanExtension
				{
					protected PackMode.Logic Mode { get; private set; }
					public override void Initialize() => Mode = Basis.Get<PackMode.Logic>();

					public virtual FlowStatus Confirm()
					{
						bool locationIsRequired = Basis.HasActive<LocationState>();

						var nothingToDoError = FlowStatus.Fail(Basis.Remove == true ? Msg.NothingToRemove : Msg.NothingToPack);

						if (Mode.PackageLineNbr == null)
							return nothingToDoError;

						var packageDetail = Mode.SelectedPackage;

						if (Basis.InventoryID == null || Basis.Qty == 0)
							return nothingToDoError;

						void KeepPackageSelection() => Mode.PackageLineNbr = packageDetail.LineNbr;

						var packedSplits = Mode.PickedForPack.SelectMain().Where(r =>
							r.InventoryID == Basis.InventoryID &&
							r.SubItemID == Basis.SubItemID &&
							(r.IsUnassigned == true || r.LotSerialNbr == (Basis.LotSerialNbr ?? r.LotSerialNbr)) &&
							locationIsRequired.Implies(r.LocationID == (Basis.LocationID ?? r.LocationID)) &&
							(Basis.Remove == true ? r.PackedQty > 0 : TargetQty(r) > r.PackedQty));

						if (Basis.HasPick == false && Basis.Shipment?.PickedViaWorksheet == false)
							packedSplits = packedSplits
							.OrderByDescending(split => split.IsUnassigned == false)
							.OrderByDescending(split => Basis.Remove == true ? split.PickedQty > 0 : split.Qty > split.PickedQty)
							.ThenByDescending(split => split.LotSerialNbr == (Basis.LotSerialNbr ?? split.LotSerialNbr))
							.ThenByDescending(split => string.IsNullOrEmpty(split.LotSerialNbr))
							.ThenByDescending(split => (split.Qty > split.PickedQty || Basis.Remove == true) && split.PickedQty > 0)
							.ThenByDescending(split => Sign.MinusIf(Basis.Remove == true) * (split.Qty - split.PickedQty));
						if (packedSplits.Any() == false)
							return nothingToDoError.WithModeReset.WithPostAction(KeepPackageSelection);

						decimal qty = Basis.BaseQty;
						string inventoryCD = Basis.SightOf<WMSScanHeader.inventoryID>();

						if (Basis.Remove == true ? packedSplits.Sum(s => s.PackedQty) - qty < 0 : packedSplits.Sum(s => TargetQty(s) - s.PackedQty) < qty)
							return FlowStatus.Fail(Basis.Remove == true ? Msg.BoxCanNotUnpack : Msg.BoxCanNotPack, inventoryCD, Basis.Qty, Basis.UOM);

						decimal unassignedQty = Sign.MinusIf(Basis.Remove == true) * qty;
						foreach (var packedSplit in packedSplits)
						{
							decimal currentQty = Basis.Remove == true
								? -Math.Min(packedSplit.PackedQty.Value, -unassignedQty)
								: +Math.Min(TargetQty(packedSplit).Value - packedSplit.PackedQty.Value, unassignedQty);

							if (PackSplit(packedSplit, packageDetail, currentQty) == false)
								return FlowStatus.Fail(Basis.Remove == true ? Msg.BoxCanNotUnpack : Msg.BoxCanNotPack, inventoryCD, Basis.Qty, Basis.UOM);

							unassignedQty -= currentQty;
							if (unassignedQty == 0)
								break;
						}

						bool packageRemoved;
						if (Basis.Graph.PackageDetailExt.PackageDetailSplit.Select(packageDetail.ShipmentNbr, packageDetail.LineNbr).Count == 0)
						{
							Basis.Graph.Packages.Delete(packageDetail);
							Basis.Clear<BoxState>();
							Mode.PackageLineNbrUI = null;
							packageRemoved = true;
						}
						else
						{
							Mode.PackageLineNbrUI = Mode.PackageLineNbr;
							packageRemoved = false;
						}

						Basis.EnsureShipmentUserLink();

						string boxConfirmPrompt = packageRemoved || !Mode.HasUnconfirmedBoxes || Mode.HasSingleAutoPackage(Basis.RefNbr, out var _)
							? null
							: Mode.CanPack == false
								? Msg.BoxConfirmPrompt
								: Basis.HasPick
									? Msg.BoxConfirmOrContinuePrompt
									: Msg.BoxConfirmOrContinuePromptNoPick;

						Basis.DispatchNext(
							Basis.Remove == true ? Msg.InventoryRemoved : Msg.InventoryAdded,
							Basis.SightOf<WMSScanHeader.inventoryID>(), Basis.Qty, Basis.UOM);

						if (packageRemoved)
						{
							Basis.RevokeQuestion<ConfirmBoxQuestion>();
							return FlowStatus.Ok;
						}
						else
						{
							Basis.Ask<ConfirmBoxQuestion>();
							return FlowStatus.Ok.WithPrompt(boxConfirmPrompt);
						}
					}

					public virtual decimal? TargetQty(SOShipLineSplit split) => Basis.HasPick ? split.PickedQty : split.Qty * Basis.Graph.GetQtyThreshold(split);

					protected virtual bool IsSelectedSplit(SOShipLineSplit split)
					{
						return
							split.InventoryID == Basis.InventoryID &&
							split.SubItemID == Basis.SubItemID &&
							split.SiteID == Basis.SiteID &&
							split.LocationID == (Basis.LocationID ?? split.LocationID) &&
							split.LotSerialNbr == (Basis.LotSerialNbr ?? split.LotSerialNbr);
					}

					public virtual bool PackSplit(SOShipLineSplit split, SOPackageDetailEx package, decimal qty)
					{
						if (Basis.HasPick)
							Basis.EnsureAssignedSplitEditing(split);
						else if (split.IsUnassigned == true)
						{
							var existingSplit = Mode.PickedForPack.SelectMain().FirstOrDefault(s =>
								s.LineNbr == split.LineNbr &&
								s.IsUnassigned == false &&
								s.LotSerialNbr == (Basis.LotSerialNbr ?? s.LotSerialNbr) &&
								IsSelectedSplit(s));

							if (existingSplit == null)
							{
								var newSplit = PXCache<SOShipLineSplit>.CreateCopy(split);
								newSplit.SplitLineNbr = null;
								newSplit.LotSerialNbr = Basis.LotSerialNbr;
								newSplit.ExpireDate = Basis.ExpireDate;
								newSplit.Qty = qty;
								newSplit.PickedQty = qty;
								newSplit.PackedQty = 0;
								newSplit.IsUnassigned = false;
								newSplit.PlanID = null;

								split = Mode.PickedForPack.Insert(newSplit);
							}
							else
							{
								existingSplit.Qty += qty;
								existingSplit.ExpireDate = Basis.ExpireDate;
								split = Mode.PickedForPack.Update(existingSplit);
							}
						}

						SOShipLineSplitPackage link = Graph.PackageDetailExt.PackageDetailSplit
							.SelectMain(package.ShipmentNbr, package.LineNbr)
							.FirstOrDefault(t =>
								t.ShipmentNbr == split.ShipmentNbr
								&& t.ShipmentLineNbr == split.LineNbr
								&& t.ShipmentSplitLineNbr == split.SplitLineNbr);

						if (qty < 0)
						{
							if (link == null || link.PackedQty + qty < 0)
								return false;

							if (Basis.IsEnterableLotSerial(isForIssue: true))
							{
								split.Qty += qty;
								if (split.Qty == 0)
									split = Mode.PickedForPack.Delete(split);
								else
									split = Mode.PickedForPack.Update(split);
							}

							if (link.PackedQty + qty > 0)
							{
								link.PackedQty += qty;
								Graph.PackageDetailExt.PackageDetailSplit.Update(link);
							}
							else if (link.PackedQty + qty == 0)
							{
								Graph.PackageDetailExt.PackageDetailSplit.Delete(link);
							}

							package.Confirmed = false;
							Graph.Packages.Update(package);
						}
						else
						{
							if (link == null)
							{
								link = (SOShipLineSplitPackage)Base.PackageDetailExt.PackageDetailSplit.Cache.CreateInstance();

								PXFieldVerifying ver = (c, a) => a.Cancel = true;
								Graph.FieldVerifying.AddHandler<SOShipLineSplitPackage.shipmentSplitLineNbr>(ver);
								link.ShipmentSplitLineNbr = split.SplitLineNbr;
								link.PackedQty = qty;
								link = Graph.PackageDetailExt.PackageDetailSplit.Insert(link);
								Graph.FieldVerifying.RemoveHandler<SOShipLineSplitPackage.shipmentSplitLineNbr>(ver);

								link.ShipmentNbr = split.ShipmentNbr;
								link.ShipmentLineNbr = split.LineNbr;
								link.PackageLineNbr = package.LineNbr;
								link.InventoryID = split.InventoryID;
								link.SubItemID = split.SubItemID;
								link.LotSerialNbr = split.LotSerialNbr;
								link.UOM = split.UOM;

								link = Graph.PackageDetailExt.PackageDetailSplit.Update(link);
							}
							else
							{
								link.PackedQty += qty;
								Graph.PackageDetailExt.PackageDetailSplit.Update(link);
							}
						}

						return true;
					}
				}
				#endregion

				#region Messages
				[PXLocalizable]
				public abstract class Msg
				{
					public const string Prompt = "Confirm packing {0} x {1} {2}.";

					public const string NothingToPack = "No items to pack.";
					public const string NothingToRemove = "No items to remove from the shipment.";

					public const string InventoryAdded = "{0} x {1} {2} has been added to the shipment.";
					public const string InventoryRemoved = "{0} x {1} {2} has been removed from the shipment.";

					public const string BoxCanNotPack = "Cannot pack {1} {2} of {0}.";
					public const string BoxCanNotUnpack = "Cannot unpack {1} {2} of {0}.";

					public const string BoxConfirmPrompt = "Confirm the package.";
					public const string BoxConfirmOrContinuePrompt = "Confirm package or scan the next item.";
					public const string BoxConfirmOrContinuePromptNoPick = "Confirm package, or scan the next item or the next location.";
				}
				#endregion
			}
			#endregion

			#region Commands
			public sealed class ConfirmPackageCommand : ScanCommand
			{
				public PackMode.Logic Mode => Get<PackMode.Logic>();

				public override string Code => "PACKAGE*CONFIRM";
				public override string ButtonName => "scanConfirmPackage";
				public override string DisplayName => Msg.DisplayName;
				protected override bool IsEnabled => Basis.CurrentState is BoxWeightState || Mode.PackageLineNbr != null && Mode.HasUnconfirmedBoxes && Mode.HasSingleAutoPackage(Basis.RefNbr, out var _) == false;
				protected override bool Process()
				{
					Mode.ConfirmPackage();
					return true;
				}

				#region Messages
				[PXLocalizable]
				public abstract class Msg
				{
					public const string DisplayName = "Confirm Package";
				}
				#endregion
			}

			public sealed class RemoveCommand : WMSBase.RemoveCommand
			{
				protected override bool IsEnabled => base.IsEnabled && Get<PackMode.Logic>().HasUnconfirmedBoxes;
			}
			#endregion

			#region Questions
			public sealed class WeightSkipQuestion : ScanQuestion
			{
				public PackMode.Logic Mode => Get<PackMode.Logic>();

				public override string Code => "SKIPWEIGHT";

				protected override void Confirm() => Mode.ConfirmPackage();
				protected override void Reject() { }
			}

			public sealed class ConfirmBoxQuestion : ScanQuestion // for backward compatibility, so OK can confirm a package
			{
				public PackMode.Logic Mode => Get<PackMode.Logic>();

				public override string Code => "CONFIRMBOX";

				protected override void Confirm() => Mode.ConfirmPackage();
				protected override void Reject() { }
			}
			#endregion

			#region Redirect
			public sealed class RedirectFrom<TForeignBasis> : WMSBase.RedirectFrom<TForeignBasis>.SetMode<PackMode>
				where TForeignBasis : PXGraphExtension, IBarcodeDrivenStateMachine
			{
				public override string Code => PackMode.Value;
				public override string DisplayName => Msg.Description;

				[Obsolete(PickPackShip.ObsoleteMsg.ScanMember)]
				public override string ButtonName => "scanModePack";

				private string RefNbr { get; set; }

				public override bool IsPossible
				{
					get
					{
						bool wmsFulfillment = PXAccess.FeatureInstalled<CS.FeaturesSet.wMSFulfillment>();
						var ppsSetup = SOPickPackShipSetup.PK.Find(Basis.Graph, Basis.Graph.Accessinfo.BranchID);
						return wmsFulfillment && ppsSetup?.ShowPackTab == true;
					}
				}

				protected override bool PrepareRedirect()
				{
					if (Basis is PickPackShip pps && pps.RefNbr != null && pps.DocumentIsConfirmed == false)
					{
						if (pps.FindMode<PackMode>().TryValidate(pps.Shipment).By<ShipmentState>() is Validation valid && valid.IsError == true)
						{
							pps.ReportError(valid.Message, valid.MessageArgs);
							return false;
						}
						else
							RefNbr = pps.RefNbr;
					}
					
					return true;
				}

				protected override void CompleteRedirect()
				{
					if (Basis is PickPackShip pps && pps.CurrentMode.Code != ReturnMode.Value && this.RefNbr != null)
						if (pps.TryProcessBy(PickPackShip.ShipmentState.Value, RefNbr, StateSubstitutionRule.KeepAll & ~StateSubstitutionRule.KeepPositiveReports))
						{
							pps.SetScanState(pps.CurrentMode.DefaultState.Code);
							RefNbr = null;
						}
				}
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public new abstract class Msg : ScanMode.Msg
			{
				public const string Description = "Pack";
				public const string PackedQtyPerBox = "Packed Qty.";

				public const string Completed = "The {0} shipment is packed.";
				public const string CannotBePacked = "The {0} shipment cannot be processed in the Pack mode because the shipment has two or more packages assigned.";

				public const string BoxConfirm = "The {0} package is ready to be confirmed. The calculated weight is {1} {2}.";
				public const string BoxConfirmed = "The package is confirmed, and its weight is set to {0} {1}.";

				public const string BoxWeightNoSkip = "The package does not have a predefined weight. Enter the package weight.";
				public const string BoxWeightWrongUnit = "Wrong weight unit: {0}, only KG and LB are supported.";

				public const string ScaleMissing = "The {0} scale is not found in the database.";
				public const string ScaleDisconnected = "No information from the {0} scales. Check connection of the scales.";
				public const string ScaleTimeout = "Measurement on the {0} scale is more than {1} seconds old. Remove the package from the scale and weigh it again.";
				public const string ScaleNoBox = "No information from the {0} scales. Make sure that items are on the scales.";
				public const string ScaleSkipPrompt = "Put a package on the scales and click OK. To skip weighting, do not use the scales and click OK.";
			}
			#endregion

			#region Attached Fields
			[PXUIField(Visible = false)]
			public class ShowPack : FieldAttached.To<ScanHeader>.AsBool.Named<ShowPack>
			{
				public override bool? GetValue(ScanHeader row) => Base.WMS.HasPack && row.Mode == PackMode.Value;
			}

			[PXUIField(DisplayName = Msg.PackedQtyPerBox)]
			public class PackedQtyPerBox : FieldAttached.To<SOShipLineSplit>.AsDecimal.Named<PackedQtyPerBox>
			{
				public override decimal? GetValue(SOShipLineSplit row)
				{
					SOShipLineSplitPackage package = Base.PackageDetailExt.PackageDetailSplit
						.SelectMain(Base.WMS.RefNbr, Base.WMS.Get<PackMode.Logic>().PackageLineNbrUI)
						.FirstOrDefault(t => t.ShipmentSplitLineNbr == row.SplitLineNbr);
					return package?.PackedQty ?? 0m;
				}
			}
			#endregion
		}
	}

	public sealed class PackScanHeader : PXCacheExtension<WMSScanHeader, QtyScanHeader, ScanHeader>
	{
		#region PackageLineNbr
		[PXInt]
		[PXFormula(typeof(Null.When<WMSScanHeader.refNbr.IsNull>.Else<packageLineNbr>))]
		public int? PackageLineNbr { get; set; }
		public abstract class packageLineNbr : BqlInt.Field<packageLineNbr> { }
		#endregion
		#region PackageLineNbrUI
		[PXInt]
		[PXUIField(DisplayName = "Package")]
		[PXSelector(
			typeof(SearchFor<SOPackageDetailEx.lineNbr>.Where<SOPackageDetailEx.shipmentNbr.IsEqual<WMSScanHeader.refNbr.FromCurrent>>),
			typeof(BqlFields.FilledWith<
				SOPackageDetailEx.confirmed,
				SOPackageDetailEx.lineNbr,
				SOPackageDetailEx.boxID,
				SOPackageDetailEx.boxDescription,
				SOPackageDetailEx.weight,
				SOPackageDetailEx.maxWeight,
				SOPackageDetailEx.weightUOM,
				SOPackageDetailEx.carrierBox,
				SOPackageDetailEx.length,
				SOPackageDetailEx.width,
				SOPackageDetailEx.height>),
			DescriptionField = typeof(SOPackageDetailEx.boxID), DirtyRead = true, SuppressUnconditionalSelect = true)]
		[PXFormula(typeof(packageLineNbr.When<packageLineNbr.IsNotNull>.Else<packageLineNbrUI>))]
		public int? PackageLineNbrUI { get; set; }
		public abstract class packageLineNbrUI : BqlInt.Field<packageLineNbrUI> { }
		#endregion
		#region Weight
		[PXDecimal(6)]
		[PXUnboundDefault(TypeCode.Decimal, "0.0")]
		public decimal? Weight { get; set; }
		public abstract class weight : BqlDecimal.Field<weight> { }
		#endregion
		#region LastWeighingTime
		[PXDate]
		public DateTime? LastWeighingTime { get; set; }
		public abstract class lastWeighingTime : BqlDateTime.Field<lastWeighingTime> { }
		#endregion
	}
}