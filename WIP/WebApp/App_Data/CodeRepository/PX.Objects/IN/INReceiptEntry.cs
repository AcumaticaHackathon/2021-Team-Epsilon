using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.Common.Extensions;

namespace PX.Objects.IN
{
	public class INReceiptEntry : INRegisterEntryBase, PXImportAttribute.IPXPrepareItems
	{
		#region Internal State
		INRegister copy;
		List<Segment> _SubItemSeg = null;
		Dictionary<short?, string> _SubItemSegVal = null; 
		#endregion

		#region Views
		public
			PXSelect<INRegister,
			Where<INRegister.docType, Equal<INDocType.receipt>>>
			receipt;

		public
			PXSelect<INRegister,
			Where<
				INRegister.docType, Equal<INDocType.receipt>,
				And<INRegister.refNbr, Equal<Current<INRegister.refNbr>>>>>
			CurrentDocument;

		[PXImport(typeof(INRegister))]
		public
			PXSelect<INTran,
			Where<
				INTran.docType, Equal<INDocType.receipt>,
				And<INTran.refNbr, Equal<Current<INRegister.refNbr>>>>>
			transactions;

		[PXCopyPasteHiddenView]
		public
			PXSelect<INTranSplit,
			Where<
				INTranSplit.docType, Equal<INDocType.receipt>,
				And<INTranSplit.refNbr, Equal<Current<INTran.refNbr>>,
				And<INTranSplit.lineNbr, Equal<Current<INTran.lineNbr>>>>>>
			splits; // using Current<> is valid, PXSyncGridParams used in aspx

		public LSINTran lsselect;
		public PXSelect<INCostSubItemXRef> costsubitemxref;
		public PXSelect<INItemSite> initemsite;
		#endregion

		#region DAC overrides
		#region INRegister
		[PXString(15, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Transfer Nbr.")]
		[TransferNbrSelector(typeof(Search2<INRegister.refNbr,
			InnerJoin<INSite, On<INRegister.FK.ToSite>,
			InnerJoin<INTransferInTransit, On<INTransferInTransit.transferNbr, Equal<INRegister.refNbr>>,
			LeftJoin<INTran, On<INTran.origRefNbr, Equal<INTransferInTransit.transferNbr>,
				And<INTran.released, NotEqual<True>>>>>>,
			Where<INRegister.docType, Equal<INDocType.transfer>,
				And<INRegister.released, Equal<boolTrue>,
				And<INTran.refNbr, IsNull,
				And<Match<INSite, Current<AccessInfo.userName>>>>>>>))]
		[TransferNbrRestrictor(typeof(Where<INRegister.origModule, Equal<GL.BatchModule.moduleIN>>), Messages.TransferShouldBeProcessedThroughPO, typeof(INRegister.refNbr))]
		protected virtual void _(Events.CacheAttached<INRegister.transferNbr> e) { } 
		#endregion

		#region INTran
		[PXDefault(typeof(SelectFrom<InventoryItem>.Where<InventoryItem.inventoryID.IsEqual<INTran.inventoryID.FromCurrent>>), SourceField = typeof(InventoryItem.purchaseUnit), CacheGlobal = true)]
		[INUnit(typeof(INTran.inventoryID))]
		protected virtual void _(Events.CacheAttached<INTran.uOM> e) { }

		[PXDBString(3, IsFixed = true)]
		[PXDefault(INTranType.Receipt)]
		[PXUIField(Enabled = false, Visible = false)]
		protected virtual void _(Events.CacheAttached<INTran.tranType> e) { }
		#endregion
		#endregion

		#region Actions
		public PXAction<INRegister> iNItemLabels;
		[PXLookupButton(CommitChanges = true)]
		[PXUIField(DisplayName = Messages.INItemLabels, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable INItemLabels(PXAdapter adapter)
		{
			if (receipt.Current != null)
			{
				var parameters = new Dictionary<string, string>
				{
					[nameof(INRegister.RefNbr)] = receipt.Current.RefNbr
				};
				throw new PXReportRequiredException(parameters, "IN619200",
					 PXBaseRedirectException.WindowMode.New, Messages.INItemLabels);
			}
			return adapter.Get();
		}
		#endregion

		#region Initialization
		public INReceiptEntry()
		{
			INSetup record = insetup.Current;

			PXUIFieldAttribute.SetVisible<INTran.tranType>(transactions.Cache, null, false);
			PXUIFieldAttribute.SetEnabled<INTran.tranType>(transactions.Cache, null, false);
		}

		public override void InitCacheMapping(Dictionary<Type, Type> map)
		{
			base.InitCacheMapping(map);
			// override INLotSerialStatus cache to allow INTransitLineLotSerialStatus with projection work currectly with LSSelect<TLSMaster, TLSDetail, Where>
			this.Caches.AddCacheMapping(typeof(INLotSerialStatus), typeof(INLotSerialStatus));
		}
		#endregion

		#region Event Handlers
		#region INRegister
		protected virtual void _(Events.FieldDefaulting<INRegister, INRegister.docType> e) => e.NewValue = INDocType.Receipt;

		protected virtual void _(Events.RowUpdated<INRegister> e)
		{
			if (insetup.Current.RequireControlTotal == false)
			{
				FillControlValue<INRegister.controlCost, INRegister.totalCost>(e.Cache, e.Row);
				FillControlValue<INRegister.controlQty, INRegister.totalQty>(e.Cache, e.Row);
			}
			else if (insetup.Current.RequireControlTotal == true && e.Row.Hold == false && e.Row.Released == false)
			{
				RaiseControlValueError<INRegister.controlCost, INRegister.totalCost>(e.Cache, e.Row);
				RaiseControlValueError<INRegister.controlQty, INRegister.totalQty>(e.Cache, e.Row);
			}
		}

		protected virtual void _(Events.RowSelected<INRegister> e)
		{
			if (e.Row == null)
				return;

			INTran tran = transactions.Current ?? transactions.SelectWindowed(0, 1);
			if (tran != null)
			{
				// Acuminator disable once PX1047 RowChangesInEventHandlersForbiddenForArgs [Justification: virtual field]
				e.Row.TransferNbr = tran.OrigRefNbr;
			}

			bool isTransfer = e.Row.TransferNbr != null;
			bool isPOTransfer = isTransfer && e.Row.OrigModule == GL.BatchModule.PO;

			PXUIFieldAttribute.SetEnabled(e.Cache, e.Row, e.Row.Released == false && e.Row.OrigModule == GL.BatchModule.IN);
			PXUIFieldAttribute.SetEnabled<INRegister.refNbr>(e.Cache, e.Row, true);
			PXUIFieldAttribute.SetEnabled<INRegister.totalQty>(e.Cache, e.Row, false);
			PXUIFieldAttribute.SetEnabled<INRegister.totalCost>(e.Cache, e.Row, false);
			PXUIFieldAttribute.SetEnabled<INRegister.status>(e.Cache, e.Row, false);

			e.Cache.AllowInsert = true;
			e.Cache.AllowUpdate = e.Row.Released == false;
			e.Cache.AllowDelete = e.Row.Released == false && e.Row.OrigModule == GL.BatchModule.IN;

			lsselect.AllowInsert = e.Row.Released == false && e.Row.OrigModule == GL.BatchModule.IN && !isTransfer;
			lsselect.AllowUpdate = e.Row.Released == false;
			lsselect.AllowDelete = e.Row.Released == false && e.Row.OrigModule == GL.BatchModule.IN && !isTransfer;

			PXUIFieldAttribute.SetVisible<INRegister.controlQty>(e.Cache, e.Row, (bool)insetup.Current.RequireControlTotal);
			PXUIFieldAttribute.SetVisible<INRegister.controlCost>(e.Cache, e.Row, (bool)insetup.Current.RequireControlTotal);

			PXUIFieldAttribute.SetEnabled<INRegister.transferNbr>(e.Cache, e.Row, e.Cache.AllowUpdate && tran == null);
			PXUIFieldAttribute.SetEnabled<INRegister.branchID>(e.Cache, e.Row, e.Cache.AllowUpdate && !isTransfer);

			if (e.Cache.Graph.IsImport != true || copy == null || !e.Cache.ObjectsEqual<INRegister.transferNbr, INRegister.released>(e.Row, copy))
			{
				if (isTransfer && !isPOTransfer)
					PXUIFieldAttribute.SetEnabled<INTran.qty>(transactions.Cache, null, true);

				copy = PXCache<INRegister>.CreateCopy(e.Row);
			}
		}

		protected virtual void _(Events.FieldUpdated<INRegister, INRegister.transferNbr> e)
		{
			INTran newtran = null;
			int? prev_linenbr = null;
			INLocationStatus2 prev_stat = null;
			decimal newtranqty = 0m;
			string transferNbr = e.Row.TransferNbr;
			decimal newtrancost = 0m;
			ParseSubItemSegKeys();

			using (new PXReadBranchRestrictedScope())
			{
				foreach (PXResult<INTransitLine, INLocationStatus2, INTransitLineLotSerialStatus, INSite, InventoryItem, INTran> res in
					PXSelectJoin<INTransitLine,
					InnerJoin<INLocationStatus2, On<INLocationStatus2.locationID, Equal<INTransitLine.costSiteID>>,
					LeftJoin<INTransitLineLotSerialStatus,
							On<INTransitLine.transferNbr, Equal<INTransitLineLotSerialStatus.transferNbr>,
							And<INTransitLine.transferLineNbr, Equal<INTransitLineLotSerialStatus.transferLineNbr>>>,
					InnerJoin<INSite, On<INTransitLine.FK.ToSite>,
					InnerJoin<InventoryItem, On<INLocationStatus2.FK.InventoryItem>,
					InnerJoin<INTran,
						On<INTran.docType, Equal<INDocType.transfer>,
						And<INTran.refNbr, Equal<INTransitLine.transferNbr>,
						And<INTran.lineNbr, Equal<INTransitLine.transferLineNbr>,
						And<INTran.invtMult, Equal<shortMinus1>>>>>>>>>>,
					Where<INTransitLine.transferNbr, Equal<Required<INTransitLine.transferNbr>>>,
					OrderBy<Asc<INTransitLine.transferNbr, Asc<INTransitLine.transferLineNbr>>>>
					.Select(this, transferNbr))
				{
					INTransitLine transitline = res;
					INLocationStatus2 stat = res;
					INTransitLineLotSerialStatus lotstat = res;
					INSite site = res;
					InventoryItem item = res;
					INTran tran = res;

					if (stat.QtyOnHand == 0m || (lotstat != null && lotstat.QtyOnHand == 0m))
						continue;

					if (prev_linenbr != transitline.TransferLineNbr)
					{
						UpdateTranCostQty(newtran, newtranqty, newtrancost);
						newtrancost = 0m;
						newtranqty = 0m;

						if (!object.Equals(receipt.Current.BranchID, site.BranchID))
						{
							INRegister copy = PXCache<INRegister>.CreateCopy(receipt.Current);
							copy.BranchID = site.BranchID;
							receipt.Update(copy);
						}
						newtran = PXCache<INTran>.CreateCopy(tran);
						if (tran.DestBranchID == null)
						{
							// AC-119895: If two-step Transfer was released before upgrade we should follow the old behavior
							// where Interbranch transactions are placed in receipt part.
							newtran.OrigBranchID = newtran.BranchID;
						}
						newtran.OrigDocType = newtran.DocType;
						newtran.OrigTranType = newtran.TranType;
						newtran.OrigRefNbr = transitline.TransferNbr;
						newtran.OrigLineNbr = transitline.TransferLineNbr;
						if (tran.TranType == INTranType.Transfer)
						{
							newtran.OrigNoteID = ((INRegister)e.Row).NoteID;
							newtran.OrigToLocationID = tran.ToLocationID;
							INTranSplit split =
								PXSelectReadonly<INTranSplit,
								Where<INTranSplit.docType, Equal<Current<INTran.docType>>,
									And<INTranSplit.refNbr, Equal<Current<INTran.refNbr>>,
									And<INTranSplit.lineNbr, Equal<Current<INTran.lineNbr>>>>>>
								.SelectSingleBound(this, new object[] { tran });
							newtran.OrigIsLotSerial = !string.IsNullOrEmpty(split?.LotSerialNbr);
						}
						newtran.BranchID = site.BranchID;
						newtran.DocType = e.Row.DocType;
						newtran.RefNbr = e.Row.RefNbr;
						newtran.LineNbr = (int)PXLineNbrAttribute.NewLineNbr<INTran.lineNbr>(transactions.Cache, e.Row);
						newtran.InvtMult = 1;
						newtran.SiteID = transitline.ToSiteID;
						newtran.LocationID = transitline.ToLocationID;
						newtran.ToSiteID = null;
						newtran.ToLocationID = null;
						newtran.BaseQty = 0m;
						newtran.Qty = 0m;
						newtran.UnitCost = 0m;
						newtran.Released = false;
						newtran.InvtAcctID = null;
						newtran.InvtSubID = null;
						newtran.ReasonCode = null;
						newtran.ARDocType = null;
						newtran.ARRefNbr = null;
						newtran.ARLineNbr = null;
						newtran.ProjectID = null;
						newtran.TaskID = null;
						newtran.CostCodeID = null;
						newtran.TranCost = 0m;
						newtran.NoteID = null;

						splits.Current = null;

						newtran = transactions.Insert(newtran);

						transactions.Current = newtran;

						if (splits.Current != null)
						{
							splits.Delete(splits.Current);
						}


						prev_linenbr = transitline.TransferLineNbr;
					}

					if (this.Caches[typeof(INLocationStatus2)].ObjectsEqual(prev_stat, stat) == false)
					{
						newtranqty += stat.QtyOnHand.Value;
						prev_stat = stat;
					}

					decimal newsplitqty;
					INTranSplit newsplit;
					if (lotstat.QtyOnHand == null)
					{
						newsplit = new INTranSplit
						{
							InventoryID = stat.InventoryID,
							IsStockItem = true,
							FromSiteID = transitline.SiteID,
							SubItemID = stat.SubItemID,
							LotSerialNbr = null
						};
						newsplitqty = stat.QtyOnHand.Value;
					}
					else
					{
						newsplit = new INTranSplit
						{
							InventoryID = lotstat.InventoryID,
							IsStockItem = true,
							FromSiteID = lotstat.FromSiteID,
							SubItemID = lotstat.SubItemID,
							LotSerialNbr = lotstat.LotSerialNbr
						};
						newsplitqty = lotstat.QtyOnHand.Value;
					}

					newsplit.DocType = e.Row.DocType;
					newsplit.RefNbr = e.Row.RefNbr;
					newsplit.LineNbr = newtran.LineNbr;
					newsplit.SplitLineNbr = (int)PXLineNbrAttribute.NewLineNbr<INTranSplit.splitLineNbr>(splits.Cache, e.Row);

					newsplit.UnitCost = 0m;
					newsplit.InvtMult = 1;
					newsplit.SiteID = transitline.ToSiteID;
					newsplit.LocationID = lotstat.ToLocationID ?? transitline.ToLocationID;
					newsplit.PlanID = null;
					newsplit.Released = false;
					newsplit.ProjectID = null;
					newsplit.TaskID = null;

					newsplit = splits.Insert(newsplit);

					UpdateCostSubItemID(newsplit, item);
					newsplit.MaxTransferBaseQty = newsplitqty;
					newsplit.BaseQty = newsplitqty;
					newsplit.Qty = newsplit.BaseQty.Value;

					SetCostAttributes(newtran, newsplit, item, transferNbr);
					newtrancost += newsplit.BaseQty.Value * newsplit.UnitCost.Value;
					newsplit.UnitCost = PXCurrencyAttribute.BaseRound(this, newsplit.UnitCost);
					splits.Update(newsplit);
				}
			}
			UpdateTranCostQty(newtran, newtranqty, newtrancost);
		}

		protected virtual void _(Events.FieldVerifying<INRegister, INRegister.transferNbr> e)
		{
			INTran tran = transactions.SelectWindowed(0, 1);
			if (tran != null)
				e.Cancel = true;
		}
		#endregion

		#region INTran
		protected virtual void _(Events.FieldDefaulting<INTran, INTran.docType> e) => e.NewValue = INDocType.Receipt;

		protected virtual void _(Events.RowInserted<INTran> e)
		{
			if (e.Row != null && e.Row.OrigModule.IsIn(BatchModule.SO, BatchModule.PO))
				OnForeignTranInsert(e.Row);
		}

		protected virtual void _(Events.RowPersisting<INTran> e)
		{
			if (e.Operation.Command() == PXDBOperation.Update)
			{
				if (!string.IsNullOrEmpty(e.Row.POReceiptNbr))
				{
					if (PXDBQuantityAttribute.Round((decimal)(e.Row.Qty + e.Row.OrigQty)) > 0m)
						e.Cache.RaiseExceptionHandling<INTran.qty>(e.Row, e.Row.Qty, new PXSetPropertyException(CS.Messages.Entry_LE, -e.Row.OrigQty));
					else if (PXDBQuantityAttribute.Round((decimal)(e.Row.Qty + e.Row.OrigQty)) < 0m)
						e.Cache.RaiseExceptionHandling<INTran.qty>(e.Row, e.Row.Qty, new PXSetPropertyException(CS.Messages.Entry_GE, -e.Row.OrigQty));
				}
			}

			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				INTranBaseQtyFieldVerifying(e.Cache, e.Row, e.Row.BaseQty);

				if (e.Row.Qty == 0 && e.Row.TranCost > 0)
				{
					if (e.Row.POReceiptNbr != null && e.Row.POReceiptLineNbr != null && e.Row.POReceiptType != null)
					{
						var poreceiptline = new PO.POReceiptLine
						{
							ReceiptType = e.Row.POReceiptType,
							LineNbr = e.Row.POReceiptLineNbr,
							ReceiptNbr = e.Row.POReceiptNbr
						};
						throw new Common.Exceptions.ErrorProcessingEntityException(
							Caches[poreceiptline.GetType()], poreceiptline, Messages.ZeroQtyWhenNonZeroCost);
					}
					e.Cache.RaiseExceptionHandling<INTran.qty>(e.Row, e.Row.Qty, new PXSetPropertyException(Messages.ZeroQtyWhenNonZeroCost));
				}
			}
		}

		protected virtual void _(Events.FieldVerifying<INTran, INTran.baseQty> e) => INTranBaseQtyFieldVerifying(e.Cache, e.Row, (decimal?)e.NewValue);

		public virtual void INTranBaseQtyFieldVerifying(PXCache cache, INTran row, decimal? value)
		{
			bool istransfer = CurrentDocument.Current != null && CurrentDocument.Current.TransferNbr != null;
			if (istransfer)
			{
				if (value > row.MaxTransferBaseQty && row.MaxTransferBaseQty.HasValue)
				{
					cache.RaiseExceptionHandling<INTran.qty>(
						row,
						value,
						new PXSetPropertyException<INTran.qty>(
							CS.Messages.Entry_LE,
							INUnitAttribute.ConvertFromBase<INTran.inventoryID, INTran.uOM>(
								cache,
								row,
								row.MaxTransferBaseQty.Value,
								INPrecision.QUANTITY)));
				}
			}
		}
		#endregion

		#region INTranSplit
		protected virtual void _(Events.FieldVerifying<INTranSplit, INTranSplit.baseQty> e) => INTranSplitBaseQtyFieldVerifying(e.Cache, e.Row, (decimal?)e.NewValue);

		protected virtual void _(Events.RowPersisting<INTranSplit> e)
		{
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
				INTranSplitBaseQtyFieldVerifying(e.Cache, e.Row, e.Row.BaseQty);
		}

		public virtual void INTranSplitBaseQtyFieldVerifying(PXCache cache, INTran row, decimal? value)
		{
			bool istransfer = CurrentDocument.Current != null && CurrentDocument.Current.TransferNbr != null;
			if (istransfer)
			{
				if (value > row.MaxTransferBaseQty && row.MaxTransferBaseQty.HasValue)
				{
					cache.RaiseExceptionHandling<INTranSplit.qty>(
						row,
						value,
						new PXSetPropertyException<INTran.qty>(
							CS.Messages.Entry_LE,
							INUnitAttribute.ConvertFromBase<INTranSplit.inventoryID, INTranSplit.uOM>(
								cache,
								row,
								row.MaxTransferBaseQty.Value,
								INPrecision.QUANTITY)));
				}
			}
		}
		#endregion
		#endregion

		public virtual void UpdateTranCostQty(INTran newtran, decimal newtranqty, decimal newtrancost)
		{
			lsselect.SuppressedMode = true;
			if (newtran != null)
			{
				newtran.BaseQty = newtranqty;
				newtran.Qty = INUnitAttribute.ConvertFromBase(transactions.Cache, newtran.InventoryID, newtran.UOM, newtran.BaseQty.Value, INPrecision.QUANTITY);
				newtran.MaxTransferBaseQty = newtranqty;
				newtran.UnitCost = PXCurrencyAttribute.BaseRound(this, newtrancost / newtran.Qty);
				newtran.TranCost = PXCurrencyAttribute.BaseRound(this, newtrancost);
				transactions.Update(newtran);
			}
			lsselect.SuppressedMode = false;
		}

		public virtual void ParseSubItemSegKeys()
		{
			if (_SubItemSeg == null)
			{
				_SubItemSeg = new List<Segment>();

				foreach (Segment seg in PXSelect<Segment, Where<Segment.dimensionID, Equal<IN.SubItemAttribute.dimensionName>>>.Select(this))
				{
					_SubItemSeg.Add(seg);
				}

				_SubItemSegVal = new Dictionary<short?, string>();

				foreach (SegmentValue val in PXSelectJoin<SegmentValue, InnerJoin<Segment, On<Segment.dimensionID, Equal<SegmentValue.dimensionID>, And<Segment.segmentID, Equal<SegmentValue.segmentID>>>>, Where<SegmentValue.dimensionID, Equal<IN.SubItemAttribute.dimensionName>, And<Segment.isCosted, Equal<boolFalse>, And<SegmentValue.isConsolidatedValue, Equal<boolTrue>>>>>.Select(this))
				{
					try
					{
						_SubItemSegVal.Add((short)val.SegmentID, val.Value);
					}
					catch (Exception excep)
					{
						throw new PXException(excep, Messages.MultipleAggregateChecksEncountred, val.SegmentID, val.DimensionID);
					}
				}
			}
		}

		public virtual string MakeCostSubItemCD(string SubItemCD)
		{
			StringBuilder sb = new StringBuilder();

			int offset = 0;

			foreach (Segment seg in _SubItemSeg)
			{
				string segval = SubItemCD.Substring(offset, (int)seg.Length);
				if (seg.IsCosted == true || segval.TrimEnd() == string.Empty)
				{
					sb.Append(segval);
				}
				else
				{
					if (_SubItemSegVal.TryGetValue(seg.SegmentID, out segval))
					{
						sb.Append(segval.PadRight(seg.Length ?? 0));
					}
					else
					{
						throw new PXException(Messages.SubItemSeg_Missing_ConsolidatedVal);
					}
				}
				offset += (int)seg.Length;
			}

			return sb.ToString();
		}

		public object GetValueExt<Field>(PXCache cache, object data)
			where Field : class, IBqlField
		{
			object val = cache.GetValueExt<Field>(data);

			if (val is PXFieldState)
			{
				return ((PXFieldState)val).Value;
			}
			else
			{
				return val;
			}
		}

		public virtual void UpdateCostSubItemID(INTranSplit split, InventoryItem item)
		{
			INCostSubItemXRef xref = new INCostSubItemXRef();

			xref.SubItemID = split.SubItemID;
			xref.CostSubItemID = split.SubItemID;

			string SubItemCD = (string)this.GetValueExt<INCostSubItemXRef.costSubItemID>(costsubitemxref.Cache, xref);

			xref.CostSubItemID = null;

			string CostSubItemCD = PXAccess.FeatureInstalled<FeaturesSet.subItem>() ? MakeCostSubItemCD(SubItemCD) : SubItemCD;

			costsubitemxref.Cache.SetValueExt<INCostSubItemXRef.costSubItemID>(xref, CostSubItemCD);
			xref = costsubitemxref.Update(xref);

			if (costsubitemxref.Cache.GetStatus(xref) == PXEntryStatus.Updated)
			{
				costsubitemxref.Cache.SetStatus(xref, PXEntryStatus.Notchanged);
			}

			split.CostSubItemID = xref.CostSubItemID;
		}

		public Int32? INTransitSiteID
		{
			get
			{
				if (insetup.Current.TransitSiteID == null)
					throw new PXException("Please fill transite site id in inventory preferences.");
				return insetup.Current.TransitSiteID;
			}
		}

		public virtual PXView GetCostStatusCommand(INTranSplit split, InventoryItem item, string transferNbr, out object[] parameters)
		{
			BqlCommand cmd = null;

			int? costsiteid;
			costsiteid = INTransitSiteID;

			switch (item.ValMethod)
			{
				case INValMethod.Standard:
				case INValMethod.Average:
				case INValMethod.FIFO:

					cmd = new Select<INCostStatus,
						Where<INCostStatus.inventoryID, Equal<Required<INCostStatus.inventoryID>>,
							And<INCostStatus.costSiteID, Equal<Required<INCostStatus.costSiteID>>,
							And<INCostStatus.costSubItemID, Equal<Required<INCostStatus.costSubItemID>>,
							And<INCostStatus.layerType, Equal<INLayerType.normal>,
							And<INCostStatus.receiptNbr, Equal<Required<INCostStatus.receiptNbr>>>>>>>,
						OrderBy<Asc<INCostStatus.receiptDate, Asc<INCostStatus.receiptNbr>>>>();

					parameters = new object[] { split.InventoryID, costsiteid, split.CostSubItemID, transferNbr };
					break;
				case INValMethod.Specific:
					cmd = new Select<INCostStatus,
						Where<INCostStatus.inventoryID, Equal<Required<INCostStatus.inventoryID>>,
						And<INCostStatus.costSiteID, Equal<Required<INCostStatus.costSiteID>>,
						And<INCostStatus.costSubItemID, Equal<Required<INCostStatus.costSubItemID>>,
						And<INCostStatus.lotSerialNbr, Equal<Required<INCostStatus.lotSerialNbr>>,
						And<INCostStatus.layerType, Equal<INLayerType.normal>,
						And<INCostStatus.receiptNbr, Equal<Required<INCostStatus.receiptNbr>>>>>>>>>();
					parameters = new object[] { split.InventoryID, costsiteid, split.CostSubItemID, split.LotSerialNbr, transferNbr };
					break;
				default:
					throw new PXException();
			}

			return new PXView(this, false, cmd);
		}

		public virtual void SetCostAttributes(INTran tran, INTranSplit split, InventoryItem item, string transferNbr)
		{
			if (split.BaseQty == 0m || split.BaseQty == null)
				return;

			object[] parameters;
			PXView cmd = GetCostStatusCommand(split, item, transferNbr, out parameters);
			INCostStatus layer = (INCostStatus)cmd.SelectSingle(parameters);
			tran.AcctID = layer.AccountID;
			tran.SubID = layer.SubID;
			split.UnitCost = layer.TotalCost.Value / layer.QtyOnHand.Value;
		}

		protected virtual bool IsPMVisible
		{
			get
			{
				PM.PMSetup setup = PXSelect<PM.PMSetup>.Select(this);
				if (setup == null)
				{
					return false;
				}
				else
				{
					if (setup.IsActive != true)
						return false;
					else
						return setup.VisibleInIN == true;
				}
			}
		}

		public virtual bool PrepareImportRow(string viewName, IDictionary keys, IDictionary values)
		{
			if (string.Compare(viewName, nameof(transactions), StringComparison.OrdinalIgnoreCase) == 0)
			{
				CorrectKey(nameof(INTran.DocType), CurrentDocument.Current.DocType, keys, values);
				CorrectKey(nameof(INTran.RefNbr), CurrentDocument.Current.RefNbr, keys, values);
				INTran tran = PXSelect<INTran, Where<INTran.docType, Equal<Current<INRegister.docType>>, And<INTran.refNbr, Equal<Current<INRegister.refNbr>>>>, OrderBy<Desc<INTran.lineNbr>>>.SelectSingleBound(this, new object[] { CurrentDocument.Current });
				CorrectKey(nameof(INTran.LineNbr), tran == null ? 1 : tran.LineNbr + 1, keys, values);
			}
			return true;
		}

		protected static void CorrectKey(string name, object value, IDictionary keys, IDictionary values)
		{
			CorrectKey(name, value, keys);
			CorrectKey(name, value, values);
		}

		protected static void CorrectKey(string name, object value, IDictionary dict)
		{
			if (dict.Contains(name))
				dict[name] = value;
			else
				dict.Add(name, value);
		}

		public virtual bool RowImporting(string viewName, object row) { return row == null; }
		public virtual bool RowImported(string viewName, object row, object oldRow) { return oldRow == null; }
		public virtual void PrepareItems(string viewName, IEnumerable items) { }

		#region INRegisterBaseEntry implementation
		public override PXSelectBase<INRegister> INRegisterDataMember => receipt;
		public override PXSelectBase<INTran> INTranDataMember => transactions;
		public override LSINTran LSSelectDataMember => lsselect;
		public override PXSelectBase<INTranSplit> INTranSplitDataMember => splits;
		protected override string ScreenID => "IN301000";
		#endregion

		#region SiteStatus Lookup
		public class SiteStatusLookup : SiteStatusLookupExt<INReceiptEntry>
		{
			protected override bool IsAddItemEnabled(INRegister doc) => LSSelect.AllowDelete;
			protected virtual void _(Events.FieldDefaulting<INSiteStatusFilter, INSiteStatusFilter.onlyAvailable> args) => args.NewValue = false;
		}
		#endregion

		public class TransferNbrSelectorAttribute : PXSelectorAttribute
		{
			protected BqlCommand _RestrictedSelect;
			protected PXView _outerview;

			public TransferNbrSelectorAttribute(Type searchType)
				: base(searchType)
			{
				_RestrictedSelect = BqlCommand.CreateInstance(typeof(Search2<INRegister.refNbr, InnerJoin<INSite, On<INRegister.FK.ToSite>>, Where<MatchWithBranch<INSite.branchID>>>));
			}

			public override void FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
			{
				INSite insite = null;

				using (new PXReadBranchRestrictedScope())
				{
					base.FieldVerifying(sender, e);

					var transfer = INRegister.PK.Find(sender.Graph, INDocType.Transfer, e.NewValue as string);
					if (transfer != null)
						insite = INSite.PK.Find(sender.Graph, transfer.ToSiteID);
				}

				if (insite != null && !_RestrictedSelect.Meet(sender.Graph.Caches[typeof(INSite)], insite))
					throw new PXSetPropertyException(ErrorMessages.ElementDoesntExistOrNoRights, _FieldName);
			}

			public override void CacheAttached(PXCache sender)
			{
				base.CacheAttached(sender);
				_outerview = new PXView(sender.Graph, true, _Select);
				PXView view = sender.Graph.Views[_ViewName] = new PXView(sender.Graph, true, _Select, (PXSelectDelegate)delegate ()
				{
					int startRow = PXView.StartRow;
					int totalRows = 0;
					List<object> res;

					using (new PXReadBranchRestrictedScope())
					{
						res = _outerview.Select(PXView.Currents, PXView.Parameters, PXView.Searches, PXView.SortColumns, PXView.Descendings, PXView.Filters, ref startRow, PXView.MaximumRows, ref totalRows);
						PXView.StartRow = 0;
					}

					PXCache cache = _outerview.Graph.Caches[typeof(INSite)];

					return res.FindAll((item) =>
					{
						return _RestrictedSelect.Meet(cache, item is PXResult ? PXResult.Unwrap<INSite>(item) : item);
					});
				});

				if (_DirtyRead)
				{
					view.IsReadOnly = false;
				}

			}

			public override BqlCommand WhereAnd(PXCache sender, Type whr)
			{
				_outerview.WhereAnd(whr);
				return _outerview.BqlSelect;
			}
		}

		public class TransferNbrRestrictorAttribute : PXRestrictorAttribute
		{
			public TransferNbrRestrictorAttribute(Type where, string message, params Type[] pars)
				: base(where, message, pars)
			{
			}

			public override void FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
			{
				using (new PXReadBranchRestrictedScope())
				{
					base.FieldVerifying(sender, e);
				}
			}
		}
	}
}
