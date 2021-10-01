using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.BarcodeProcessing;

namespace PX.Objects.IN.WMS
{
	using WMSBase = INScanRegisterBase<INScanTransfer, INScanTransfer.Host, INDocType.transfer>;

	public class INScanTransfer : WMSBase
	{
		public class Host : INTransferEntry { }

		public new class QtySupport : WMSBase.QtySupport { }
		public new class GS1Support : WMSBase.GS1Support { }
		public new class UserSetup : WMSBase.UserSetup { }

		#region Configuration
		public override bool ExplicitConfirmation => Setup.Current.ExplicitLineConfirmation == true;

		public override bool PromptLocationForEveryLine => Setup.Current.RequestLocationForEachItemInTransfer == true;
		public override bool UseDefaultReasonCode => Setup.Current.UseDefaultReasonCodeInTransfer == true;
		public override bool UseDefaultWarehouse => UserSetup.For(Graph).DefaultWarehouse == true;

		protected override bool UseQtyCorrectection => Setup.Current.UseDefaultQtyInTransfer != true;
		protected override bool CanOverrideQty => (!DocumentLoaded || NotReleasedAndHasLines) && SelectedLotSerialClass?.LotSerTrack != INLotSerTrack.SerialNumbered;

		public override void Initialize()
		{
			base.Initialize();
			Base.SuppressLocationDefaultingForWMS = true;
		}
		#endregion

		#region State
		public TransferScanHeader TransferHeader => Header.Get<TransferScanHeader>();
		public ValueSetter<ScanHeader>.Ext<TransferScanHeader> TransferSetter => HeaderSetter.With<TransferScanHeader>();

		#region ToSiteID
		public int? ToSiteID
		{
			get => TransferHeader.ToSiteID;
			set => TransferSetter.Set(h => h.ToSiteID, value);
		}
		#endregion
		#region ToLocationID
		public int? ToLocationID
		{
			get => TransferHeader.ToLocationID;
			set => TransferSetter.Set(h => h.ToLocationID, value);
		}
		#endregion
		#region AmbiguousLocationCD
		public string AmbiguousLocationCD
		{
			get => TransferHeader.AmbiguousLocationCD;
			set => TransferSetter.Set(h => h.AmbiguousLocationCD, value);
		}
		#endregion
		#region AmbiguousSource
		public bool? AmbiguousSource
		{
			get => TransferHeader.AmbiguousSource;
			set => TransferSetter.Set(h => h.AmbiguousSource, value);
		}
		#endregion
		#endregion

		#region DAC overrides
		[Common.Attributes.BorrowedNote(typeof(INRegister), typeof(INTransferEntry))]
		protected virtual void _(Events.CacheAttached<ScanHeader.noteID> e) { }
		#endregion

		#region Event Handlers
		protected override void _(Events.RowSelected<INTran> e)
		{
			base._(e);
			Details.Cache.AdjustUI().For<INTran.toLocationID>(ui => ui.Enabled = Graph.IsMobile && (Document == null || Document.Released != true));
		}
		#endregion

		protected override IEnumerable<ScanMode<INScanTransfer>> CreateScanModes() { yield return new TransferMode(); }
		public sealed class TransferMode : ScanMode
		{
			public const string Value = "INTR";
			public class value : BqlString.Constant<value> { public value() : base(TransferMode.Value) { } }

			public override string Code => Value;
			public override string Description => Msg.Description;

			#region State Machine
			protected override IEnumerable<ScanState<INScanTransfer>> CreateStates()
			{
				foreach (var state in base.CreateStates())
					yield return state;
				yield return new WarehouseState()
					.Intercept.ClearState.ByAppend(basis => basis.ToSiteID = null);
				yield return new SourceLocationState();
				yield return new TargetLocationState();
				yield return new SpecifyWarehouseState();
				yield return new InventoryItemState()
					.Intercept.HandleAbsence.ByOverride(
						(basis, barcode, base_HandleAbsence) =>
						{
							if (basis.TryProcessBy<SourceLocationState>(barcode, StateSubstitutionRule.KeepPositiveReports | StateSubstitutionRule.KeepApplication | StateSubstitutionRule.KeepStateChange))
								return AbsenceHandling.Done;

							if (basis.TryProcessBy<ReasonCodeState>(barcode, StateSubstitutionRule.KeepPositiveReports | StateSubstitutionRule.KeepApplication | StateSubstitutionRule.KeepStateChange))
								return AbsenceHandling.Done;

							return base_HandleAbsence(barcode);
						});
				yield return new LotSerialState()
					.Intercept.IsStateActive.ByConjoin(basis => basis.IsEnterableLotSerial(isForIssue: false));
				yield return new ReasonCodeState()
					.Intercept.IsStateSkippable.ByDisjoin(basis => !basis.PromptLocationForEveryLine && basis.ReasonCodeID != null);
				yield return new ConfirmState();
			}

			protected override IEnumerable<ScanTransition<INScanTransfer>> CreateTransitions()
			{
				if (Basis.PromptLocationForEveryLine)
				{
					return StateFlow(flow => flow
						.From<WarehouseState>()
						.NextTo<SourceLocationState>()
						.NextTo<InventoryItemState>()
						.NextTo<LotSerialState>()
						.NextTo<TargetLocationState>()
						.NextTo<ReasonCodeState>());
				}
				else
				{
					return StateFlow(flow => flow
						.From<WarehouseState>()
						.NextTo<SourceLocationState>()
						.NextTo<TargetLocationState>()
						.NextTo<ReasonCodeState>()
						.NextTo<InventoryItemState>()
						.NextTo<LotSerialState>());
				}
			}

			protected override IEnumerable<ScanCommand<INScanTransfer>> CreateCommands()
			{
				return new ScanCommand<INScanTransfer>[]
				{
					new RemoveCommand(),
					new QtySupport.SetQtyCommand(),
					new ReleaseCommand()
				};
			}

			protected override IEnumerable<ScanRedirect<INScanTransfer>> CreateRedirects() => AllWMSRedirects.CreateFor<INScanTransfer>();

			protected override void ResetMode(bool fullReset = false)
			{
				base.ResetMode(fullReset);

				Clear<WarehouseState>(when: fullReset);
				Clear<SourceLocationState>(when: fullReset || Basis.PromptLocationForEveryLine);
				Clear<TargetLocationState>(when: fullReset || Basis.PromptLocationForEveryLine);
				Clear<ReasonCodeState>(when: fullReset || Basis.PromptLocationForEveryLine);
				Clear<InventoryItemState>();
				Clear<LotSerialState>();
			}
			#endregion

			#region States
			public abstract class AmbiguousLocationState : EntityState<INLocation[]>
			{
				private readonly bool _isSource;
				protected AmbiguousLocationState(bool isSource) => _isSource = isSource;

				protected override string StatePrompt => _isSource ? Msg.PromptSource : Msg.PromptTarget;
				protected override bool IsStateActive() => PXAccess.FeatureInstalled<CS.FeaturesSet.warehouseLocation>();
				protected override bool IsStateSkippable() => !Basis.PromptLocationForEveryLine && (_isSource ? Basis.LocationID != null : Basis.ToLocationID != null);

				private int? SiteID => _isSource ? Basis.SiteID : Basis.ToSiteID;

				private bool ambiguousLocation = false;
				private bool filteredByBuilding = false;

				protected override INLocation[] GetByBarcode(string barcode)
				{
					if (Basis.Document == null)
					{
						var locations = ReadLocationsByBarcode(SiteID, barcode).ToArray();

						if (_isSource == false) // target location
						{
							int notFilteredCount = locations.Length;

							int? siteID = Basis.SiteID;
							int? buildingID = Basis.SelectedSite.BuildingID;
							locations = locations
								.Where(r => // only locations of sites that are in same building as source site
									r.site.SiteID == siteID ||
									r.site.BuildingID != null && r.site.BuildingID == buildingID)
								.ToArray();
							filteredByBuilding = locations.Length < notFilteredCount;
						}

						if (locations.Length == 0)
							return null;

						ambiguousLocation = locations.Length > 1;
						return locations.Select(t => t.location).ToArray();
					}
					else
					{
						return ReadLocationByBarcode(SiteID, barcode).With(loc => new[] { loc });
					}
				}

				protected override void ReportMissing(string barcode)
				{
					if (filteredByBuilding)
					{
						Basis.Reporter.Error(SpecifyWarehouseState.Msg.InterWarehouseTransferNotPossible);
					}
					else
					{
						var site = INSite.PK.Find(Basis, SiteID);
						Basis.Reporter.Error(Msg.Missing, barcode, site.SiteCD);
					}
				}

				protected override void ReportSuccess(INLocation[] locations)
				{
					if (ambiguousLocation == false)
						Basis.Reporter.Info(_isSource ? Msg.ReadySource : Msg.ReadyTarget, locations[0].LocationCD);
				}

				protected override Validation Validate(INLocation[] locations)
				{
					if (ambiguousLocation == false)
					{
						var location = locations[0];

						if (location.Active != true)
							return Validation.Fail(Messages.InactiveLocation, location.LocationCD);

						if (Basis.Document != null)
						{

							if (_isSource == true && location.SiteID != Basis.Document.SiteID)
								return Validation.Fail(Msg.MismatchSource, location.LocationCD);

							if (_isSource == false && location.SiteID != Basis.Document.ToSiteID)
								return Validation.Fail(Msg.MismatchTarget, location.LocationCD);
						}
					}

					return Validation.Ok;
				}

				protected override void Apply(INLocation[] locations)
				{
					if (ambiguousLocation)
					{
						Basis.AmbiguousLocationCD = Basis.Header.Barcode;
						Basis.AmbiguousSource = _isSource;
					}
					else
					{
						var location = locations[0];
						if (_isSource)
						{
							if (Basis.Document == null)
							{
								Basis.SiteID = location.SiteID;
								Basis.ToSiteID = location.SiteID;
							}
							Basis.LocationID = location.LocationID;
							Basis.ToLocationID = null;
						}
						else
						{
							if (Basis.Document == null)
								Basis.ToSiteID = location.SiteID;
							Basis.ToLocationID = location.LocationID;
						}
					}
				}

				protected override void ClearState()
				{
					if (ambiguousLocation)
					{
						Basis.AmbiguousLocationCD = null;
						Basis.AmbiguousSource = null;
					}
					else
					{
						if (_isSource)
							Basis.LocationID = null;
						else
							Basis.ToLocationID = null;
					}
				}

				protected override void SetNextState()
				{
					if (ambiguousLocation)
						Basis.SetScanState<SpecifyWarehouseState>(Msg.Ambiguous, Basis.Header.Barcode);
					else
						base.SetNextState();
				}

				protected virtual INLocation ReadLocationByBarcode(int? siteID, string locationCD)
				{
					return
						SelectFrom<INLocation>.
						Where<INLocation.siteID.IsEqual<@P.AsInt>.
							And<INLocation.locationCD.IsEqual<@P.AsString>>>.
						View.Select(Basis, siteID, locationCD);
				}

				protected virtual IEnumerable<(INLocation location, INSite site)> ReadLocationsByBarcode(int? siteID, string locationCD)
				{
					const int nonExistingBuildingID = -1;
					int? buildingID = INSite.PK.Find(Basis, siteID)?.BuildingID ?? nonExistingBuildingID;
					var locations =
						SelectFrom<INLocation>.
						InnerJoin<INSite>.On<INLocation.FK.Site>.
						Where<
							Match<INSite, AccessInfo.userName.FromCurrent>.
							And<INLocation.locationCD.IsEqual<@P.AsString>>>.
						OrderBy<
							Desc<TestIf<INLocation.siteID.IsEqual<@P.AsInt>>>,
							Desc<TestIf<INSite.buildingID.IsEqual<@P.AsInt>>>,
							Desc<INLocation.active>>.
						View
						.Select(Basis, locationCD, siteID, buildingID)
						.AsEnumerable()
						.Cast<PXResult<INLocation, INSite>>()
						.Select(l => ((INLocation)l, (INSite)l))
						.ToArray();
					return locations;
				}

				#region Messages
				[PXLocalizable]
				public abstract class Msg
				{
					public const string PromptSource = "Scan the barcode of the origin location.";
					public const string PromptTarget = "Scan the barcode of the destination location.";
					public const string ReadySource = "The {0} location is selected as origin.";
					public const string ReadyTarget = "The {0} location is selected as destination.";
					public const string MismatchSource = "The {0} location cannot be used because it does not belong to the origin warehouse.";
					public const string MismatchTarget = "The {0} location cannot be used because it does not belong to the destination warehouse.";

					public const string Missing = "The {0} location is not found in the {1} warehouse.";
					public const string Ambiguous = "The {0} location is defined in multiple warehouses.";
				}
				#endregion
			}

			public sealed class SourceLocationState : AmbiguousLocationState
			{
				public const string Value = "FLOC";
				public class value : BqlString.Constant<value> { public value() : base(SourceLocationState.Value) { } }

				public SourceLocationState() : base(isSource: true) { }

				public override string Code => Value;

				#region Messages
				[PXLocalizable]
				public new abstract class Msg : AmbiguousLocationState.Msg
				{
					public const string NotSet = "The origin location is not selected.";
				}
				#endregion
			}

			public sealed class TargetLocationState : AmbiguousLocationState
			{
				public const string Value = "TLOC";
				public class value : BqlString.Constant<value> { public value() : base(TargetLocationState.Value) { } }

				public TargetLocationState() : base(isSource: false) { }

				public override string Code => Value;

				#region Messages
				[PXLocalizable]
				public new abstract class Msg : AmbiguousLocationState.Msg
				{
					public const string NotSet = "The destination location is not selected.";
				}
				#endregion
			}

			public sealed class SpecifyWarehouseState : EntityState<PXResult<INSite, INLocation>>
			{
				public const string Value = "SPWH";
				public class value : BqlString.Constant<value> { public value() : base(SpecifyWarehouseState.Value) { } }

				private bool AmbiguousSource => Basis.AmbiguousSource.Value;

				public override string Code => Value;
				protected override string StatePrompt => Basis.Localize(Msg.Prompt, Basis.AmbiguousLocationCD);

				protected override PXResult<INSite, INLocation> GetByBarcode(string barcode)
				{
					var result =
						SelectFrom<INSite>.
						LeftJoin<INLocation>.On<
							INLocation.FK.Site.
							And<INLocation.locationCD.IsEqual<@P.AsString>>>.
						Where<
							Match<INSite, AccessInfo.userName.FromCurrent>.
							And<INSite.siteCD.IsEqual<@P.AsString>>>.
						View
						.Select(Basis, Basis.AmbiguousLocationCD, barcode)
						.AsEnumerable()
						.Cast<PXResult<INSite, INLocation>>()
						.FirstOrDefault();
					return result;
				}

				protected override void ReportMissing(string barcode) => Basis.Reporter.Error(WarehouseState.Msg.Missing, barcode);

				protected override Validation Validate(PXResult<INSite, INLocation> entity)
				{
					(var site, var location) = entity;
					if (site == null || location == null)
						return Validation.Fail(Msg.NoLocationSiteRelation, Basis.AmbiguousLocationCD, Basis.Header.Barcode);
					if (location.Active == false)
						return Validation.Fail(Messages.InactiveLocation, location.LocationCD);
					if (AmbiguousSource == false && (site.BuildingID == null || site.BuildingID != Basis.SelectedSite.BuildingID))
						return Validation.Fail(Msg.InterWarehouseTransferNotPossible);

					return Validation.Ok;
				}

				protected override void Apply(PXResult<INSite, INLocation> entity)
				{
					(var site, var location) = entity;
					if (AmbiguousSource)
					{
						Basis.SiteID = location.SiteID;
						Basis.ToSiteID = location.SiteID;
						Basis.LocationID = location.LocationID;
					}
					else // ambiguousTarget
					{
						Basis.ToSiteID = location.SiteID;
						Basis.ToLocationID = location.LocationID;
					}
				}

				protected override void ReportSuccess(PXResult<INSite, INLocation> entity)
				{
					Basis.Reporter.Info(
						AmbiguousSource
							? AmbiguousLocationState.Msg.ReadySource
							: AmbiguousLocationState.Msg.ReadyTarget,
						entity.GetItem<INLocation>().LocationCD);
				}

				protected override void SetNextState()
				{
					if (AmbiguousSource)
						Basis.FindState<SourceLocationState>().MoveToNextState();
					else // ambiguousTarget
						Basis.FindState<TargetLocationState>().MoveToNextState();
				}

				protected override void OnDismissing()
				{
					Basis.AmbiguousLocationCD = null;
					Basis.AmbiguousSource = null;
				}

				#region Messages
				[PXLocalizable]
				public abstract class Msg
				{
					public const string Prompt = "Scan the barcode of the warehouse to which the {0} location belongs.";
					public const string NoLocationSiteRelation = "The {0} location does not belong to the {1} warehouse.";
					public const string InterWarehouseTransferNotPossible = "You can perform one-step transfers only between warehouses located in the same building.";
				}
				#endregion
			}

			public sealed class ConfirmState : ConfirmationState
			{
				public override string Prompt => Basis.Localize(Msg.Prompt, Basis.SightOf<WMSScanHeader.inventoryID>(), Basis.Qty, Basis.UOM);

				protected override FlowStatus PerformConfirmation() => Get<Logic>().Confirm();

				#region Logic
				public class Logic : ScanExtension
				{
					public virtual FlowStatus Confirm()
					{
						if (!CanConfirm(out var error))
							return error;

						return Basis.Remove == true
							? ConfirmRemove()
							: ConfirmAdd();
					}

					protected virtual bool CanConfirm(out FlowStatus error)
					{
						if (Basis.Document?.Released == true)
						{
							error = FlowStatus.Fail(Messages.Document_Status_Invalid);
							return false;
						}

						if (Basis.CurrentMode.HasActive<SourceLocationState>() && Basis.LocationID == null)
						{
							error = FlowStatus.Fail(SourceLocationState.Msg.NotSet);
							return false;
						}

						if (Basis.CurrentMode.HasActive<TargetLocationState>() && Basis.ToLocationID == null)
						{
							error = FlowStatus.Fail(TargetLocationState.Msg.NotSet);
							return false;
						}

						if (Basis.InventoryID == null)
						{
							error = FlowStatus.Fail(InventoryItemState.Msg.NotSet);
							return false;
						}

						if (Basis.CurrentMode.HasActive<LotSerialState>() && Basis.LotSerialNbr == null)
						{
							error = FlowStatus.Fail(LotSerialState.Msg.NotSet);
							return false;
						}

						if (Basis.CurrentMode.HasActive<LotSerialState>() &&
							Basis.SelectedLotSerialClass.LotSerTrack == INLotSerTrack.SerialNumbered &&
							Basis.BaseQty != 1)
						{
							error = FlowStatus.Fail(InventoryItemState.Msg.SerialItemNotComplexQty);
							return false;
						}

						error = FlowStatus.Ok;
						return true;
					}

					protected virtual FlowStatus ConfirmAdd()
					{
						var lsClass = Basis.SelectedLotSerialClass;

						bool newDocument = Basis.Document == null;

						EnsureDocument();

						INTran existTransaction = FindTransferRow();

						Action rollbackAction;
						if (existTransaction != null)
						{
							decimal? newQty = existTransaction.Qty + Basis.Qty;

							if (lsClass.LotSerTrack == INLotSerTrack.SerialNumbered && newQty != 1)
								return FlowStatus.Fail(InventoryItemState.Msg.SerialItemNotComplexQty);

							var backup = PXCache<INTran>.CreateCopy(existTransaction);

							Basis.Details.Cache.SetValueExt<INTran.qty>(existTransaction, newQty);
							Basis.Details.Cache.SetValueExt<INTran.lotSerialNbr>(existTransaction, null);
							existTransaction = Basis.Details.Update(existTransaction);

							Basis.Details.Cache.SetValueExt<INTran.lotSerialNbr>(existTransaction, Basis.LotSerialNbr);
							existTransaction = Basis.Details.Update(existTransaction);

							rollbackAction = () =>
							{
								Basis.Details.Delete(existTransaction);
								Basis.Details.Insert(backup);
							};
						}
						else
						{
							existTransaction = Basis.Details.Insert();
							var setter = Basis.Details.GetSetterForCurrent().WithEventFiring;

							setter.Set(tr => tr.InventoryID, Basis.InventoryID);
							setter.Set(tr => tr.SiteID, Basis.SiteID);
							setter.Set(tr => tr.ToSiteID, Basis.ToSiteID);
							setter.Set(tr => tr.LocationID, Basis.LocationID);
							setter.Set(tr => tr.ToLocationID, Basis.ToLocationID);
							setter.Set(tr => tr.UOM, Basis.UOM);
							setter.Set(tr => tr.ReasonCode, Basis.ReasonCodeID);
							existTransaction = Basis.Details.Update(existTransaction);

							Basis.Details.Cache.SetValueExt<INTran.qty>(existTransaction, Basis.Qty);
							existTransaction = Basis.Details.Update(existTransaction);

							Basis.Details.Cache.SetValueExt<INTran.lotSerialNbr>(existTransaction, Basis.LotSerialNbr);
							existTransaction = Basis.Details.Update(existTransaction);

							rollbackAction = () =>
							{
								if (newDocument)
									Basis.DocumentView.DeleteCurrent();
								else
									Basis.Details.Delete(existTransaction);
							};
						}

						if (HasErrors(existTransaction, out var error))
						{
							rollbackAction();
							return error;
						}
						else
						{
							Basis.DispatchNext(
								Msg.InventoryAdded,
								Basis.SightOf<WMSScanHeader.inventoryID>(),
								Basis.Qty,
								Basis.UOM);

							if (Basis.DocumentView.Cache.GetStatus(Basis.DocumentView.Current) == PXEntryStatus.Inserted)
								return FlowStatus.Ok.WithSaveSkip;
							else
								return FlowStatus.Ok;
						}
					}

					protected virtual INRegister EnsureDocument()
					{
						if (Basis.Document == null)
						{
							Basis.DocumentView.Insert();
							Basis.DocumentView.Current.Hold = false;
							Basis.DocumentView.Current.Status = INDocStatus.Balanced;
							Basis.DocumentView.Current.NoteID = Basis.NoteID;
						}

						Basis.DocumentView.SetValueExt<INRegister.siteID>(Basis.Document, Basis.SiteID);
						Basis.DocumentView.SetValueExt<INRegister.toSiteID>(Basis.Document, Basis.ToSiteID);

						return Basis.DocumentView.Update(Basis.Document);
					}

					protected virtual bool HasErrors(INTran tran, out FlowStatus error)
					{
						if (Basis.HasUIErrors(tran, out error))
							return true;
						if (HasLotSerialError(tran, out error))
							return true;
						if (HasLocationError(tran, out error))
							return true;
						if (HasAvailabilityError(tran, out error))
							return true;

						error = FlowStatus.Ok;
						return false;
					}

					protected virtual bool HasLotSerialError(INTran tran, out FlowStatus error)
					{
						if (Basis.HasActive<LotSerialState>() && !string.IsNullOrEmpty(Basis.LotSerialNbr) && Basis.Graph.splits.SelectMain().Any(s => s.LotSerialNbr != Basis.LotSerialNbr))
						{
							error = FlowStatus.Fail(
								Msg.QtyExceedsOnLot,
								Basis.LotSerialNbr,
								Basis.SightOf<WMSScanHeader.inventoryID>());
							return true;
						}

						error = FlowStatus.Ok;
						return false;
					}

					protected virtual bool HasLocationError(INTran tran, out FlowStatus error)
					{
						if (Basis.HasActive<SourceLocationState>() && Basis.Graph.splits.SelectMain().Any(s => s.LocationID != Basis.LocationID))
						{
							error = FlowStatus.Fail(
								Msg.QtyExceedsOnLocation,
								Basis.SightOf<WMSScanHeader.locationID>(),
								Basis.SightOf<WMSScanHeader.inventoryID>());
							return true;
						}

						error = FlowStatus.Ok;
						return false;
					}

					protected virtual bool HasAvailabilityError(INTran tran, out FlowStatus error)
					{
						var errorInfo = Basis.Graph.lsselect.GetAvailabilityCheckErrors(Basis.Details.Cache, tran).FirstOrDefault();
						if (errorInfo != null)
						{
							PXCache lsCache = Basis.Graph.lsselect.Cache;
							error = FlowStatus.Fail(errorInfo.MessageFormat, new object[]
							{
								lsCache.GetStateExt<INTran.inventoryID>(tran),
								lsCache.GetStateExt<INTran.subItemID>(tran),
								lsCache.GetStateExt<INTran.siteID>(tran),
								lsCache.GetStateExt<INTran.locationID>(tran),
								lsCache.GetValue<INTran.lotSerialNbr>(tran)
							});
							return true;
						}

						error = FlowStatus.Ok;
						return false;
					}

					protected virtual FlowStatus ConfirmRemove()
					{
						INTran existTransaction = FindTransferRow();

						if (existTransaction == null)
							return FlowStatus.Fail(Msg.LineMissing, Basis.SightOf<WMSScanHeader.inventoryID>());

						if (existTransaction.Qty == Basis.Qty)
						{
							Basis.Details.Delete(existTransaction);
						}
						else
						{
							var newQty = existTransaction.Qty - Basis.Qty;

							if (!Basis.IsValid<INTran.qty, INTran>(existTransaction, newQty, out string error))
								return FlowStatus.Fail(error);

							Basis.Details.Cache.SetValueExt<INTran.qty>(existTransaction, newQty);
							Basis.Details.Update(existTransaction);
						}

						Basis.DispatchNext(
							Msg.InventoryRemoved,
							Basis.SightOf<WMSScanHeader.inventoryID>(),
							Basis.Qty,
							Basis.UOM);

						if (Basis.DocumentView.Cache.GetStatus(Basis.DocumentView.Current) == PXEntryStatus.Inserted)
							return FlowStatus.Ok.WithSaveSkip;
						else
							return FlowStatus.Ok;
					}

					protected virtual INTran FindTransferRow()
					{
						var existTransactions = Basis.Details.SelectMain().Where(t =>
							t.InventoryID == Basis.InventoryID &&
							t.SiteID == Basis.SiteID &&
							t.ToSiteID == Basis.ToSiteID &&
							t.LocationID == (Basis.LocationID ?? t.LocationID) &&
							t.ToLocationID == (Basis.ToLocationID ?? t.ToLocationID) &&
							t.ReasonCode == (Basis.ReasonCodeID ?? t.ReasonCode) &&
							t.UOM == Basis.UOM);

						INTran existTransaction = null;

						if (Basis.CurrentMode.HasActive<LotSerialState>())
						{
							foreach (var tran in existTransactions)
							{
								Basis.Details.Current = tran;
								if (Basis.Graph.splits.SelectMain().Any(t => (t.LotSerialNbr ?? "") == (Basis.LotSerialNbr ?? "")))
								{
									existTransaction = tran;
									break;
								}
							}
						}
						else
						{
							existTransaction = existTransactions.FirstOrDefault();
						}

						return existTransaction;
					}
				}
				#endregion

				#region Messages
				[PXLocalizable]
				public abstract class Msg
				{
					public const string Prompt = "Confirm transferring {0} x {1} {2}.";
					public const string LineMissing = "The {0} item is not found in the transfer.";
					public const string InventoryAdded = "{0} x {1} {2} has been added to the transfer.";
					public const string InventoryRemoved = "{0} x {1} {2} has been removed from the transfer.";

					public const string QtyExceedsOnLot = "The quantity of the {1} item in the transfer exceeds the item quantity in the {0} lot.";
					public const string QtyExceedsOnLocation = "The quantity of the {1} item in the transfer exceeds the item quantity in the {0} location.";
				}
				#endregion
			}
			#endregion

			#region Commands
			public new sealed class ReleaseCommand : WMSBase.ReleaseCommand
			{
				protected override string DocumentReleasing => Msg.DocumentReleasing;
				protected override string DocumentIsReleased => Msg.DocumentIsReleased;
				protected override string DocumentReleaseFailed => Msg.DocumentReleaseFailed;

				#region Messages
				[PXLocalizable]
				public new abstract class Msg : WMSBase.ReleaseCommand.Msg
				{
					public const string DocumentReleasing = "The {0} transfer is being released.";
					public const string DocumentIsReleased = "The transfer has been successfully released.";
					public const string DocumentReleaseFailed = "The transfer release failed.";
				}
				#endregion
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public new abstract class Msg
			{
				public const string Description = "Scan and Transfer";
			}
			#endregion
		}

		#region Redirect
		public new sealed class RedirectFrom<TForeignBasis> : WMSBase.RedirectFrom<TForeignBasis>
			where TForeignBasis : PXGraphExtension, IBarcodeDrivenStateMachine
		{
			public override string Code => "INTRANSFER";
			public override string DisplayName => Msg.DisplayName;

			#region Messages
			[PXLocalizable]
			public abstract class Msg
			{
				public const string DisplayName = "IN Transfer";
			}
			#endregion
		}
		#endregion
	}

	public sealed class TransferScanHeader : PXCacheExtension<RegisterScanHeader, WMSScanHeader, QtyScanHeader, ScanHeader>
	{
		#region ToSiteID
		[Site(DisplayName = "To Warehouse")]
		[PXUIVisible(typeof(toSiteID.IsNotNull.And<toSiteID.IsNotEqual<WMSScanHeader.siteID>>))]
		public int? ToSiteID { get; set; }
		public abstract class toSiteID : BqlInt.Field<toSiteID> { }
		#endregion
		#region ToLocationID
		[Location]
		public int? ToLocationID { get; set; }
		public abstract class toLocationID : BqlInt.Field<toLocationID> { }
		#endregion
		#region AmbiguousLocationCD
		[PXString]
		public string AmbiguousLocationCD { get; set; }
		public abstract class ambiguousLocationCD : BqlString.Field<ambiguousLocationCD> { }
		#endregion
		#region AmbiguousSource
		[PXBool]
		public bool? AmbiguousSource { get; set; }
		public abstract class ambiguousSource : BqlBool.Field<ambiguousSource> { }
		#endregion
	}
}