using PX.Common;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.AP;
using PX.Objects.CM;
using PX.Objects.Common.DAC;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.PO.DAC.Unbound;
using PX.Objects.SO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SOLineSplit3 = PX.Objects.PO.POOrderEntry.SOLineSplit3;

namespace PX.Objects.PO.GraphExtensions.POOrderEntryExt
{
	public class DropShipLinksExt : PXGraphExtension<POOrderEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.dropShipments>();
		}

		[PXCopyPasteHiddenView()]
		public PXSelect<DropShipLink,
			Where<DropShipLink.pOOrderType, Equal<Required<POLine.orderType>>,
				And<DropShipLink.pOOrderNbr, Equal<Required<POLine.orderNbr>>,
				And<DropShipLink.pOLineNbr, Equal<Required<POLine.lineNbr>>>>>> DropShipLinks;

		public virtual IEnumerable dropShipLinks()
		{
			var parameters = PXView.Parameters;

			if (parameters.Length >= 3)
			{
				var key = new DropShipLink();
				key.POOrderType = (string)parameters[0];
				key.POOrderNbr = (string)parameters[1];
				key.POLineNbr = (int?)parameters[2];

				DropShipLink cached = (DropShipLink)DropShipLinks.Cache.Locate(key);
				if (cached != null && DropShipLinks.Cache.GetStatus(cached).IsNotIn(PXEntryStatus.Deleted, PXEntryStatus.InsertedDeleted))
				{
					yield return cached;
					yield break;
				}

				cached = DropShipLink.UK.ByPOLine.FindDirty(Base, key.POOrderType, key.POOrderNbr, key.POLineNbr);
				yield return cached;
			}
			yield break;
		}

		[PXCopyPasteHiddenView()]
		public PXSelect<DemandSOOrder,
			Where<DemandSOOrder.orderType, Equal<Required<DemandSOOrder.orderType>>,
				And<DemandSOOrder.orderNbr, Equal<Required<DemandSOOrder.orderNbr>>>>> DemandSOOrders;

		public virtual IEnumerable demandSOOrders()
		{
			var parameters = PXView.Parameters;

			if (parameters.Length >= 2)
			{
				var key = new DemandSOOrder();
				key.OrderType = (string)parameters[0];
				key.OrderNbr = (string)parameters[1];

				DemandSOOrder cached = (DemandSOOrder)DemandSOOrders.Cache.Locate(key);
				if (cached != null && DemandSOOrders.Cache.GetStatus(cached).IsNotIn(PXEntryStatus.Deleted, PXEntryStatus.InsertedDeleted))
				{
					yield return cached;
					yield break;
				}

				cached = DemandSOOrder.PK.FindDirty(Base, key.OrderType, key.OrderNbr);
				yield return cached;
			}
			yield break;
		}


		public PXFilter<CreateSOOrderFilter> CreateSOFilter;

		#region Actions

		public PXAction<POOrder> unlinkFromSO;

		[PXUIField(DisplayName = "Unlink from Sales Order", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(CommitChanges = true)]
		protected virtual IEnumerable UnlinkFromSO(PXAdapter adapter)
		{
			POOrder order = Base.Document.Current;
			if (order == null || order.OrderType != POOrderType.DropShip || order.IsLegacyDropShip == true
				|| order.Status.IsIn(POOrderStatus.Completed, POOrderStatus.Closed, POOrderStatus.Cancelled))
				return adapter.Get();

			PXResult<POOrderReceipt> receipt = PXSelectJoin<POOrderReceipt,
				InnerJoin<POReceipt, On<POOrderReceipt.FK.Receipt>>,
				Where<POOrderReceipt.pOType, Equal<Current<POOrder.orderType>>,
					And<POOrderReceipt.pONbr, Equal<Current<POOrder.orderNbr>>>>,
				OrderBy<Desc<POReceipt.released>>>.SelectWindowed(Base, 0, 1);
			if (receipt != null)
				throw new PXException(Messages.DropShipUnlinkErrorReceipt, order.OrderNbr);

			if (string.IsNullOrEmpty(order.SOOrderNbr))
				throw new PXException(Messages.DropShipUnlinkErrorNoLink);

			string question = PXMessages.LocalizeFormatNoPrefixNLA(Messages.DropShipUnlinkConfirmation, order.OrderNbr, order.SOOrderNbr);
			if (Base.Transactions.View.Ask("UnlinkFromSOAsk", question, MessageButtons.OKCancel) == WebDialogResult.OK)
			{
				Base.Save.Press();

				PXLongOperation.StartOperation(Base, () =>
				{
					POOrderEntry graph = PXGraph.CreateInstance<POOrderEntry>();
					graph.Document.Search<POOrder.orderNbr>(order.OrderNbr, order.OrderType);
					DropShipLinksExt ext = graph.GetExtension<DropShipLinksExt>();

					var linksQuery = new PXSelect<DropShipLink,
						Where<DropShipLink.pOOrderType, Equal<Required<POLine.orderType>>,
							And<DropShipLink.pOOrderNbr, Equal<Required<POLine.orderNbr>>>>>(graph);
					foreach (DropShipLink link in linksQuery.Select(order.OrderType, order.OrderNbr))
					{
						ext.DropShipLinks.Delete(link);
					}

					graph.Save.Press();
				});
			}

			return adapter.Get();
		}		

		public PXAction<POOrder> convertToNormal;

		[PXUIField(DisplayName = "Convert to Normal", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(CommitChanges = true)]
		public virtual IEnumerable ConvertToNormal(PXAdapter adapter)
		{
			if (Base.Document.Current == null || Base.Document.Current.OrderType != POOrderType.DropShip
				|| Base.Document.Current.IsLegacyDropShip == true)
				return adapter.Get();

			POOrderPrepayment prepayment = PXSelectJoin<POOrderPrepayment,
				InnerJoin<APRegister,
					On<APRegister.docType, Equal<POOrderPrepayment.aPDocType>,
						And<APRegister.refNbr, Equal<POOrderPrepayment.aPRefNbr>>>>,
				Where<POOrderPrepayment.orderType, Equal<Current<POOrder.orderType>>,
					And<POOrderPrepayment.orderNbr, Equal<Current<POOrder.orderNbr>>>>>
				.SelectWindowed(Base, 0, 1);
			if (prepayment != null)
			{
				throw new PXException(Messages.DSConvertToNormalPrepaymentExists, prepayment.APRefNbr, Base.Document.Current.OrderNbr);
			}

			APTran tran = PXSelect<APTran,
				Where<APTran.pOOrderType, Equal<Current<POOrder.orderType>>,
					And<APTran.pONbr, Equal<Current<POOrder.orderNbr>>>>>
				.SelectWindowed(Base, 0, 1);
			if (tran != null)
			{
				throw new PXException(Messages.DSConvertToNormalBillExists, tran.RefNbr, Base.Document.Current.OrderNbr);
			}

			POOrderReceipt receipt = PXSelectJoin<POOrderReceipt,
				InnerJoin<POReceipt, On<POOrderReceipt.FK.Receipt>>,
				Where<POOrderReceipt.pOType, Equal<Current<POOrder.orderType>>,
					And<POOrderReceipt.pONbr, Equal<Current<POOrder.orderNbr>>>>>
				.SelectWindowed(Base, 0, 1);
			if (receipt != null)
			{
				throw new PXException(Messages.DSConvertToNormalReceiptExists, receipt.ReceiptNbr, Base.Document.Current.OrderNbr);
			}

			string question = Base.Document.Current.SOOrderNbr != null
				? PXMessages.LocalizeFormatNoPrefixNLA(Messages.DSLinkedConvertToNormalAsk, Base.Document.Current.SOOrderNbr)
				: PXMessages.LocalizeNoPrefix(Messages.DSNotLinkedConvertToNormalAsk);
			if (Base.Transactions.View.Ask("ConvertToNormalAsk", question, MessageButtons.OKCancel) == WebDialogResult.OK)
			{
				Base.Save.Press();

				POOrder origOrder = PXCache<POOrder>.CreateCopy(Base.Document.Current);
				CurrencyInfo origCuryInfo = CurrencyInfoAttribute.GetCurrencyInfo<POOrder.curyInfoID>(Base.Document.Cache, Base.Document.Current);

				POOrderEntry graph = PXGraph.CreateInstance<POOrderEntry>();
				POOrder newOrder = graph.Document.Insert(new POOrder
				{
					OrderType = POOrderType.RegularOrder
				});
				CurrencyInfo info = graph.currencyinfo.Select();
				PXCache<CurrencyInfo>.RestoreCopy(info, origCuryInfo);
				info.CuryInfoID = newOrder.CuryInfoID;

				POOrder orderCopy = PXCache<POOrder>.CreateCopy(origOrder);
				orderCopy.OrderType = newOrder.OrderType;
				orderCopy.OrderNbr = newOrder.OrderNbr;

				orderCopy.CuryDiscTot = newOrder.CuryDiscTot;
				orderCopy.CuryLineRetainageTotal = newOrder.CuryLineRetainageTotal;
				orderCopy.CuryLineTotal = newOrder.CuryLineTotal;
				orderCopy.CuryOrderTotal = newOrder.CuryOrderTotal;
				orderCopy.CuryPrepaidTotal = newOrder.CuryPrepaidTotal;
				orderCopy.CuryRetainageTotal = newOrder.CuryRetainageTotal;
				orderCopy.CuryRetainedDiscTotal = newOrder.CuryRetainedDiscTotal;
				orderCopy.CuryRetainedTaxTotal = newOrder.CuryRetainedTaxTotal;
				orderCopy.CuryTaxTotal = newOrder.CuryTaxTotal;
				orderCopy.CuryUnbilledLineTotal = newOrder.CuryUnbilledLineTotal;
				orderCopy.CuryUnbilledOrderTotal = newOrder.CuryUnbilledOrderTotal;
				orderCopy.CuryUnbilledTaxTotal = newOrder.CuryUnbilledTaxTotal;
				orderCopy.CuryUnprepaidTotal = newOrder.CuryUnprepaidTotal;
				orderCopy.CuryVatExemptTotal = newOrder.CuryVatExemptTotal;
				orderCopy.CuryVatTaxableTotal = newOrder.CuryVatTaxableTotal;
				orderCopy.OrderQty = newOrder.OrderQty;
				orderCopy.OpenOrderQty = newOrder.OpenOrderQty;
				orderCopy.UnbilledOrderQty = newOrder.UnbilledOrderQty;
				orderCopy.OrderWeight = newOrder.OrderWeight;
				orderCopy.OrderVolume = newOrder.OrderVolume;
				orderCopy.ShipDestType = newOrder.ShipDestType;
				orderCopy.ShipToBAccountID = newOrder.ShipToBAccountID;
				orderCopy.ShipToLocationID = newOrder.ShipToLocationID;

				orderCopy.LineCntr = 0;
				orderCopy.LinesToCloseCntr = 0;
				orderCopy.LinesToCompleteCntr = 0;
				orderCopy.DropShipLinesCount = 0;
				orderCopy.DropShipLinkedLinesCount = 0;
				orderCopy.DropShipActiveLinksCount = 0;

				orderCopy.CuryInfoID = newOrder.CuryInfoID;
				orderCopy.Status = newOrder.Status;
				orderCopy.Hold = newOrder.Hold;
				orderCopy.Approved = newOrder.Approved;
				orderCopy.Rejected = newOrder.Rejected;
				orderCopy.RequestApproval = newOrder.RequestApproval;
				orderCopy.Cancelled = newOrder.Cancelled;
				orderCopy.TaxZoneID = newOrder.TaxZoneID;
				orderCopy.IsTaxValid = newOrder.IsTaxValid;
				orderCopy.IsUnbilledTaxValid = newOrder.IsUnbilledTaxValid;
				orderCopy.SOOrderType = newOrder.SOOrderType;
				orderCopy.SOOrderNbr = newOrder.SOOrderNbr;
				orderCopy.DontEmail = newOrder.DontEmail;
				orderCopy.DontPrint = newOrder.DontPrint;
				orderCopy.NoteID = newOrder.NoteID;
				orderCopy.CreatedByID = newOrder.CreatedByID;
				orderCopy.CreatedByScreenID = newOrder.CreatedByScreenID;
				orderCopy.CreatedDateTime = newOrder.CreatedDateTime;
				orderCopy.LastModifiedByID = newOrder.LastModifiedByID;
				orderCopy.LastModifiedByScreenID = newOrder.LastModifiedByScreenID;
				orderCopy.LastModifiedDateTime = newOrder.LastModifiedDateTime;
				orderCopy.tstamp = newOrder.tstamp;
				orderCopy.OriginalPOType = origOrder.OrderType;
				orderCopy.OriginalPONbr = origOrder.OrderNbr;
				newOrder = graph.Document.Update(orderCopy);

				foreach (POLine line in Base.Transactions.Select())
				{
					POLine newLine = graph.Transactions.Insert();
					POLine lineCopy = PXCache<POLine>.CreateCopy(newLine);
					PXCache<POLine>.RestoreCopy(lineCopy, line);
					lineCopy.OrderType = newLine.OrderType;
					lineCopy.OrderNbr = newLine.OrderNbr;
					lineCopy.LineNbr = newLine.LineNbr;

					lineCopy.PlanID = null;
					lineCopy.LineType = POLineType.IsService(line.LineType) ? POLineType.Service
						: POLineType.IsStock(line.LineType) ? POLineType.GoodsForInventory
						: POLineType.IsNonStock(line.LineType) ? POLineType.NonStock
						: POLineType.Freight;
					lineCopy.SOOrderType = newLine.SOOrderType;
					lineCopy.SOOrderNbr = newLine.SOOrderNbr;
					lineCopy.SOLinkActive = newLine.SOLinkActive;
					lineCopy.OrderNoteID = newLine.OrderNoteID;
					lineCopy.NoteID = newLine.NoteID;
					lineCopy.Completed = false;
					lineCopy.Cancelled = false;
					lineCopy.Closed = false;
					lineCopy.CreatedByID = newLine.CreatedByID;
					lineCopy.CreatedByScreenID = newLine.CreatedByScreenID;
					lineCopy.CreatedDateTime = newLine.CreatedDateTime;
					lineCopy.LastModifiedByID = newLine.LastModifiedByID;
					lineCopy.LastModifiedByScreenID = newLine.LastModifiedByScreenID;
					lineCopy.LastModifiedDateTime = newLine.LastModifiedDateTime;
					lineCopy.tstamp = newLine.tstamp;

					newLine = graph.Transactions.Update(lineCopy);

					if (newLine.ProjectID == null && lineCopy.ProjectID != null)
					{
						newLine.ProjectID = lineCopy.ProjectID;
						newLine = graph.Transactions.Update(newLine);
					}
				}

				using (var transaction = new PXTransactionScope())
				{
					graph.Save.Press();

					Base.Document.View.SetAnswer("LinkedDSOrderCancellAsk", WebDialogResult.OK);
					Base.cancelOrder.Press();

					transaction.Complete();
				}

				throw new PXRedirectRequiredException(graph, "Redirect to Normal PO");
			}

			return adapter.Get();
		}

		public PXAction<POOrder> createSalesOrder;

		[PXUIField(DisplayName = "Create Sales Order", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(CommitChanges = true)]
		public virtual IEnumerable CreateSalesOrder(PXAdapter adapter)
		{
			POOrder currentOrder = Base.Document.Current;
			if (currentOrder?.OrderType != POOrderType.DropShip)
				return adapter.Get();

			bool isLegacy = currentOrder.IsLegacyDropShip == true;
			if (currentOrder.SOOrderNbr != null && !isLegacy)
				throw new PXException(Messages.DSCreateSOAlreadyLinked, currentOrder.OrderNbr, currentOrder.SOOrderNbr);

			var notLinkedLinesQuery = new PXSelectJoin<POLine,
				LeftJoin<SOLineSplit, On<SOLineSplit.FK.POLine>>,
				Where<SOLineSplit.orderNbr, IsNull,
					And<POLine.lineType, In3<POLineType.goodsForDropShip, POLineType.nonStockForDropShip>,
					And<POLine.cancelled, NotEqual<True>,
					And<POLine.orderType, Equal<Required<POOrder.orderType>>,
					And<POLine.orderNbr, Equal<Required<POOrder.orderNbr>>>>>>>>(Base);
			if (isLegacy && !notLinkedLinesQuery.Select(currentOrder.OrderType, currentOrder.OrderNbr).Any())
				throw new PXException(Messages.DSLegacyCreateSOAlreadyLinked, currentOrder.OrderNbr);

			if (CreateSOFilter.AskExt(LineMessagesDialogInitializer, true) == WebDialogResult.OK)
			{
				if (string.IsNullOrEmpty(CreateSOFilter.Current.OrderType) || CreateSOFilter.Current.CustomerID == null
					|| CreateSOFilter.Current.CustomerLocationID == null)
					return adapter.Get();

				Base.Save.Press();
				List<POLine> lines = null;
				if (!isLegacy)
				{
					lines = Base.Transactions.Select().RowCast<POLine>()
						.Where(l => l.LineType.IsIn(POLineType.GoodsForDropShip, POLineType.NonStockForDropShip) && l.Completed != true).ToList();
				}
				else
				{
					lines = notLinkedLinesQuery.Select(currentOrder.OrderType, currentOrder.OrderNbr)
						.RowCast<POLine>().ToList();
				}

				PXLongOperation.StartOperation(Base, () =>
				{
					var graph = PXGraph.CreateInstance<SOOrderEntry>();
					var doc = new SOOrder
					{
						OrderType = CreateSOFilter.Current.OrderType,
						Hold = true,
					};
					doc = PXCache<SOOrder>.CreateCopy(graph.Document.Insert(doc));
					doc.CustomerID = CreateSOFilter.Current.CustomerID;
					doc.CustomerLocationID = CreateSOFilter.Current.CustomerLocationID;
					doc.ProjectID = Base.Document.Current.ProjectID;
					doc = graph.Document.Update(doc);

					var dsLinkExt = graph.GetExtension<SO.GraphExtensions.SOOrderEntryExt.DropShipLinkDialog>();
					var dsLinkLegacyExt = graph.GetExtension<SO.GraphExtensions.SOOrderEntryExt.DropShipLegacyDialog>();

					if (!isLegacy)
					{
						foreach (POLine line in lines)
						{
							SOLine soLine = CreateSOLineFromDropShipLine(graph, line, isLegacy);
							SOLineSplit currentSOSplit = graph.splits.Select().RowCast<SOLineSplit>().Single();

							var supply = new SupplyPOLine
							{
								OrderType = line.OrderType,
								OrderNbr = line.OrderNbr,
								LineNbr = line.LineNbr,
								InventoryID = line.InventoryID,
								SiteID = line.SiteID,
								BaseOrderQty = line.BaseOrderQty,
								PlanID = line.PlanID
							};

							dsLinkExt.LinkToSupplyLine(currentSOSplit, supply, false);
							graph.Transactions.Cache.SetValue<SOLine.pOCreated>(soLine, true);
						}
					}

					using (var transaction = new PXTransactionScope())
					{
						if (isLegacy)
						{
							graph.Save.Press(); // We should save SOOrder to correctly add already shipped lines.

							foreach (POLine line in lines)
							{
								SOLine soLine = CreateSOLineFromDropShipLine(graph, line, isLegacy);
								List<SOLineSplit> splits = graph.splits.Select().RowCast<SOLineSplit>().ToList();

								var supply = new POLine3
								{
									OrderType = line.OrderType,
									OrderNbr = line.OrderNbr,
									LineNbr = line.LineNbr,
									InventoryID = line.InventoryID,
									SiteID = line.SiteID,
									BaseOrderQty = line.BaseOrderQty,
									BaseReceivedQty = line.BaseReceivedQty,
									PlanID = line.PlanID,
									Completed = line.Completed,
									VendorID = line.VendorID,
									DemandQty = 0m
								};

								bool addedLink = false;
								bool poLineCompleted = false;
								dsLinkLegacyExt.LinkToSuplyLine(soLine, splits, supply, ref addedLink, ref poLineCompleted);

								if (addedLink)
								{
									if (soLine.POCreated != true)
										Base.Transactions.Cache.SetValue<SOLine.pOCreated>(soLine, true);

									if (poLineCompleted)
										graph.lsselect.CompleteSchedules(graph.lsselect.Cache, soLine);
								}
							}

							doc.ForceCompleteOrder = true;
						}

						graph.Save.Press();
						transaction.Complete();
					}

					throw new PXOperationCompletedException(Messages.CreateSalesOrderCompleted, graph.Document.Current.OrderNbr);
				});
			}

			return adapter.Get();
		}

		public virtual SOLine CreateSOLineFromDropShipLine(SOOrderEntry graph, POLine line, bool isLegacy)
		{
			SOLine newLine = PXCache<SOLine>.CreateCopy(graph.Transactions.Insert());
			newLine.InventoryID = line.InventoryID;
			newLine.SubItemID = line.SubItemID;
			newLine.TranDesc = line.TranDesc;
			newLine.OrderQty = line.OrderQty;
			newLine.UOM = line.UOM;
			newLine.TaxCategoryID = line.TaxCategoryID;
			newLine.SiteID = line.SiteID;
			newLine.ProjectID = line.ProjectID;
			newLine.TaskID = line.TaskID;
			newLine.CostCodeID = line.CostCodeID;
			newLine.IsLegacyDropShip = isLegacy;
			newLine = PXCache<SOLine>.CreateCopy(graph.Transactions.Update(newLine));
			newLine.POCreate = true;
			newLine.POSource = INReplenishmentSource.DropShipToOrder;
			newLine.POSiteID = line.SiteID;
			newLine.VendorID = line.VendorID;
			return PXCache<SOLine>.CreateCopy(graph.Transactions.Update(newLine));
		}

		public virtual void LineMessagesDialogInitializer(PXGraph graph, string viewName)
		{
			CreateSOFilter.Current.OrderType = null;
			// TODO: If the following checks are the only usage of FixedCustomer field, let's remove the field.
			CreateSOFilter.Current.FixedCustomer = Base.Document.Current.ShipDestType == POShippingDestination.Customer
				&& Base.Document.Current.ShipToBAccountID != null;
			CreateSOFilter.Current.CustomerID = CreateSOFilter.Current.FixedCustomer == true ?
				Base.Document.Current.ShipToBAccountID : null;
			CreateSOFilter.Current.CustomerLocationID = CreateSOFilter.Current.FixedCustomer == true ?
				Base.Document.Current.ShipToLocationID : null;
		}

		public virtual void _(Events.RowSelected<CreateSOOrderFilter> e)
		{
			if (e.Row == null || Base.Document.Current == null)
				return;

			bool fixedCustomer = Base.Document.Current.ShipDestType == POShippingDestination.Customer;
			bool locationsFeature = PXAccess.FeatureInstalled<FeaturesSet.accountLocations>();

			PXUIFieldAttribute.SetEnabled<CreateSOOrderFilter.customerID>(e.Cache, e.Row, !fixedCustomer);
			PXUIFieldAttribute.SetEnabled<CreateSOOrderFilter.customerLocationID>(e.Cache, e.Row, !fixedCustomer && locationsFeature);
		}

		public virtual void _(Events.FieldUpdated<CreateSOOrderFilter.customerID> e)
		{
			if (e.Row == null || Base.Document.Current == null)
				return;

			e.Cache.SetDefaultExt<CreateSOOrderFilter.customerLocationID>(e.Row);
		}

		#endregion Actions

		#region CacheAttached
		[PXCustomizeBaseAttribute(typeof(PXDBStringAttribute), nameof(PXDBFieldAttribute.IsKey), false)]
		public virtual void _(Events.CacheAttached<DropShipLink.sOOrderType> e)
		{
		}

		[PXCustomizeBaseAttribute(typeof(PXDBStringAttribute), nameof(PXDBFieldAttribute.IsKey), false)]
		public virtual void _(Events.CacheAttached<DropShipLink.sOOrderNbr> e)
		{
		}

		[PXCustomizeBaseAttribute(typeof(PXDBIntAttribute), nameof(PXDBFieldAttribute.IsKey), false)]
		public virtual void _(Events.CacheAttached<DropShipLink.sOLineNbr> e)
		{
		}

		[PXCustomizeBaseAttribute(typeof(PXDBStringAttribute), nameof(PXDBFieldAttribute.IsKey), true)]
		public virtual void _(Events.CacheAttached<DropShipLink.pOOrderType> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXParent(typeof(DropShipLink.FK.POOrder))]
		[PXFormula(null, typeof(CountCalc<POOrder.dropShipLinkedLinesCount>))]
		[PXCustomizeBaseAttribute(typeof(PXDBStringAttribute), nameof(PXDBFieldAttribute.IsKey), true)]
		public virtual void _(Events.CacheAttached<DropShipLink.pOOrderNbr> e)
		{
		}

		[PXCustomizeBaseAttribute(typeof(PXDBIntAttribute), nameof(PXDBFieldAttribute.IsKey), true)]
		public virtual void _(Events.CacheAttached<DropShipLink.pOLineNbr> e)
		{
		}

		[PXUnboundFormula(typeof(Switch<Case<Where<POLine.lineType, In3<POLineType.goodsForDropShip, POLineType.nonStockForDropShip>, And<POLine.completed, Equal<False>>>, int1>, int0>), typeof(SumCalc<POOrder.dropShipLinesCount>))]
		[PXMergeAttributes(Method = MergeMethod.Append)]
		public virtual void _(Events.CacheAttached<POLine.lineType> e)
		{
		}

		[PXUnboundFormula(typeof(Switch<Case<Where<DropShipLink.active, Equal<True>, And<DropShipLink.poCompleted, Equal<False>>>, int1>, int0>), typeof(SumCalc<POOrder.dropShipActiveLinksCount>))]
		[PXMergeAttributes(Method = MergeMethod.Append)]
		public virtual void _(Events.CacheAttached<DropShipLink.active> e)
		{
		}

		#endregion

		#region POLine events

		public virtual void _(Events.FieldSelecting<POLine, POLine.sOOrderStatus> e)
		{
			DemandSOOrder demandOrder = GetDemandOrder(e.Row);

			if (e.Row != null && demandOrder != null && e.Row.SOOrderStatus != demandOrder.Status)
			{
				e.Row.SOOrderStatus = demandOrder.Status;
			}

			e.ReturnState = demandOrder?.Status;
		}

		public virtual void _(Events.FieldSelecting<POLine, POLine.sOOrderNbr> e)
		{
			DropShipLink link = GetDropShipLink(e.Row);

			if (e.Row != null && link != null && e.Row.SOOrderNbr != link.SOOrderNbr)
			{
				e.Row.SOOrderType = link.SOOrderType;
				e.Row.SOOrderNbr = link.SOOrderNbr;
			}

			e.ReturnState = link?.SOOrderNbr;
		}

		public virtual void _(Events.FieldSelecting<POLine, POLine.sOLineNbr> e)
		{
			DropShipLink link = GetDropShipLink(e.Row);

			if (e.Row != null && link != null && e.Row.SOLineNbr != link.SOLineNbr)
			{
				e.Row.SOLineNbr = link.SOLineNbr;
			}

			e.ReturnState = link?.SOLineNbr;
		}

		public virtual void _(Events.FieldSelecting<POLine, POLine.sOLinkActive> e)
		{
			DropShipLink link = GetDropShipLink(e.Row);

			if (e.Row != null && link != null && e.Row.SOLinkActive != link.Active)
			{
				e.Row.SOLinkActive = link.Active;
			}

			e.ReturnState = link?.Active;
		}

		public virtual void _(Events.FieldVerifying<POLine, POLine.inventoryID> e)
		{
			POOrder document = Base.Document.Current;
			if (document == null || document.IsLegacyDropShip == true || document.OrderType != POOrderType.DropShip
				|| e.Row == null || e.Row.LineType.IsNotIn(POLineType.GoodsForDropShip, POLineType.NonStockForDropShip))
				return;

			DropShipLink link = GetDropShipLink(e.Row);
			if (link != null && link.Active == true)
			{
				InventoryItem item = InventoryItem.PK.Find(Base, (int?)e.NewValue);
				e.NewValue = item?.InventoryCD;
				throw new PXSetPropertyException(Messages.DropShipPOLineHasActiveLink, link.SOOrderNbr);
			}

			var newItem = InventoryItem.PK.Find(Base, (int?)e.NewValue);
			if (newItem.StkItem != true && (newItem.NonStockReceipt != true || newItem.NonStockShip != true))
			{
				InventoryItem item = InventoryItem.PK.Find(Base, (int?)e.NewValue);
				e.NewValue = item?.InventoryCD;
				throw new PXSetPropertyException(SO.Messages.ReceiptShipmentRequiredForDropshipNonstock);
			}
		}

		public virtual void _(Events.FieldVerifying<POLine, POLine.siteID> e)
		{
			POOrder document = Base.Document.Current;
			if (document == null || document.IsLegacyDropShip == true || document.OrderType != POOrderType.DropShip
				|| e.Row == null || e.Row.LineType.IsNotIn(POLineType.GoodsForDropShip, POLineType.NonStockForDropShip))
				return;

			DropShipLink link = GetDropShipLink(e.Row);
			if (link != null && link.Active == true)
			{
				INSite newSite = INSite.PK.Find(Base, (int?)e.NewValue);
				e.NewValue = newSite?.SiteCD;
				throw new PXSetPropertyException(Messages.DropShipPOLineHasActiveLink, link.SOOrderNbr);
			}
		}

		public virtual void _(Events.FieldVerifying<POLine, POLine.orderQty> e)
		{
			POOrder document = Base.Document.Current;
			if (document == null || document.IsLegacyDropShip == true || document.OrderType != POOrderType.DropShip
				|| e.Row == null || e.Row.LineType.IsNotIn(POLineType.GoodsForDropShip, POLineType.NonStockForDropShip))
				return;

			DropShipLink link = GetDropShipLink(e.Row);
			if (link == null)
				return;

			if (link.Active == true)
			{
				throw new PXSetPropertyException(Messages.DropShipPOLineHasActiveLink, link.SOOrderNbr);
			}

			if (link.BaseReceivedQty > 0)
			{
				decimal? newBaseOrderQty = 0;
				if ((decimal?)e.NewValue > 0m)
					newBaseOrderQty = INUnitAttribute.ConvertToBase<POLine.inventoryID, POLine.uOM>(e.Cache, e.Row, (decimal)e.NewValue, INPrecision.QUANTITY);

				if (link.Active != true && newBaseOrderQty < link.BaseReceivedQty)
				{
					decimal minOrderQty = INUnitAttribute.ConvertFromBase<POLine.inventoryID, POLine.uOM>(e.Cache, e.Row, (decimal)link.BaseReceivedQty, INPrecision.QUANTITY);
					throw new PXSetPropertyException(CS.Messages.Entry_GE, minOrderQty);
				}
			}
		}

		public virtual void _(Events.FieldVerifying<POLine, POLine.uOM> e)
		{
			POOrder document = Base.Document.Current;
			if (document == null || document.IsLegacyDropShip == true || document.OrderType != POOrderType.DropShip
				|| e.Row == null || e.Row.LineType.IsNotIn(POLineType.GoodsForDropShip, POLineType.NonStockForDropShip))
				return;

			DropShipLink link = GetDropShipLink(e.Row);
			if (link != null && link.Active == true)
			{
				throw new PXSetPropertyException(Messages.DropShipPOLineHasActiveLink, link.SOOrderNbr);
			}
		}

		public virtual void _(Events.FieldVerifying<POLine, POLine.sOLinkActive> e)
		{
			POOrder document = Base.Document.Current;
			if (document == null || document.IsLegacyDropShip == true || document.OrderType != POOrderType.DropShip
				|| e.Row == null || e.Row.LineType.IsNotIn(POLineType.GoodsForDropShip, POLineType.NonStockForDropShip))
				return;

			DropShipLink link = GetDropShipLink(e.Row);
			if (link == null || link.Active == true || (bool?)e.NewValue != true)
				return;

			string firstMismatch = null;
			if (e.Row.InventoryID != link.SOInventoryID)
				firstMismatch = PXUIFieldAttribute.GetDisplayName<POLine.inventoryID>(e.Cache);
			else if (e.Row.SiteID != link.SOSiteID)
				firstMismatch = PXUIFieldAttribute.GetDisplayName<POLine.siteID>(e.Cache);
			else if (e.Row.BaseOrderQty != link.SOBaseOrderQty)
				firstMismatch = PXUIFieldAttribute.GetDisplayName<POLine.orderQty>(e.Cache);

			if (firstMismatch != null)
			{
				throw new PXSetPropertyException(Messages.DropShipPOLineValidationFailed, firstMismatch, link.SOOrderNbr);
			}
		}


		[PXOverride]
		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R1)]
		public virtual void ThrowErrorWhenPurchaseReceiptExists(POReceipt receipt, POOrder order, Action<POReceipt, POOrder> baseMethod)
		{
			baseMethod(receipt, order);
		}

		[PXOverride]
		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R1)]
		public virtual void ThrowErrorWhenPurchaseReceiptExists(POReceipt receipt, POLine line, Action<POReceipt, POLine> baseMethod)
		{
			baseMethod(receipt, line);
		}

		public virtual void _(Events.RowUpdated<POLine> e)
		{
			POOrder document = Base.Document.Current;
			if (document == null || document.IsLegacyDropShip == true || document.OrderType != POOrderType.DropShip
				|| e.Row.LineType.IsNotIn(POLineType.GoodsForDropShip, POLineType.NonStockForDropShip))
				return;

			DropShipLink link = GetDropShipLink(e.Row);
			if (link != null && e.Row.Cancelled == true)
			{
				DropShipLinks.Delete(link);
				return;
			}

			if (link != null && !e.Cache.ObjectsEqual<POLine.sOLinkActive, POLine.inventoryID, POLine.siteID, POLine.baseOrderQty, POLine.completed>(e.Row, e.OldRow))
			{
				link.Active = e.Row.SOLinkActive;
				link.POInventoryID = e.Row.InventoryID;
				link.POSiteID = e.Row.SiteID;
				link.POBaseOrderQty = e.Row.BaseOrderQty;
				link.POCompleted = e.Row.Completed;
				DropShipLinks.Update(link);

				if (e.Row.SOLinkActive != e.OldRow.SOLinkActive)
				{
					// We should cleanup warings.
					Base.Transactions.View.RequestRefresh();
				}
			}
		}

		public virtual void _(Events.RowDeleting<POLine> e)
		{
			POOrder document = Base.Document.Current;
			if (document == null || document.IsLegacyDropShip == true || document.OrderType != POOrderType.DropShip
				|| e.Row == null || e.Row.LineType.IsNotIn(POLineType.GoodsForDropShip, POLineType.NonStockForDropShip))
				return;

			DropShipLink link = GetDropShipLink(e.Row);
			if (link?.Active == true && Base.Document.Cache.GetStatus(document).IsNotIn(PXEntryStatus.Deleted, PXEntryStatus.InsertedDeleted))
			{
				e.Cancel = true;
				throw new PXException(Messages.DropShipPOLineHasLinkAndCantBeDeleted, link.SOOrderNbr);
			}
		}

		public virtual void _(Events.RowSelected<POLine> e)
		{
			POOrder document = Base.Document.Current;
			if (document == null || document.IsLegacyDropShip == true || document.OrderType != POOrderType.DropShip
				|| e.Row == null || e.Row.LineType.IsNotIn(POLineType.GoodsForDropShip, POLineType.NonStockForDropShip))
				return;

			PXUIFieldAttribute.SetEnabled<POLine.sOOrderNbr>(e.Cache, e.Row, false);
			PXUIFieldAttribute.SetEnabled<POLine.sOOrderStatus>(e.Cache, e.Row, false);
			PXUIFieldAttribute.SetEnabled<POLine.sOLineNbr>(e.Cache, e.Row, false);

			DropShipLink link = GetDropShipLink(e.Row);

			bool fullQtyReceived = link != null && link.Active == true && link.BaseReceivedQty == link.POBaseOrderQty;
			PXUIFieldAttribute.SetEnabled<POLine.sOLinkActive>(e.Cache, e.Row, link != null && !fullQtyReceived && document.Hold == true);

			if (PXUIFieldAttribute.GetErrorOnly<POLine.sOLinkActive>(e.Cache, e.Row) != null)
				return;

			if (link != null && link.SOCompleted == true && e.Row.Completed != true && link.BaseReceivedQty < link.SOBaseOrderQty)
			{
				e.Cache.RaiseExceptionHandling<POLine.sOLinkActive>(e.Row, e.Row.SOLinkActive,
					new PXSetPropertyException<POLine.sOLinkActive>(Messages.DropShipLinkedSOLineCompleted, PXErrorLevel.Warning, link.SOOrderNbr));
			}
			else if (link?.Active != true && e.Row.Completed != true)
			{
				e.Cache.RaiseExceptionHandling<POLine.sOLinkActive>(e.Row, e.Row.SOLinkActive,
					new PXSetPropertyException<POLine.sOLinkActive>(Messages.DropShipSOLineNoLink, PXErrorLevel.Warning));
			}
			else
			{
				e.Cache.RaiseExceptionHandling<POLine.sOLinkActive>(e.Row, e.Row.SOLinkActive, null);
			}
		}

		#endregion POLine events

		#region Other events

		/// <summary>
		/// Overrides <see cref="POOrderEntry.POOrder_RowDeleting(PXCache, PXRowDeletingEventArgs)"/>
		/// </summary>
		[PXOverride]
		public virtual void POOrder_RowDeleting(PXCache sender, PXRowDeletingEventArgs e, PXRowDeleting baseMethod)
		{
			baseMethod(sender, e);

			POOrder doc = (POOrder)e.Row;
			if (doc.OrderType != POOrderType.DropShip || doc.IsLegacyDropShip == true || doc.SOOrderNbr == null)
				return;

			string message = PXMessages.LocalizeFormatNoPrefixNLA(Messages.DropShipPOOrderDeletionConfirmation, doc.OrderNbr);
			if (Base.Document.View.Ask(message, MessageButtons.OKCancel) == WebDialogResult.Cancel)
			{
				e.Cancel = true;
				return;
			}
		}

		protected virtual void _(Events.RowSelecting<POOrder> e)
		{
			if (e.Row?.OrderType == POOrderType.DropShip && e.Row.IsLegacyDropShip != true && e.Row.Cancelled == true)
			{
				using (new PXConnectionScope())
				{
					POOrder successorOrder = PXSelectReadonly<POOrder,
						Where<POOrder.originalPOType, Equal<Current<POOrder.orderType>>,
							And<POOrder.originalPONbr, Equal<Current<POOrder.orderNbr>>>>>
						.SelectSingleBound(Base, new[] { e.Row });
					e.Row.SuccessorPONbr = successorOrder?.OrderNbr;
				}
			}
		}

		protected virtual void _(Events.RowSelected<POOrder> e)
		{
			if (e.Row == null)
				return;

			PXUIFieldAttribute.SetVisible<POOrder.originalPONbr>(e.Cache, e.Row, e.Row.OrderType == POOrderType.RegularOrder);
			PXUIFieldAttribute.SetVisible<POOrder.successorPONbr>(e.Cache, e.Row, e.Row.OrderType == POOrderType.DropShip);

			if (e.Row.OrderType != POOrderType.DropShip || Base.BlockUIUpdate)
				return;

			bool newCreateSOAvail = e.Row.IsLegacyDropShip != true && e.Row.Status == POOrderStatus.AwaitingLink;
			bool legacyCreateSOAvail = e.Row.IsLegacyDropShip == true && e.Row.Status.IsIn(POOrderStatus.Open, POOrderStatus.Completed, POOrderStatus.Closed);
			createSalesOrder.SetEnabled(newCreateSOAvail || legacyCreateSOAvail);

			PXUIFieldAttribute.SetEnabled<POOrder.sOOrderType>(e.Cache, e.Row, e.Row.IsLegacyDropShip == true);
			PXUIFieldAttribute.SetEnabled<POOrder.sOOrderNbr>(e.Cache, e.Row, e.Row.IsLegacyDropShip == true);

			if (e.Row.Cancelled != true && e.Row.Status.IsNotIn(POOrderStatus.Completed, POOrderStatus.Closed))
			{
				PXException expectedDateWarn = null;
				bool hasSOLink = !string.IsNullOrEmpty(e.Row.SOOrderType) && !string.IsNullOrEmpty(e.Row.SOOrderNbr);
				if (hasSOLink)
				{
					var soOrder = (SO.SOOrder)PXSelectorAttribute.Select<POOrder.sOOrderNbr>(e.Cache, e.Row);
					if (soOrder != null && soOrder.RequestDate < e.Row.ExpectedDate)
					{
						expectedDateWarn = new PXSetPropertyException(Messages.SORequestedDateEarlier, PXErrorLevel.Warning,
							soOrder.OrderNbr, soOrder.RequestDate?.ToShortDateString());
					}
				}
				e.Cache.RaiseExceptionHandling<POOrder.expectedDate>(e.Row, e.Row.ExpectedDate, expectedDateWarn);
			}
		}

		/// <summary>
		/// Overrides <see cref="POOrderEntry.POOrder_Cancelled_FieldVerifying(PXCache, PXFieldVerifyingEventArgs)"/>
		/// </summary>
		[PXOverride]
		public virtual void POOrder_Cancelled_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e, PXFieldVerifying baseMethod)
		{
			baseMethod(sender, e);

			POOrder order = (POOrder)e.Row;
			if (order == null || order.OrderType != POOrderType.DropShip || order.IsLegacyDropShip == true)
				return;

			if ((bool?)e.NewValue != true && order.SuccessorPONbr != null)
			{
				throw new PXException(Messages.DSConvertToNormalRepoen, order.OrderNbr, order.SuccessorPONbr);
			}
			else if ((bool?)e.NewValue == true && order.SOOrderNbr != null)
			{
				string message = PXMessages.LocalizeFormatNoPrefixNLA(Messages.DropShipPOOrderCancelationConfirmation, order.OrderNbr);
				if (Base.Document.View.Ask("LinkedDSOrderCancellAsk", message, MessageButtons.OKCancel) == WebDialogResult.Cancel)
				{
					e.Cancel = true;
					return;
				}
			}
		}

		public virtual void _(Events.FieldUpdated<POOrder, POOrder.dropShipLinkedLinesCount> e)
		{
			int? oldValue = (int?)e.OldValue;
			if (e.Row == null || e.Row.DropShipLinkedLinesCount == oldValue || e.Row.OrderType != POOrderType.DropShip || e.Row.IsLegacyDropShip == true)
				return;

			if (e.Row.DropShipLinkedLinesCount == 0)
			{
				Base.Document.Cache.SetValue<POOrder.sOOrderType>(e.Row, null);
				Base.Document.Cache.SetValue<POOrder.sOOrderNbr>(e.Row, null);
			}
		}

		public virtual void _(Events.RowUpdated<POOrder> e)
		{
			if (e.Row.OrderType != POOrderType.DropShip || e.Row.IsLegacyDropShip == true)
				return;

			if (!Base.Caches<POOrder>().ObjectsEqual<POOrder.dropShipLinesCount, POOrder.dropShipActiveLinksCount>(e.Row, e.OldRow))
			{
				UpdateDocumentState(e.Row);
			}
		}

		public virtual void _(Events.RowDeleted<DropShipLink> e)
		{
			// TODO: move to graph persisting.
			SOLineSplit3 split = new PXSelect<SOLineSplit3,
				Where<SOLineSplit3.orderType, Equal<Required<DropShipLink.sOOrderType>>,
					And<SOLineSplit3.orderNbr, Equal<Required<DropShipLink.sOOrderNbr>>,
					And<SOLineSplit3.lineNbr, Equal<Required<DropShipLink.sOLineNbr>>>>>>(Base)
				.SelectWindowed(0, 1, e.Row.SOOrderType, e.Row.SOOrderNbr, e.Row.SOLineNbr);

			split.POType = null;
			split.PONbr = null;
			split.POLineNbr = null;
			split.RefNoteID = null;

			Base.UpdateSOLine(split, split.VendorID, false);
			Base.FixedDemand.Update(split);

			INItemPlan plan = PXSelect<INItemPlan, Where<INItemPlan.planID, Equal<Required<INItemPlan.planID>>>>.Select(Base, split.PlanID);
			if (plan != null)
			{
				plan.SupplyPlanID = null;
				Base.Caches[typeof(INItemPlan)].Update(plan);
			}
		}

		#endregion Other events

		protected bool prefetched = false;

		[PXOverride]
		public virtual void PrefetchWithDetails()
		{
			if (Base.Document.Current == null || prefetched || DropShipLinks.Cache.IsDirty)
				return;

			var linesWithLinksQuery = new PXSelectReadonly2<POLine,
				LeftJoin<DropShipLink, On<DropShipLink.FK.POLine>,
				LeftJoin<DemandSOOrder, On<DropShipLink.FK.DemandSOOrder>>>,
				Where<POLine.orderType, Equal<Current<POOrder.orderType>>,
					And<POLine.orderNbr, Equal<Current<POOrder.orderNbr>>>>>(Base);

			var fieldsAndTables = new[]
			{
				typeof(POLine.orderType), typeof(POLine.orderNbr), typeof(POLine.lineNbr), typeof(DropShipLink), typeof(DemandSOOrder)
			};
			using (new PXFieldScope(linesWithLinksQuery.View, fieldsAndTables))
			{
				int startRow = PXView.StartRow;
				int totalRows = 0;
				foreach (PXResult<POLine, DropShipLink, DemandSOOrder> record in linesWithLinksQuery.View.Select(
					PXView.Currents, PXView.Parameters, PXView.Searches, PXView.SortColumns,
					PXView.Descendings, PXView.Filters, ref startRow, PXView.MaximumRows, ref totalRows))
				{
					POLine line = record;
					DropShipLink link = record;
					DemandSOOrder demandOrder = record;

					DropShipLinkStoreCached(link, line);

					if (demandOrder?.OrderNbr != null)
					{
						DemandOrderStoreCached(demandOrder);
					}
				}
			}

			prefetched = true;
		}

		public virtual void DropShipLinkStoreCached(DropShipLink link, POLine line)
		{
			if (!(DropShipLinks.Cache.Locate(link) is DropShipLink cached) || DropShipLinks.Cache.GetStatus(cached) == PXEntryStatus.Notchanged)
			{
				DropShipLinks.Cache.Hold(link);
			}
		}

		public virtual DropShipLink GetDropShipLink(POLine line)
		{
			if (line == null || line.LineType.IsNotIn(POLineType.GoodsForDropShip, POLineType.NonStockForDropShip))
				return null;

			return DropShipLinks.SelectWindowed(0, 1, line.OrderType, line.OrderNbr, line.LineNbr);
		}

		public virtual void DemandOrderStoreCached(DemandSOOrder order)
		{
			if (!(DemandSOOrders.Cache.Locate(order) is DemandSOOrder cached) || DemandSOOrders.Cache.GetStatus(cached) == PXEntryStatus.Notchanged)
			{
				DemandSOOrders.Cache.Hold(order);
			}
		}

		public virtual DemandSOOrder GetDemandOrder(POLine line)
		{
			if (line == null || line.LineType.IsNotIn(POLineType.GoodsForDropShip, POLineType.NonStockForDropShip))
				return null;

			DropShipLink link = GetDropShipLink(line);
			if (link == null)
				return null;

			return DemandSOOrders.SelectWindowed(0, 1, link.SOOrderType, link.SOOrderNbr);
		}

		public virtual void UpdateDocumentState(POOrder order)
		{
			Base.Document.Search<POOrder.orderNbr>(order.OrderNbr, order.OrderType);
			order = Base.Document.Current;

			if (order.OrderType != POOrderType.DropShip || order.IsLegacyDropShip == true || order.Hold == true || order.Approved != true)
				return;

			bool hasAllLinesLinked = order.DropShipLinesCount == 0 || order.DropShipActiveLinksCount == order.DropShipLinesCount;
			if (hasAllLinesLinked)
			{
				POOrder.Events
					.Select(e => e.LinesLinked)
					.FireOn(Base, order);
			}
			else if (order.DropShipLinesCount > 0)
			{
				POOrder.Events
					.Select(e => e.LinesUnlinked)
					.FireOn(Base, order);
			}
		}
	}
}
