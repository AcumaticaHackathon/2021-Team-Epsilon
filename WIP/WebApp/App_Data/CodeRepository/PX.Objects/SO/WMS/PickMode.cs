using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

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
		public sealed class PickMode : ScanMode
		{
			public const string Value = "PICK";
			public class value : BqlString.Constant<value> { public value() : base(PickMode.Value) { } }

			public override string Code => Value;
			public override string Description => Msg.Description;

			#region State Machine
			protected override IEnumerable<ScanState<PickPackShip>> CreateStates()
			{
				yield return new ShipmentState();
				yield return new LocationState()
					.Intercept.IsStateActive.ByConjoin(basis => !basis.DefaultLocation)
					.Intercept.IsStateSkippable.ByDisjoin(basis => !basis.PromptLocationForEveryLine && basis.LocationID != null)
					.Intercept.Validate.ByAppend((basis, location) => basis.IsLocationMissing(basis.Get<Logic>().Picked, location, out var error) ? error : Validation.Ok);
				yield return new InventoryItemState() { AlternateType = INPrimaryAlternateType.CPN, IsForIssue = true }
					.Intercept.Validate.ByAppend((basis, inventory) => basis.IsItemMissing(basis.Get<Logic>().Picked, inventory, out var error) ? error : Validation.Ok)
					.Intercept.HandleAbsence.ByAppend((basis, barcode) =>
					{
						if (basis.TryProcessBy<LocationState>(barcode, StateSubstitutionRule.KeepPositiveReports | StateSubstitutionRule.KeepApplication))
							return AbsenceHandling.Done;
						return AbsenceHandling.Skipped;
					});
				yield return new LotSerialState()
					.Intercept.IsStateActive.ByConjoin(basis => !basis.DefaultLotSerial || basis.IsEnterableLotSerial(isForIssue: true))
					.Intercept.Validate.ByAppend((basis, lotSerialNbr) =>  basis.IsLotSerialMissing(basis.Get<Logic>().Picked, lotSerialNbr, out var error) ? error : Validation.Ok);
				yield return new ExpireDateState() { IsForIssue = true }
					.Intercept.IsStateActive.ByConjoin(basis => basis.Get<Logic>().Picked.SelectMain().Any(t => t.IsUnassigned == true || t.LotSerialNbr == basis.LotSerialNbr && t.PickedQty == 0));
				yield return new ConfirmState();
				yield return new CommandOrShipmentOnlyState();
			}

			protected override IEnumerable<ScanTransition<PickPackShip>> CreateTransitions()
			{
				return StateFlow(flow => flow
					.From<ShipmentState>()
					.NextTo<LocationState>()
					.NextTo<InventoryItemState>()
					.NextTo<LotSerialState>()
					.NextTo<ExpireDateState>());
			}

			protected override IEnumerable<ScanCommand<PickPackShip>> CreateCommands()
			{
				yield return new RemoveCommand();
				yield return new QtySupport.SetQtyCommand();
				yield return new ConfirmShipmentCommand();
			}

			protected override IEnumerable<ScanRedirect<PickPackShip>> CreateRedirects() => AllWMSRedirects.CreateFor<PickPackShip>();

			protected override void ResetMode(bool fullReset)
			{
				base.ResetMode(fullReset);

				Clear<ShipmentState>(when: fullReset && !Basis.IsWithinReset);
				Clear<LocationState>(when: fullReset || Basis.PromptLocationForEveryLine);
				Clear<InventoryItemState>();
				Clear<LotSerialState>();
				Clear<ExpireDateState>();
			}
			#endregion

			#region Logic
			public class Logic : ScanExtension
			{
				#region Views
				public
					SelectFrom<SOShipLineSplit>.
					InnerJoin<SOShipLine>.On<SOShipLineSplit.FK.ShipmentLine>.
					OrderBy<
						SOShipLineSplit.shipmentNbr.Asc,
						SOShipLineSplit.isUnassigned.Desc,
						SOShipLineSplit.lineNbr.Asc>.
					View Picked;
				protected virtual IEnumerable picked()
				{
					var delegateResult = new PXDelegateResult { IsResultSorted = true };
					delegateResult.AddRange(Basis.GetSplits(Basis.RefNbr, includeUnassigned: true, s => s.PickedQty >= s.Qty));
					return delegateResult;
				}
				#endregion

				#region Buttons
				public PXAction<ScanHeader> ReviewReturn;
				[PXButton, PXUIField(DisplayName = "Review")]
				protected virtual IEnumerable reviewReturn(PXAdapter adapter) => adapter.Get();
				#endregion

				protected virtual void _(Events.RowSelected<ScanHeader> e)
				{
					ReviewReturn.SetVisible(Base.IsMobile && e.Row?.Mode == ReturnMode.Value);
				}

				public virtual bool CanPick => Picked.SelectMain().Any(s => s.PickedQty < s.Qty);
			}
			#endregion

			#region States
			public new sealed class ShipmentState : PickPackShip.ShipmentState
			{
				protected override Validation Validate(SOShipment shipment)
				{
					if (shipment.Operation != SOOperation.Issue)
						return Validation.Fail(Msg.InvalidOperation, shipment.ShipmentNbr, Basis.SightOf<SOShipment.operation>(shipment));

					if (shipment.Status != SOShipmentStatus.Open)
						return Validation.Fail(Msg.InvalidStatus, shipment.ShipmentNbr, Basis.SightOf<SOShipment.status>(shipment));

					return Validation.Ok;
				}

				protected override void ReportSuccess(SOShipment shipment) => Basis.ReportInfo(Msg.Ready, shipment.ShipmentNbr);

				protected override void SetNextState()
				{
					if (Basis.Remove == false && Basis.Get<PickMode.Logic>().CanPick == false)
						Basis.SetScanState(BuiltinScanStates.Command, PickMode.Msg.Completed, Basis.RefNbr);
					else
						base.SetNextState();
				}

				#region Messages
				[PXLocalizable]
				public new abstract class Msg : PickPackShip.ShipmentState.Msg
				{
					public new const string Ready = "The {0} shipment is loaded and ready to be picked.";
					public const string InvalidStatus = "The {0} shipment cannot be picked because it has the {1} status.";
					public const string InvalidOperation = "The {0} shipment cannot be picked because it has the {1} operation.";
				}
				#endregion
			}

			public sealed class ConfirmState : ConfirmationState
			{
				public override string Prompt => Basis.Localize(Msg.Prompt, Basis.SightOf<WMSScanHeader.inventoryID>(), Basis.Qty, Basis.UOM);
				protected override FlowStatus PerformConfirmation() => Get<Logic>().ConfirmPicked();

				#region Logic
				public class Logic : ScanExtension
				{
					protected PickMode.Logic Mode { get; private set; }
					public override void Initialize() => Mode = Basis.Get<PickMode.Logic>();

					public virtual FlowStatus ConfirmPicked()
					{
						SOShipLineSplit pickedSplit = GetPickedSplit();
						if (pickedSplit == null)
							return FlowStatus.Fail(Basis.Remove == true ? Msg.NothingToRemove : Msg.NothingToPick).WithModeReset;

						decimal qty = Basis.BaseQty;
						decimal threshold = Graph.GetQtyThreshold(pickedSplit);

						if (qty != 0)
						{
							bool splitUpdated = false;

							if (Basis.Remove == true)
							{
								if (pickedSplit.PickedQty - qty < 0)
									return FlowStatus.Fail(Msg.Underpicking);
								if (pickedSplit.PickedQty - qty < pickedSplit.PackedQty)
									return FlowStatus.Fail(Msg.UnderpickingByPack);
							}
							else
							{
								if (pickedSplit.PickedQty + qty > pickedSplit.Qty * threshold)
									return FlowStatus.Fail(Msg.Overpicking);

								if (pickedSplit.LotSerialNbr != Basis.LotSerialNbr && Basis.IsEnterableLotSerial(isForIssue: true))
								{
									if (!SetLotSerialNbrAndQty(pickedSplit, qty))
										return FlowStatus.Fail(Msg.Overpicking);
									splitUpdated = true;
								}
							}

							if (!splitUpdated)
							{
								Basis.EnsureAssignedSplitEditing(pickedSplit);

								pickedSplit.PickedQty += Basis.Remove == true ? -qty : qty;

								if (Basis.Remove == true && Basis.IsEnterableLotSerial(isForIssue: true))
								{
									if (pickedSplit.PickedQty + qty == pickedSplit.Qty)
										pickedSplit.Qty = pickedSplit.PickedQty;

									if (pickedSplit.Qty == 0)
										Mode.Picked.Delete(pickedSplit);
									else
										Mode.Picked.Update(pickedSplit);
								}
								else
									Mode.Picked.Update(pickedSplit);
							}
						}

						Basis.EnsureShipmentUserLink();

						Basis.DispatchNext(
							Basis.Remove == true ? Msg.InventoryRemoved : Msg.InventoryAdded,
							Basis.SightOf<WMSScanHeader.inventoryID>(), Basis.Qty, Basis.UOM);

						return FlowStatus.Ok;
					}

					public virtual bool IsSelectedSplit(SOShipLineSplit split)
					{
						return
							split.InventoryID == Basis.InventoryID &&
							split.SubItemID == Basis.SubItemID &&
							split.SiteID == Basis.SiteID &&
							split.LocationID == (Basis.LocationID ?? split.LocationID) &&
							(
								split.LotSerialNbr == (Basis.LotSerialNbr ?? split.LotSerialNbr) ||
								Basis.Remove == false &&
								(
									Basis.SelectedLotSerialClass.LotSerAssign == INLotSerAssign.WhenUsed ||
									(
										Basis.SelectedLotSerialClass.LotSerIssueMethod == INLotSerIssueMethod.UserEnterable &&
										split.PackedQty == 0
									)
								)
							);
					}

					public virtual bool SetLotSerialNbrAndQty(SOShipLineSplit pickedSplit, decimal qty)
					{
						PXSelectBase<SOShipLineSplit> splitsView = Mode.Picked;
						if (pickedSplit.PickedQty == 0 && pickedSplit.IsUnassigned == false)
						{
							if (Basis.SelectedLotSerialClass.LotSerTrack == INLotSerTrack.SerialNumbered && Basis.SelectedLotSerialClass.LotSerIssueMethod == INLotSerIssueMethod.UserEnterable)
							{
								SOShipLineSplit originalSplit =
									SelectFrom<SOShipLineSplit>.
									InnerJoin<SOLineSplit>.On<SOShipLineSplit.FK.OriginalLineSplit>.
									Where<
										SOShipLineSplit.shipmentNbr.IsEqual<WMSScanHeader.refNbr.FromCurrent>.
										And<SOLineSplit.lotSerialNbr.IsEqual<@P.AsString>>>.
									View.Select(Basis, Basis.LotSerialNbr);

								if (originalSplit == null)
								{
									pickedSplit.LotSerialNbr = Basis.LotSerialNbr;
									pickedSplit.PickedQty += qty;
									pickedSplit = splitsView.Update(pickedSplit);
								}
								else
								{
									if (originalSplit.LotSerialNbr == Basis.LotSerialNbr) return false;

									var tempOriginalSplit = PXCache<SOShipLineSplit>.CreateCopy(originalSplit);
									var tempPickedSplit = PXCache<SOShipLineSplit>.CreateCopy(pickedSplit);

									originalSplit.Qty = 0;
									originalSplit.LotSerialNbr = Basis.LotSerialNbr;
									originalSplit = splitsView.Update(originalSplit);
									originalSplit.Qty = tempOriginalSplit.Qty;
									originalSplit.PickedQty = tempPickedSplit.PickedQty + qty;
									originalSplit.ExpireDate = tempPickedSplit.ExpireDate;
									originalSplit = splitsView.Update(originalSplit);

									pickedSplit.Qty = 0;
									pickedSplit.LotSerialNbr = tempOriginalSplit.LotSerialNbr;
									pickedSplit = splitsView.Update(pickedSplit);
									pickedSplit.Qty = tempPickedSplit.Qty;
									pickedSplit.PickedQty = tempOriginalSplit.PickedQty;
									pickedSplit.ExpireDate = tempOriginalSplit.ExpireDate;
									pickedSplit = splitsView.Update(pickedSplit);
								}
							}
							else
							{
								pickedSplit.LotSerialNbr = Basis.LotSerialNbr;
								if (Basis.SelectedLotSerialClass.LotSerTrackExpiration == true && Basis.ExpireDate != null)
									pickedSplit.ExpireDate = Basis.ExpireDate; // TODO: use expire date of the same lot/serial in the shipment
								pickedSplit.PickedQty += qty;
								pickedSplit = splitsView.Update(pickedSplit);
							}
						}
						else
						{
							var existingSplit = pickedSplit.IsUnassigned == true || Basis.SelectedLotSerialClass.LotSerTrack == INLotSerTrack.LotNumbered
								? splitsView.SelectMain().FirstOrDefault(s =>
									s.LineNbr == pickedSplit.LineNbr &&
									s.IsUnassigned == false &&
									s.LotSerialNbr == (Basis.LotSerialNbr ?? s.LotSerialNbr) &&
									IsSelectedSplit(s))
								: null;

							bool suppressMode = pickedSplit.IsUnassigned == false && Basis.SelectedLotSerialClass.LotSerTrack == INLotSerTrack.LotNumbered;

							if (existingSplit != null)
							{
								existingSplit.PickedQty += qty;
								if (existingSplit.PickedQty > existingSplit.Qty)
									existingSplit.Qty = existingSplit.PickedQty;

								using (Graph.lsselect.SuppressedModeScope(suppressMode))
									existingSplit = splitsView.Update(existingSplit);
							}
							else
							{
								var newSplit = PXCache<SOShipLineSplit>.CreateCopy(pickedSplit);
								newSplit.SplitLineNbr = null;
								newSplit.LotSerialNbr = Basis.LotSerialNbr;
								if (pickedSplit.Qty - qty > 0 || pickedSplit.IsUnassigned == true)
								{
									newSplit.Qty = qty;
									newSplit.PickedQty = qty;
									newSplit.PackedQty = 0;
									newSplit.IsUnassigned = false;
									newSplit.PlanID = null;
								}
								else
								{
									newSplit.Qty = pickedSplit.Qty;
									newSplit.PickedQty = pickedSplit.PickedQty;
									newSplit.PackedQty = pickedSplit.PackedQty;
								}

								using (Graph.lsselect.SuppressedModeScope(suppressMode))
									newSplit = splitsView.Insert(newSplit);
							}

							if (pickedSplit.IsUnassigned == false) // Unassigned splits will be processed automatically
							{
								if (pickedSplit.Qty <= 0)
									pickedSplit = splitsView.Delete(pickedSplit);
								else
								{
									pickedSplit.Qty -= qty;
									pickedSplit = splitsView.Update(pickedSplit);
								}
							}
						}

						return true;
					}

					public virtual SOShipLineSplit GetPickedSplit()
					{
						SOShipLineSplit pickedSplit = Mode.Picked
							.SelectMain()
							.Where(r => IsSelectedSplit(r))
							.OrderByDescending(split => split.IsUnassigned == false && Basis.Remove == true ? split.PickedQty > 0 : split.Qty > split.PickedQty)
							.OrderByDescending(split => Basis.Remove == true ? split.PickedQty > 0 : split.Qty > split.PickedQty)
							.ThenByDescending(split => split.LotSerialNbr == (Basis.LotSerialNbr ?? split.LotSerialNbr))
							.ThenByDescending(split => string.IsNullOrEmpty(split.LotSerialNbr))
							.ThenByDescending(split => (split.Qty > split.PickedQty || Basis.Remove == true) && split.PickedQty > 0)
							.ThenByDescending(split => Sign.MinusIf(Basis.Remove == true) * (split.Qty - split.PickedQty))
							.FirstOrDefault();
						return pickedSplit;
					}
				}
				#endregion

				#region Messages
				[PXLocalizable]
				public new abstract class Msg
				{
					public const string Prompt = "Confirm picking {0} x {1} {2}.";

					public const string NothingToPick = "No items to pick.";
					public const string NothingToRemove = "No items to remove from the shipment.";

					public const string Overpicking = "The picked quantity cannot be greater than the quantity in the shipment line.";
					public const string Underpicking = "The picked quantity cannot become negative.";
					public const string UnderpickingByPack = "The picked quantity cannot be less than the already packed quantity.";

					public const string InventoryAdded = "{0} x {1} {2} has been added to the shipment.";
					public const string InventoryRemoved = "{0} x {1} {2} has been removed from the shipment.";
				}
				#endregion
			}
			#endregion

			#region Redirect
			public sealed class RedirectFrom<TForeignBasis> : WMSBase.RedirectFrom<TForeignBasis>.SetMode<PickMode>
				where TForeignBasis : PXGraphExtension, IBarcodeDrivenStateMachine
			{
				public override string Code => PickMode.Value;
				public override string DisplayName => Msg.Description;

				[Obsolete(PickPackShip.ObsoleteMsg.ScanMember)]
				public override string ButtonName => "scanModePick";

				private string RefNbr { get; set; }

				public override bool IsPossible
				{
					get
					{
						bool wmsFulfillment = PXAccess.FeatureInstalled<CS.FeaturesSet.wMSFulfillment>();
						var ppsSetup = SOPickPackShipSetup.PK.Find(Basis.Graph, Basis.Graph.Accessinfo.BranchID);
						return wmsFulfillment && ppsSetup?.ShowPickTab == true;
					}
				}

				protected override bool PrepareRedirect()
				{
					if (Basis is PickPackShip pps && pps.RefNbr != null && pps.DocumentIsConfirmed == false)
					{
						if (pps.FindMode<PickMode>().TryValidate(pps.Shipment).By<ShipmentState>() is Validation valid && valid.IsError == true)
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
				public const string Description = "Pick";
				public const string Completed = "The {0} shipment is picked.";
			}
			#endregion

			#region Attached Fields
			[PXUIField(Visible = false)]
			public class ShowPick : FieldAttached.To<ScanHeader>.AsBool.Named<ShowPick>
			{
				public override bool? GetValue(ScanHeader row) => Base.WMS.HasPick && row.Mode == PickMode.Value;
			}
			#endregion
		}
	}
}