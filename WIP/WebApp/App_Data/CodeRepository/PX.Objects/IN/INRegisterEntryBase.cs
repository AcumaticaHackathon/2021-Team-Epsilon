using System.Collections;
using System.Collections.Generic;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.DependencyInjection;
using PX.Data.WorkflowAPI;
using PX.LicensePolicy;
using PX.Objects.GL;

namespace PX.Objects.IN
{
	public abstract class INRegisterEntryBase : PXGraph<PXGraph, INRegister>, IGraphWithInitialization // the first generic parameter is not used by the platform
	{
		#region Views

		public PXSetup<INSetup> insetup;

		public abstract PXSelectBase<INRegister> INRegisterDataMember { get; }
		public abstract PXSelectBase<INTran> INTranDataMember { get; }
		public abstract PXSelectBase<INTranSplit> INTranSplitDataMember { get; }
		public abstract LSINTran LSSelectDataMember { get; }
		#endregion // Views

		protected abstract string ScreenID { get; }

		#region DAC overrides
		#region INTran

		[PXString(2)]
		[PXFormula(typeof(Parent<INRegister.origModule>))]
		public virtual void INTran_OrigModule_CacheAttached(PXCache sender)
		{
		}

		#endregion // INTran

		#region INTranSplit
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[INTranSplitPlanID(typeof(INRegister.noteID), typeof(INRegister.hold), typeof(INRegister.transferType))]
		protected virtual void _(Events.CacheAttached<INTranSplit.planID> e) { }
		#endregion
		#endregion

		#region License Limits
		[InjectDependency]
		protected ILicenseLimitsService _licenseLimits { get; set; }

		void IGraphWithInitialization.Initialize()
		{
			if (_licenseLimits != null)
			{
				OnBeforeCommit += _licenseLimits.GetCheckerDelegate<INRegister>(
					new TableQuery(TransactionTypes.LinesPerMasterRecord, typeof(INTran), graph =>
					{
						var inEntry = (INRegisterEntryBase)graph;
						return new PXDataFieldValue[]
						{
							new PXDataFieldValue<INTran.docType>(PXDbType.Char, inEntry.INRegisterDataMember.Current?.DocType),
							new PXDataFieldValue<INTran.refNbr>(inEntry.INRegisterDataMember.Current?.RefNbr),
							new PXDataFieldValue<INTran.createdByScreenID>(PXDbType.Char, inEntry.ScreenID)
						};
					}),
					new TableQuery(TransactionTypes.SerialsPerDocument, typeof(INTranSplit), graph =>
					{
						var inEntry = (INRegisterEntryBase)graph;
						return new PXDataFieldValue[]
						{
							new PXDataFieldValue<INTranSplit.docType>(PXDbType.Char, inEntry.INRegisterDataMember.Current?.DocType),
							new PXDataFieldValue<INTranSplit.refNbr>(inEntry.INRegisterDataMember.Current?.RefNbr),
							new PXDataFieldValue<INTranSplit.createdByScreenID>(PXDbType.Char, inEntry.ScreenID)
						};
					}));
			}
		}
		#endregion

		#region Actions
		public PXInitializeState<INRegister> initializeState;

		public PXAction<INRegister> putOnHold;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Hold")]
		protected virtual IEnumerable PutOnHold(PXAdapter adapter) => adapter.Get();

		public PXAction<INRegister> releaseFromHold;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Remove Hold")]
		protected virtual IEnumerable ReleaseFromHold(PXAdapter adapter) => adapter.Get();

		public PXAction<INRegister> release;
		[PXProcessButton(CommitChanges = true)]
		[PXUIField(DisplayName = Messages.Release, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		protected virtual IEnumerable Release(PXAdapter adapter)
		{
			PXCache cache = INRegisterDataMember.Cache;
			var list = new List<INRegister>();
			foreach (INRegister indoc in adapter.Get<INRegister>())
			{
				if (indoc.Hold == false && indoc.Released == false)
				{
					cache.Update(indoc);
					list.Add(indoc);
				}
			}
			if (list.Count == 0)
			{
				throw new PXException(Messages.Document_Status_Invalid);
			}
			Save.Press();
			PXLongOperation.StartOperation(this, delegate () { INDocumentRelease.ReleaseDoc(list, false, adapter.QuickProcessFlow); });
			return list;
		}

		public PXAction<INRegister> viewBatch;
		[PXLookupButton(CommitChanges = true)]
		[PXUIField(DisplayName = "Review Batch", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
		protected virtual IEnumerable ViewBatch(PXAdapter adapter)
		{
			if (INRegisterDataMember.Current is INRegister doc && !string.IsNullOrEmpty(doc.BatchNbr))
			{
				GL.JournalEntry graph = PXGraph.CreateInstance<GL.JournalEntry>();
				graph.BatchModule.Current = graph.BatchModule.Search<GL.Batch.batchNbr>(doc.BatchNbr, "IN");
				throw new PXRedirectRequiredException(graph, "Current batch record");
			}
			return adapter.Get();
		}

		public PXAction<INRegister> inventorySummary;
		[PXLookupButton(CommitChanges = true, VisibleOnDataSource = false)]
		[PXUIField(DisplayName = "Inventory Summary", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable InventorySummary(PXAdapter adapter)
		{
			PXCache tCache = INTranDataMember.Cache;
			INTran line = INTranDataMember.Current;
			if (line == null) return adapter.Get();

			InventoryItem item = InventoryItem.PK.Find(this, line.InventoryID);
			if (item != null && item.StkItem == true)
			{
				INSubItem sbitem = (INSubItem)PXSelectorAttribute.Select<INTran.subItemID>(tCache, line);
				InventorySummaryEnq.Redirect(item.InventoryID, sbitem?.SubItemCD, line.SiteID, line.LocationID);
			}
			return adapter.Get();
		}

		public PXAction<INRegister> iNEdit;
		[PXLookupButton(CommitChanges = true)]
		[PXUIField(DisplayName = Messages.INEditDetails, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable INEdit(PXAdapter adapter)
		{
			if (INRegisterDataMember.Current is INRegister doc)
			{
				var parameters = new Dictionary<string, string>
				{
					[nameof(INRegister.DocType)] = doc.DocType,
					[nameof(INRegister.RefNbr)] = doc.RefNbr,
					["PeriodTo"] = null,
					["PeriodFrom"] = null
				};
				throw new PXReportRequiredException(parameters, "IN611000",
					PXBaseRedirectException.WindowMode.New, Messages.INEditDetails);
			}
			return adapter.Get();
		}

		public PXAction<INRegister> iNRegisterDetails;
		[PXLookupButton(CommitChanges = true)]
		[PXUIField(DisplayName = Messages.INRegisterDetails, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable INRegisterDetails(PXAdapter adapter)
		{
			if (INRegisterDataMember.Current is INRegister doc)
			{
				var parameters = new Dictionary<string, string>
				{
					[nameof(INRegister.DocType)] = doc.DocType,
					[nameof(INRegister.RefNbr)] = doc.RefNbr,
					["PeriodID"] = (string)INRegisterDataMember.GetValueExt<INRegister.finPeriodID>(doc)
				};
				throw new PXReportRequiredException(parameters, "IN614000",
					PXBaseRedirectException.WindowMode.New, Messages.INRegisterDetails);
			}
			return adapter.Get();
		}
		#endregion

		#region Event Handlers
		#region INTran
		protected virtual void _(Events.FieldDefaulting<INTran, INTran.invtMult> e) => e.NewValue = INTranType.InvtMult(e.Row.TranType);
		protected virtual void _(Events.FieldUpdated<INTran, INTran.uOM> e) => DefaultUnitCost(e.Cache, e.Row);
		protected virtual void _(Events.FieldUpdated<INTran, INTran.siteID> e) => DefaultUnitCost(e.Cache, e.Row);
		protected virtual void _(Events.FieldUpdated<INTran, INTran.inventoryID> e)
		{
			e.Cache.SetDefaultExt<INTran.uOM>(e.Row);
			e.Cache.SetDefaultExt<INTran.tranDesc>(e.Row);
		}
		#endregion
		#endregion

		public PXWorkflowEventHandler<INRegister> OnDocumentReleased;

		protected virtual void OnForeignTranInsert(INTran foreignTran)
		{
			INRegister doc = PXParentAttribute.SelectParent<INRegister>(INTranDataMember.Cache, foreignTran);
			if (doc != null)
			{
				PXCache cache = INRegisterDataMember.Cache;
				object copy = cache.CreateCopy(doc);

				doc.SOShipmentType = foreignTran.SOShipmentType;
				doc.SOShipmentNbr = foreignTran.SOShipmentNbr;

				doc.SOOrderType = foreignTran.SOOrderType;
				doc.SOOrderNbr = foreignTran.SOOrderNbr;

				doc.POReceiptType = foreignTran.POReceiptType;
				doc.POReceiptNbr = foreignTran.POReceiptNbr;

				if (object.Equals(doc, cache.Current))
				{
					if (cache.GetStatus(doc).IsIn(PXEntryStatus.Notchanged, PXEntryStatus.Held))
						cache.SetStatus(doc, PXEntryStatus.Updated);
					cache.RaiseRowUpdated(doc, copy);
				}
				else
				{
					cache.Update(doc);
				}
			}
		}

		protected void FillControlValue<TControlField, TTotalField>(PXCache cache, INRegister document)
			where TControlField : IBqlField, IImplement<IBqlDecimal>
			where TTotalField : IBqlField, IImplement<IBqlDecimal>
		{
			decimal? total = (decimal?)cache.GetValue<TTotalField>(document);
			if (CM.PXCurrencyAttribute.IsNullOrEmpty(total))
				cache.SetValue<TControlField>(document, 0m);
			else
				cache.SetValue<TControlField>(document, total);
		}

		protected void RaiseControlValueError<TControlField, TTotalField>(PXCache cache, INRegister document)
			where TControlField : IBqlField, IImplement<IBqlDecimal>
			where TTotalField : IBqlField, IImplement<IBqlDecimal>
		{
			decimal? control = (decimal?)cache.GetValue<TControlField>(document);
			decimal? total = (decimal?)cache.GetValue<TTotalField>(document);
			if (total != control)
				cache.RaiseExceptionHandling<TControlField>(document, control, new PXSetPropertyException(Messages.DocumentOutOfBalance));
			else
				cache.RaiseExceptionHandling<TControlField>(document, control, null);
		}

		protected virtual void DefaultUnitCost(PXCache cache, INTran tran) => DefaultUnitAmount<INTran.unitCost>(cache, tran);
		protected virtual void DefaultUnitPrice(PXCache cache, INTran tran) => DefaultUnitAmount<INTran.unitPrice>(cache, tran);

		protected virtual void DefaultUnitAmount<TUnitAmount>(PXCache cache, INTran tran)
			where TUnitAmount : IBqlField, IImplement<IBqlDecimal>
		{
			cache.RaiseFieldDefaulting<TUnitAmount>(tran, out object unitAmount);
			if (unitAmount is decimal amount && amount != 0m)
			{
				decimal? unitamount = INUnitAttribute.ConvertToBase<INTran.inventoryID>(cache, tran, tran.UOM, amount, INPrecision.UNITCOST);
				cache.SetValueExt<TUnitAmount>(tran, unitamount);
			}
		}
	}

	// TODO: generalized it even more to use in all other screens (such as SO, PO, RQ, FS)
	public abstract class SiteStatusLookupExt<TGraph, TSiteStatus> : PXGraphExtension<TGraph>
		where TGraph : INRegisterEntryBase
		where TSiteStatus : class, IBqlTable, new()
	{
		protected PXSelectBase<INRegister> Document => Base.INRegisterDataMember;
		protected PXSelectBase<INTran> Transactions => Base.INTranDataMember;
		protected PXSelectBase<INTranSplit> Splits => Base.INTranSplitDataMember;
		protected LSINTran LSSelect => Base.LSSelectDataMember;

		public PXFilter<INSiteStatusFilter> sitestatusfilter;

		[PXFilterable]
		[PXCopyPasteHiddenView]
		public INSiteStatusLookup<TSiteStatus, INSiteStatusFilter> sitestatus;

		public PXAction<INRegister> addInvBySite;
		[PXLookupButton(CommitChanges = true, VisibleOnDataSource = false)]
		[PXUIField(DisplayName = "Add Items", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		public virtual IEnumerable AddInvBySite(PXAdapter adapter)
		{
			sitestatusfilter.Cache.Clear();
			if (sitestatus.AskExt() == WebDialogResult.OK)
				return AddInvSelBySite(adapter);

			sitestatusfilter.Cache.Clear();
			sitestatus.Cache.Clear();
			return adapter.Get();
		}

		public PXAction<INRegister> addInvSelBySite;
		[PXLookupButton(CommitChanges = true, VisibleOnDataSource = false)]
		[PXUIField(DisplayName = "Add", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
		public virtual IEnumerable AddInvSelBySite(PXAdapter adapter)
		{
			Transactions.Cache.ForceExceptionHandling = true;

			foreach (TSiteStatus siteStatus in sitestatus.Cache.Cached)
			{
				if (IsSelected(siteStatus) && GetSelectedQty(siteStatus) > 0)
				{
					INTran newline = PXCache<INTran>.CreateCopy(Transactions.Insert(new INTran()));
					newline = InitTran(newline, siteStatus);
					newline.Qty = GetSelectedQty(siteStatus);
					Transactions.Update(newline);
				}
			}
			sitestatus.Cache.Clear();
			return adapter.Get();
		}

		protected abstract bool IsSelected(TSiteStatus siteStatus);
		protected abstract decimal GetSelectedQty(TSiteStatus siteStatus);
		protected abstract INTran InitTran(INTran newTran, TSiteStatus siteStatus);
		protected abstract bool IsAddItemEnabled(INRegister doc);

		protected virtual void _(Events.RowSelected<INRegister> args)
		{
			if (args.Row != null)
			{
				bool isEnabled = IsAddItemEnabled(args.Row);
				addInvBySite.SetEnabled(isEnabled);
				addInvSelBySite.SetEnabled(isEnabled);
			}
		}

		protected virtual void _(Events.RowInserted<INSiteStatusFilter> args)
		{
			if (args.Row != null && Document.Current != null)
				args.Row.SiteID = Document.Current.SiteID;
		}

	}

	public abstract class SiteStatusLookupExt<TGraph> : SiteStatusLookupExt<TGraph, INSiteStatusSelected>
		where TGraph : INRegisterEntryBase
	{
		protected override bool IsSelected(INSiteStatusSelected siteStatus) => siteStatus.Selected == true;
		protected override decimal GetSelectedQty(INSiteStatusSelected siteStatus) => siteStatus.QtySelected ?? 0;
		protected override INTran InitTran(INTran newTran, INSiteStatusSelected siteStatus)
		{
			newTran.SiteID = siteStatus.SiteID ?? newTran.SiteID;
			newTran.InventoryID = siteStatus.InventoryID;
			newTran.SubItemID = siteStatus.SubItemID;
			newTran.UOM = siteStatus.BaseUnit;
			newTran = PXCache<INTran>.CreateCopy(Transactions.Update(newTran));
			if (siteStatus.LocationID != null)
			{
				newTran.LocationID = siteStatus.LocationID;
				newTran = PXCache<INTran>.CreateCopy(Transactions.Update(newTran));
			}
			return newTran;
		}
	}
}