using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CM;
using PX.Objects.Common;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.PO;
using PX.Objects.PO.GraphExtensions.POOrderEntryExt;
using PX.TM;

namespace PX.Objects.SO
{
	[TableAndChartDashboardType]
	public class SOCreateIntercompanySalesOrders : PXGraph<SOCreateIntercompanySalesOrders>
	{
		public PXCancel<POForSalesOrderFilter> Cancel;
		public PXFilter<POForSalesOrderFilter> Filter;
		public PXSetup<SOSetup> SOSetup;
		public PXSetup<INSetup> INSetup;

		[PXFilterable]
		[PXVirtualDAC]
		public PXFilteredProcessingOrderBy<POForSalesOrderDocument, POForSalesOrderFilter, OrderBy<Asc<POForSalesOrderDocument.docNbr>>> Documents;

		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.interBranch>() && PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();
		}

		public SOCreateIntercompanySalesOrders()
		{
			SOSetup sosetup = SOSetup.Current;
			INSetup insetup = INSetup.Current;

			Documents.SetSelected<POForSalesOrderDocument.selected>();
			Documents.SetProcessCaption(Messages.Process);
			Documents.SetProcessAllCaption(Messages.ProcessAll);

			PXUIFieldAttribute.SetEnabled<POForSalesOrderDocument.excluded>(Documents.Cache, null, PXLongOperation.GetStatus(this.UID) == PXLongRunStatus.NotExists);
		}

		public override void InitCacheMapping(Dictionary<Type, Type> map)
		{
			base.InitCacheMapping(map);

			this.Caches.AddCacheMapping(typeof(BAccount), typeof(BAccount));
		}

		protected virtual IEnumerable documents()
		{
			List<POForSalesOrderDocument> list = new List<POForSalesOrderDocument>();

			if (Filter.Current != null)
			{
				using (new PXReadBranchRestrictedScope())
				{
					if (Filter.Current.PODocType == POCrossCompanyDocType.PurchaseOrders)
					{
						var orders =
							PXSelectJoin<POOrder,
								InnerJoin<Branch, On<Branch.branchID, Equal<POOrder.branchID>>,
								InnerJoin<BAccount, On<BAccount.bAccountID, Equal<Branch.bAccountID>,
									And<BAccount.isBranch, Equal<True>>>,
								LeftJoin<SOOrder, On<SOOrder.FK.IntercompanyPOOrder>>>>,
								Where2<Where<POOrder.orderDate, LessEqual<Current<POForSalesOrderFilter.docDate>>,
										Or<Current<POForSalesOrderFilter.docDate>, IsNull>>,
									And2<Where<POOrder.vendorID, Equal<Current<POForSalesOrderFilter.sellingCompany>>,
										Or<Current<POForSalesOrderFilter.sellingCompany>, IsNull>>,
									And2<Where<BAccount.bAccountID, Equal<Current<POForSalesOrderFilter.purchasingCompany>>,
										Or<Current<POForSalesOrderFilter.purchasingCompany>, IsNull>>,
									And<POOrder.isIntercompany, Equal<True>,
									And<POOrder.status, Equal<POOrderStatus.open>,
									And<POOrder.excludeFromIntercompanyProc, Equal<False>,
									And<SOOrder.orderNbr, IsNull>>>>>>>>.Select(this);

						foreach (PXResult<POOrder, Branch, BAccount, SOOrder> document in orders)
						{
							POOrder order = (POOrder)document;

							POForSalesOrderDocument doc = new POForSalesOrderDocument
							{
								DocType = order.OrderType,
								DocNbr = order.OrderNbr,
								VendorID = order.VendorID,
								BranchID = order.BranchID,
								CuryID = order.CuryID,
								CuryDocTotal = order.CuryOrderTotal,
								CuryDiscTot = order.CuryDiscTot,
								CuryTaxTotal = order.CuryTaxTotal,
								ExpectedDate = order.ExpectedDate,
								DocDate = order.OrderDate,
								EmployeeID = order.EmployeeID,
								DocDesc = order.OrderDesc
							};

							POForSalesOrderDocument cachedDoc = Documents.Locate(doc);
							if (cachedDoc != null)
							{
								doc.Selected = cachedDoc.Selected;
								doc.Excluded = cachedDoc.Excluded;
							}

							list.Add(Documents.Update(doc));
						}
					}
					else if (Filter.Current.PODocType == POCrossCompanyDocType.PurchaseReturns)
					{
						var receipts =
							PXSelectJoin<POReceipt,
									InnerJoin<Branch, On<Branch.branchID, Equal<POReceipt.branchID>>,
									InnerJoin<BAccount, On<BAccount.bAccountID, Equal<Branch.bAccountID>,
											And<BAccount.isBranch, Equal<True>>>,
									LeftJoin<SOOrder, On<SOOrder.FK.IntercompanyPOReturn>>>>,
									Where2<Where<POReceipt.receiptDate, LessEqual<Current<POForSalesOrderFilter.docDate>>,
											Or<Current<POForSalesOrderFilter.docDate>, IsNull>>,
										And2<Where<POReceipt.vendorID, Equal<Current<POForSalesOrderFilter.sellingCompany>>,
											Or<Current<POForSalesOrderFilter.sellingCompany>, IsNull>>,
										And2<Where<BAccount.bAccountID, Equal<Current<POForSalesOrderFilter.purchasingCompany>>,
											Or<Current<POForSalesOrderFilter.purchasingCompany>, IsNull>>,
										And<POReceipt.isIntercompany, Equal<True>,
										And<POReceipt.receiptType, Equal<POReceiptType.poreturn>,
										And<POReceipt.released, Equal<True>,
										And<POReceipt.excludeFromIntercompanyProc, Equal<False>,
										And<SOOrder.orderNbr, IsNull>>>>>>>>>.Select(this);

						foreach (PXResult<POReceipt, Branch, BAccount, SOOrder> document in receipts)
						{
							POReceipt poReturn = (POReceipt)document;

							POForSalesOrderDocument doc = new POForSalesOrderDocument
							{
								DocType = poReturn.ReceiptType,
								DocNbr = poReturn.ReceiptNbr,
								VendorID = poReturn.VendorID,
								BranchID = poReturn.BranchID,
								CuryID = poReturn.CuryID,
								CuryDocTotal = poReturn.CuryOrderTotal,
								DocQty = poReturn.OrderQty,
								DocDate = poReturn.ReceiptDate,
								OwnerID = poReturn.OwnerID,
								WorkgroupID = poReturn.WorkgroupID,
								FinPeriodID = poReturn.FinPeriodID
							};

							POForSalesOrderDocument cachedDoc = Documents.Locate(doc);
							if (cachedDoc != null)
							{
								doc.Selected = cachedDoc.Selected;
								doc.Excluded = cachedDoc.Excluded;
							}

							list.Add(Documents.Update(doc));
						}
					}
				}
			}

			return list;
		}

		#region Event Handlers
		public virtual void _(Events.RowSelected<POForSalesOrderFilter> e)
		{
			POForSalesOrderFilter filter = e.Row;
			Documents.SetProcessDelegate(itemsList => GenerateSalesOrder(itemsList, filter));

			if (filter == null) return;

			bool isPOOrders = filter.PODocType == POCrossCompanyDocType.PurchaseOrders;

			PXUIFieldAttribute.SetVisible<POForSalesOrderFilter.copyProjectDetails>(Filter.Cache, filter, isPOOrders && PXAccess.FeatureInstalled<FeaturesSet.projectAccounting>());
			PXUIFieldAttribute.SetVisible<POForSalesOrderDocument.expectedDate>(Documents.Cache, null, isPOOrders);
			PXUIFieldAttribute.SetVisible<POForSalesOrderDocument.curyID>(Documents.Cache, null, isPOOrders);
			PXUIFieldAttribute.SetVisible<POForSalesOrderDocument.docDesc>(Documents.Cache, null, isPOOrders);

			PXUIFieldAttribute.SetVisible<POForSalesOrderDocument.docQty>(Documents.Cache, null, !isPOOrders);
		}

		public virtual void _(Events.RowUpdated<POForSalesOrderFilter> e)
		{
			POForSalesOrderFilter row = e.Row;
			POForSalesOrderFilter oldRow = e.OldRow;
			if (row != null && oldRow != null && !Filter.Cache.ObjectsEqual<POForSalesOrderFilter.docDate, POForSalesOrderFilter.pODocType, POForSalesOrderFilter.purchasingCompany, POForSalesOrderFilter.sellingCompany>(row, oldRow))
			{
				Documents.Cache.Clear();
			}
		}

		public virtual void _(Events.FieldUpdated<POForSalesOrderFilter, POForSalesOrderFilter.pODocType> e)
		{
			POForSalesOrderFilter filter = e.Row;
			if (filter == null) return;

			Filter.Cache.SetDefaultExt<POForSalesOrderFilter.intercompanyOrderType>(filter);

			if (filter.PODocType == POCrossCompanyDocType.PurchaseReturns)
				Filter.Cache.SetDefaultExt<POForSalesOrderFilter.copyProjectDetails>(filter);
		}

		public virtual void _(Events.FieldDefaulting<POForSalesOrderFilter, POForSalesOrderFilter.sellingCompany> e)
		{
			POForSalesOrderFilter filter = e.Row;
			if (filter == null) return;

			Branch branchExtendedToVendor = PXSelectJoin<Branch,
				InnerJoin<BAccountR, On<BAccountR.bAccountID, Equal<Branch.bAccountID>>,
				InnerJoin<Vendor, On<Vendor.bAccountID, Equal<BAccountR.bAccountID>>>>,
				Where<Branch.branchID, Equal<Required<Branch.branchID>>>>.SelectSingleBound(this, null, Accessinfo.BranchID);

			if (branchExtendedToVendor != null)
			{
				e.NewValue = branchExtendedToVendor.BAccountID;
				e.Cancel = true;
			}
		}
		#endregion

		#region Processing
		public static void GenerateSalesOrder(List<POForSalesOrderDocument> itemsList, POForSalesOrderFilter filter)
		{
			if (filter == null)
				return;

			POCreateSalesOrderProcess processingGraph = PXGraph.CreateInstance<POCreateSalesOrderProcess>();
			if (filter.PODocType == POCrossCompanyDocType.PurchaseOrders)
			{
				processingGraph.GenerateSalesOrdersFromPurchaseOrders(itemsList, filter);
			}
			else if (filter.PODocType == POCrossCompanyDocType.PurchaseReturns)
			{
				processingGraph.GenerateSalesOrdersFromPurchaseReturns(itemsList, filter);
			}
		}
		#endregion

		public PXAction<POForSalesOrderFilter> viewPODocument;

		[PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
		[PXEditDetailButton(ImageKey = PX.Web.UI.Sprite.Main.DataEntry)]
		public virtual IEnumerable ViewPODocument(PXAdapter adapter)
		{
			POForSalesOrderDocument doc = Documents.Current;
			POForSalesOrderFilter filter = Filter.Current;
			if (doc != null && filter != null)
			{
				if (filter.PODocType == POCrossCompanyDocType.PurchaseOrders)
				{
					POOrderEntry orderEntry = PXGraph.CreateInstance<POOrderEntry>();
					orderEntry.Document.Current = POOrder.PK.Find(orderEntry, doc.DocType, doc.DocNbr);
					PXRedirectHelper.TryRedirect(orderEntry, PXRedirectHelper.WindowMode.NewWindow);
				}
				else if (filter.PODocType == POCrossCompanyDocType.PurchaseReturns)
				{
					POReceiptEntry receiptEntry = PXGraph.CreateInstance<POReceiptEntry>();
					receiptEntry.Document.Current = POReceipt.PK.Find(receiptEntry, doc.DocNbr);
					PXRedirectHelper.TryRedirect(receiptEntry, PXRedirectHelper.WindowMode.NewWindow);
				}
			}
			return Filter.Select();
		}

		public PXAction<POForSalesOrderFilter> viewSOOrder;

		[PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
		[PXEditDetailButton(ImageKey = PX.Web.UI.Sprite.Main.DataEntry)]
		public virtual IEnumerable ViewSOOrder(PXAdapter adapter)
		{
			POForSalesOrderDocument doc = Documents.Current;
			if (doc != null)
			{
				SOOrderEntry orderEntry = PXGraph.CreateInstance<SOOrderEntry>();
				orderEntry.Document.Current = SOOrder.PK.Find(orderEntry, doc.OrderType, doc.OrderNbr);
				PXRedirectHelper.TryRedirect(orderEntry, PXRedirectHelper.WindowMode.NewWindow);
			}
			return Filter.Select();
		}

		public override bool IsDirty { get { return false; } }
	}

	public class POCreateSalesOrderProcess : PXGraph<POCreateSalesOrderProcess>
	{
		public virtual void GenerateSalesOrdersFromPurchaseOrders(List<POForSalesOrderDocument> itemsList, POForSalesOrderFilter filter)
		{
			var orderEntry = PXGraph.CreateInstance<POOrderEntry>();

			foreach (POForSalesOrderDocument item in itemsList)
			{
				SetProcessingResult(GenerateSalesOrderFromPurchaseOrder(orderEntry, item, filter));
			}
		}

		public virtual void GenerateSalesOrdersFromPurchaseReturns(List<POForSalesOrderDocument> itemsList, POForSalesOrderFilter filter)
		{
			var receiptEntry = PXGraph.CreateInstance<POReceiptEntry>();

			foreach (POForSalesOrderDocument item in itemsList)
			{
				SetProcessingResult(GenerateSalesOrderFromPurchaseReturn(receiptEntry, item, filter));
			}
		}

		public virtual ProcessingResult GenerateSalesOrderFromPurchaseOrder(POOrderEntry orderEntry, POForSalesOrderDocument item, POForSalesOrderFilter filter)
		{
			ProcessingResult result = new ProcessingResult();
			PXFilteredProcessing<POForSalesOrderDocument, POForSalesOrderFilter>.SetCurrentItem(item);

			orderEntry.Clear();
			orderEntry.Document.Current = orderEntry.Document.Search<POOrder.orderNbr>(item.DocNbr, item.DocType);
			POOrder po = orderEntry.Document.Current;

			if (item.Excluded == true)
			{
				try
				{
					po.ExcludeFromIntercompanyProc = true;
					po = orderEntry.Document.Update(po);
					orderEntry.Save.Press();
				}
				catch (Exception ex)
				{
					result.AddErrorMessage(ex.Message);
				}
				return result;
			}

			List<POLine> pOLines = orderEntry.Transactions.Select().RowCast<POLine>().ToList();

			if (filter.CopyProjectDetails == true)
			{
				int? projectID = null;
				foreach (POLine poLine in pOLines)
				{
					if (projectID == null)
					{
						projectID = poLine.ProjectID;
					}
					else if (projectID != poLine.ProjectID)
					{
						result.AddErrorMessage(Messages.IntercompanyDifferentProjectIDsOnPOLines);
						return result;
					}
				}
			}

			POShipAddress shipAddress = orderEntry.Shipping_Address.Select();
			POShipContact shipContact = orderEntry.Shipping_Contact.Select();
			List<POOrderDiscountDetail> discountLines = orderEntry.DiscountDetails.Select().RowCast<POOrderDiscountDetail>().ToList();

			try
			{
				var ext = orderEntry.GetExtension<Intercompany>();
				SOOrder generatedSO = ext.GenerateIntercompanySalesOrder(po, shipAddress, shipContact, pOLines, discountLines, filter.IntercompanyOrderType, filter.CopyProjectDetails ?? false);

				item.OrderType = generatedSO.OrderType;
				item.OrderNbr = generatedSO.OrderNbr;
				result = ProcessingResult.CreateSuccess(generatedSO);
				result.AddMessage(PXErrorLevel.RowInfo, Messages.SOCreatedSuccessfully, generatedSO.OrderType, generatedSO.OrderNbr);

				if (generatedSO.CuryTaxTotal != item.CuryTaxTotal)
				{
					result.AddMessage(PXErrorLevel.RowWarning, Messages.IntercompanyTaxTotalDiffers);
				}
				if (generatedSO.CuryOrderTotal != item.CuryDocTotal)
				{
					result.AddMessage(PXErrorLevel.RowWarning, Messages.IntercompanyOrderTotalDiffers);
				}
			}
			catch (Exception ex)
			{
				result.AddErrorMessage(ex.Message);
			}

			return result;
		}

		public virtual ProcessingResult GenerateSalesOrderFromPurchaseReturn(POReceiptEntry receiptEntry, POForSalesOrderDocument item, POForSalesOrderFilter filter)
		{
			ProcessingResult result = new ProcessingResult();
			PXFilteredProcessing<POForSalesOrderDocument, POForSalesOrderFilter>.SetCurrentItem(item);

			receiptEntry.Clear();
			receiptEntry.Document.Current = receiptEntry.Document.Search<POReceipt.receiptNbr>(item.DocNbr, POReceiptType.POReturn);
			POReceipt poReturn = receiptEntry.Document.Current;

			if (item.Excluded == true)
			{
				try
				{
					poReturn.ExcludeFromIntercompanyProc = true;
					poReturn = receiptEntry.Document.Update(poReturn);
					receiptEntry.Save.Press();
				}
				catch (Exception ex)
				{
					result.AddErrorMessage(ex.Message);
				}
				return result;
			}

			try
			{
				var ext = receiptEntry.GetExtension<PX.Objects.PO.GraphExtensions.POReceiptEntryExt.Intercompany>();
				SOOrder generatedSO = ext.GenerateIntercompanySOReturn(poReturn, filter.IntercompanyOrderType);

				item.OrderType = generatedSO.OrderType;
				item.OrderNbr = generatedSO.OrderNbr;
				result = ProcessingResult.CreateSuccess(generatedSO);
				result.AddMessage(PXErrorLevel.RowInfo, Messages.SOCreatedSuccessfully, generatedSO.OrderType, generatedSO.OrderNbr);
			}
			catch (Exception ex)
			{
				result.AddErrorMessage(ex.Message);
			}

			return result;
		}

		private static void SetProcessingResult(ProcessingResult result)
		{
			if (!result.IsSuccess)
			{
				PXFilteredProcessing<POForSalesOrderDocument, POForSalesOrderDocument>.SetError(result.GeneralMessage);
			}
			else if (result.HasWarning)
			{
				PXFilteredProcessing<POForSalesOrderDocument, POForSalesOrderDocument>.SetWarning(result.GeneralMessage);
			}
			else
			{
				PXFilteredProcessing<POForSalesOrderDocument, POForSalesOrderDocument>.SetProcessed();
				PXFilteredProcessing<POForSalesOrderDocument, POForSalesOrderDocument>.SetInfo(result.GeneralMessage);
			}
		}
	}
}
