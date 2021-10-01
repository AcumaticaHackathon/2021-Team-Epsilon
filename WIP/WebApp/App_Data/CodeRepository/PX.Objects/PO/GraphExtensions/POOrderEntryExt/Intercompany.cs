using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CM;
using PX.Objects.Common;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.GL;
using PX.Objects.SO;

namespace PX.Objects.PO.GraphExtensions.POOrderEntryExt
{
	public class Intercompany : PXGraphExtension<POOrderEntry>
	{
		public static bool IsActive()
			=> PXAccess.FeatureInstalled<FeaturesSet.interBranch>()
			&& PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();

		public PXAction<POOrder> generateSalesOrder;
		[PXUIField(DisplayName = "Generate Sales Order", MapViewRights = PXCacheRights.Select, MapEnableRights = PXCacheRights.Select)]
		[PXButton]
		protected virtual IEnumerable GenerateSalesOrder(PXAdapter adapter)
		{
			Base.Save.Press();

			POOrder po = Base.Document.Current;
			List<POLine> lines = Base.Transactions.Select().RowCast<POLine>().ToList();

			bool copyProjectDetails = true;
			bool generateSalesOrder = true;
			ValidateProject(lines, out copyProjectDetails, out generateSalesOrder);

			if (generateSalesOrder)
			{
				POShipAddress shipAddress = Base.Shipping_Address.Select();
				POShipContact shipContact = Base.Shipping_Contact.Select();
				List<POOrderDiscountDetail> discountLines = Base.DiscountDetails.Select().RowCast<POOrderDiscountDetail>().ToList();

				PXLongOperation.StartOperation(Base, () =>
				{
					var baseGraph = PXGraph.CreateInstance<POOrderEntry>();
					var ext = baseGraph.GetExtension<Intercompany>();
					ext.GenerateIntercompanySalesOrder(po, shipAddress, shipContact, lines, discountLines, null, copyProjectDetails);
				});
			}

			yield return po;
		}

		public virtual void ValidateProject(List<POLine> lines, out bool copyProjectDetails, out bool generateSalesOrder)
		{
			copyProjectDetails = true;
			generateSalesOrder = true;

			if (PXAccess.FeatureInstalled<FeaturesSet.projectModule>())
			{
				bool allLinesHaveTheSameProjectID = true;
				bool hasNonEmptyProject = false;

				int? nonProjectID = PM.ProjectDefaultAttribute.NonProject();
				int? prevProjectID = null;
				foreach (POLine line in lines)
				{
					if (prevProjectID == null)
						prevProjectID = line.ProjectID;

					if (line.ProjectID != nonProjectID)
						hasNonEmptyProject = true;

					if (prevProjectID != line.ProjectID)
					{
						allLinesHaveTheSameProjectID = false;
						break;
					}
				}

				if (hasNonEmptyProject)
				{
					if (allLinesHaveTheSameProjectID)
					{
						if (Base.Document.Ask(Messages.Warning, Messages.CopyProjectDetailsToSalesOrder, MessageButtons.YesNo) != WebDialogResult.Yes)
							copyProjectDetails = false;
					}
					else
					{
						if (Base.Document.Ask(Messages.Warning, Messages.CopyProjectDetailsToSalesOrderNonProjectCode, MessageButtons.YesNo) == WebDialogResult.Yes)
							copyProjectDetails = false;
						else
							generateSalesOrder = false;
					}
				}
			}
		}

		public virtual SOOrder GenerateIntercompanySalesOrder(
			POOrder po,
			POShipAddress shipAddress, POShipContact shipContact,
			IEnumerable<POLine> lines,
			IEnumerable<POOrderDiscountDetail> discountLines,
			string orderType,
			bool copyProjectDetails)
		{
			if (!string.IsNullOrEmpty(po.IntercompanySONbr)
				|| po.OrderType != POOrderType.RegularOrder)
			{
				throw new PXInvalidOperationException();
			}
			Branch customerBranch = Branch.PK.Find(Base, po.BranchID);
			Customer customer = Customer.PK.Find(Base, customerBranch?.BAccountID);
			if (customer == null)
			{
				throw new PXException(Messages.BranchIsNotExtendedToCustomer, customerBranch?.BranchCD.TrimEnd());
			}
			var vendorBranch = PXAccess.GetBranchByBAccountID(po.VendorID);

			var graph = PXGraph.CreateInstance<SOOrderEntry>();
			orderType = orderType ?? graph.sosetup.Current.DfltIntercompanyOrderType;
			bool hold = false;
			if (PXAccess.FeatureInstalled<FeaturesSet.approvalWorkflow>())
			{
				SOSetupApproval setupApproval = graph.SetupApproval.Select(orderType);
				hold = (setupApproval?.IsActive == true);
			}
			var doc = new SOOrder
			{
				OrderType = orderType,
				BranchID = vendorBranch.BranchID,
				Hold = hold,
			};
			doc = PXCache<SOOrder>.CreateCopy(graph.Document.Insert(doc));
			doc.CustomerID = customer.BAccountID;
			doc.ProjectID = (copyProjectDetails && lines.Any()) ? lines.First().ProjectID : PM.ProjectDefaultAttribute.NonProject();
			doc.IntercompanyPOType = po.OrderType;
			doc.IntercompanyPONbr = po.OrderNbr;
			doc.IntercompanyPOWithEmptyInventory = lines.Any(pol => pol.InventoryID == null && pol.LineType != POLineType.Description);
			doc.ShipSeparately = true;
			doc = PXCache<SOOrder>.CreateCopy(graph.Document.Update(doc));
			doc.OrderDate = po.OrderDate;
			doc.RequestDate = po.ExpectedDate;
			doc.CustomerOrderNbr = po.OrderNbr;
			doc = PXCache<SOOrder>.CreateCopy(graph.Document.Update(doc));
			doc.DisableAutomaticDiscountCalculation = true;
			doc = PXCache<SOOrder>.CreateCopy(graph.Document.Update(doc));

			AddressAttribute.CopyRecord<SOOrder.shipAddressID>(graph.Document.Cache, doc, shipAddress, true);
			ContactAttribute.CopyRecord<SOOrder.shipContactID>(graph.Document.Cache, doc, shipContact, true);
			doc = PXCache<SOOrder>.CreateCopy(graph.Document.Update(doc));

			doc.CuryID = po.CuryID;
			doc = PXCache<SOOrder>.CreateCopy(graph.Document.Update(doc));
			CurrencyInfo origCuryInfo = CurrencyInfo.PK.Find(graph, po.CuryInfoID);
			CurrencyInfo curyInfo = graph.currencyinfo.Select();
			PXCache<CurrencyInfo>.RestoreCopy(curyInfo, origCuryInfo);
			curyInfo.CuryInfoID = doc.CuryInfoID;

			foreach (POLine line in lines.Where(pol => pol.InventoryID != null))
			{
				var soLine = new SOLine
				{
					BranchID = vendorBranch.BranchID,
				};
				soLine = PXCache<SOLine>.CreateCopy(graph.Transactions.Insert(soLine));
				soLine.InventoryID = line.InventoryID;
				soLine.SubItemID = line.SubItemID;
				soLine.RequestDate = line.PromisedDate;
				soLine.TaxCategoryID = line.TaxCategoryID;
				soLine.TaskID = copyProjectDetails ? line.TaskID : null;
				soLine.CostCodeID = copyProjectDetails ? line.CostCodeID : null;
				soLine.IntercompanyPOLineNbr = line.LineNbr;
				soLine = PXCache<SOLine>.CreateCopy(graph.Transactions.Update(soLine));
				soLine.TranDesc = line.TranDesc;
				soLine.UOM = line.UOM;
				soLine.ManualPrice = true;
				soLine = PXCache<SOLine>.CreateCopy(graph.Transactions.Update(soLine));
				soLine.OrderQty = line.OrderQty;
				soLine.CuryUnitPrice = line.CuryUnitCost;
				soLine = PXCache<SOLine>.CreateCopy(graph.Transactions.Update(soLine));
				soLine.CuryExtPrice = line.CuryLineAmt;
				soLine = PXCache<SOLine>.CreateCopy(graph.Transactions.Update(soLine));
				soLine.DiscPct = line.DiscPct;
				soLine = PXCache<SOLine>.CreateCopy(graph.Transactions.Update(soLine));
				soLine.CuryDiscAmt = line.CuryDiscAmt;
				soLine = graph.Transactions.Update(soLine);
			}

			if (PXAccess.FeatureInstalled<FeaturesSet.customerDiscounts>())
			{
				foreach (POOrderDiscountDetail discountLine in discountLines)
				{
					SOOrderDiscountDetail soDiscLine = new SOOrderDiscountDetail
					{
						IsManual = true,
					};
					soDiscLine = PXCache<SOOrderDiscountDetail>.CreateCopy(graph.DiscountDetails.Insert(soDiscLine));
					soDiscLine.CuryDiscountAmt = discountLine.CuryDiscountAmt;
					soDiscLine.Description = discountLine.Description;
					soDiscLine.CuryDiscountableAmt = discountLine.CuryDiscountableAmt;
					soDiscLine.DiscountableQty = discountLine.DiscountableQty;
					soDiscLine = graph.DiscountDetails.Update(soDiscLine);
				}
			}

			graph.RowPersisted.AddHandler<SOOrder>(UpdatePOOrderOnSOOrderRowPersisted);
			var uniquenessChecker = new UniquenessChecker<
				SelectFrom<SOOrder>
				.Where<SOOrder.FK.IntercompanyPOOrder.SameAsCurrent>>(po);
			graph.OnBeforeCommit += uniquenessChecker.OnBeforeCommitImpl;
			try
			{
				graph.Save.Press();
			}
			finally
			{
				graph.RowPersisted.RemoveHandler<SOOrder>(UpdatePOOrderOnSOOrderRowPersisted);
				graph.OnBeforeCommit -= uniquenessChecker.OnBeforeCommitImpl;
			}

			return graph.Document.Current;
		}

		protected virtual void UpdatePOOrderOnSOOrderRowPersisted(PXCache sender, PXRowPersistedEventArgs e)
		{
			SOOrder doc = (SOOrder)e.Row;
			if (!string.IsNullOrEmpty(doc?.IntercompanyPONbr)
				&& e.Operation == PXDBOperation.Insert && e.TranStatus == PXTranStatus.Open)
			{
				POOrder po = SelectFrom<POOrder>
					.Where<POOrder.orderType.IsEqual<SOOrder.intercompanyPOType.FromCurrent>
						.And<POOrder.orderNbr.IsEqual<SOOrder.intercompanyPONbr.FromCurrent>>>
					.View.SelectSingleBound(sender.Graph, new[] { doc });
				po.VendorRefNbr = doc.OrderNbr;
				po.IsIntercompanySOCreated = true;
				po = (POOrder)sender.Graph.Caches[typeof(POOrder)].Update(po);
			}
		}

		protected virtual void _(Events.RowSelecting<POOrder> eventArgs)
		{
			if (eventArgs.Row == null)
				return;

			if (eventArgs.Row.IsIntercompany == true)
			{
				using (new PXConnectionScope())
				using (new PXReadBranchRestrictedScope())
				{
					SOOrder intercompanySO =
						SelectFrom<SOOrder>
							.Where<SOOrder.FK.IntercompanyPOOrder.SameAsCurrent>
						.View.SelectSingleBound(Base, new[] { eventArgs.Row });
					eventArgs.Row.IntercompanySOType = intercompanySO?.OrderType;
					eventArgs.Row.IntercompanySONbr = intercompanySO?.OrderNbr;
					eventArgs.Row.IntercompanySOCancelled = intercompanySO?.Cancelled;
					eventArgs.Row.IntercompanySOWithEmptyInventory = intercompanySO?.IntercompanyPOWithEmptyInventory;
				}
			}
		}

		protected virtual void _(Events.RowSelected<POOrder> eventArgs)
		{
			if (eventArgs.Row == null)
				return;

			eventArgs.Cache.Adjust<PXUIFieldAttribute>(eventArgs.Row)
				.For<POOrder.intercompanySOType>(a =>
				{
					a.Visible = (eventArgs.Row.IsIntercompany == true);
					a.Enabled = false;
				})
				.SameFor<POOrder.intercompanySONbr>()
				.For<POOrder.excludeFromIntercompanyProc>(a =>
					a.Visible = (eventArgs.Row.IsIntercompany == true));

			if (eventArgs.Row.IsIntercompany == true && eventArgs.Row.IntercompanySONbr != null && eventArgs.Row.IntercompanySOCancelled == true)
			{
				Base.Document.Cache.RaiseExceptionHandling<POOrder.intercompanySONbr>(eventArgs.Row, eventArgs.Row.IntercompanySONbr,
					new PXSetPropertyException(Messages.IntercompanySOCancelled, PXErrorLevel.Warning, eventArgs.Row.IntercompanySONbr));
			}
			else if (eventArgs.Row.IsIntercompanySOCreated == true && eventArgs.Row.IntercompanySONbr == null)
			{
				Base.Document.Cache.RaiseExceptionHandling<POOrder.intercompanySONbr>(eventArgs.Row, eventArgs.Row.IntercompanySONbr,
					new PXSetPropertyException(Messages.RelatedSalesOrderDeleted, PXErrorLevel.Warning));
			}
			else if (eventArgs.Row.IntercompanySOWithEmptyInventory == true)
			{
				Base.Document.Cache.RaiseExceptionHandling<POOrder.intercompanySONbr>(eventArgs.Row, eventArgs.Row.IntercompanySONbr,
					new PXSetPropertyException(Messages.IntercompanySOEmptyInventory, PXErrorLevel.Warning));
			}
		}

		protected virtual void POOrder_Cancelled_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e,
			PXFieldVerifying baseFunc)
		{
			POOrder row = (POOrder)e.Row;
			if (row != null && row.IsIntercompany == true && row.IntercompanySONbr != null && (bool?)e.NewValue == true)
			{
				SOOrder intercompanySO;
				using (new PXReadBranchRestrictedScope())
				{
					intercompanySO =
						SelectFrom<SOOrder>
							.Where<SOOrder.FK.IntercompanyPOOrder.SameAsCurrent>
						.View.SelectSingleBound(Base, new[] { e.Row });
				}
				if (intercompanySO != null && intercompanySO.ShipmentCntr != 0)
				{
					throw new PXException(Messages.IntercompanyPOCannotBeCancelled, row.IntercompanySONbr);
				}
			}

			baseFunc(sender, e);
		}

		protected virtual void _(Events.RowDeleting<POOrder> eventArgs)
		{
			if (eventArgs.Row.IsIntercompany == true && !string.IsNullOrEmpty(eventArgs.Row.IntercompanySONbr))
			{
				throw new PXException(Messages.IntercompanyPOCannotBeDeleted, eventArgs.Row.IntercompanySONbr);
			}
		}

		public delegate void PersistDelegate();
		[PXOverride]
		public virtual void Persist(PersistDelegate baseMethod)
		{
			if (Base.Document.Current?.IsIntercompany == true && !string.IsNullOrEmpty(Base.Document.Current.IntercompanySONbr))
			{
				bool intercompanyPOCancelled = Base.Document.Current?.Cancelled == true && 
					(bool?)Base.Document.Cache.GetValueOriginal<POOrder.cancelled>(Base.Document.Current) != true;
				if (intercompanyPOCancelled)
				{
					using (new PXReadBranchRestrictedScope())
					{
						SOOrder intercompanySO =
							SelectFrom<SOOrder>
								.Where<SOOrder.FK.IntercompanyPOOrder.SameAsCurrent>
							.View.Select(Base);
						if (intercompanySO?.ShipmentCntr == 0 && intercompanySO.Cancelled != true)
						{
							var soOrderGraph = PXGraph.CreateInstance<SOOrderEntry>();
							soOrderGraph.Document.Current = intercompanySO;
							using (var tranScope = new PXTransactionScope())
							{
								soOrderGraph.cancelOrder.Press();
								baseMethod();
								tranScope.Complete();
								return;
							}
						}
					}
				}
			}
			baseMethod();
		}
	}
}
