using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.BarcodeProcessing;
using PX.Objects.Common;

namespace PX.Objects.IN.WMS
{
	using WMSBase = INScanRegisterBase<INScanReceive, INScanReceive.Host, INDocType.receipt>;

	public class INScanReceive : WMSBase
	{
		public class Host : INReceiptEntry { }

		public new class QtySupport : WMSBase.QtySupport { }
		public new class GS1Support : WMSBase.GS1Support { }
		public new class UserSetup : WMSBase.UserSetup { }

		#region Configuration
		public override bool ExplicitConfirmation => Setup.Current.ExplicitLineConfirmation == true;

		public override bool PromptLocationForEveryLine => Setup.Current.RequestLocationForEachItemInReceipt == true;
		public override bool UseDefaultReasonCode => Setup.Current.UseDefaultReasonCodeInReceipt == true;
		public override bool UseDefaultWarehouse => UserSetup.For(Graph).DefaultWarehouse == true;

		protected override bool UseQtyCorrectection => Setup.Current.UseDefaultQtyInReceipt != true;
		protected override bool CanOverrideQty => (!DocumentLoaded || NotReleasedAndHasLines) && (SelectedLotSerialClass?.LotSerTrack == INLotSerTrack.SerialNumbered).Implies(SelectedLotSerialClass?.LotSerAssign == INLotSerAssign.WhenUsed);
		#endregion

		#region DAC overrides
		[Common.Attributes.BorrowedNote(typeof(INRegister), typeof(INReceiptEntry))]
		protected virtual void _(Events.CacheAttached<ScanHeader.noteID> e) { }
		#endregion

		protected override IEnumerable<ScanMode<INScanReceive>> CreateScanModes() { yield return new ReceiptMode(); }
		public sealed class ReceiptMode : ScanMode
		{
			public const string Value = "INRE";
			public class value : BqlString.Constant<value> { public value() : base(ReceiptMode.Value) { } }

			public override string Code => Value;
			public override string Description => Msg.Description;

			#region State Machine
			protected override IEnumerable<ScanState<INScanReceive>> CreateStates()
			{
				foreach (var state in base.CreateStates())
					yield return state;
				yield return new WarehouseState();
				yield return new LocationState()
					.Intercept.IsStateSkippable.ByDisjoin(basis => !basis.PromptLocationForEveryLine && basis.LocationID != null);
				yield return new ReasonCodeState()
					.Intercept.IsStateSkippable.ByDisjoin(basis => !basis.PromptLocationForEveryLine && basis.ReasonCodeID != null);
				yield return new InventoryItemState()
					.Intercept.HandleAbsence.ByOverride(
						(basis, barcode, base_HandleAbsence) =>
						{
							if (basis.TryProcessBy<LocationState>(barcode, StateSubstitutionRule.KeepPositiveReports | StateSubstitutionRule.KeepApplication))
								return AbsenceHandling.Done;
							if (basis.TryProcessBy<ReasonCodeState>(barcode, StateSubstitutionRule.KeepPositiveReports | StateSubstitutionRule.KeepApplication))
								return AbsenceHandling.Done;
							return base_HandleAbsence(barcode);
						});
				yield return new LotSerialState()
					.Intercept.IsStateActive.ByConjoin(basis => basis.IsEnterableLotSerial(isForIssue: false));
				yield return new ExpireDateState()
					.Intercept.IsStateActive.ByConjoin(basis => basis.EnsureExpireDateDefault() == null);
				yield return new ConfirmState();
			}

			protected override IEnumerable<ScanTransition<INScanReceive>> CreateTransitions()
			{
				if (Basis.PromptLocationForEveryLine)
				{
					return StateFlow(flow => flow
						.From<WarehouseState>()
						.NextTo<InventoryItemState>()
						.NextTo<LotSerialState>()
						.NextTo<ExpireDateState>()
						.NextTo<LocationState>()
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

			protected override IEnumerable<ScanCommand<INScanReceive>> CreateCommands()
			{
				return new ScanCommand<INScanReceive>[]
				{
					new RemoveCommand(),
					new QtySupport.SetQtyCommand(),
					new ReleaseCommand()
				};
			}

			protected override IEnumerable<ScanRedirect<INScanReceive>> CreateRedirects() => AllWMSRedirects.CreateFor<INScanReceive>();

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

						if (Basis.CurrentMode.HasActive<ExpireDateState>()
							&& Basis.ExpireDate == null)
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

						INTran existTransaction = FindReceiptRow();

						if (existTransaction != null)
						{
							var newQty = existTransaction.Qty + Basis.Qty;

							if (Basis.CurrentMode.HasActive<LotSerialState>() &&
								lsClass.LotSerTrack == INLotSerTrack.SerialNumbered &&
								newQty != 1) // TODO: use base qty
							{
								return FlowStatus.Fail(InventoryItemState.Msg.SerialItemNotComplexQty);
							}

							Basis.Details.Cache.SetValueExt<INTran.qty>(existTransaction, newQty);
							existTransaction = Basis.Details.Update(existTransaction);
						}
						else
						{
							INTran tran = Basis.Details.Insert();
							Basis.Details.Cache.SetValueExt<INTran.inventoryID>(tran, Basis.InventoryID);
							tran = Basis.Details.Update(tran);

							Basis.Details.Cache.SetValueExt<INTran.siteID>(tran, Basis.SiteID);
							Basis.Details.Cache.SetValueExt<INTran.locationID>(tran, Basis.LocationID);
							Basis.Details.Cache.SetValueExt<INTran.uOM>(tran, Basis.UOM);
							Basis.Details.Cache.SetValueExt<INTran.qty>(tran, Basis.Qty);
							Basis.Details.Cache.SetValueExt<INTran.expireDate>(tran, Basis.ExpireDate);
							Basis.Details.Cache.SetValueExt<INTran.lotSerialNbr>(tran, Basis.LotSerialNbr);
							Basis.Details.Cache.SetValueExt<INTran.reasonCode>(tran, Basis.ReasonCodeID);
							existTransaction = Basis.Details.Update(tran);

							if (HasErrors(existTransaction, out var error))
							{
								Base.transactions.Delete(existTransaction);
								return error;
							}
						}

						if (!string.IsNullOrEmpty(Basis.LotSerialNbr))
						{
							foreach (INTranSplit split in Basis.Graph.splits.Select())
							{
								Basis.Graph.splits.Cache.SetValueExt<INTranSplit.expireDate>(split, Basis.ExpireDate ?? existTransaction.ExpireDate);
								Basis.Graph.splits.Cache.SetValueExt<INTranSplit.lotSerialNbr>(split, Basis.LotSerialNbr);
								Basis.Graph.splits.Update(split);
							}
						}

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

					protected virtual FlowStatus ConfirmRemove()
					{
						INTran existTransaction = FindReceiptRow();

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

					protected virtual bool HasErrors(INTran tran, out FlowStatus error)
					{
						if (Basis.HasUIErrors(tran, out error))
							return true;

						error = FlowStatus.Ok;
						return false;
					}

					protected virtual INTran FindReceiptRow()
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
					public const string Prompt = "Confirm receiving {0} x {1} {2}.";
					public const string LineMissing = "The {0} item is not found in the receipt.";
					public const string InventoryAdded = "{0} x {1} {2} has been added to the receipt.";
					public const string InventoryRemoved = "{0} x {1} {2} has been removed from the receipt.";
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

				protected override void OnAfterRelease(INRegister doc)
				{
					base.OnAfterRelease(doc);

					if (PXAccess.FeatureInstalled<CS.FeaturesSet.deviceHub>() && doc.RefNbr != null)
					{
						var setup = UserSetup.For(Basis);
						bool printInventory = setup.PrintInventoryLabelsAutomatically == true;
						string printLabelsReportID = setup.InventoryLabelsReportID;

						if (printInventory && !string.IsNullOrEmpty(printLabelsReportID))
						{
							var reportParameters = new Dictionary<string, string>()
							{
								[nameof(INRegister.RefNbr)] = doc.RefNbr
							};

							DeviceHubTools.PrintReportViaDeviceHub<CR.BAccount>(Basis, printLabelsReportID, reportParameters, INNotificationSource.None, null);
						}
					}
				}

				#region Messages
				[PXLocalizable]
				public new abstract class Msg : WMSBase.ReleaseCommand.Msg
				{
					public const string DocumentReleasing = "The {0} receipt is being released.";
					public const string DocumentIsReleased = "The receipt has been successfully released.";
					public const string DocumentReleaseFailed = "The receipt release failed.";
				}
				#endregion
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public new abstract class Msg
			{
				public const string Description = "Scan and Receive";
			}
			#endregion
		}

		#region Redirect
		public new sealed class RedirectFrom<TForeignBasis> : WMSBase.RedirectFrom<TForeignBasis>
			where TForeignBasis : PXGraphExtension, IBarcodeDrivenStateMachine
		{
			public override string Code => "INRECEIVE";
			public override string DisplayName => Msg.DisplayName;

			#region Messages
			[PXLocalizable]
			public abstract class Msg
			{
				public const string DisplayName = "IN Receive";
			}
			#endregion
		}
		#endregion
	}
}