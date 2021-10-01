using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

using PX.BarcodeProcessing;
using PX.Objects.IN;
using PX.Objects.IN.WMS;

namespace PX.Objects.PO.WMS
{
	using WMSBase = WarehouseManagementSystem<ReceivePutAway, ReceivePutAway.Host>;

	public partial class ReceivePutAway : WMSBase
	{
		public sealed class ReturnMode : ScanMode
		{
			public const string Value = "VRTN";
			public class value : BqlString.Constant<value> { public value() : base(ReturnMode.Value) { } }

			public ReturnMode.Logic Body => Get<ReturnMode.Logic>();

			public override string Code => Value;
			public override string Description => Msg.Description;

			#region State Machine
			protected override IEnumerable<ScanState<ReceivePutAway>> CreateStates()
			{
				yield return new ReturnState();
				yield return new LocationState()
					.Intercept.IsStateSkippable.ByDisjoin(basis => !basis.Get<ReturnMode.Logic>().PromptLocationForEveryLine && basis.LocationID != null);
				yield return new InventoryItemState() { AlternateType = INPrimaryAlternateType.VPN }
					.Intercept.HandleAbsence.ByAppend(
						(basis, barcode) =>
						{
							if (basis.Get<Logic>().IsSingleLocation == false && basis.TryProcessBy<LocationState>(barcode, StateSubstitutionRule.KeepPositiveReports | StateSubstitutionRule.KeepApplication))
								return AbsenceHandling.Done;
							return AbsenceHandling.Skipped;
						});
				yield return new LotSerialState();
				yield return new ConfirmState();
				yield return new CommandOrReturnOnlyState();
			}

			protected override IEnumerable<ScanTransition<ReceivePutAway>> CreateTransitions()
			{
				return StateFlow(flow => flow
					.From<ReturnState>()
					.NextTo<LocationState>()
					.NextTo<InventoryItemState>()
					.NextTo<LotSerialState>());
			}

			protected override IEnumerable<ScanCommand<ReceivePutAway>> CreateCommands()
			{
				yield return new RemoveCommand();
				yield return new QtySupport.SetQtyCommand();
				yield return new ReleaseReturnCommand();
			}

			protected override IEnumerable<ScanRedirect<ReceivePutAway>> CreateRedirects() => AllWMSRedirects.CreateFor<ReceivePutAway>();

			protected override void ResetMode(bool fullReset)
			{
				base.ResetMode(fullReset);

				Clear<ReturnState>(when: fullReset && !Basis.IsWithinReset);
				Clear<LocationState>(when: fullReset || Body.PromptLocationForEveryLine);
				Clear<InventoryItemState>();
				Clear<LotSerialState>();
			}
			#endregion

			#region Logic
			public class Logic : ScanExtension
			{
				public virtual bool CanReturn => Returned.SelectMain().Any(s => s.ReceivedQty < s.Qty);

				#region Views
				public
					SelectFrom<POReceiptLineSplit>.
					InnerJoin<POReceiptLine>.On<POReceiptLineSplit.FK.ReceiptLine>.
					View Returned;
				public virtual IEnumerable returned() => Basis.SortedResult(Basis.GetSplits());

				public
					SelectFrom<POReceiptLineSplit>.
					InnerJoin<POReceiptLine>.On<POReceiptLineSplit.FK.ReceiptLine>.
					View ReturnedNotZero;
				public virtual IEnumerable returnedNotZero() => Basis.SortedResult(Basis.GetSplits().Where(s => ((POReceiptLineSplit)s).ReceivedQty > 0));
				#endregion

				#region Buttons
				public PXAction<ScanHeader> ReviewReturn;
				[PXButton(CommitChanges = true), PXUIField(DisplayName = "Review")]
				protected virtual IEnumerable reviewReturn(PXAdapter adapter) => adapter.Get();
				#endregion

				#region Event Handlers
				protected virtual void _(Events.RowSelected<ScanHeader> e)
				{
					ReviewReturn.SetVisible(Base.IsMobile && e.Row?.Mode == ReturnMode.Value);
				}
				#endregion

				#region Configuration
				public bool PromptLocationForEveryLine => Basis.Setup.Current.RequestLocationForEachItemInReturn == true;
				public virtual bool IsSingleLocation => Returned.SelectMain().GroupBy(s => s.LocationID).Count() < 2;
				#endregion
			}
			#endregion

			#region States
			public sealed class ReturnState : ReceivePutAway.ReceiptState
			{
				private int? siteID;

				protected override string StatePrompt => Msg.Prompt;

				protected override Validation Validate(POReceipt receipt)
				{
					if (receipt.Released == true || receipt.Hold == true)
						return Validation.Fail(Msg.InvalidStatus, receipt.ReceiptNbr, Basis.SightOf<POReceipt.status>(receipt));

					if (receipt.ReceiptType != POReceiptType.POReturn)
						return Validation.Fail(Msg.InvalidType, receipt.ReceiptNbr, Basis.SightOf<POReceipt.receiptType>(receipt));

					if (receipt.POType == POOrderType.DropShip)
						return Validation.Fail(Msg.InvalidOrderType, receipt.ReceiptNbr, Basis.SightOf<POReceipt.pOType>(receipt));

					if (Basis.HasSingleSiteInLines(receipt, out int? singleSiteID) == false)
						return Validation.Fail(Msg.MultiSites, receipt.ReceiptNbr);
					else if (receipt.SiteID == null)
						siteID = singleSiteID;
					else
						siteID = receipt.SiteID;

					if (Basis.HasNonStockKit(receipt))
						Validation.Fail(Msg.HasNonStockKit, receipt.ReceiptNbr);

					return Validation.Ok;
				}

				protected override void Apply(POReceipt receipt)
				{
					Basis.SiteID = siteID;
					base.Apply(receipt);
				}

				protected override void ClearState()
				{
					Basis.SiteID = null;
					base.ClearState();
				}

				protected override void ReportSuccess(POReceipt receipt) => Basis.ReportInfo(Msg.Ready, receipt.ReceiptNbr);
				protected override void ReportMissing(string barcode) => Basis.ReportError(Msg.Missing, barcode);

				protected override void SetNextState()
				{
					if (Basis.Remove == false && Get<ReturnMode.Logic>().CanReturn == false)
						Basis.SetScanState(BuiltinScanStates.Command, ReturnMode.Msg.Completed, Basis.RefNbr);
					else
						base.SetNextState();
				}

				#region Messages
				[PXLocalizable]
				public new abstract class Msg
				{
					public const string Prompt = "Scan the return number.";
					public const string Ready = "The {0} return is loaded and ready to be processed.";
					public const string Missing = "The {0} return is not found.";
					public const string InvalidStatus = "The {0} return cannot be processed because it has the {1} status.";
					public const string InvalidType = "The {0} return cannot be processed because it has the {1} type.";
					public const string InvalidOrderType = "The {0} return cannot be processed because it has the {1} order type.";
					public const string MultiSites = "The {0} return should have only one warehouse to be processed.";
					public const string HasNonStockKit = "The {0} return cannot be processed because it contains a non-stock kit item.";
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
					public ReturnMode.Logic Mode { get; private set; }
					public override void Initialize() => Mode = Basis.Get<ReturnMode.Logic>();

					public virtual FlowStatus Confirm()
					{
						if (!CanReturn(out var error))
							return error;

						if (Basis.Remove == false)
							return ProcessAdd();
						else
							return ProcessRemove();
					}

					protected virtual bool CanReturn(out FlowStatus error)
					{
						if (Basis.InventoryID == null)
						{
							error = FlowStatus.Fail(InventoryItemState.Msg.NotSet).WithModeReset;
							return false;
						}

						if (Basis.HasActive<LocationState>() && Basis.LocationID == null)
						{
							error = FlowStatus.Fail(LocationState.Msg.NotSet).WithModeReset;
							return false;
						}

						if (Basis.HasActive<LotSerialState>() && Basis.LotSerialNbr == null)
						{
							error = FlowStatus.Fail(LotSerialState.Msg.NotSet).WithModeReset;
							return false;
						}

						error = FlowStatus.Ok;
						return true;
					}

					protected virtual FlowStatus ProcessAdd()
					{
						var splits = Mode.Returned.SelectMain()
							.Where(r =>
								r.InventoryID == Basis.InventoryID &&
								r.SubItemID == Basis.SubItemID &&
								r.LotSerialNbr == (Basis.LotSerialNbr ?? r.LotSerialNbr) &&
								r.LocationID == (Basis.LocationID ?? r.LocationID) &&
								r.Qty - r.ReceivedQty > 0)
							.ToArray();

						decimal qty = Basis.BaseQty;

						if (splits.Sum(s => s.Qty.Value - s.ReceivedQty.Value) < qty)
							return FlowStatus.Fail(Msg.Overreturning);

						decimal restQty = qty;

						foreach (var split in splits)
						{
							decimal qtyToAdd = Math.Min(restQty, split.Qty.Value - split.ReceivedQty.Value);

							split.ReceivedQty += qtyToAdd;
							Mode.Returned.Update(split);

							restQty -= qtyToAdd;

							if (restQty == 0)
								break;
						}

						Basis.DispatchNext(Msg.InventoryAdded, Basis.SelectedInventoryItem.InventoryCD, Basis.Qty, Basis.UOM);

						return FlowStatus.Ok;
					}

					protected virtual FlowStatus ProcessRemove()
					{
						var splits = Mode.Returned.SelectMain()
							.Where(r =>
								r.InventoryID == Basis.InventoryID &&
								r.SubItemID == Basis.SubItemID &&
								r.LotSerialNbr == (Basis.LotSerialNbr ?? r.LotSerialNbr) &&
								r.LocationID == (Basis.LocationID ?? r.LocationID) &&
								r.ReceivedQty > 0)
							.ToArray();

						if (splits.Any() == false)
							return FlowStatus.Fail(Msg.NothingToRemove).WithModeReset;

						decimal qty = Basis.BaseQty;

						if (splits.Sum(s => s.ReceivedQty.Value) - qty < 0)
							return FlowStatus.Fail(Msg.Underreturning);

						decimal restQty = qty;

						foreach (var split in splits)
						{
							decimal qtyToRemove = Math.Min(restQty, split.ReceivedQty ?? 0);

							split.ReceivedQty -= qtyToRemove;
							Mode.Returned.Update(split);

							restQty -= qtyToRemove;

							if (restQty == 0)
								break;
						}

						Basis.DispatchNext(Msg.InventoryRemoved, Basis.SelectedInventoryItem.InventoryCD, Basis.Qty, Basis.UOM);

						return FlowStatus.Ok;
					}
				}
				#endregion

				#region Messages
				[PXLocalizable]
				public abstract class Msg
				{
					public const string Prompt = "Confirm returning {0} x {1} {2}.";
					public const string NothingToRemove = "No items to remove from the return.";
					public const string Overreturning = "The returned quantity cannot be greater than the expected quantity.";
					public const string Underreturning = "The returned quantity cannot become negative.";
					public const string InventoryAdded = "{0} x {1} {2} has been added to the return.";
					public const string InventoryRemoved = "{0} x {1} {2} has been removed from the return.";
				}
				#endregion
			}

			public sealed class CommandOrReturnOnlyState : CommandOnlyStateBase<ReceivePutAway>
			{
				public override void MoveToNextState() { }
				public override string Prompt => Msg.UseCommandOrReturnToContinue;
				public override bool Process(string barcode)
				{
					if (Basis.TryProcessBy<ReturnState>(barcode))
					{
						Basis.Clear<ReturnState>();
						Basis.Reset(fullReset: false);
						Basis.SetScanState<ReturnState>();
						Basis.CurrentMode.FindState<ReturnState>().Process(barcode);
						return true;
					}
					else
					{
						Basis.Reporter.Error(Msg.OnlyCommandsAndReturnsAreAllowed);
						return false;
					}
				}

				#region Messages
				[PXLocalizable]
				public new abstract class Msg
				{
					public const string UseCommandOrReturnToContinue = "Use any command or scan the next return to continue.";
					public const string OnlyCommandsAndReturnsAreAllowed = "Only commands or a return can be used to continue.";
				}
				#endregion
			}
			#endregion

			#region Commands
			public sealed class ReleaseReturnCommand : ScanCommand
			{
				public override string Code => "RELEASE";
				public override string ButtonName => "scanReleaseReturn";
				public override string DisplayName => Msg.DisplayName;

				protected override bool IsEnabled => Basis.DocumentIsEditable;

				protected override bool Process()
				{
					Get<Logic>().ReleaseReturn();
					return true;
				}

				#region Logic
				public class Logic : ScanExtension
				{
					public virtual void ReleaseReturn()
					{
						Basis.ApplyLinesQtyChanges(false);

						if (Basis.Graph.Document.Current.Hold == true) // TODO: move to static method and use releaseFromHold action
						{
							Basis.Graph.Document.Current.Hold = false;
							Basis.Graph.Document.UpdateCurrent();
						}

						Basis.CurrentMode.Reset(fullReset: false);
						Basis.SaveChanges();

						Basis
						.WaitFor<POReceipt>((basis, doc) => POReleaseReceipt.ReleaseDoc(new List<POReceipt>() { doc }, false))
						.WithDescription(Msg.InProcess, Basis.RefNbr)
						.ActualizeDataBy((basis, doc) => POReceipt.PK.Find(basis, doc))
						.OnSuccess(x => x.Say(Msg.Success).ChangeStateTo<ReturnState>())
						.OnFail(x => x.Say(Msg.Fail))
						.BeginAwait(Basis.Receipt);
					}
				}
				#endregion

				#region Messages
				[PXLocalizable]
				public abstract class Msg
				{
					public const string DisplayName = "Release Return";
					public const string InProcess = "The {0} return is being released.";
					public const string Success = "The return has been successfully released.";
					public const string Fail = "The return release failed.";
				}
				#endregion
			}
			#endregion

			#region Redirect
			public sealed class RedirectFrom<TForeignBasis> : WMSBase.RedirectFrom<TForeignBasis>.SetMode<ReturnMode>
				where TForeignBasis : PXGraphExtension, IBarcodeDrivenStateMachine
			{
				public override string Code => "PORETURN";
				public override string DisplayName => Msg.DisplayName;

				public override bool IsPossible
				{
					get
					{
						bool wmsReceiving = PXAccess.FeatureInstalled<CS.FeaturesSet.wMSReceiving>();
						var rpSetup = POReceivePutAwaySetup.PK.Find(Basis.Graph, Basis.Graph.Accessinfo.BranchID);
						return wmsReceiving && rpSetup?.ShowReturningTab == true;
					}
				}

				#region Messages
				[PXLocalizable]
				public abstract class Msg
				{
					public const string DisplayName = "PO Return";
				}
				#endregion
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public new abstract class Msg : ScanMode.Msg
			{
				public const string Description = "Return";
				public const string Completed = "The {0} return has been returned.";
			}
			#endregion

			#region Attached Fields
			[PXUIField(Visible = false)]
			public class ShowReturn : FieldAttached.To<ScanHeader>.AsBool.Named<ShowReturn>
			{
				public override bool? GetValue(ScanHeader row) => Base.WMS.Setup.Current.ShowReturningTab == true && row.Mode == ReturnMode.Value;
			}
			#endregion
		}
	}
}