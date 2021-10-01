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

using SiteStatus = PX.Objects.IN.Overrides.INDocumentRelease.SiteStatus;
using LocationStatus = PX.Objects.IN.Overrides.INDocumentRelease.LocationStatus;
using LotSerialStatus = PX.Objects.IN.Overrides.INDocumentRelease.LotSerialStatus;
using ItemLotSerial = PX.Objects.IN.Overrides.INDocumentRelease.ItemLotSerial;
using SiteLotSerial = PX.Objects.IN.Overrides.INDocumentRelease.SiteLotSerial;

namespace PX.Objects.PO.WMS
{
	using WMSBase = WarehouseManagementSystem<ReceivePutAway, ReceivePutAway.Host>;

	public partial class ReceivePutAway : WMSBase
	{
		public sealed class PutAwayMode : ScanMode
		{
			public const string Value = "PTAW";
			public class value : BqlString.Constant<value> { public value() : base(PutAwayMode.Value) { } }

			public PutAwayMode.Logic Body => Get<PutAwayMode.Logic>();

			public override string Code => Value;
			public override string Description => Msg.Description;

			#region State Machine
			protected override IEnumerable<ScanState<ReceivePutAway>> CreateStates()
			{
				yield return new ReceiptState();
				yield return new SourceLocationState();
				yield return new TargetLocationState();
				yield return new InventoryItemState() { AlternateType = INPrimaryAlternateType.VPN };
				yield return new LotSerialState()
					.Intercept.IsStateActive.ByConjoin(basis => basis.SelectedLotSerialClass.LotSerAssign == INLotSerAssign.WhenReceived);
				yield return new ConfirmState();
				yield return new CommandOrReceiptOnlyState();
			}

			protected override IEnumerable<ScanTransition<ReceivePutAway>> CreateTransitions()
			{
				if (Body.PromptLocationForEveryLine)
				{
					return StateFlow(flow => flow
						.From<ReceiptState>()
						.NextTo<SourceLocationState>()
						.NextTo<InventoryItemState>()
						.NextTo<LotSerialState>()
						.NextTo<TargetLocationState>());
				}
				else
				{
					return StateFlow(flow => flow
						.From<ReceiptState>()
						.NextTo<SourceLocationState>()
						.NextTo<TargetLocationState>()
						.NextTo<InventoryItemState>()
						.NextTo<LotSerialState>());
				}
			}

			protected override IEnumerable<ScanCommand<ReceivePutAway>> CreateCommands()
			{
				yield return new RemoveCommand();
				yield return new QtySupport.SetQtyCommand();
				yield return new ReleaseTransferCommand();
			}

			protected override IEnumerable<ScanRedirect<ReceivePutAway>> CreateRedirects() => AllWMSRedirects.CreateFor<ReceivePutAway>();

			protected override void ResetMode(bool fullReset)
			{
				base.ResetMode(fullReset);

				Clear<ReceiptState>(when: fullReset && !Basis.IsWithinReset);
				Clear<SourceLocationState>(when: fullReset || Body.PromptLocationForEveryLine && Body.IsSingleLocation == false);
				Clear<TargetLocationState>(when: fullReset || Body.PromptLocationForEveryLine);
				Clear<InventoryItemState>();
				Clear<LotSerialState>();
			}
			#endregion

			#region Logic
			public class Logic : ScanExtension
			{
				public override void Initialize() => _transferEntryLazy = Lazy.By(CreateTransferEntry);

				public virtual bool CanPutAway => PutAway.SelectMain().Any(s => s.PutAwayQty != s.Qty);

				#region Views
				public
					SelectFrom<POReceiptLineSplit>.
					View PutAway;
				public virtual IEnumerable putAway() => Basis.SortedResult(Basis.GetSplits());

				public
					SelectFrom<POReceiptSplitToTransferSplitLink>.
					InnerJoin<INTranSplit>.On<POReceiptSplitToTransferSplitLink.FK.TransferLineSplit>.
					InnerJoin<INTran>.On<POReceiptSplitToTransferSplitLink.FK.TransferLine>.
					Where<POReceiptSplitToTransferSplitLink.FK.ReceiptLineSplit.SameAsCurrent>.
					View TransferSplitLinks;
				#endregion

				#region Buttons
				public PXAction<ScanHeader> ReviewPutAway;
				[PXButton(CommitChanges = true), PXUIField(DisplayName = "Review")]
				protected virtual IEnumerable reviewPutAway(PXAdapter adapter) => adapter.Get();

				public PXAction<ScanHeader> ViewTransferInfo;
				[PXButton, PXUIField(DisplayName = "Transfer Allocations")]
				protected virtual void viewTransferInfo() => TransferSplitLinks.AskExt();
				#endregion

				#region Event Handlers
				protected virtual void _(Events.RowSelected<ScanHeader> e)
				{
					if (e.Row == null)
						return;

					ReviewPutAway.SetVisible(Base.IsMobile && e.Row.Mode == PutAwayMode.Value);
					INItemPlanIDAttribute.SetReleaseMode<POReceiptLineSplit.planID>(Base.splits.Cache, e.Row.Mode == PutAwayMode.Value);

					if (!String.IsNullOrEmpty(Basis.RefNbr))
					{
						if (e.Row.Mode == PutAwayMode.Value)
							Basis.TransferRefNbr = GetTransfer()?.RefNbr;
					}
				}
				#endregion

				#region Configuration
				public virtual bool PromptLocationForEveryLine => Basis.Setup.Current.RequestLocationForEachItemInPutAway == true;
				public virtual bool IsSingleLocation => PutAway.SelectMain().GroupBy(s => s.LocationID).Count() < 2;
				#endregion

				#region Transfer Entry
				public virtual INRegister GetTransfer()
				{
					return
						SelectFrom<INRegister>.
						Where<
							INRegister.docType.IsEqual<INDocType.transfer>.
							And<INRegister.transferType.IsEqual<INTransferType.oneStep>>.
							And<INRegister.released.IsEqual<False>>.
							And<INRegister.pOReceiptType.IsEqual<POReceipt.receiptType.FromCurrent>>.
							And<INRegister.pOReceiptNbr.IsEqual<POReceipt.receiptNbr.FromCurrent>>>.
						View.ReadOnly.SelectSingleBound(Basis, new[] { Basis.Receipt });
				}

				public INTransferEntry TransferEntry => _transferEntryLazy.Value;
				private Lazy<INTransferEntry> _transferEntryLazy;
				private INTransferEntry CreateTransferEntry()
				{
					var transferEntry = PXGraph.CreateInstance<INTransferEntry>();

					transferEntry.FieldDefaulting.AddHandler<INTran.pOReceiptType>(
						(cache, args) => args.NewValue = ((INTransferEntry)cache.Graph).transfer.Current?.POReceiptType);
					transferEntry.FieldDefaulting.AddHandler<INTran.pOReceiptNbr>(
						(cache, args) => args.NewValue = ((INTransferEntry)cache.Graph).transfer.Current?.POReceiptNbr);

					Basis.Graph.Caches[typeof(SiteStatus)] = transferEntry.Caches[typeof(SiteStatus)];
					Basis.Graph.Caches[typeof(LocationStatus)] = transferEntry.Caches[typeof(LocationStatus)];
					Basis.Graph.Caches[typeof(LotSerialStatus)] = transferEntry.Caches[typeof(LotSerialStatus)];
					Basis.Graph.Caches[typeof(SiteLotSerial)] = transferEntry.Caches[typeof(SiteLotSerial)];
					Basis.Graph.Caches[typeof(ItemLotSerial)] = transferEntry.Caches[typeof(ItemLotSerial)];

					Basis.Graph.Views.Caches.Remove(typeof(SiteStatus));
					Basis.Graph.Views.Caches.Remove(typeof(LocationStatus));
					Basis.Graph.Views.Caches.Remove(typeof(LotSerialStatus));
					Basis.Graph.Views.Caches.Remove(typeof(SiteLotSerial));
					Basis.Graph.Views.Caches.Remove(typeof(ItemLotSerial));

					OnTransferEntryCreated(transferEntry);

					return transferEntry;
				}

				protected virtual void OnTransferEntryCreated(INTransferEntry transferEntry)
				{
					if (Basis.TransferRefNbr != null)
						transferEntry.transfer.Current = GetTransfer();
				}
				#endregion
			}
			#endregion

			#region States
			public new sealed class ReceiptState : ReceivePutAway.ReceiptState
			{
				private int? siteID = null;

				protected override Validation Validate(POReceipt receipt)
				{
					if (receipt.Released == false)
						return Validation.Fail(Msg.InvalidStatus, receipt.ReceiptNbr, Basis.SightOf<POReceipt.status>(receipt));

					if (receipt.ReceiptType == POReceiptType.POReturn)
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
						return Validation.Fail(Msg.HasNonStockKit, receipt.ReceiptNbr);

					if (Get<Logic>().CanBePutAway(receipt, out var error) == false)
						return error;

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

					Basis.TransferRefNbr = null;
					Get<PutAwayMode.Logic>().TransferEntry.transfer.Current = null;
				}

				protected override void SetNextState()
				{
					if (Basis.Remove == false && Get<PutAwayMode.Logic>().CanPutAway == false)
						Basis.SetScanState(BuiltinScanStates.Command, PutAwayMode.Msg.Completed, Basis.RefNbr);
					else
						base.SetNextState();
				}

				#region Logic
				public class Logic : ScanExtension
				{
					public virtual bool CanBePutAway(POReceipt receipt, out Validation error)
					{
						POReceiptLineSplit notPutSplit =
							SelectFrom<POReceiptLineSplit>.
							Where<
								POReceiptLineSplit.receiptNbr.IsEqual<@P.AsString>.
								And<POReceiptLineSplit.putAwayQty.IsLess<POReceiptLineSplit.qty>>>.
							View.Select(Basis, receipt.ReceiptNbr);

						if (notPutSplit == null)
						{
							INRegister notReleasedTransfer =
								SelectFrom<INRegister>.
								Where<
									INRegister.docType.IsEqual<INDocType.transfer>.
									And<INRegister.transferType.IsEqual<INTransferType.oneStep>>.
									And<INRegister.released.IsEqual<False>>.
									And<INRegister.pOReceiptType.IsEqual<POReceipt.receiptType.FromCurrent>>.
									And<INRegister.pOReceiptNbr.IsEqual<POReceipt.receiptNbr.FromCurrent>>>.
								View.ReadOnly.SelectSingleBound(Basis, new[] { receipt });

							if (notReleasedTransfer == null)
							{
								error = Validation.Fail(Msg.AlreadyPutAwayInFull, receipt.ReceiptNbr);
								return false;
							}
						}

						error = Validation.Ok;
						return true;
					}
				}
				#endregion

				#region Messages
				[PXLocalizable]
				public new abstract class Msg : ReceivePutAway.ReceiptState.Msg
				{
					public const string AlreadyPutAwayInFull = "The {0} receipt has already been put away in full.";
				}
				#endregion
			}

			public sealed class SourceLocationState : LocationState
			{
				protected override bool IsStateActive() => base.IsStateActive() && Get<PutAwayMode.Logic>().IsSingleLocation == false;
				protected override bool IsStateSkippable() => Basis.LocationID != null;

				protected override string StatePrompt => Msg.Prompt;
				protected override void ReportSuccess(INLocation location) => Basis.Reporter.Info(Msg.Ready, location.LocationCD);

				protected override Validation Validate(INLocation location)
				{
					if (Get<PutAwayMode.Logic>().PutAway.SelectMain().All(t => t.LocationID != location.LocationID))
						return Validation.Fail(Msg.MissingInReceipt, location.LocationCD);
					return base.Validate(location);
				}

				#region Messages
				[PXLocalizable]
				public new abstract class Msg : LocationState.Msg
				{
					public new const string Prompt = "Scan the barcode of the origin location.";
					public new const string Ready = "The {0} location is selected as the origin location.";
					public new const string NotSet = "The origin location is not selected.";
					public const string MissingInReceipt = "The {0} location is not present in the receipt.";
				}
				#endregion
			}

			public sealed class TargetLocationState : LocationState
			{
				public new const string Value = "TLOC";
				public new class value : BqlString.Constant<value> { public value() : base(TargetLocationState.Value) { } }

				public override string Code => Value;
				protected override string StatePrompt => Msg.Prompt;

				protected override bool IsStateSkippable() => Basis.ToLocationID != null;

				protected override void ReportSuccess(INLocation location) => Basis.Reporter.Info(Msg.Ready, location.LocationCD);

				protected override void Apply(INLocation location) => Basis.ToLocationID = location.LocationID;
				protected override void ClearState() => Basis.ToLocationID = null;

				#region Messages
				[PXLocalizable]
				public new abstract class Msg : LocationState.Msg
				{
					public new const string Prompt = "Scan the barcode of the destination location.";
					public new const string Ready = "The {0} location is selected as the destination location.";
					public new const string NotSet = "Destination Location is not selected";
				}
				#endregion
			}

			public sealed class InventoryItemState : WMSBase.InventoryItemState
			{
				protected override AbsenceHandling.Of<PXResult<INItemXRef, InventoryItem>> HandleAbsence(string barcode)
				{
					if (Get<PutAwayMode.Logic>().IsSingleLocation)
					{
						if (Basis.TryProcessBy<TargetLocationState>(barcode, StateSubstitutionRule.KeepPositiveReports | StateSubstitutionRule.KeepApplication))
							return AbsenceHandling.Done;
					}
					else
					{
						if (Basis.TryProcessBy<SourceLocationState>(barcode, StateSubstitutionRule.KeepPositiveReports | StateSubstitutionRule.KeepApplication))
							return AbsenceHandling.Done;
					}

					return base.HandleAbsence(barcode);
				}

				protected override Validation Validate(PXResult<INItemXRef, InventoryItem> entity)
				{
					if (Basis.HasFault(entity, base.Validate, out var fault))
						return fault;

					if (Get<PutAwayMode.Logic>().PutAway.SelectMain().All(t => t.InventoryID != entity.GetItem<InventoryItem>().InventoryID))
						return Validation.Fail(Msg.MissingInReceipt, entity.GetItem<InventoryItem>().InventoryCD);

					return Validation.Ok;
				}

				#region Messages
				[PXLocalizable]
				public new abstract class Msg : WMSBase.InventoryItemState.Msg
				{
					public const string MissingInReceipt = "The {0} item is not present in the receipt.";
				}
				#endregion
			}

			public sealed class LotSerialState : WMSBase.LotSerialState
			{
				protected override Validation Validate(string lotSerial)
				{
					if (Basis.HasFault(lotSerial, base.Validate, out var fault))
						return fault;

					if (Get<PutAwayMode.Logic>().PutAway.SelectMain().All(t => t.LotSerialNbr != lotSerial))
						return Validation.Fail(Msg.MissingInReceipt, lotSerial);

					return Validation.Ok;
				}

				#region Messages
				[PXLocalizable]
				public new abstract class Msg : WMSBase.LotSerialState.Msg
				{
					public const string MissingInReceipt = "The {0} lot/serial number is not present in the receipt.";
				}
				#endregion
			}

			public sealed class ConfirmState : ConfirmationState
			{
				public override string Prompt => Basis.Localize(Msg.Prompt, Basis.SightOf<WMSScanHeader.inventoryID>(), Basis.Qty, Basis.UOM);
				protected override bool ExecuteInTransaction => true;

				protected override FlowStatus PerformConfirmation() => Get<Logic>().ProcessPutAway();

				#region Logic
				public class Logic : ScanExtension
				{
					public PutAwayMode.Logic Mode { get; private set; }
					public override void Initialize() => Mode = Basis.Get<PutAwayMode.Logic>();

					public virtual FlowStatus ProcessPutAway()
					{
						bool remove = Basis.Remove == true;

						var receivedSplits =
							Mode.PutAway.SelectMain().Where(
								r => r.InventoryID == Basis.InventoryID
									&& r.SubItemID == Basis.SubItemID
									&& r.LotSerialNbr == (Basis.LotSerialNbr ?? r.LotSerialNbr)
									&& r.LocationID == (Basis.LocationID ?? r.LocationID)
									&& (remove ? r.PutAwayQty > 0 : r.PutAwayQty < r.Qty));
						if (receivedSplits.Any() == false)
							return FlowStatus.Fail(Msg.NothingToPutAway).WithModeReset;

						if (!Basis.EnsureLocationPrimaryItem(Basis.InventoryID, Basis.LocationID))
							return FlowStatus.Fail(IN.Messages.NotPrimaryLocation);

						decimal qty = Sign.MinusIf(remove) * Basis.BaseQty;

						if (!remove && receivedSplits.Sum(s => s.Qty - s.PutAwayQty) < qty)
							return FlowStatus.Fail(Msg.Overputting);
						if (remove && receivedSplits.Sum(s => s.PutAwayQty) + qty < 0)
							return FlowStatus.Fail(Msg.Underputting);

						decimal unassignedQty = qty;
						foreach (var receivedSplit in remove ? receivedSplits.Reverse() : receivedSplits)
						{
							decimal currentQty = remove
								? -Math.Min(receivedSplit.PutAwayQty.Value, -unassignedQty)
								: +Math.Min(receivedSplit.Qty.Value - receivedSplit.PutAwayQty.Value, unassignedQty);

							FlowStatus transferStatus = SyncWithTransfer(receivedSplit, currentQty);
							if (transferStatus.IsError != false)
								return transferStatus;

							receivedSplit.PutAwayQty += currentQty;
							Mode.PutAway.Update(receivedSplit);

							unassignedQty -= currentQty;
							if (unassignedQty == 0)
								break;
						}

						Basis.DispatchNext(
							remove ? Msg.InventoryRemoved : Msg.InventoryAdded,
							Basis.SelectedInventoryItem.InventoryCD, Basis.Qty, Basis.UOM);

						return FlowStatus.Ok;
					}

					public virtual FlowStatus SyncWithTransfer(POReceiptLineSplit receivedSplit, decimal qty)
					{
						bool isNewDocument = false;

						PXSelectBase<INRegister> transfer = Mode.TransferEntry.transfer;
						PXSelectBase<INTran> lines = Mode.TransferEntry.transactions;
						PXSelectBase<INTranSplit> splits = Mode.TransferEntry.splits;

						if (transfer.Current == null)
						{
							var doc = (INRegister)transfer.Cache.CreateInstance();
							doc.SiteID = Basis.SiteID;
							doc.ToSiteID = Basis.SiteID;
							doc.POReceiptType = Basis.Receipt.ReceiptType;
							doc.POReceiptNbr = Basis.Receipt.ReceiptNbr;
							doc.OrigModule = GL.BatchModule.PO;
							doc = transfer.Insert(doc);

							isNewDocument = true;
						}

						INTranSplit[] linkedSplits = isNewDocument
							? Array.Empty<INTranSplit>()
							: SelectFrom<POReceiptSplitToTransferSplitLink>.
								InnerJoin<INTranSplit>.On<POReceiptSplitToTransferSplitLink.FK.TransferLineSplit>.
								Where<
									POReceiptSplitToTransferSplitLink.FK.ReceiptLineSplit.SameAsCurrent.
									And<POReceiptSplitToTransferSplitLink.transferRefNbr.IsEqual<P.AsString>>.
									And<INTranSplit.toLocationID.IsEqual<P.AsInt>>>.
								View.ReadOnly.SelectMultiBound(Basis, new object[] { receivedSplit }, transfer.Current.RefNbr, Basis.ToLocationID)
								.RowCast<INTranSplit>()
								.ToArray();

						INTranSplit[] appropriateSplits = isNewDocument
							? Array.Empty<INTranSplit>()
							: SelectFrom<INTranSplit>.
								Where<
									INTranSplit.refNbr.IsEqual<P.AsString>.
									And<INTranSplit.inventoryID.IsEqual<POReceiptLineSplit.inventoryID.FromCurrent>>.
									And<INTranSplit.subItemID.IsEqual<POReceiptLineSplit.subItemID.FromCurrent>>.
									And<INTranSplit.siteID.IsEqual<POReceiptLineSplit.siteID.FromCurrent>>.
									And<INTranSplit.locationID.IsEqual<POReceiptLineSplit.locationID.FromCurrent>>.
									And<INTranSplit.lotSerialNbr.IsEqual<POReceiptLineSplit.lotSerialNbr.FromCurrent>>.
									And<INTranSplit.toLocationID.IsEqual<P.AsInt>>>.
								View.ReadOnly.SelectMultiBound(Basis, new object[] { receivedSplit }, transfer.Current.RefNbr, Basis.ToLocationID)
								.RowCast<INTranSplit>()
								.ToArray();

						INTranSplit[] existingINSplits = isNewDocument
							? Array.Empty<INTranSplit>()
							: linkedSplits.Concat(appropriateSplits).ToArray();

						bool isNewSplit = false;
						INTran tran;
						INTranSplit tranSplit;

						if (existingINSplits.Length == 0)
						{
							tran = lines.With(_ => _.Insert() ?? _.Insert());
							tran.InventoryID = receivedSplit.InventoryID;
							tran.SubItemID = receivedSplit.SubItemID;
							tran.LotSerialNbr = receivedSplit.LotSerialNbr;
							tran.ExpireDate = receivedSplit.ExpireDate;
							tran.UOM = receivedSplit.UOM;
							tran.SiteID = receivedSplit.SiteID;
							tran.LocationID = receivedSplit.LocationID;
							tran.ToSiteID = Basis.SiteID;
							tran.ToLocationID = Basis.ToLocationID;
							tran.POReceiptLineNbr = receivedSplit.LineNbr;
							tran = lines.Update(tran);

							tranSplit = splits.Search<INTranSplit.lineNbr>(tran.LineNbr);
							if (tranSplit == null)
							{
								tranSplit = splits.With(_ => _.Insert() ?? _.Insert());
								tranSplit.LotSerialNbr = Basis.LotSerialNbr;
								tranSplit.ExpireDate = Basis.ExpireDate;
								tranSplit.ToSiteID = Basis.SiteID;
								tranSplit.ToLocationID = Basis.ToLocationID;
								tranSplit = splits.Update(tranSplit);
							}

							isNewSplit = true;
						}
						else
						{
							tranSplit = existingINSplits.FirstOrDefault(s => s.LotSerialNbr == (Basis.LotSerialNbr ?? s.LotSerialNbr));
							if (tranSplit != null)
							{
								tran = lines.Current = lines.Search<INTran.lineNbr>(tranSplit.LineNbr);
								tranSplit = splits.Search<INTranSplit.splitLineNbr>(tranSplit.SplitLineNbr);
							}
							else
							{
								tran = lines.Current = lines.Search<INTran.lineNbr>(existingINSplits.First().LineNbr);
								tranSplit = splits.With(_ => _.Insert() ?? _.Insert());
								tranSplit.LotSerialNbr = Basis.LotSerialNbr;
								tranSplit.ExpireDate = Basis.ExpireDate;
								tranSplit.ToSiteID = Basis.SiteID;
								tranSplit.ToLocationID = Basis.ToLocationID;
								tranSplit = splits.Update(tranSplit);

								isNewSplit = true;
							}
						}

						tranSplit.Qty += qty;
						tranSplit = splits.Update(tranSplit);
						if (tranSplit.Qty == 0)
							splits.Delete(tranSplit);

						tran = lines.Search<INTran.lineNbr>(tran.LineNbr);

						if (qty < 0)
						{
							if (tran.Qty + qty == 0)
								lines.Delete(tran);
							else
							{
								// qty deduction is not synchronized with splits - we don't want unassigned qty to show up
								tran.Qty += INUnitAttribute.ConvertFromBase(lines.Cache, tran.InventoryID, tran.UOM, qty, INPrecision.NOROUND);
								tran = lines.Update(tran);
							}
						}
						Mode.TransferEntry.Save.Press();

						if (isNewDocument)
							Basis.TransferRefNbr = transfer.Current.RefNbr;

						if (isNewSplit)
							tranSplit = splits.Search<INTranSplit.lineNbr, INTranSplit.splitLineNbr>(tranSplit.LineNbr, tranSplit.SplitLineNbr);

						return EnsureReceiptTransferSplitLink(receivedSplit, tranSplit, qty);
					}

					protected virtual FlowStatus EnsureReceiptTransferSplitLink(POReceiptLineSplit poSplit, INTranSplit inSplit, decimal deltaQty)
					{
						var allLinks =
							SelectFrom<POReceiptSplitToTransferSplitLink>.
							Where<
								POReceiptSplitToTransferSplitLink.FK.TransferLineSplit.SameAsCurrent.
								Or<POReceiptSplitToTransferSplitLink.FK.ReceiptLineSplit.SameAsCurrent>>.
							View.SelectMultiBound(Basis, new object[] { inSplit, poSplit })
							.RowCast<POReceiptSplitToTransferSplitLink>()
							.ToArray();

						POReceiptSplitToTransferSplitLink currentLink = allLinks.FirstOrDefault(
							link => POReceiptSplitToTransferSplitLink.FK.TransferLineSplit.Match(Basis, inSplit, link)
								&& POReceiptSplitToTransferSplitLink.FK.ReceiptLineSplit.Match(Basis, poSplit, link));

						decimal transferQty = allLinks.Where(link => POReceiptSplitToTransferSplitLink.FK.TransferLineSplit.Match(Basis, inSplit, link)).Sum(link => link.Qty ?? 0);
						decimal receiptQty = allLinks.Where(link => POReceiptSplitToTransferSplitLink.FK.ReceiptLineSplit.Match(Basis, poSplit, link)).Sum(link => link.Qty ?? 0);

						if (transferQty + deltaQty > inSplit.Qty)
						{
							return FlowStatus.Fail(Msg.LinkTransferOverpicking);
						}
						if (receiptQty + deltaQty > poSplit.Qty)
						{
							return FlowStatus.Fail(Msg.LinkReceiptOverpicking);
						}
						if (currentLink == null ? deltaQty < 0 : currentLink.Qty + deltaQty < 0)
						{
							return FlowStatus.Fail(Msg.LinkUnderpicking);
						}

						if (currentLink == null)
						{
							currentLink = Mode.TransferSplitLinks.Insert(new POReceiptSplitToTransferSplitLink
							{
								ReceiptNbr = poSplit.ReceiptNbr,
								ReceiptLineNbr = poSplit.LineNbr,
								ReceiptSplitLineNbr = poSplit.SplitLineNbr,
								TransferRefNbr = inSplit.RefNbr,
								TransferLineNbr = inSplit.LineNbr,
								TransferSplitLineNbr = inSplit.SplitLineNbr,
								Qty = deltaQty
							});
						}
						else
						{
							currentLink.Qty += deltaQty;
							currentLink = Mode.TransferSplitLinks.Update(currentLink);
						}

						if (currentLink.Qty == 0)
							Mode.TransferSplitLinks.Delete(currentLink);

						return FlowStatus.Ok;
					}
				}
				#endregion

				#region Messages
				[PXLocalizable]
				public abstract class Msg
				{
					public const string Prompt = "Confirm putting away {0} x {1} {2}.";
					public const string NothingToPutAway = "No items to put away.";

					public const string Overputting = "The put away quantity cannot be greater than the received quantity.";
					public const string Underputting = "The put away quantity cannot become negative.";

					public const string InventoryAdded = "{0} x {1} {2} has been added to the target location.";
					public const string InventoryRemoved = "{0} x {1} {2} has been removed from the target location.";

					public const string LinkTransferOverpicking = "Link quantity cannot be greater than the quantity of a transfer line split.";
					public const string LinkReceiptOverpicking = "Link quantity cannot be greater than the quantity of a receipt line split.";
					public const string LinkUnderpicking = "Link quantity cannot be negative.";
				}
				#endregion
			}

			public sealed class CommandOrReceiptOnlyState : CommandOnlyStateBase<ReceivePutAway>
			{
				public override void MoveToNextState() { }
				public override string Prompt => Msg.UseCommandOrReceiptToContinue;
				public override bool Process(string barcode)
				{
					if (Basis.TryProcessBy<ReceiptState>(barcode))
					{
						Basis.Clear<ReceiptState>();
						Basis.Reset(fullReset: false);
						Basis.SetScanState<ReceiptState>();
						Basis.CurrentMode.FindState<ReceiptState>().Process(barcode);
						return true;
					}
					else
					{
						Basis.Reporter.Error(Msg.OnlyCommandsAndReceiptsAreAllowed);
						return false;
					}
				}

				#region Messages
				[PXLocalizable]
				public new abstract class Msg
				{
					public const string UseCommandOrReceiptToContinue = "Use any command or scan the next receipt to continue.";
					public const string OnlyCommandsAndReceiptsAreAllowed = "Only commands or a receipt can be used to continue.";
				}
				#endregion
			}
			#endregion

			#region Commands
			public sealed class ReleaseTransferCommand : ScanCommand
			{
				public override string Code => "RELEASE";
				public override string ButtonName => "scanReleaseTransfer";
				public override string DisplayName => Msg.DisplayName;
				protected override bool IsEnabled => Basis.DocumentIsEditable && Basis.TransferRefNbr != null && Get<PutAwayMode.Logic>().GetTransfer()?.Released == false;
				protected override bool Process() => Get<Logic>().ReleaseTransfer();

				#region Logic
				public class Logic : ScanExtension
				{
					public virtual bool ReleaseTransfer()
					{
						var transfer = Basis.Get<PutAwayMode.Logic>().TransferEntry.transfer.Current;
						if (transfer?.Released != false)
						{
							Basis.ReportError(Msg.NotPossible);
							return true;
						}

						Basis
						.WaitFor<INRegister>((basis, doc) => ReleaseTransferImpl(doc))
						.WithDescription(Msg.InProgress, Basis.TransferRefNbr)
						.ActualizeDataBy((basis, doc) => INRegister.PK.Find(basis, doc))
						.OnSuccess(x => x.Say(Msg.Success).ChangeStateTo<ReceiptState>())
						.OnFail(x => x.Say(Msg.Fail))
						.BeginAwait(transfer);

						return true;
					}

					private static void ReleaseTransferImpl(INRegister transfer)
					{
						INTransferEntry te = PXGraph.CreateInstance<INTransferEntry>();
						te.transfer.Current = INRegister.PK.Find(te, transfer);
						te.transfer.Cache.SetValueExt<INRegister.hold>(te.transfer.Current, false);
						te.transfer.UpdateCurrent();
						te.release.Press();
					}
				}
				#endregion

				#region Messages
				[PXLocalizable]
				public abstract class Msg
				{
					public const string DisplayName = "Release Transfer";
					public const string NotPossible = "There are no transfers to release.";
					public const string InProgress = "The {0} transfer is being released.";
					public const string Success = "The transfer has been successfully released.";
					public const string Fail = "The transfer release failed.";
				}
				#endregion
			}
			#endregion

			#region Redirect
			public sealed class RedirectFrom<TForeignBasis> : WMSBase.RedirectFrom<TForeignBasis>.SetMode<PutAwayMode>
				where TForeignBasis : PXGraphExtension, IBarcodeDrivenStateMachine
			{
				public override string Code => "PUTAWAY";
				public override string DisplayName => Msg.DisplayName;

				[Obsolete(ReceivePutAway.ObsoleteMsg.ScanMember)]
				public override string ButtonName => "scanModePutAway";

				private string RefNbr { get; set; }

				public override bool IsPossible
				{
					get
					{
						bool wmsReceiving = PXAccess.FeatureInstalled<CS.FeaturesSet.wMSReceiving>();
						var rpSetup = POReceivePutAwaySetup.PK.Find(Basis.Graph, Basis.Graph.Accessinfo.BranchID);
						return wmsReceiving && rpSetup?.ShowPutAwayTab == true;
					}
				}

				protected override bool PrepareRedirect()
				{
					if (Basis is ReceivePutAway rpa && rpa.RefNbr != null)
					{
						if (rpa.FindMode<PutAwayMode>().TryValidate(rpa.Receipt).By<ReceiptState>() is Validation valid && valid.IsError == true)
						{
							rpa.ReportError(valid.Message, valid.MessageArgs);
							return false;
						}
						else
							RefNbr = rpa.RefNbr;
					}

					return true;
				}

				protected override void CompleteRedirect()
				{
					if (Basis is ReceivePutAway rpa && rpa.CurrentMode.Code != ReturnMode.Value && this.RefNbr != null)
						if (rpa.TryProcessBy(ReceivePutAway.ReceiptState.Value, RefNbr, StateSubstitutionRule.KeepAll & ~StateSubstitutionRule.KeepPositiveReports))
						{
							rpa.SetScanState(rpa.CurrentMode.DefaultState.Code);
							RefNbr = null;
						}
				}

				#region Messages
				[PXLocalizable]
				public abstract class Msg
				{
					public const string DisplayName = "Put Away";
				}
				#endregion
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public new abstract class Msg : ScanMode.Msg
			{
				public const string Description = "Put Away";
				public const string Completed = "The {0} receipt is put away.";
			}
			#endregion

			#region Attached Fields
			[PXUIField(Visible = false)]
			public class ShowPutAway : FieldAttached.To<ScanHeader>.AsBool.Named<ShowPutAway>
			{
				public override bool? GetValue(ScanHeader row) => Base.WMS.Setup.Current.ShowPutAwayTab == true && row.Mode == PutAwayMode.Value;
			}

			public class ToLocationID : FieldAttached.To<POReceiptLineSplit>.AsInteger
			{
				public override int? GetValue(POReceiptLineSplit Row) => null;
				protected override bool SuppressValueSetting => true;

				protected override PXFieldState AdjustStateByRow(PXFieldState state, POReceiptLineSplit row)
				{
					PXCache tranCache = Base.WMS.Get<PutAwayMode.Logic>().TransferEntry.transactions.Cache;
					if (row == null)
					{
						state = (PXFieldState)tranCache.GetStateExt<INTran.toLocationID>(null);
					}
					else
					{
						var links =
							SelectFrom<POReceiptSplitToTransferSplitLink>.
							InnerJoin<INTran>.On<POReceiptSplitToTransferSplitLink.FK.TransferLine>.
							Where<POReceiptSplitToTransferSplitLink.FK.ReceiptLineSplit.SameAsCurrent>.
							View.SelectMultiBound(Base, new[] { row })
							.AsEnumerable()
							.Cast<PXResult<POReceiptSplitToTransferSplitLink, INTran>>()
							.ToArray();

						if (links.Length == 0)
						{
							state = (PXFieldState)tranCache.GetStateExt<INTran.toLocationID>(null);
							state.Value = "";
						}
						else if (links.Length == 1 || links.GroupBy(l => l.GetItem<INTran>().ToLocationID).Count() == 1)
						{
							INTran firstTran = links[0].GetItem<INTran>();
							var location = INLocation.PK.Find(Base, firstTran.ToLocationID);

							state = (PXStringState)tranCache.GetStateExt<INTran.toLocationID>(firstTran);
							state.Value = Base.IsMobile ? location?.Descr : location?.LocationCD;
						}
						else
						{
							state = (PXFieldState)tranCache.GetStateExt<INTran.toLocationID>(null);
							state.Value = Base.WMS.Localize("<SPLIT>");
						}
					}

					state = PXFieldState.CreateInstance(
						value: state.Value,
						dataType: typeof(string),
						isKey: false,
						nullable: null,
						required: null,
						precision: null,
						length: null,
						defaultValue: null,
						fieldName: nameof(INTran.toLocationID),
						descriptionName: null,
						displayName: state.DisplayName,
						error: null,
						errorLevel: PXErrorLevel.Undefined,
						enabled: false,
						visible: Base.WMS.Header.Mode == PutAwayMode.Value,
						readOnly: true,
						visibility: PXUIVisibility.Visible,
						viewName: null,
						fieldList: null,
						headerList: null);
					return state;
				}
			}
			#endregion
		}
	}
}