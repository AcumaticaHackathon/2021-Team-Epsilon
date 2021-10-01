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
using PX.Objects.PO.GraphExtensions.POOrderEntryExt;
using PX.Objects.SO;
using PX.TM;

namespace PX.Objects.PO
{
	[TableAndChartDashboardType]
	public class POCreateIntercompanySalesOrder : PXGraph<POCreateIntercompanySalesOrder>
	{
		public PXCancel<SOForPurchaseReceiptFilter> Cancel;
		public PXFilter<SOForPurchaseReceiptFilter> Filter;
		public PXSetup<POSetup> POSetup;
		public PXSetup<INSetup> INSetup;
		public PXSelectReadonly<SOOrder, Where<True, Equal<False>>> Order;

		[PXFilterable]
		[PXVirtualDAC]
		public PXFilteredProcessingOrderBy<SOShipment, SOForPurchaseReceiptFilter, OrderBy<Asc<SOShipment.shipmentNbr>>> Documents;

		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.interBranch>() && PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXDefault(POOrderType.RegularOrder)]
		protected virtual void _(Events.CacheAttached<SOOrder.intercompanyPOType> eventArgs)
		{
		}

		public POCreateIntercompanySalesOrder()
		{
			POSetup posetup = POSetup.Current;
			INSetup insetup = INSetup.Current;

			Documents.SetSelected<SOShipment.selected>();
			Documents.SetProcessCaption(Messages.Process);
			Documents.SetProcessAllCaption(Messages.ProcessAll);

			PXUIFieldAttribute.SetDisplayName<SOShipment.customerID>(Documents.Cache, SO.Messages.PurchasingCompany);
			PXUIFieldAttribute.SetDisplayName<SOOrder.branchID>(Order.Cache, SO.Messages.SellingCompany);
			PXUIFieldAttribute.SetDisplayName<SOOrder.intercompanyPONbr>(Order.Cache, SO.Messages.IntercompanyPONbr);

			PXUIFieldAttribute.SetVisible<SOShipment.workgroupID>(Documents.Cache, null, false);
			PXUIFieldAttribute.SetVisible<SOShipment.shipmentWeight>(Documents.Cache, null, false);
			PXUIFieldAttribute.SetVisible<SOShipment.shipmentVolume>(Documents.Cache, null, false);
			PXUIFieldAttribute.SetVisible<SOShipment.packageCount>(Documents.Cache, null, false);
			PXUIFieldAttribute.SetVisible<SOShipment.packageWeight>(Documents.Cache, null, false);
			PXUIFieldAttribute.SetVisible<SOShipment.status>(Documents.Cache, null, false);
			PXUIFieldAttribute.SetEnabled<SOShipment.excluded>(Documents.Cache, null, PXLongOperation.GetStatus(this.UID) == PXLongRunStatus.NotExists);
		}

		public override void InitCacheMapping(Dictionary<Type, Type> map)
		{
			base.InitCacheMapping(map);

			this.Caches.AddCacheMapping(typeof(BAccount), typeof(BAccount));
		}

		protected virtual IEnumerable documents()
		{
			List<PXResult<SOShipment, SOOrderShipment, SOOrder, Branch, BAccount, POReceipt>> list = new List<PXResult<SOShipment, SOOrderShipment, SOOrder, Branch, BAccount, POReceipt>>();

			if (Filter.Current != null)
			{
				using (new PXReadBranchRestrictedScope())
				{
					var shipments = PXSelectJoin<SOShipment,
								InnerJoin<SOOrderShipment, On<SOOrderShipment.FK.Shipment>,
								InnerJoin<SOOrder, On<SOOrderShipment.FK.Order>,
								InnerJoin<Branch, On<Branch.branchID, Equal<SOOrder.branchID>>,
								InnerJoin<BAccount, On<BAccount.bAccountID, Equal<Branch.bAccountID>,
									And<BAccount.isBranch, Equal<True>>>,
								LeftJoin<POReceipt, On<POReceipt.FK.IntercompanyShipment>>>>>>,
									Where2<Where<SOShipment.shipDate, LessEqual<Current<SOForPurchaseReceiptFilter.docDate>>,
											Or<Current<SOForPurchaseReceiptFilter.docDate>, IsNull>>,
										And2<Where<SOShipment.customerID, Equal<Current<SOForPurchaseReceiptFilter.purchasingCompany>>,
											Or<Current<SOForPurchaseReceiptFilter.purchasingCompany>, IsNull>>,
										And2<Where<BAccount.bAccountID, Equal<Current<SOForPurchaseReceiptFilter.sellingCompany>>,
											Or<Current<SOForPurchaseReceiptFilter.sellingCompany>, IsNull>>,
										And<SOShipment.shipmentType, Equal<SOShipmentType.issue>, And<SOShipment.operation, Equal<SOOperation.issue>,
										And<POReceipt.intercompanyShipmentNbr, IsNull, And<SOShipment.isIntercompany, Equal<True>,
										And<SOShipment.excludeFromIntercompanyProc, Equal<False>,
										And<SOShipment.confirmed, Equal<True>>>>>>>>>>>.Select(this);

					foreach (PXResult<SOShipment, SOOrderShipment, SOOrder, Branch, BAccount, POReceipt> shipment in shipments)
					{
						list.Add(shipment);
					}
				}
			}

			return list;
		}

		public virtual void _(Events.RowSelecting<SOShipment> e)
		{
			if (e.Row == null)
				return;

			SOShipment row = (SOShipment)e.Row;
			using (new PXConnectionScope())
			{
				row.PackageCount = PXSelect<SOPackageDetailEx, Where<SOPackageDetailEx.shipmentNbr, Equal<Required<SOShipment.shipmentNbr>>>>.Select(e.Cache.Graph, row.ShipmentNbr).Count();
			}
		}

		#region Event Handlers
		public virtual void _(Events.RowSelected<SOForPurchaseReceiptFilter> e)
		{
			SOForPurchaseReceiptFilter filter = e.Row;
			Documents.SetProcessDelegate(itemsList => GeneratePurchaseReceipt(itemsList, filter));
		}

		public virtual void _(Events.RowUpdated<SOForPurchaseReceiptFilter> e)
		{
			SOForPurchaseReceiptFilter row = e.Row;
			SOForPurchaseReceiptFilter oldRow = e.OldRow;
			if (row != null && oldRow != null && !Filter.Cache.ObjectsEqual<SOForPurchaseReceiptFilter.docDate, SOForPurchaseReceiptFilter.purchasingCompany, SOForPurchaseReceiptFilter.sellingCompany>(row, oldRow))
			{
				Documents.Cache.Clear();
				Documents.Cache.ClearQueryCache();
			}
		}

		public virtual void _(Events.FieldDefaulting<SOForPurchaseReceiptFilter, SOForPurchaseReceiptFilter.purchasingCompany> e)
		{
			SOForPurchaseReceiptFilter filter = e.Row;
			if (filter == null) return;

			Branch branchExtendedToCustomer = PXSelectJoin<Branch,
				InnerJoin<BAccountR, On<BAccountR.bAccountID, Equal<Branch.bAccountID>>,
				InnerJoin<Customer, On<Customer.bAccountID, Equal<BAccountR.bAccountID>>>>,
				Where<Branch.branchID, Equal<Required<Branch.branchID>>>>.SelectSingleBound(this, null, Accessinfo.BranchID);

			if (branchExtendedToCustomer != null)
			{
				e.NewValue = branchExtendedToCustomer.BAccountID;
				e.Cancel = true;
			}
		}
		#endregion

		#region Processing
		public static void GeneratePurchaseReceipt(List<SOShipment> itemsList, SOForPurchaseReceiptFilter filter)
		{
			if (filter == null)
				return;

			SOCreatePurchaseReceiptProcess processingGraph = PXGraph.CreateInstance<SOCreatePurchaseReceiptProcess>();
			processingGraph.GeneratePurchaseReceiptsFromShipment(itemsList, filter);
		}
		#endregion

		public PXAction<SOForPurchaseReceiptFilter> viewSODocument;

		[PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
		[PXEditDetailButton(ImageKey = PX.Web.UI.Sprite.Main.DataEntry)]
		public virtual IEnumerable ViewSODocument(PXAdapter adapter)
		{
			SOShipment doc = Documents.Current;
			SOForPurchaseReceiptFilter filter = Filter.Current;
			if (doc != null && filter != null)
			{
				SOShipmentEntry shipmentEntry = PXGraph.CreateInstance<SOShipmentEntry>();
				shipmentEntry.Document.Current = SOShipment.PK.Find(shipmentEntry, doc.ShipmentNbr);
				PXRedirectHelper.TryRedirect(shipmentEntry, PXRedirectHelper.WindowMode.NewWindow);
			}
			return Filter.Select();
		}

		public PXAction<POForSalesOrderFilter> viewPOReceipt;

		[PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
		[PXEditDetailButton(ImageKey = PX.Web.UI.Sprite.Main.DataEntry)]
		public virtual IEnumerable ViewPOReceipt(PXAdapter adapter)
		{
			SOShipment doc = Documents.Current;
			if (doc != null)
			{
				POReceiptEntry receiptEntry = PXGraph.CreateInstance<POReceiptEntry>();
				receiptEntry.Document.Current = POReceipt.PK.Find(receiptEntry, doc.IntercompanyPOReceiptNbr);
				PXRedirectHelper.TryRedirect(receiptEntry, PXRedirectHelper.WindowMode.NewWindow);
			}
			return Filter.Select();
		}

		public override bool IsDirty { get { return false; } }
	}

	public class SOCreatePurchaseReceiptProcess : PXGraph<SOCreatePurchaseReceiptProcess>
	{
		public virtual void GeneratePurchaseReceiptsFromShipment(List<SOShipment> shipmentList, SOForPurchaseReceiptFilter filter)
		{
			var shipmentEntry = PXGraph.CreateInstance<SOShipmentEntry>();

			foreach (SOShipment shipment in shipmentList)
			{
				SetProcessingResult(GeneratePurchaseReceiptFromShipment(shipmentEntry, shipment, filter));
			}
		}

		public virtual ProcessingResult GeneratePurchaseReceiptFromShipment(SOShipmentEntry shipmentEntry, SOShipment shipment, SOForPurchaseReceiptFilter filter)
		{
			ProcessingResult result = new ProcessingResult();
			PXFilteredProcessing<SOShipment, SOForPurchaseReceiptFilter>.SetCurrentItem(shipment);

			shipmentEntry.Clear();
			if (shipment.Excluded == true)
			{
				shipmentEntry.Document.Current = shipmentEntry.Document.Search<SOShipment.shipmentNbr>(shipment.ShipmentNbr);
				try
				{
					shipmentEntry.Document.Current.ExcludeFromIntercompanyProc = true;
					shipmentEntry.Document.UpdateCurrent();
					shipmentEntry.Save.Press();
				}
				catch (Exception ex)
				{
					result.AddErrorMessage(ex.Message);
				}
				return result;
			}

			List<PXResult<SOShipLine, SOLine>> shipLines =
				SelectFrom<SOShipLine>
					.InnerJoin<SOLine>.On<SOShipLine.FK.OrderLine>
					.Where<SOShipLine.FK.Shipment.SameAsCurrent>
					.View.SelectMultiBound(this, new object[] { shipment })
					.Cast<PXResult<SOShipLine, SOLine>>()
					.ToList();
			
			try
			{
				var ext = shipmentEntry.GetExtension<SO.GraphExtensions.SOShipmentEntryExt.Intercompany>();
				POReceipt generatedPR = ext.GenerateIntercompanyPOReceipt(shipment, shipLines, filter.PutReceiptsOnHold, null);
			
				shipment.IntercompanyPOReceiptNbr = generatedPR.ReceiptNbr;
				result = ProcessingResult.CreateSuccess(generatedPR);
				result.AddMessage(PXErrorLevel.RowInfo, SO.Messages.PRCreatedSuccessfully, generatedPR.ReceiptNbr);
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
				PXFilteredProcessing<SOShipment, SOForPurchaseReceiptFilter>.SetError(result.GeneralMessage);
			}
			else if (result.HasWarning)
			{
				PXFilteredProcessing<SOShipment, SOForPurchaseReceiptFilter>.SetWarning(result.GeneralMessage);
			}
			else
			{
				PXFilteredProcessing<SOShipment, SOForPurchaseReceiptFilter>.SetProcessed();
				PXFilteredProcessing<SOShipment, SOForPurchaseReceiptFilter>.SetInfo(result.GeneralMessage);
			}
		}
	}
}
