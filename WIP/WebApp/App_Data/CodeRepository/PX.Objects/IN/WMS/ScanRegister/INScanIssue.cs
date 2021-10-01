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
	using WMSBase = INScanRegisterBase<INScanIssue, INScanIssue.Host, INDocType.issue>;

	public class INScanIssue : WMSBase
	{
		public class Host : INIssueEntry { }

		public new class QtySupport : WMSBase.QtySupport { }
		public new class GS1Support : WMSBase.GS1Support { }
		public new class UserSetup : WMSBase.UserSetup { }

		#region Configuration
		public override bool ExplicitConfirmation => Setup.Current.ExplicitLineConfirmation == true;

		public override bool PromptLocationForEveryLine => Setup.Current.RequestLocationForEachItemInIssue == true;
		public override bool UseDefaultReasonCode => Setup.Current.UseDefaultReasonCodeInIssue == true;
		public override bool UseDefaultWarehouse => UserSetup.For(Graph).DefaultWarehouse == true;

		protected override bool UseQtyCorrectection => Setup.Current.UseDefaultQtyInIssue != true;
		protected override bool CanOverrideQty => (!DocumentLoaded || NotReleasedAndHasLines) && SelectedLotSerialClass?.LotSerTrack != INLotSerTrack.SerialNumbered;
		#endregion

		#region DAC overrides
		[Common.Attributes.BorrowedNote(typeof(INRegister), typeof(INIssueEntry))]
		protected virtual void _(Events.CacheAttached<ScanHeader.noteID> e) { }
		#endregion

		protected override IEnumerable<ScanMode<INScanIssue>> CreateScanModes() { yield return new IssueMode(); }
		public sealed class IssueMode : ScanMode
		{
			public const string Value = "ISSU";
			public class value : BqlString.Constant<value> { public value() : base(IssueMode.Value) { } }

			public override string Code => Value;
			public override string Description => Msg.Description;

			#region State Machine
			protected override IEnumerable<ScanState<INScanIssue>> CreateStates()
			{
				foreach (var state in base.CreateStates())
					yield return state;
				yield return new WarehouseState();
				yield return new LocationState()
					.Intercept.IsStateSkippable.ByDisjoin(basis => !basis.PromptLocationForEveryLine && basis.LocationID != null);
				yield return new InventoryItemState() { IsForIssue = true }
					.Intercept.HandleAbsence.ByOverride(
						(basis, barcode, base_HandleAbsence) =>
						{
							if (basis.TryProcessBy<LocationState>(barcode, StateSubstitutionRule.KeepPositiveReports | StateSubstitutionRule.KeepApplication))
								return AbsenceHandling.Done;
							if (basis.TryProcessBy<ReasonCodeState>(barcode, StateSubstitutionRule.KeepPositiveReports | StateSubstitutionRule.KeepApplication))
								return AbsenceHandling.Done;
							return base_HandleAbsence(barcode);
						});
				yield return new LotSerialState();
				yield return new ExpireDateState() { IsForIssue = true }
					.Intercept.IsStateActive.ByConjoin(basis => basis.EnsureExpireDateDefault() == null);
				yield return new ReasonCodeState()
					.Intercept.IsStateSkippable.ByDisjoin(basis => !basis.PromptLocationForEveryLine && basis.ReasonCodeID != null);
				yield return new ConfirmState();
			}

			protected override IEnumerable<ScanTransition<INScanIssue>> CreateTransitions()
			{
				if (Basis.PromptLocationForEveryLine)
				{
					return StateFlow(flow => flow
						.From<WarehouseState>()
						.NextTo<LocationState>()
						.NextTo<InventoryItemState>()
						.NextTo<LotSerialState>()
						.NextTo<ExpireDateState>()
						.NextTo<ReasonCodeState>());
				}
				else
				{
					return StateFlow(flow => flow
						.From<WarehouseState>()
						.NextTo<LocationState>()
						.NextTo<ReasonCodeState>()
						.NextTo<InventoryItemState>()
						.NextTo<LotSerialState>()
						.NextTo<ExpireDateState>());
				}
			}

			protected override IEnumerable<ScanCommand<INScanIssue>> CreateCommands()
			{
				return new ScanCommand<INScanIssue>[]
				{
					new RemoveCommand(),
					new QtySupport.SetQtyCommand(),
					new ReleaseCommand()
				};
			}

			protected override IEnumerable<ScanRedirect<INScanIssue>> CreateRedirects() => AllWMSRedirects.CreateFor<INScanIssue>();

			protected override void ResetMode(bool fullReset = false)
			{
				base.ResetMode(fullReset);

				Clear<WarehouseState>(when: fullReset);
				Clear<LocationState>(when: fullReset || Basis.PromptLocationForEveryLine);
				Clear<ReasonCodeState>(when: fullReset || Basis.PromptLocationForEveryLine);
				Clear<InventoryItemState>();
				Clear<LotSerialState>();
				Clear<ExpireDateState>();
			}
			#endregion

			#region States
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

						if (Basis.InventoryID == null)
						{
							error = FlowStatus.Fail(InventoryItemState.Msg.NotSet);
							return false;
						}

						var lsClass = Basis.SelectedLotSerialClass;


						if (Basis.CurrentMode.HasActive<LotSerialState>() && Basis.LotSerialNbr == null)
						{
							error = FlowStatus.Fail(LotSerialState.Msg.NotSet);
							return false;
						}

						if (Basis.CurrentMode.HasActive<ExpireDateState>() && Basis.ExpireDate == null)
						{
							error = FlowStatus.Fail(ExpireDateState.Msg.NotSet);
							return false;
						}

						if (Basis.CurrentMode.HasActive<LotSerialState>() &&
							lsClass.LotSerTrack == INLotSerTrack.SerialNumbered &&
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
						if (newDocument)
						{
							Basis.DocumentView.Insert();
							Basis.DocumentView.Current.Hold = false;
							Basis.DocumentView.Current.Status = INDocStatus.Balanced;
							Basis.DocumentView.Current.NoteID = Basis.NoteID;
						}

						INTran existTransaction = FindIssueRow();

						Action rollbackAction;
						if (existTransaction != null)
						{
							decimal? newQty = existTransaction.Qty + Basis.Qty;

							if (Basis.CurrentMode.HasActive<LotSerialState>() &&
								lsClass.LotSerTrack == INLotSerTrack.SerialNumbered &&
								newQty != 1) // TODO: use base qty
							{
								return FlowStatus.Fail(InventoryItemState.Msg.SerialItemNotComplexQty);
							}

							var backup = PXCache<INTran>.CreateCopy(existTransaction);

							Basis.Details.Cache.SetValueExt<INTran.qty>(existTransaction, newQty);
							Basis.Details.Cache.SetValueExt<INTran.lotSerialNbr>(existTransaction, null);
							existTransaction = Basis.Details.Update(existTransaction);

							Basis.Details.Cache.SetValueExt<INTran.lotSerialNbr>(existTransaction, Basis.LotSerialNbr);
							if (lsClass.LotSerTrackExpiration == true && Basis.ExpireDate != null)
								Basis.Details.Cache.SetValueExt<INTran.expireDate>(existTransaction, Basis.ExpireDate);
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
							setter.Set(tr => tr.LocationID, Basis.LocationID);
							setter.Set(tr => tr.UOM, Basis.UOM);
							setter.Set(tr => tr.ReasonCode, Basis.ReasonCodeID);
							existTransaction = Basis.Details.Update(existTransaction);

							Basis.Details.Cache.SetValueExt<INTran.qty>(existTransaction, Basis.Qty);
							existTransaction = Basis.Details.Update(existTransaction);

							Basis.Details.Cache.SetValueExt<INTran.lotSerialNbr>(existTransaction, Basis.LotSerialNbr);
							if (lsClass.LotSerTrackExpiration == true && Basis.ExpireDate != null)
								Basis.Details.Cache.SetValueExt<INTran.expireDate>(existTransaction, Basis.ExpireDate);
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
						if (Basis.CurrentMode.HasActive<LotSerialState>() && !string.IsNullOrEmpty(Basis.LotSerialNbr) && Basis.Graph.splits.SelectMain().Any(s => s.LotSerialNbr != Basis.LotSerialNbr))
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
						if (Basis.CurrentMode.HasActive<LocationState>() && Basis.Graph.splits.SelectMain().Any(s => s.LocationID != Basis.LocationID))
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
						INTran existTransaction = FindIssueRow();

						if (existTransaction == null)
							return FlowStatus.Fail(Msg.LineMissing, Basis.SelectedInventoryItem.InventoryCD);

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

					protected virtual INTran FindIssueRow()
					{
						var existTransactions = Basis.Details.SelectMain().Where(t =>
							t.InventoryID == Basis.InventoryID &&
							t.SiteID == Basis.SiteID &&
							t.LocationID == (Basis.LocationID ?? t.LocationID) &&
							t.ReasonCode == (Basis.ReasonCodeID ?? t.ReasonCode)
							&& t.UOM == Basis.UOM);

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
					public const string Prompt = "Confirm issuing {0} x {1} {2}.";
					public const string LineMissing = "The {0} item is not found in the issue.";
					public const string InventoryAdded = "{0} x {1} {2} has been added to the issue.";
					public const string InventoryRemoved = "{0} x {1} {2} has been removed from the issue.";

					public const string QtyExceedsOnLot = "The quantity of the {1} item in the issue exceeds the item quantity in the {0} lot.";
					public const string QtyExceedsOnLocation = "The quantity of the {1} item in the issue exceeds the item quantity in the {0} location.";
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
					public const string DocumentReleasing = "The {0} issue is being released.";
					public const string DocumentIsReleased = "The issue has been successfully released.";
					public const string DocumentReleaseFailed = "The issue release failed.";
				}
				#endregion
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public new abstract class Msg
			{
				public const string Description = "Scan and Issue";
			}
			#endregion
		}

		#region Redirect
		public new sealed class RedirectFrom<TForeignBasis> : WMSBase.RedirectFrom<TForeignBasis>
			where TForeignBasis : PXGraphExtension, IBarcodeDrivenStateMachine
		{
			public override string Code => "INISSUE";
			public override string DisplayName => Msg.DisplayName;

			#region Messages
			[PXLocalizable]
			public abstract class Msg
			{
				public const string DisplayName = "IN Issue";
			}
			#endregion
		}
		#endregion
	}
}