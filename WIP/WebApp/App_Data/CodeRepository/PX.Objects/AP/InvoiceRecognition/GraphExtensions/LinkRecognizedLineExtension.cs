using PX.Common;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;
using PX.Objects.TX;
using PX.Objects.PO;
using PX.Objects.CR;
using PX.Objects.IN;
using PX.Objects.PO.DAC.Unbound;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Objects.AP.InvoiceRecognition.DAC;
using PX.CloudServices.DAC;
using static PX.Objects.PO.LinkLineFilter;

namespace PX.Objects.AP.InvoiceRecognition
{
	[Serializable]
	public class LinkRecognizedLineExtension : PXGraphExtension<APInvoiceRecognitionEntry>
	{
		internal static string[] APTranPOFields =
		{
			nameof(APRecognizedTran.POAccrualType),
			nameof(APRecognizedTran.POAccrualRefNoteID),
			nameof(APRecognizedTran.POAccrualLineNbr),
			nameof(APRecognizedTran.ReceiptType),
			nameof(APRecognizedTran.ReceiptNbr),
			nameof(APRecognizedTran.ReceiptLineNbr),
			nameof(APRecognizedTran.SubItemID),
			nameof(APRecognizedTran.POOrderType),
			nameof(APRecognizedTran.PONbr),
			nameof(APRecognizedTran.POLineNbr),
			nameof(APRecognizedTran.BranchID),
			nameof(APRecognizedTran.LineType),
			nameof(APRecognizedTran.AccountID),
			nameof(APRecognizedTran.SubID),
			nameof(APRecognizedTran.SiteID)
		};

		#region Data Members

		[PXCopyPasteHiddenView]
		public PXSelectJoin<POLineS,
			LeftJoin<POOrder, On<POLineS.orderNbr, Equal<POOrder.orderNbr>, And<POLineS.orderType, Equal<POOrder.orderType>>>>,
			Where<POLineS.pOAccrualType, Equal<POAccrualType.order>,
				And<POLineS.orderNbr, Equal<Required<POLineS.orderNbr>>,
				And<POLineS.orderType, Equal<Required<POLineS.orderType>>,
				And<POLineS.lineNbr, Equal<Required<POLineS.lineNbr>>>>>>> POLineLink;

		[PXCopyPasteHiddenView]
		public PXSelectJoin<
			POReceiptLineS,
				LeftJoin<POReceipt,
					On<POReceiptLineS.receiptNbr, Equal<POReceipt.receiptNbr>,
					And<POReceiptLineS.receiptType, Equal<POReceipt.receiptType>>>>,
			Where<POReceiptLineS.receiptNbr, Equal<Required<LinkLineReceipt.receiptNbr>>,
				And<POReceiptLineS.lineNbr, Equal<Required<LinkLineReceipt.receiptLineNbr>>>>>
			ReceipLineLinked;

		[PXCopyPasteHiddenView]
		public PXSelectJoin<
			POReceiptLineS,
				LeftJoin<POReceipt,
					On<POReceiptLineS.receiptNbr, Equal<POReceipt.receiptNbr>,
					And<POReceiptLineS.receiptType, Equal<POReceipt.receiptType>>>>>
			linkLineReceiptTran;

		[PXCopyPasteHiddenView]
		public PXSelectJoin<
			POLineS,
				LeftJoin<POOrder,
					On<POLineS.orderNbr, Equal<POOrder.orderNbr>,
						And<POLineS.orderType, Equal<POOrder.orderType>>>>>
			linkLineOrderTran;

		public PXFilter<LinkLineFilter> linkLineFilter;

		#endregion

		#region Cache attached
		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDBString(15, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Order Nbr.")]
		[PXSelector(typeof(Search5<POOrderRS.orderNbr,
			LeftJoin<LinkLineReceipt,
				On<POOrderRS.orderNbr, Equal<LinkLineReceipt.orderNbr>,
					And<POOrderRS.orderType, Equal<LinkLineReceipt.orderType>,
					And<Current<LinkLineFilter.selectedMode>, Equal<LinkLineFilter.selectedMode.receipt>>>>,
			LeftJoin<LinkLineOrder,
				On<POOrderRS.orderNbr, Equal<LinkLineOrder.orderNbr>,
					And<POOrderRS.orderType, Equal<LinkLineOrder.orderType>,
					And<Current<LinkLineFilter.selectedMode>, Equal<LinkLineFilter.selectedMode.order>>>>>>,
			Where2<
				Where<
					LinkLineReceipt.orderNbr, IsNotNull,
					Or<LinkLineOrder.orderType, IsNotNull>>,
				And<Where<
						POOrderRS.vendorID, Equal<Current<APRecognizedInvoice.vendorID>>,
						And<POOrderRS.vendorLocationID, Equal<Current<APRecognizedInvoice.vendorLocationID>>,
						And2<Not<FeatureInstalled<FeaturesSet.vendorRelations>>,
					Or2<FeatureInstalled<FeaturesSet.vendorRelations>,
						And<POOrderRS.vendorID, Equal<Current<APRecognizedInvoice.suppliedByVendorID>>,
						And<POOrderRS.vendorLocationID, Equal<Current<APRecognizedInvoice.suppliedByVendorLocationID>>,
						And<POOrderRS.payToVendorID, Equal<Current<APRecognizedInvoice.vendorID>>>>>>>>>>>,
			Aggregate<
				GroupBy<POOrderRS.orderNbr,
				GroupBy<POOrderRS.orderType>>>>))]
		protected virtual void LinkLineFilter_pOOrderNbr_CacheAttached(PXCache sender)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXString(1)]
		[PXUIField(DisplayName = "Selected Mode")]
		[PXStringList(new[] { selectedMode.Order, selectedMode.Receipt }, new[] { AP.Messages.POOrderMode, AP.Messages.POReceiptMode })]
		protected virtual void LinkLineFilter_SelectedMode_CacheAttached(PXCache sender)
		{
		}
		#endregion

		#region Initialize

		public override void Initialize()
		{
			base.Initialize();

			linkLineReceiptTran.Cache.AllowDelete = false;
			linkLineReceiptTran.Cache.AllowInsert = false;
			linkLineOrderTran.Cache.AllowDelete = false;
			linkLineOrderTran.Cache.AllowInsert = false;

			PXUIFieldAttribute.SetEnabled(linkLineReceiptTran.Cache, null, false);
			PXUIFieldAttribute.SetEnabled<POReceiptLineS.selected>(linkLineReceiptTran.Cache, null, true);
			PXUIFieldAttribute.SetEnabled(linkLineOrderTran.Cache, null, false);
			PXUIFieldAttribute.SetEnabled<LinkLineOrder.selected>(linkLineOrderTran.Cache, null, true);
		}

		#endregion

		#region Actions

		public PXAction<APRecognizedInvoice> linkLine;

		[PXUIField(DisplayName = AP.Messages.LinkLine, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Select, FieldClass = "DISTR", Visible = true)]
		[PXLookupButton]
		[APMigrationModeDependentActionRestriction(
					restrictInMigrationMode: true,
					restrictForRegularDocumentInMigrationMode: true,
					restrictForUnreleasedMigratedDocumentInNormalMode: true)]
		public virtual IEnumerable LinkLine(PXAdapter adapter)
		{
			if (Base.Transactions.Current == null || Base.Transactions.Current.TranType != APDocType.Invoice)
				return adapter.Get();

			if (Base.Transactions.Current.InventoryID == null)
			{
				PXUIFieldAttribute.SetWarning<APRecognizedTran.inventoryID>(Base.Transactions.Cache, Base.Transactions.Current, PXMessages.LocalizeNoPrefix(Messages.CannotLinkEmptyInventoryID));
			}
			else
			{
				Base.Transactions.Cache.ClearQueryCache(); // for correct PO link detection
				WebDialogResult res;
				if ((res = linkLineFilter.AskExt(
						(graph, view) =>
						{
							linkLineFilter.Cache.SetValueExt<LinkLineFilter.inventoryID>(linkLineFilter.Current,
								Base.Transactions.Current.InventoryID);
							linkLineFilter.Current.UOM = Base.Transactions.Current?.UOM;
							linkLineFilter.Current.POOrderNbr = null;
							linkLineFilter.Current.SiteID = null;

							Location location = Base.VendorLocation.Select();
							if (location != null)
								linkLineFilter.Cache.SetValueExt<LinkLineFilter.selectedMode>(linkLineFilter.Current,
									location.VAllowAPBillBeforeReceipt == true ? selectedMode.Order : selectedMode.Receipt);

							APRecognizedTran apTran = Base.Transactions.Current;

							linkLineOrderTran.Cache.Clear(); //TODO: closing modal window can't be handled
							linkLineReceiptTran.Cache.Clear(); //TODO: closing modal window can't be handled
							linkLineOrderTran.View.Clear(); //TODO: closing modal window can't be handled
							linkLineReceiptTran.View.Clear(); //TODO: closing modal window can't be handled
							linkLineOrderTran.Cache.ClearQueryCache(); //TODO: closing modal window can't be handled
							linkLineReceiptTran.Cache.ClearQueryCache(); //TODO: closing modal window can't be handled
						}, true
					)) != WebDialogResult.None)
				{
					if (res == WebDialogResult.Yes &&
						(linkLineReceiptTran.Cache.Updated.Count() > 0 || linkLineOrderTran.Cache.Updated.Count() > 0))
					{
						APRecognizedTran tranToUpdate = (APRecognizedTran)Base.Transactions.Cache.CreateCopy(Base.Transactions.Current);
						tranToUpdate = ClearAPTranReferences(tranToUpdate);

						if (linkLineFilter.Current.SelectedMode == LinkLineFilter.selectedMode.Receipt)
						{
							foreach (POReceiptLineS receipt in linkLineReceiptTran.Cache.Updated)
							{
								if (receipt.Selected == true)
								{
									LinkToReceipt(tranToUpdate, receipt);
									break;
								}
							}
						}
						if (linkLineFilter.Current.SelectedMode == LinkLineFilter.selectedMode.Order)
						{
							foreach (POLineS order in linkLineOrderTran.Cache.Updated)
							{
								if (order.Selected == true)
								{
									LinkToOrder(tranToUpdate, order);
									break;
								}
							}
						}
						Base.Transactions.Cache.Update(tranToUpdate);
						if (string.IsNullOrEmpty(tranToUpdate.ReceiptNbr) && string.IsNullOrEmpty(tranToUpdate.PONbr))
						{
							Base.Transactions.Cache.SetDefaultExt<APRecognizedTran.accountID>(tranToUpdate);
							Base.Transactions.Cache.SetDefaultExt<APRecognizedTran.subID>(tranToUpdate);
						}
					}
				}

			}
			return adapter.Get();
		}

		private APRecognizedTran ClearAPTranReferences(APRecognizedTran apTran)
		{
			apTran.ReceiptType = null;
			apTran.ReceiptNbr = null;
			apTran.ReceiptLineNbr = null;
			apTran.POOrderType = null;
			apTran.PONbr = null;
			apTran.POLineNbr = null;
			apTran.POAccrualType = null;
			apTran.POAccrualRefNoteID = null;
			apTran.POAccrualLineNbr = null;
			apTran.AccountID = null;
			apTran.SubID = null;
			apTran.SiteID = null;
			apTran.RecognizedPONumber = null;
			apTran.POLinkStatus = APPOLinkStatus.NotLinked;
			return apTran;
		}

		private static void LinkToReceipt(APRecognizedTran apTran, POReceiptLineS receipt)
		{
			receipt.SetReferenceKeyTo(apTran);
			apTran.RecognizedPONumber = apTran.PONbr;
			apTran.BranchID = receipt.BranchID;
			apTran.LineType = receipt.LineType;
			apTran.AccountID = receipt.POAccrualAcctID ?? receipt.ExpenseAcctID;
			apTran.SubID = receipt.POAccrualSubID ?? receipt.ExpenseSubID;
			apTran.SiteID = receipt.SiteID;
			apTran.POLinkStatus = APPOLinkStatus.Linked;
		}

		private static void LinkToOrder(APRecognizedTran apTran, POLineS order)
		{
			order.SetReferenceKeyTo(apTran);
			apTran.RecognizedPONumber = apTran.PONbr;
			apTran.BranchID = order.BranchID;
			apTran.LineType = order.LineType;
			apTran.AccountID = order.POAccrualAcctID ?? order.ExpenseAcctID;
			apTran.SubID = order.POAccrualSubID ?? order.ExpenseSubID;
			apTran.SiteID = order.SiteID;
			apTran.POLinkStatus = APPOLinkStatus.Linked;
		}

		#endregion

		#region Selecting override

		public virtual IEnumerable LinkLineReceiptTran()
		{
			return GetAvailableReceiptLines(Base.Transactions.Current, linkLineFilter.Current?.POOrderNbr, linkLineFilter.Current?.InventoryID, linkLineFilter.Current?.UOM);
		}

		public virtual List<PXResult<POReceiptLineS, POReceipt>> GetAvailableReceiptLines(APRecognizedTran currentRecognizedTran, string pONbr, int? inventoryID, string uOM)
		{
			List<PXResult<POReceiptLineS, POReceipt>> receiptLines = new List<PXResult<POReceiptLineS, POReceipt>>();
			if (currentRecognizedTran == null)
				return receiptLines;
			//yield break;

			var comparer = new POAccrualComparer();
			HashSet<APRecognizedTran> usedSourceLine = new HashSet<APRecognizedTran>(comparer);
			HashSet<APRecognizedTran> unusedSourceLine = new HashSet<APRecognizedTran>(comparer);
			foreach (APRecognizedTran aPTran in Base.Transactions.Cache.Inserted)
			{
				if (currentRecognizedTran.InventoryID == aPTran.InventoryID
					&& currentRecognizedTran.UOM == aPTran.UOM)
				{
					usedSourceLine.Add(aPTran);
				}
			}

			foreach (APRecognizedTran aPTran in Base.Transactions.Cache.Deleted)
			{
				if (currentRecognizedTran.InventoryID == aPTran.InventoryID
					&& currentRecognizedTran.UOM == aPTran.UOM
					&& Base.Transactions.Cache.GetStatus(aPTran) != PXEntryStatus.InsertedDeleted)
				{
					if (!usedSourceLine.Remove(aPTran))
					{
						unusedSourceLine.Add(aPTran);
					}
				}
			}

			foreach (APRecognizedTran aPTran in Base.Transactions.Cache.Updated)
			{
				if (currentRecognizedTran.InventoryID == aPTran.InventoryID && currentRecognizedTran.UOM == aPTran.UOM)
				{
					APRecognizedTran originAPTran = new APRecognizedTran
					{
						POAccrualType = (string)Base.Transactions.Cache.GetValueOriginal<APRecognizedTran.pOAccrualType>(aPTran),
						POAccrualRefNoteID = (Guid?)Base.Transactions.Cache.GetValueOriginal<APRecognizedTran.pOAccrualRefNoteID>(aPTran),
						POAccrualLineNbr = (int?)Base.Transactions.Cache.GetValueOriginal<APRecognizedTran.pOAccrualLineNbr>(aPTran),
						POOrderType = (string)Base.Transactions.Cache.GetValueOriginal<APRecognizedTran.pOOrderType>(aPTran),
						PONbr = (string)Base.Transactions.Cache.GetValueOriginal<APRecognizedTran.pONbr>(aPTran),
						POLineNbr = (int?)Base.Transactions.Cache.GetValueOriginal<APRecognizedTran.pOLineNbr>(aPTran),
						ReceiptNbr = (string)Base.Transactions.Cache.GetValueOriginal<APRecognizedTran.receiptNbr>(aPTran),
						ReceiptType = (string)Base.Transactions.Cache.GetValueOriginal<APRecognizedTran.receiptType>(aPTran),
						ReceiptLineNbr = (int?)Base.Transactions.Cache.GetValueOriginal<APRecognizedTran.receiptLineNbr>(aPTran)
					};

					if (!usedSourceLine.Remove(originAPTran))
					{
						unusedSourceLine.Add(originAPTran);
					}

					if (!unusedSourceLine.Remove(aPTran))
					{
						usedSourceLine.Add(aPTran);
					}
				}
			}

			unusedSourceLine.Add(currentRecognizedTran);

			foreach (LinkLineReceipt item in GetLinkLineReceipts(pONbr, inventoryID, uOM))
			{
				APRecognizedTran aPTran = new APRecognizedTran
				{
					POAccrualType = item.POAccrualType,
					POAccrualRefNoteID = item.POAccrualRefNoteID,
					POAccrualLineNbr = item.POAccrualLineNbr,
					POOrderType = item.OrderType,
					PONbr = item.OrderNbr,
					POLineNbr = item.OrderLineNbr,
					ReceiptType = item.ReceiptType,
					ReceiptNbr = item.ReceiptNbr,
					ReceiptLineNbr = item.ReceiptLineNbr
				};

				if (!usedSourceLine.Contains(aPTran))
				{
					var res = (PXResult<POReceiptLineS, POReceipt>)ReceipLineLinked.Select(item.ReceiptNbr, item.ReceiptLineNbr);
					if (linkLineReceiptTran.Cache.GetStatus((POReceiptLineS)res) != PXEntryStatus.Updated
						&& ((POReceiptLineS)res).CompareReferenceKey(currentRecognizedTran))
					{
						linkLineReceiptTran.Cache.SetValue<POReceiptLineS.selected>((POReceiptLineS)res, true);
						linkLineReceiptTran.Cache.SetStatus((POReceiptLineS)res, PXEntryStatus.Updated);
					}
					receiptLines.Add(res);
				}
			}

			foreach (APRecognizedTran item in unusedSourceLine.Where(t => t.POAccrualType != null))
			{
				foreach (PXResult<POReceiptLineS, POReceipt> res in PXSelectJoin<POReceiptLineS,
					LeftJoin<POReceipt,
						On<POReceiptLineS.receiptNbr, Equal<POReceipt.receiptNbr>, And<POReceiptLineS.receiptType, Equal<POReceipt.receiptType>>>>,
					Where<POReceiptLineS.pOAccrualType, Equal<Required<LinkLineReceipt.pOAccrualType>>,
						And<POReceiptLineS.pOAccrualRefNoteID, Equal<Required<LinkLineReceipt.pOAccrualRefNoteID>>,
						And<POReceiptLineS.pOAccrualLineNbr, Equal<Required<LinkLineReceipt.pOAccrualLineNbr>>>>>>
					.Select(Base, item.POAccrualType, item.POAccrualRefNoteID, item.POAccrualLineNbr))
				{
					if (currentRecognizedTran.InventoryID == ((POReceiptLineS)res).InventoryID)
					{
						if (linkLineReceiptTran.Cache.GetStatus((POReceiptLineS)res) != PXEntryStatus.Updated
							&& ((POReceiptLineS)res).CompareReferenceKey(currentRecognizedTran))
						{
							linkLineReceiptTran.Cache.SetValue<POReceiptLineS.selected>((POReceiptLineS)res, true);
							linkLineReceiptTran.Cache.SetStatus((POReceiptLineS)res, PXEntryStatus.Updated);
						}
						receiptLines.Add(res);
					}
				}
			}

			return receiptLines;
		}

		private PXResultset<LinkLineReceipt> GetLinkLineReceipts(string pONbr, int? inventoryID, string uOM)
		{
			PXSelectBase<LinkLineReceipt> cmd = new PXSelect<LinkLineReceipt,
				Where2<
					Where<Required<LinkLineFilter.pOOrderNbr>, Equal<LinkLineReceipt.orderNbr>,
						Or<Required<LinkLineFilter.pOOrderNbr>, IsNull>>,
					And<LinkLineReceipt.inventoryID, Equal<Required<APRecognizedTran.inventoryID>>,
					And2<Where<LinkLineReceipt.uOM, Equal<Required<APRecognizedTran.uOM>>,
						Or<Required<APRecognizedTran.uOM>, IsNull>>,
					And<LinkLineReceipt.receiptType, Equal<POReceiptType.poreceipt>>>>>>(Base);

			if (Base.APSetup.Current.RequireSingleProjectPerDocument == true)
			{
				cmd.WhereAnd<Where<LinkLineReceipt.projectID, Equal<Current<APRecognizedInvoice.projectID>>>>();
			}

			if (PXAccess.FeatureInstalled<FeaturesSet.vendorRelations>())
			{
				cmd.WhereAnd<Where<LinkLineReceipt.vendorID, Equal<Current<APRecognizedInvoice.suppliedByVendorID>>,
					And<LinkLineReceipt.vendorLocationID, Equal<Current<APRecognizedInvoice.suppliedByVendorLocationID>>,
					And<Where<LinkLineReceipt.payToVendorID, IsNull, Or<LinkLineReceipt.payToVendorID, Equal<Current<APRecognizedInvoice.vendorID>>>>>>>>();
			}
			else
			{
				cmd.WhereAnd<Where<LinkLineReceipt.vendorID, Equal<Current<APRecognizedInvoice.vendorID>>,
					And<LinkLineReceipt.vendorLocationID, Equal<Current<APRecognizedInvoice.vendorLocationID>>>>>();
			}

			return cmd.Select(pONbr, pONbr, inventoryID, uOM, uOM);
		}

		public virtual IEnumerable LinkLineOrderTran()
		{
			return GetAvailableOrderLines(Base.Transactions.Current, linkLineFilter.Current?.POOrderNbr, linkLineFilter.Current?.InventoryID, linkLineFilter.Current?.UOM);
		}

		public virtual List<PXResult<POLineS, POOrder>> GetAvailableOrderLines(APRecognizedTran currentRecognizedTran, string pONbr, int? inventoryID, string uOM)
		{
			List<PXResult<POLineS, POOrder>> orderLines = new List<PXResult<POLineS, POOrder>>();

			if (currentRecognizedTran == null)
				return orderLines;

			var usedPOAccrual = new Lazy<POAccrualSet>(() =>
			{
				var r = new POAccrualSet(
				Base.Transactions.Select().RowCast<APRecognizedTran>(),
				Base.Document.Current?.DocType == APDocType.Prepayment
					? (IEqualityComparer<APTran>)new POLineComparer()
					: new POAccrualComparer());
				r.Remove(currentRecognizedTran);
				return r;
			});

			foreach (LinkLineOrder item in GetLinkLineOrders(pONbr, inventoryID, uOM).RowCast<LinkLineOrder>().AsEnumerable()
				.Where(l => !usedPOAccrual.Value.Contains(l))
				)
			{
				var res = (PXResult<POLineS, POOrder>)POLineLink.Select(item.OrderNbr, item.OrderType, item.OrderLineNbr);
				if (linkLineOrderTran.Cache.GetStatus((POLineS)res) != PXEntryStatus.Updated &&
					((POLineS)res).CompareReferenceKey(currentRecognizedTran))
				{
					linkLineOrderTran.Cache.SetValue<POLineS.selected>((POLineS)res, true);
					linkLineOrderTran.Cache.SetStatus((POLineS)res, PXEntryStatus.Updated);
				}
				orderLines.Add(res);
			}

			return orderLines;
		}

		private PXResultset<LinkLineOrder> GetLinkLineOrders(string pONbr, int? inventoryID, string uOM)
		{
			PXSelectBase<LinkLineOrder> cmd = new PXSelect<LinkLineOrder,
				Where2<
					Where<Required<LinkLineFilter.pOOrderNbr>, Equal<LinkLineOrder.orderNbr>,
						Or<Required<LinkLineFilter.pOOrderNbr>, IsNull>>,
					And<LinkLineOrder.inventoryID, Equal<Required<APRecognizedTran.inventoryID>>,
					And2<Where<LinkLineOrder.uOM, Equal<Required<APRecognizedTran.uOM>>,
						Or<Required<APRecognizedTran.uOM>, IsNull>>,
					And<LinkLineOrder.orderCuryID, Equal<Current<APRecognizedInvoice.curyID>>>>>>>(Base);

			if (Base.APSetup.Current.RequireSingleProjectPerDocument == true)
			{
				cmd.WhereAnd<Where<LinkLineOrder.projectID, Equal<Current<APRecognizedInvoice.projectID>>>>();
			}

			if (PXAccess.FeatureInstalled<FeaturesSet.vendorRelations>())
			{
				cmd.WhereAnd<Where<LinkLineOrder.vendorID, Equal<Current<APRecognizedInvoice.suppliedByVendorID>>,
					And<LinkLineOrder.vendorLocationID, Equal<Current<APRecognizedInvoice.suppliedByVendorLocationID>>,
					And<LinkLineOrder.payToVendorID, Equal<Current<APRecognizedInvoice.vendorID>>>>>>();
			}
			else
			{
				cmd.WhereAnd<Where<LinkLineOrder.vendorID, Equal<Current<APRecognizedInvoice.vendorID>>,
					And<LinkLineOrder.vendorLocationID, Equal<Current<APRecognizedInvoice.vendorLocationID>>>>>();
			}

			return cmd.Select(pONbr, pONbr, inventoryID, uOM, uOM);
		}

		#endregion

		[PXOverride]
		public virtual void AutoLinkAPAndPO(APRecognizedTran tran, string poNumber, Action<APRecognizedTran, string> baseFunction)
		{
			if (tran == null || tran.TranType != APDocType.Invoice)
				return;

			//searching through all the available PO documents might be a very slow operation
			//uncomment this part or introducve new option or parameter in case of performance issues
			/*if (poNumber == null)
			{
				ClearAPTranReferences(tran);
				return;
			}*/

			Location location = Base.VendorLocation.Select();
			if (location != null)
			{
				POReceiptLineS matchingReceiptLine = null;
				POLineS matchingOrderLine = null;
				bool multiplePRLinesFound = false;
				bool multiplePOLinesFound = false;

				if (location.VAllowAPBillBeforeReceipt != true)
				{
					foreach (PXResult<POReceiptLineS, POReceipt> res in GetAvailableReceiptLines(tran, poNumber, tran.InventoryID, tran.UOM))
					{
						if (matchingReceiptLine == null)
							matchingReceiptLine = res;
						else if (matchingReceiptLine.ReceiptType != ((POReceiptLineS)res).ReceiptType ||
								 matchingReceiptLine.ReceiptNbr != ((POReceiptLineS)res).ReceiptNbr ||
								 matchingReceiptLine.LineNbr != ((POReceiptLineS)res).LineNbr)
							multiplePRLinesFound = true;
					}
				}
				else
				{
					foreach (PXResult<POLineS, POOrder> res in GetAvailableOrderLines(tran, poNumber, tran.InventoryID, tran.UOM))
					{
						if (matchingOrderLine == null)
							matchingOrderLine = res;
						else
							multiplePOLinesFound = true;
					}
				}

				if (multiplePRLinesFound || multiplePOLinesFound)
				{
					ClearAPTranReferences(tran);
					Base.Transactions.Cache.SetValueExt<APRecognizedTran.recognizedPONumber>(tran, poNumber);
					if (poNumber != null)
					{
						if (multiplePRLinesFound)
							Base.Transactions.Cache.SetValueExt<APRecognizedTran.pOLinkStatus>(tran, APPOLinkStatus.MultiplePRLinesFound);
						else
							Base.Transactions.Cache.SetValueExt<APRecognizedTran.pOLinkStatus>(tran, APPOLinkStatus.MultiplePOLinesFound);
					}
					else
					{
						Base.Transactions.Cache.SetValueExt<APRecognizedTran.pOLinkStatus>(tran, APPOLinkStatus.NotLinked);
					}
					return;
				}

				if (matchingReceiptLine == null && matchingOrderLine == null)
				{
					ClearAPTranReferences(tran);
					return;
				}

				if (matchingReceiptLine != null || matchingOrderLine != null)
				{
					ClearAPTranReferences(tran);

					if (matchingReceiptLine != null)
					{
						LinkToReceipt(tran, matchingReceiptLine);
					}
					else
					{
						LinkToOrder(tran, matchingOrderLine);
					}

					if (string.IsNullOrEmpty(tran.ReceiptNbr) && string.IsNullOrEmpty(tran.PONbr))
					{
						Base.Transactions.Cache.SetDefaultExt<APRecognizedTran.accountID>(tran);
						Base.Transactions.Cache.SetDefaultExt<APRecognizedTran.subID>(tran);
					}

					return;
				}
			}
			else
			{
				ClearAPTranReferences(tran);
				return;
			}
		}


		#region Event handlers


		protected virtual void _(Events.RowSelected<APRecognizedInvoice> e)
		{
			if (e.Row == null)
			{
				return;
			}

			linkLine.SetEnabled(e.Row.DocType == APDocType.Invoice && e.Row.VendorID != null &&
				e.Row.RecognitionStatus.IsNotIn(RecognizedRecordStatusListAttribute.Processed, RecognizedRecordStatusListAttribute.PendingRecognition));
		}

		protected virtual void _(Events.RowUpdated<APRecognizedInvoice> e)
		{
			if (e.Row == null || e.OldRow == null)
			{
				return;
			}

			if (!Base.Document.Cache.ObjectsEqual<APRecognizedInvoice.docType>(e.Row, e.OldRow) && e.Row.DocType != APDocType.Invoice)
			{
				foreach (APRecognizedTran tran in Base.Transactions.Select())
				{
					ClearAPTranReferences(tran);
				}
			}
		}

		protected virtual void _(Events.RowSelected<APRecognizedTran> e)
		{
			if (e.Row == null || Base.Document.Current == null || Base.Document.Current.RecognitionStatus == RecognizedRecordStatusListAttribute.Processed)
			{
				return;
			}

			bool showWarning = false;
			if (Base.Document.Current.DocType == APDocType.Invoice && e.Row.InventoryID != null)
			{
				InventoryItem item = PXSelect<InventoryItem,
						Where<InventoryItem.inventoryID, Equal<Required<InventoryItem.inventoryID>>>>.Select(Base, e.Row.InventoryID);
				showWarning = item?.StkItem == true;
			}

			if (showWarning)
			{
				switch (e.Row.POLinkStatus)
				{
					case APPOLinkStatus.NotLinked:
						PXUIFieldAttribute.SetWarning<APRecognizedTran.recognizedPONumber>(e.Cache, e.Row, PXMessages.LocalizeNoPrefix(Messages.NotLinked));
						break;
					case APPOLinkStatus.MultiplePRLinesFound:
						PXUIFieldAttribute.SetWarning<APRecognizedTran.recognizedPONumber>(e.Cache, e.Row, PXMessages.LocalizeNoPrefix(Messages.MultiplePRLinesFound));
						break;
					case APPOLinkStatus.MultiplePOLinesFound:
						PXUIFieldAttribute.SetWarning<APRecognizedTran.recognizedPONumber>(e.Cache, e.Row, PXMessages.LocalizeNoPrefix(Messages.MultiplePOLinesFound));
						break;
					default:
						PXUIFieldAttribute.SetWarning<APRecognizedTran.recognizedPONumber>(e.Cache, e.Row, null);
						break;
				}
			}
			else
			{
				PXUIFieldAttribute.SetWarning<APRecognizedTran.recognizedPONumber>(e.Cache, e.Row, null);
			}
		}

		protected virtual void _(Events.RowSelected<LinkLineFilter> e)
		{
			if (e.Row != null)
			{
				PXCache orderReceiptCache = linkLineReceiptTran.Cache;

				linkLineReceiptTran.View.AllowSelect = e.Row.SelectedMode == LinkLineFilter.selectedMode.Receipt;
				linkLineOrderTran.View.AllowSelect = e.Row.SelectedMode == LinkLineFilter.selectedMode.Order;
			}

		}

		protected virtual void _(Events.FieldUpdated<POLineS.selected> e)
		{
			POLineS row = (POLineS)e.Row;
			if (row != null && !(bool)e.OldValue && (bool)row.Selected)
			{
				foreach (POLineS item in e.Cache.Updated)
				{
					if (item.Selected == true && item != row)
					{
						e.Cache.SetValue<POLineS.selected>(item, false);
						linkLineOrderTran.View.RequestRefresh();
					}

				}

				foreach (POReceiptLineS item in linkLineReceiptTran.Cache.Updated)
				{
					if (item.Selected == true)
					{
						linkLineReceiptTran.Cache.SetValue<POReceiptLineS.selected>(item, false);
						linkLineReceiptTran.View.RequestRefresh();
					}
				}

			}
		}

		protected virtual void _(Events.FieldUpdated<POReceiptLineS.selected> e)
		{
			POReceiptLineS row = (POReceiptLineS)e.Row;
			if (row != null && !(bool)e.OldValue && (bool)row.Selected)
			{
				foreach (POReceiptLineS item in linkLineReceiptTran.Cache.Updated)
				{
					if (item.Selected == true && item != row)
					{
						e.Cache.SetValue<POReceiptLineS.selected>(item, false);
						linkLineReceiptTran.View.RequestRefresh();
					}
				}

				foreach (POLineS item in linkLineOrderTran.Cache.Updated)
				{
					if (item.Selected == true)
					{
						linkLineOrderTran.Cache.SetValue<POLineS.selected>(item, false);
						linkLineOrderTran.View.RequestRefresh();
					}
				}

			}
		}
		#endregion

	}
}
