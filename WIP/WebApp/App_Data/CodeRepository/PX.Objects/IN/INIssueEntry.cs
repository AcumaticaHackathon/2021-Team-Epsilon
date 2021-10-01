using System;
using System.Collections;
using System.Collections.Generic;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

using PX.Objects.CS;
using PX.Objects.GL;

namespace PX.Objects.IN
{
	public class INIssueEntry : INRegisterEntryBase
	{
		#region Views
		public
			PXSelect<INRegister,
			Where<INRegister.docType, Equal<INDocType.issue>>>
			issue;

		public
			PXSelect<INRegister,
			Where<
				INRegister.docType, Equal<INDocType.issue>,
				And<INRegister.refNbr, Equal<Current<INRegister.refNbr>>>>>
			CurrentDocument;

		[PXImport(typeof(INRegister))]
		public
			PXSelect<INTran,
			Where<
				INTran.docType, Equal<INDocType.issue>,
				And<INTran.refNbr, Equal<Current<INRegister.refNbr>>>>>
			transactions;

		[PXCopyPasteHiddenView]
		public
			PXSelect<INTranSplit,
			Where<
				INTranSplit.docType, Equal<INDocType.issue>,
				And<INTranSplit.refNbr, Equal<Current<INTran.refNbr>>,
				And<INTranSplit.lineNbr, Equal<Current<INTran.lineNbr>>>>>>
			splits;

		public LSINTran lsselect;
		#endregion

		#region DAC overrides
		#region INTran
		[PXDefault(typeof(SelectFrom<InventoryItem>.Where<InventoryItem.inventoryID.IsEqual<INTran.inventoryID.FromCurrent>>), SourceField = typeof(InventoryItem.salesUnit), CacheGlobal = true)]
		[INUnit(typeof(INTran.inventoryID))]
		protected virtual void _(Events.CacheAttached<INTran.uOM> e) { }


		[LocationAvail(typeof(INTran.inventoryID), typeof(INTran.subItemID), typeof(INTran.siteID),
			IsSalesType: typeof(Where<
				INTran.tranType,Equal<INTranType.invoice>,
				Or<INTran.tranType, Equal<INTranType.debitMemo>,
				Or<INTran.origModule, NotEqual<BatchModule.modulePO>,
				And<INTran.tranType, Equal<INTranType.issue>>>>>),
			IsReceiptType: typeof(Where<
				INTran.tranType, Equal<INTranType.receipt>,
				Or<INTran.tranType, Equal<INTranType.return_>,
				Or<INTran.tranType, Equal<INTranType.creditMemo>,
				Or<INTran.origModule, Equal<BatchModule.modulePO>,
				And<INTran.tranType, Equal<INTranType.issue>>>>>>),
			IsTransferType: typeof(Where<
				INTran.tranType, Equal<INTranType.transfer>,
				And<INTran.invtMult, Equal<short1>,
				Or<INTran.tranType, Equal<INTranType.transfer>,
				And<INTran.invtMult, Equal<shortMinus1>>>>>))]
		protected virtual void _(Events.CacheAttached<INTran.locationID> e) { }

		[PXMergeAttributes]
		[PXRemoveBaseAttribute(typeof(PXRestrictorAttribute))]
		[PXRestrictor(typeof(Where<ReasonCode.usage, Equal<Optional<INTran.docType>>,
			Or<ReasonCode.usage, Equal<ReasonCodeUsages.vendorReturn>, And<Optional<INTran.origModule>, Equal<BatchModule.modulePO>>>>),
			Messages.ReasonCodeDoesNotMatch)]
		protected virtual void _(Events.CacheAttached<INTran.reasonCode> e) { }
		#endregion

		#region INTranSplit
		[LocationAvail(typeof(INTranSplit.inventoryID), typeof(INTranSplit.subItemID), typeof(INTranSplit.siteID),
			IsSalesType: typeof(Where<
				INTranSplit.tranType, Equal<INTranType.invoice>,
				Or<INTranSplit.tranType, Equal<INTranType.debitMemo>,
				Or<INTranSplit.origModule, NotEqual<BatchModule.modulePO>,
				And<INTranSplit.tranType, Equal<INTranType.issue>>>>>),
			IsReceiptType: typeof(Where<
				INTranSplit.tranType, Equal<INTranType.receipt>,
				Or<INTranSplit.tranType, Equal<INTranType.return_>,
				Or<INTranSplit.tranType, Equal<INTranType.creditMemo>,
				Or<INTranSplit.origModule, Equal<BatchModule.modulePO>,
				And<INTranSplit.tranType, Equal<INTranType.issue>>>>>>),
			IsTransferType: typeof(Where<
				INTranSplit.tranType, Equal<INTranType.transfer>,
				And<INTranSplit.invtMult, Equal<short1>,
				Or<INTranSplit.tranType, Equal<INTranType.transfer>,
				And<INTranSplit.invtMult, Equal<shortMinus1>>>>>))]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void _(Events.CacheAttached<INTranSplit.locationID> e) { }

		[PXString(2)]
		[PXFormula(typeof(Parent<INRegister.origModule>))]
		protected virtual void _(Events.CacheAttached<INTranSplit.origModule> e) { }
		#endregion
		#endregion

		#region Initialization
		public INIssueEntry()
		{
			if (PXAccess.FeatureInstalled<FeaturesSet.inventory>())
			{
				INSetup record = insetup.Current;
			}

			PXStringListAttribute.SetList<INTran.tranType>(transactions.Cache, null, new INTranType.IssueListAttribute().AllowedValues, new INTranType.IssueListAttribute().AllowedLabels);

			this.FieldDefaulting.AddHandler<IN.Overrides.INDocumentRelease.SiteStatus.negAvailQty>((sender, e) =>
			{
				if (!e.Cancel)
					e.NewValue = true;
				e.Cancel = true;
			});
		}

		public override void InitCacheMapping(Dictionary<Type, Type> map)
		{
			base.InitCacheMapping(map);
			// override INLotSerialStatus cache to allow INTransitLineLotSerialStatus with projection work currectly with LSSelect<TLSMaster, TLSDetail, Where>
			Caches.AddCacheMapping(typeof(INLotSerialStatus), typeof(INLotSerialStatus));
		}
		#endregion

		#region Event Handlers
		#region INRegister
		protected virtual void _(Events.FieldDefaulting<INRegister, INRegister.docType> e) => e.NewValue = INDocType.Issue;

		protected virtual void _(Events.RowInserting<INRegister> e)
		{
			if (e.Row.DocType == INDocType.Undefined)
				e.Cancel = true;
		}

		protected virtual void _(Events.RowUpdated<INRegister> e)
		{
			if (insetup.Current.RequireControlTotal == false)
			{
				FillControlValue<INRegister.controlAmount, INRegister.totalAmount>(e.Cache, e.Row);
				FillControlValue<INRegister.controlQty, INRegister.totalQty>(e.Cache, e.Row);
			}
			else if (insetup.Current.RequireControlTotal == true && e.Row.Hold == false && e.Row.Released == false)
			{
				RaiseControlValueError<INRegister.controlAmount, INRegister.totalAmount>(e.Cache, e.Row);
				RaiseControlValueError<INRegister.controlQty, INRegister.totalQty>(e.Cache, e.Row);
			}
		}

		protected virtual void _(Events.RowSelected<INRegister> e)
		{
			if (e.Row == null)
				return;

			PXUIFieldAttribute.SetEnabled(e.Cache, e.Row, e.Row.Released == false && e.Row.OrigModule == BatchModule.IN);
			PXUIFieldAttribute.SetEnabled<INRegister.refNbr>(e.Cache, e.Row, true);
			PXUIFieldAttribute.SetEnabled<INRegister.totalQty>(e.Cache, e.Row, false);
			PXUIFieldAttribute.SetEnabled<INRegister.totalAmount>(e.Cache, e.Row, false);
			PXUIFieldAttribute.SetEnabled<INRegister.totalCost>(e.Cache, e.Row, false);
			PXUIFieldAttribute.SetEnabled<INRegister.status>(e.Cache, e.Row, false);

			e.Cache.AllowInsert = true;
			e.Cache.AllowUpdate = e.Row.Released == false;
			e.Cache.AllowDelete = e.Row.Released == false && e.Row.OrigModule == BatchModule.IN;

			lsselect.AllowInsert = e.Row.Released == false && e.Row.OrigModule == BatchModule.IN;
			lsselect.AllowUpdate = e.Row.Released == false;
			lsselect.AllowDelete = e.Row.Released == false && e.Row.OrigModule == BatchModule.IN;

			PXUIFieldAttribute.SetVisible<INRegister.controlQty>(e.Cache, e.Row, (bool)insetup.Current.RequireControlTotal);
			PXUIFieldAttribute.SetVisible<INRegister.controlAmount>(e.Cache, e.Row, (bool)insetup.Current.RequireControlTotal);
			PXUIFieldAttribute.SetVisible<INRegister.totalCost>(e.Cache, e.Row, e.Row.Released == true);

			/// added because IN Transfer is created via INIssueEntry in
			/// <see cref="SO.SOShipmentEntry.PostShipment(INIssueEntry, PXResult{SO.SOOrderShipment, SO.SOOrder}, DocumentList{INRegister}, AR.ARInvoice)"/>
			// TODO: move it to the Ctor or CacheAttached in 2019R1 after AC-118791
			switch (e.Row.DocType)
			{
				case INDocType.Issue:
					PXFormulaAttribute.SetAggregate<INTran.tranAmt>(transactions.Cache, typeof(SumCalc<INRegister.totalAmount>));
					break;
				default:
					PXFormulaAttribute.SetAggregate<INTran.tranAmt>(transactions.Cache, null);
					break;
			}
		}
		#endregion

		#region INTran
		protected virtual void _(Events.FieldDefaulting<INTran, INTran.docType> e) => e.NewValue = INDocType.Issue;
		protected virtual void _(Events.FieldDefaulting<INTran, INTran.tranType> e) => e.NewValue = INTranType.Issue;

		protected override void _(Events.FieldUpdated<INTran, INTran.uOM> e)
		{
			DefaultUnitPrice(e.Cache, e.Row);
			base._(e);
		}

		protected override void _(Events.FieldUpdated<INTran, INTran.siteID> e)
		{
			DefaultUnitPrice(e.Cache, e.Row);
			base._(e);
		}

		protected virtual void _(Events.FieldVerifying<INTran, INTran.sOOrderNbr> e) => e.Cancel = true;
		protected virtual void _(Events.FieldVerifying<INTran, INTran.sOShipmentNbr> e) => e.Cancel = true;

		protected virtual void _(Events.FieldVerifying<INTran, INTran.reasonCode> e)
		{
			if (e.Row != null)
			{
				ReasonCode reason = ReasonCode.PK.Find(this, (string)e.NewValue);
				e.Cancel = reason != null && (e.Row.TranType.IsNotIn(INTranType.Issue, INTranType.Return) && reason.Usage == ReasonCodeUsages.Sales || reason.Usage == e.Row.DocType);
			}
		}

		protected virtual void _(Events.FieldVerifying<INTran, INTran.locationID> e)
		{
			if (issue.Current != null && issue.Current.OrigModule != BatchModule.IN)
				e.Cancel = true;
		}

		protected virtual void _(Events.FieldVerifying<INTran, INTran.lotSerialNbr> e)
		{
			if (issue.Current != null && issue.Current.OrigModule != BatchModule.IN)
				e.Cancel = true;
		}

		protected virtual void _(Events.RowSelected<INTran> e)
		{
			if (e.Row != null)
			{
				PXUIFieldAttribute.SetEnabled<INTran.unitCost>(e.Cache, e.Row, e.Row.InvtMult == 1);
				PXUIFieldAttribute.SetEnabled<INTran.tranCost>(e.Cache, e.Row, e.Row.InvtMult == 1);
			}
		}

		protected virtual void _(Events.RowInserted<INTran> e)
		{
			if (e.Row != null && e.Row.OrigModule.IsIn(BatchModule.SO, BatchModule.PO))
				OnForeignTranInsert(e.Row);
		}

		protected virtual void _(Events.RowPersisting<INTran> e)
		{
			if (e.Operation.Command() == PXDBOperation.Update)
			{
				if (!string.IsNullOrEmpty(e.Row.SOShipmentNbr))
				{
					if (PXDBQuantityAttribute.Round((decimal)(e.Row.Qty + e.Row.OrigQty)) > 0m)
						e.Cache.RaiseExceptionHandling<INTran.qty>(e.Row, e.Row.Qty, new PXSetPropertyException(CS.Messages.Entry_LE, -e.Row.OrigQty));
					else if (PXDBQuantityAttribute.Round((decimal)(e.Row.Qty + e.Row.OrigQty)) < 0m)
						e.Cache.RaiseExceptionHandling<INTran.qty>(e.Row, e.Row.Qty, new PXSetPropertyException(CS.Messages.Entry_GE, -e.Row.OrigQty));
				}
			}
		}
		#endregion

		#region INTranSplit
		protected virtual void _(Events.FieldVerifying<INTranSplit, INTranSplit.locationID> e)
		{
			if (issue.Current != null && issue.Current.OrigModule != BatchModule.IN)
				e.Cancel = true;
		}

		protected virtual void _(Events.FieldVerifying<INTranSplit, INTranSplit.lotSerialNbr> e)
		{
			if (issue.Current != null && issue.Current.OrigModule != BatchModule.IN)
				e.Cancel = true;
		}

		protected virtual void _(Events.RowPersisted<INTranSplit> e)
		{
			//for cluster only. SelectQueries sometimes does not contain all the needed records after failed Save operation
			if (e.TranStatus == PXTranStatus.Aborted && WebConfig.IsClusterEnabled)
				e.Cache.ClearQueryCacheObsolete();
		}
		#endregion
		#endregion

		#region INRegisterEntryBase members
		public override PXSelectBase<INRegister> INRegisterDataMember => issue;
		public override PXSelectBase<INTran> INTranDataMember => transactions;
		public override LSINTran LSSelectDataMember => lsselect;
		public override PXSelectBase<INTranSplit> INTranSplitDataMember => splits;
		protected override string ScreenID => "IN302000";
		#endregion

		#region SiteStatus Lookup
		public class SiteStatusLookup : SiteStatusLookupExt<INIssueEntry>
		{
			protected override bool IsAddItemEnabled(INRegister doc) => LSSelect.AllowDelete;
		} 
		#endregion
	}
}