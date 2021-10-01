using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.WorkflowAPI;
using PX.Data.ReferentialIntegrity.Attributes;

using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN.Overrides.INDocumentRelease;
using PX.Objects.Common;
using PX.Objects.Common.Bql;
using PX.Objects.Common.Attributes;
using PX.Objects.Common.Exceptions;
using PX.Objects.PM;
using System.Security.Policy;
using PX.Common;

namespace PX.Objects.IN
{
	public class KitAssemblyEntry : PXGraph<KitAssemblyEntry, INKitRegister>
	{
		#region Views
		public PXSelect<INTran> dummy_intran;

		public
			PXSelect<INKitRegister,
			Where<INKitRegister.docType, Equal<Optional<INKitRegister.docType>>>>
			Document;

		public
			PXSelect<INKitRegister,
			Where<
				INKitRegister.docType, Equal<Current<INKitRegister.docType>>,
				And<INKitRegister.refNbr, Equal<Current<INKitRegister.refNbr>>>>>
			DocumentProperties;
		
		public
			PXSelectJoin<INComponentTran,
			InnerJoin<InventoryItem, On<InventoryItem.inventoryID, Equal<INComponentTran.inventoryID>>,
			LeftJoin<INKitSpecStkDet, On<
				INKitSpecStkDet.kitInventoryID, Equal<Current<INKitRegister.kitInventoryID>>,
				And2<Where<
					INKitSpecStkDet.compSubItemID, Equal<INComponentTran.subItemID>,
					Or<Where<
						INKitSpecStkDet.compSubItemID, IsNull,
						And<INComponentTran.subItemID, IsNull>>>>,
				And<INKitSpecStkDet.revisionID, Equal<Current<INKitRegister.kitRevisionID>>,
				And<INKitSpecStkDet.compInventoryID, Equal<INComponentTran.inventoryID>>>>>>>,
			Where<
				INComponentTran.docType, Equal<Current<INKitRegister.docType>>,
				And<INComponentTran.refNbr, Equal<Current<INKitRegister.refNbr>>,
				And<InventoryItem.stkItem, Equal<boolTrue>,
				And<INComponentTran.lineNbr, NotEqual<Current<INKitRegister.kitLineNbr>>>>>>>
			Components;

		public
			PXSelectJoin<INOverheadTran,
			InnerJoin<InventoryItem, On<InventoryItem.inventoryID, Equal<INOverheadTran.inventoryID>>,
			LeftJoin<INKitSpecNonStkDet, On<
				INKitSpecNonStkDet.kitInventoryID, Equal<Current<INKitRegister.kitInventoryID>>,
				And<INKitSpecNonStkDet.revisionID, Equal<Current<INKitRegister.kitRevisionID>>,
				And<INKitSpecNonStkDet.compInventoryID, Equal<INOverheadTran.inventoryID>>>>>>,
			Where<
				INOverheadTran.docType, Equal<Current<INKitRegister.docType>>,
				And<INOverheadTran.refNbr, Equal<Current<INKitRegister.refNbr>>,
				And<InventoryItem.stkItem, Equal<False>>>>>
			Overhead;

		public
			PXSelect<INKitSpecHdr,
			Where<
				INKitSpecHdr.kitInventoryID, Equal<Current<INKitRegister.kitInventoryID>>,
				And<INKitSpecHdr.revisionID, Equal<Current<INKitRegister.kitRevisionID>>>>>
			Spec;
		
		public
			PXSelectJoin<INKitSpecStkDet,
			InnerJoin<InventoryItem, On<INKitSpecStkDet.FK.ComponentInventoryItem>>,
			Where<
				INKitSpecStkDet.kitInventoryID, Equal<Current<INKitRegister.kitInventoryID>>,
				And<INKitSpecStkDet.revisionID, Equal<Current<INKitRegister.kitRevisionID>>>>>
			SpecComponents;

		public
			PXSelectJoin<INKitSpecNonStkDet,
			InnerJoin<InventoryItem, On<INKitSpecNonStkDet.FK.ComponentInventoryItem>>,
			Where<
				INKitSpecNonStkDet.kitInventoryID, Equal<Current<INKitRegister.kitInventoryID>>,
				And<INKitSpecNonStkDet.revisionID, Equal<Current<INKitRegister.kitRevisionID>>>>>
			SpecOverhead;

		[PXCopyPasteHiddenView]
		public
			PXSelect<INComponentTranSplit,
			Where<
				INComponentTranSplit.docType, Equal<Current<INComponentTran.docType>>,
				And<INComponentTranSplit.refNbr, Equal<Current<INComponentTran.refNbr>>,
				And<INComponentTranSplit.lineNbr, Equal<Current<INComponentTran.lineNbr>>>>>>
			ComponentSplits;

		[PXCopyPasteHiddenView]
		public
			PXSelect<INKitTranSplit,
			Where<
				INKitTranSplit.docType, Equal<Current<INKitRegister.docType>>,
				And<INKitTranSplit.refNbr, Equal<Current<INKitRegister.refNbr>>,
				And<INKitTranSplit.lineNbr, Equal<Current<INKitRegister.kitLineNbr>>>>>>
			MasterSplits;

		public
			PXSelect<INKitSerialPart,
			Where<
				INKitSerialPart.docType, Equal<Current<INKitRegister.docType>>,
				And<INKitSerialPart.refNbr, Equal<Current<INKitRegister.refNbr>>>>>
			SerialNumberedParts;

		public PXSetup<INSetup> Setup;
		public LSINComponentTran lsselect;
		public LSINComponentMasterTran lsselect2;
		#endregion

		#region Initialization
		public KitAssemblyEntry()
		{
			Spec.Cache.AllowInsert = false;
			Spec.Cache.AllowDelete = false;
			Spec.Cache.AllowUpdate = false;
			SpecComponents.Cache.AllowInsert = false;
			SpecComponents.Cache.AllowDelete = false;
			SpecComponents.Cache.AllowUpdate = false;
			SpecOverhead.Cache.AllowInsert = false;
			SpecOverhead.Cache.AllowDelete = false;
			SpecOverhead.Cache.AllowUpdate = false;

			this.Views["LSINComponentMasterTran_lotseropts"].AllowSelect =
				PXAccess.FeatureInstalled<FeaturesSet.lotSerialTracking>()
				&& (
					PXAccess.FeatureInstalled<FeaturesSet.warehouseLocation>()
					|| PXAccess.FeatureInstalled<FeaturesSet.lotSerialTracking>()
					|| PXAccess.FeatureInstalled<FeaturesSet.subItem>()
					|| PXAccess.FeatureInstalled<FeaturesSet.replenishment>()
					|| PXAccess.FeatureInstalled<FeaturesSet.sOToPOLink>()
				)
				;

			OpenPeriodAttribute.SetValidatePeriod<INKitRegister.finPeriodID>(Document.Cache, null, PeriodValidation.DefaultSelectUpdate);
		}
		#endregion

		#region DAC overrides
		#region INKitSerialPart
		[PXDBString(1, IsFixed = true, IsKey = true)]
		[PXDefault(typeof(INKitRegister.docType))]
		protected virtual void _(Events.CacheAttached<INKitSerialPart.docType> e) { }

		[PXDBString(15, IsUnicode = true, IsKey = true)]
		[PXDBDefault(typeof(INKitRegister.refNbr))]
		[PXParent(typeof(Select<INKitRegister, Where<INKitRegister.docType, Equal<Current<INKitSerialPart.docType>>, And<INKitRegister.refNbr, Equal<Current<INKitSerialPart.refNbr>>>>>))]
		protected virtual void _(Events.CacheAttached<INKitSerialPart.refNbr> e) { }
		#endregion

		#region INComponentTranSplit
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[INKitTranSplitPlanID(typeof(INKitRegister.noteID), typeof(INKitRegister.hold), typeof(INKitRegister.transferType))]
		protected virtual void _(Events.CacheAttached<INComponentTranSplit.planID> e) { }
		#endregion

		#region INKitTranSplit
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[INKitTranSplitPlanID(typeof(INKitRegister.noteID), typeof(INKitRegister.hold), typeof(INKitRegister.transferType))]
		protected virtual void _(Events.CacheAttached<INKitTranSplit.planID> e) { }
		#endregion
		#endregion

		#region Actions
		public PXInitializeState<INKitRegister> initializeState;

		public PXAction<INKitRegister> putOnHold;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Hold", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable PutOnHold(PXAdapter adapter) => adapter.Get();

		public PXAction<INKitRegister> releaseFromHold;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Remove Hold")]
		protected virtual IEnumerable ReleaseFromHold(PXAdapter adapter) => adapter.Get();

		public PXAction<INKitRegister> release;
		[PXProcessButton(CommitChanges = true)]
		[PXUIField(DisplayName = Messages.Release, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		protected virtual IEnumerable Release(PXAdapter adapter)
		{
			List<INKitRegister> ret = new List<INKitRegister>();
			foreach (INKitRegister inKitDoc in adapter.Get<INKitRegister>())
			{
				if (inKitDoc.Hold == false && inKitDoc.Released == false)
				{
					Document.Cache.Update(inKitDoc);
					ret.Add(inKitDoc);
				}
			}
			if (ret.Count == 0)
			{
				throw new PXException(Messages.Document_Status_Invalid);
			}

			Save.Press();

			List<INRegister> list = new List<INRegister>();
			foreach (INKitRegister kitRegister in ret)
			{
				INRegister doc = PXSelect<INRegister, Where<INRegister.docType, Equal<Required<INRegister.docType>>, And<INRegister.refNbr, Equal<Required<INRegister.refNbr>>>>>.Select(this, kitRegister.DocType, kitRegister.RefNbr);
				list.Add(doc);
			}

			PXLongOperation.StartOperation(this, delegate () { INDocumentRelease.ReleaseDoc(list, false); });
			return ret;
		}

		public PXAction<INKitRegister> viewBatch;
		[PXButton]
		[PXUIField(DisplayName = "Review Batch", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable ViewBatch(PXAdapter adapter)
		{
			if (Document.Current != null && !String.IsNullOrEmpty(Document.Current.BatchNbr))
			{
				JournalEntry graph = PXGraph.CreateInstance<GL.JournalEntry>();
				graph.BatchModule.Current = graph.BatchModule.Search<Batch.batchNbr>(Document.Current.BatchNbr, "IN");
				throw new PXRedirectRequiredException(graph, "Current batch record");
			}
			return adapter.Get();
		}
		#endregion

		#region Event Handlers
		protected bool _InternalCall = false;
		#region INKitRegister
		protected virtual void _(Events.FieldDefaulting<INKitRegister, INKitRegister.invtMult> e)
		{
			if (e.Row != null)
				e.NewValue = e.Row.DocType == INDocType.Disassembly
					? (short?)-1
					: (short?)+1;
		}

		protected virtual void _(Events.FieldDefaulting<INKitRegister, INKitRegister.projectID> e)
		{
			if (e.Row != null && TryGetNonProject(out var nonProject))
				e.NewValue = nonProject.ContractID;
		}

		protected virtual void _(Events.FieldUpdated<INKitRegister, INKitRegister.kitInventoryID> e)
		{
			if (e.Row != null)
			{
				PXResultset<INKitSpecHdr> resultset = PXSelect<INKitSpecHdr, Where<INKitSpecHdr.kitInventoryID, Equal<Current<INKitRegister.kitInventoryID>>>>.SelectWindowed(this, 0, 2);
				if (resultset.Count == 1)
				{
					e.Row.KitRevisionID = ((INKitSpecHdr)resultset).RevisionID;
					e.Row.SubItemID = ((INKitSpecHdr)resultset).KitSubItemID;
				}
				else
				{
					e.Row.KitRevisionID = null;
				}

				e.Cache.SetDefaultExt<INKitRegister.uOM>(e.Row);
			}
		}

		protected virtual void _(Events.FieldUpdated<INKitRegister, INKitRegister.kitRevisionID> e)
		{
			if (e.Row != null)
			{
				PXResultset<INKitSpecHdr> resultset = PXSelect<INKitSpecHdr, Where<INKitSpecHdr.kitInventoryID, Equal<Current<INKitRegister.kitInventoryID>>>>.SelectWindowed(this, 0, 2);
				if (resultset != null)
				{
					e.Row.SubItemID = ((INKitSpecHdr)resultset).KitSubItemID;
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<INKitRegister, INKitRegister.siteID> e)
		{
			e.Cache.SetDefaultExt<INKitRegister.branchID>(e.Row);
		}

		protected virtual void _(Events.RowSelecting<INKitRegister> e)
		{
			if (e.Row != null && (e.Row.TotalCostStock == null || e.Row.TotalCostNonStock == null))
			{
				using (new PXConnectionScope())
				{
					PXFormulaAttribute.CalcAggregate<INComponentTran.tranCost>(Components.Cache, e.Row, true);
					e.Cache.RaiseFieldUpdated<INKitRegister.totalCostStock>(e.Row, null);

					PXFormulaAttribute.CalcAggregate<INOverheadTran.tranCost>(Overhead.Cache, e.Row, true);
					e.Cache.RaiseFieldUpdated<INKitRegister.totalCostNonStock>(e.Row, null);
				}
			}
		}

		protected virtual void _(Events.RowSelected<INKitRegister> e)
		{
			if (e.Row == null) return;

			INKitSpecHdr spec = Spec.Select();
			bool allowCompAddition = spec?.AllowCompAddition == true;

			lsselect.AllowInsert = e.Row.Released != true && e.Row.KitInventoryID != null && allowCompAddition;
			lsselect.AllowUpdate = e.Row.Released != true;
			lsselect.AllowDelete = e.Row.Released != true && allowCompAddition;

			lsselect2.AllowUpdate = e.Row.Released != true;
			lsselect2.AllowDelete = e.Row.Released != true;

			Overhead.Cache.AllowInsert = e.Row.Released != true && e.Row.KitInventoryID != null && allowCompAddition;
			Overhead.Cache.AllowUpdate = e.Row.Released != true;
			Overhead.Cache.AllowDelete = e.Row.Released != true && allowCompAddition;

			PXUIFieldAttribute.SetEnabled<INKitRegister.kitInventoryID>(e.Cache, e.Row, e.Cache.GetStatus(e.Row) == PXEntryStatus.Inserted);
			PXUIFieldAttribute.SetEnabled<INKitRegister.kitRevisionID>(e.Cache, e.Row, e.Cache.GetStatus(e.Row) == PXEntryStatus.Inserted);
			PXUIFieldAttribute.SetEnabled<INKitRegister.reasonCode>(e.Cache, e.Row, e.Cache.AllowUpdate && e.Row.DocType == INDocType.Disassembly);
		}

		protected virtual void _(Events.RowUpdated<INKitRegister> e)
		{
			if (e.Row != null)
			{
				if (!(e.Row.DocType == INDocType.Disassembly && IsWhenReceivedSerialNumbered(e.Row.KitInventoryID)))
				{
					if (!e.Cache.ObjectsEqual<INKitRegister.kitInventoryID, INKitRegister.kitRevisionID>(e.Row, e.OldRow))
					{
						decimal inBase = INUnitAttribute.ConvertToBase(e.Cache, e.Row.KitInventoryID, e.Row.UOM, (e.Row.Qty ?? 0), INPrecision.QUANTITY);
						RebuildComponents(inBase);
					}
					else if (!e.Cache.ObjectsEqual<INKitRegister.qty, INKitRegister.uOM>(e.Row, e.OldRow))
					{
						decimal inBaseQty = INUnitAttribute.ConvertToBase(e.Cache, e.Row.KitInventoryID, e.Row.UOM, (e.Row.Qty ?? 0), INPrecision.QUANTITY);
						decimal inBaseQtyOld = INUnitAttribute.ConvertToBase(e.Cache, e.Row.KitInventoryID, ((INKitRegister)e.OldRow).UOM, (((INKitRegister)e.OldRow).Qty ?? 0), INPrecision.QUANTITY);
						RecountComponents(inBaseQty, inBaseQtyOld);
					}
				}

				if (!e.Cache.ObjectsEqual<INKitRegister.siteID>(e.Row, e.OldRow))
				{
					foreach (INComponentTran tran in Components.Select())
					{
						INComponentTran copy = (INComponentTran)Components.Cache.CreateCopy(tran);
						copy.BranchID = e.Row.BranchID;
						copy.SiteID = e.Row.SiteID;
						Components.Update(copy);
					}

					foreach (INOverheadTran tran in Overhead.Select())
					{
						INOverheadTran copy = (INOverheadTran)Overhead.Cache.CreateCopy(tran);
						copy.BranchID = e.Row.BranchID;
						copy.SiteID = e.Row.SiteID;
						Overhead.Update(copy);
					}
				}

				if (e.Row.Hold != true && e.Row.Qty == 0m && !e.Cache.ObjectsEqual<INKitRegister.hold, INKitRegister.qty>(e.Row, e.OldRow) &&
					(Components.SelectMain().Any(c => c.Qty > 0m) || Overhead.SelectMain().Any(c => c.Qty > 0m)))
				{
					e.Cache.RaiseExceptionHandling<INKitRegister.qty>(e.Row, e.Row.Qty, new PXSetPropertyException(CS.Messages.Entry_GT, (object)0m));
				}
			}
		}

		protected virtual void _(Events.RowDeleted<INKitRegister> e) => e.Row.LineCntr = 1;

		protected virtual void _(Events.RowPersisting<INKitRegister> e)
		{
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				if (e.Row != null && e.Row.Hold != true && e.Row.Qty == 0m &&
					(Components.SelectMain().Any(c => c.Qty > 0m) || Overhead.SelectMain().Any(c => c.Qty > 0m)))
				{
					e.Cache.RaiseExceptionHandling<INKitRegister.qty>(e.Row, e.Row.Qty, new PXSetPropertyException(CS.Messages.Entry_GT, (object)0m));
				}

				PXDefaultAttribute.SetPersistingCheck<INKitRegister.reasonCode>(e.Cache, e.Row, e.Row.DocType == INDocType.Disassembly ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			}
		}

		protected virtual void _(Events.RowInserted<INKitRegister> e)
		{
			//to compensate for lsselects - delete/insert on Master_Update

			if (e.Row != null)
			{
				if (e.Row.KitInventoryID != null && e.Row.KitRevisionID != null)
				{
					decimal inBase = INUnitAttribute.ConvertToBase(e.Cache, e.Row.KitInventoryID, e.Row.UOM, e.Row.Qty ?? 0, INPrecision.QUANTITY);
					RebuildComponents(inBase);
				}
			}

			Guid? noteid = e.Row.NoteID;

			if (noteid != null)
			{
				PXCache cache = e.Cache.Graph.Caches[typeof(Note)];
				foreach (Note note in cache.Cached)
				{
					if (note.NoteID == noteid)
					{
						if (cache.GetStatus(note) == PXEntryStatus.Deleted)
						{
							cache.SetStatus(note, PXEntryStatus.Updated);
						}
						if (cache.GetStatus(note) == PXEntryStatus.InsertedDeleted)
						{
							cache.SetStatus(note, PXEntryStatus.Inserted);
						}
					}
				}

				cache = e.Cache.Graph.Caches[typeof(NoteDoc)];
				foreach (NoteDoc note in cache.Cached)
				{
					if (note.NoteID == noteid)
					{
						if (cache.GetStatus(note) == PXEntryStatus.Deleted)
						{
							cache.SetStatus(note, PXEntryStatus.Updated);
						}
						if (cache.GetStatus(note) == PXEntryStatus.InsertedDeleted)
						{
							cache.SetStatus(note, PXEntryStatus.Inserted);
						}
					}
				}
			}
		}
		#endregion

		#region INComponentTran
		protected virtual void _(Events.FieldDefaulting<INComponentTran, INComponentTran.unitCost> e)
		{
			e.NewValue = 0m;
			if (e.Row != null && e.Row.InventoryID != null && e.Row.UOM != null && e.Row.SiteID != null)
			{
				var res = (PXResult<INItemSite, InventoryItem>)
					SelectFrom<INItemSite>.
					InnerJoin<InventoryItem>.On<INItemSite.FK.InventoryItem>.
					Where<
						INItemSite.inventoryID.IsEqual<INComponentTran.inventoryID.FromCurrent>.
						And<INItemSite.siteID.IsEqual<INComponentTran.siteID.FromCurrent>>>.
					View.SelectSingleBound(this, new object[] { e.Row });

				if (res == null)
					return;

				(var itemSite, var item) = res;
				if (item != null && item.InventoryID != null)
					e.NewValue = PO.POItemCostManager.ConvertUOM(this, item, item.BaseUnit, itemSite.TranUnitCost.GetValueOrDefault(), e.Row.UOM);
			}
		}

		protected virtual void _(Events.FieldDefaulting<INComponentTran, INComponentTran.projectID> e)
		{
			if (e.Row != null && TryGetNonProject(out var nonProject))
				e.NewValue = nonProject.ContractID;
		}

		protected virtual void _(Events.FieldDefaulting<INComponentTran, INComponentTran.invtMult> e) => e.NewValue = GetInvtMult(e.Row);

		protected virtual void _(Events.FieldDefaulting<INComponentTran, INComponentTran.lineNbr> e)
		{
			if (_InternalCall == false)
			{
				_InternalCall = true;
				object newval;
				try
				{
					e.Cache.RaiseFieldDefaulting<INComponentTran.lineNbr>(e.Row, out newval);
				}
				finally
				{
					_InternalCall = false;
				}

				foreach (INOverheadTran other in Overhead.Cache.Deleted)
				{
					if (other.LineNbr == (int)newval)
					{
						newval = (short)newval + 1;
					}
				}

				foreach (INOverheadTran other in Overhead.Cache.Updated)
				{
					if (other.LineNbr == (short)newval)
					{
						newval = (short)newval + 1;
					}
				}
				e.NewValue = newval;
				e.Cancel = true;
			}
		}

		protected virtual void _(Events.FieldUpdated<INComponentTran, INComponentTran.inventoryID> e)
		{
			e.Cache.SetDefaultExt<INComponentTran.uOM>(e.Row);
			e.Cache.SetDefaultExt<INComponentTran.tranDesc>(e.Row);
			e.Cache.SetDefaultExt<INComponentTran.unitCost>(e.Row);
		}

		protected virtual void _(Events.FieldUpdated<INComponentTran, INComponentTran.siteID> e)
		{
			e.Cache.SetDefaultExt<INComponentTran.unitCost>(e.Row);
		}

		protected virtual void _(Events.FieldVerifying<INComponentTran, INComponentTran.inventoryID> e)
		{
			if (e.Row != null)
			{
				INKitSpecStkDet spec = GetComponentSpecByID(e.Row.InventoryID, e.Row.SubItemID);
				if (spec != null && spec.AllowSubstitution == false && spec.CompInventoryID != Convert.ToInt32(e.NewValue))
				{
					var ex = new PXSetPropertyException(Messages.KitSubstitutionIsRestricted, PXErrorLevel.Error);

					InventoryItem item = InventoryItem.PK.Find(this, (int?)e.NewValue);
					ex.ErrorValue = item?.InventoryCD;

					throw ex;
				}
			}
		}

		protected virtual void _(Events.RowSelected<INComponentTran> e)
		{
			if (e.Row != null)
			{
				PXUIFieldAttribute.SetEnabled<INComponentTran.unitCost>(e.Cache, e.Row, e.Cache.AllowUpdate && e.Row.DocType == INDocType.Disassembly);
				PXUIFieldAttribute.SetEnabled<INComponentTran.reasonCode>(e.Cache, e.Row, e.Cache.AllowUpdate && e.Row.DocType == INDocType.Disassembly);
			}
		}

		protected virtual void _(Events.RowInserting<INComponentTran> e)
		{
			if (e.Row != null && e.Row.InventoryID != null)
				e.Cache.SetDefaultExt<INComponentTran.unitCost>(e.Row);
		}

		protected virtual void _(Events.RowPersisting<INComponentTran> e)
		{
			Debug.Print("INComponentTran_RowPersisting: {0} {1}", e.Operation, e.Row.LineNbr);
			if (e.Row == null || e.Operation.Command() == PXDBOperation.Delete)
				return;

			INKitSpecStkDet spec = GetComponentSpecByID(e.Row.InventoryID, e.Row.SubItemID);
			INKitRegister assembly = Document.Current;

			if (!VerifyQtyVariance(e.Cache, e.Row, spec, assembly))
			{
				if (e.Cache.RaiseExceptionHandling<INComponentTran.qty>(e.Row, e.Row.Qty, new PXSetPropertyException(Messages.KitQtyVarianceIsRestricted)))
				{
					throw new PXSetPropertyException(typeof(INComponentTran.qty).Name, null, Messages.KitQtyVarianceIsRestricted);
				}
			}
			else if (!VerifyQtyBounds(e.Cache, e.Row, spec, assembly))
			{
				if (e.Cache.RaiseExceptionHandling<INComponentTran.qty>(e.Row, e.Row.Qty, new PXSetPropertyException(Messages.KitQtyOutOfBounds, spec.MinCompQty * Document.Current.Qty, spec.MaxCompQty * Document.Current.Qty, spec.UOM)))
				{
					throw new PXSetPropertyException(typeof(INComponentTran.qty).Name, null, Messages.KitQtyOutOfBounds, spec.MinCompQty * Document.Current.Qty, spec.MaxCompQty * Document.Current.Qty, spec.UOM);
				}
			}
		}
		#endregion

		#region INOverheadTran
		protected virtual void _(Events.FieldDefaulting<INOverheadTran, INOverheadTran.unitCost> e)
		{
			e.NewValue = 0m;
			if (e.Row != null && e.Row.InventoryID != null && e.Row.UOM != null)
			{
				InventoryItem item = InventoryItem.PK.Find(this, e.Row.InventoryID);
				if (item != null)
					e.NewValue = PO.POItemCostManager.ConvertUOM(this, item, item.BaseUnit, item.StdCost.GetValueOrDefault(), e.Row.UOM);
			}
		}

		protected virtual void _(Events.FieldDefaulting<INOverheadTran, INOverheadTran.projectID> e)
		{
			if (e.Row != null && TryGetNonProject(out var nonProject))
				e.NewValue = nonProject.ContractID;
		}

		protected virtual void _(Events.FieldDefaulting<INOverheadTran, INOverheadTran.invtMult> e) => e.NewValue = GetInvtMult(e.Row);

		protected virtual void _(Events.FieldDefaulting<INOverheadTran, INOverheadTran.lineNbr> e)
		{
			if (_InternalCall == false)
			{
				_InternalCall = true;
				object newval;
				try
				{
					e.Cache.RaiseFieldDefaulting<INOverheadTran.lineNbr>(e.Row, out newval);
				}
				finally
				{
					_InternalCall = false;
				}

				foreach (INComponentTran other in Components.Cache.Deleted)
				{
					if (other.LineNbr == (short)newval)
					{
						newval = (short)newval + 1;
					}
				}

				foreach (INComponentTran other in Components.Cache.Updated)
				{
					if (other.LineNbr == (short)newval)
					{
						newval = (short)newval + 1;
					}
				}
				e.NewValue = newval;
				e.Cancel = true;
			}
		}

		protected virtual void _(Events.FieldUpdated<INOverheadTran, INOverheadTran.inventoryID> e)
		{
			e.Cache.SetDefaultExt<INOverheadTran.uOM>(e.Row);
			e.Cache.SetDefaultExt<INOverheadTran.tranDesc>(e.Row);
			e.Cache.SetDefaultExt<INOverheadTran.unitCost>(e.Row);
		}

		protected virtual void _(Events.RowSelected<INOverheadTran> e)
		{
			if (e.Row != null)
				PXUIFieldAttribute.SetEnabled<INOverheadTran.reasonCode>(e.Cache, e.Row, e.Cache.AllowUpdate && e.Row.DocType == INDocType.Disassembly);
		}

		protected virtual void _(Events.RowPersisting<INOverheadTran> e)
		{
			if (e.Row == null || e.Operation.Command() == PXDBOperation.Delete)
				return;

			INKitSpecNonStkDet spec = GetNonStockComponentSpecByID(e.Row.InventoryID);
			INKitRegister assembly = Document.Current;

			if (!VerifyQtyVariance(e.Cache, e.Row, spec, assembly))
			{
				if (e.Cache.RaiseExceptionHandling<INOverheadTran.qty>(e.Row, e.Row.Qty, new PXSetPropertyException(Messages.KitQtyVarianceIsRestricted)))
				{
					throw new PXSetPropertyException(typeof(INOverheadTran.qty).Name, null, Messages.KitQtyVarianceIsRestricted);
				}
			}
			else if (!VerifyQtyBounds(e.Cache, e.Row, spec, assembly))
			{
				if (e.Cache.RaiseExceptionHandling<INOverheadTran.qty>(e.Row, e.Row.Qty, new PXSetPropertyException(Messages.KitQtyOutOfBounds, spec.MinCompQty * Document.Current.Qty, spec.MaxCompQty * Document.Current.Qty, spec.UOM)))
				{
					throw new PXSetPropertyException(typeof(INOverheadTran.qty).Name, null, Messages.KitQtyOutOfBounds, spec.MinCompQty * Document.Current.Qty, spec.MaxCompQty * Document.Current.Qty, spec.UOM);
				}
			}
		}
		#endregion

		#region INKitTranSplit
		protected virtual void _(Events.RowSelected<INKitTranSplit> e)
		{
			if (e.Row != null)
			{
				PXUIFieldAttribute.SetEnabled<INKitTranSplit.lotSerialNbr>(e.Cache, e.Row, e.Row.DocType == INDocType.Disassembly);
				PXUIFieldAttribute.SetEnabled<INKitTranSplit.subItemID>(e.Cache, e.Row, e.Cache.GetStatus(e.Row) == PXEntryStatus.Inserted);
				PXUIFieldAttribute.SetEnabled<INKitTranSplit.locationID>(e.Cache, e.Row, e.Cache.GetStatus(e.Row) == PXEntryStatus.Inserted);
			}
		}

		protected virtual void _(Events.RowInserted<INKitTranSplit> e)
		{
			if (e.Row != null && e.Row.DocType == INDocType.Disassembly && IsWhenReceivedSerialNumbered(e.Row.InventoryID) && e.Row.Qty > 0)
				AddKit(e.Row.LotSerialNbr, e.Row.InventoryID);
		}

		protected virtual void _(Events.RowUpdated<INKitTranSplit> e)
		{
			if (e.Row != null && e.Row.DocType == INDocType.Disassembly && IsWhenReceivedSerialNumbered(e.Row.InventoryID))
			{
				if (!e.Cache.ObjectsEqual<INKitTranSplit.qty>(e.Row, e.OldRow))
				{
					if (e.Row.Qty == 0)
						RemoveKit(e.Row.LotSerialNbr, e.Row.InventoryID);
					else
						AddKit(e.Row.LotSerialNbr, e.Row.InventoryID);
				}
				else if (!e.Cache.ObjectsEqual<INKitTranSplit.lotSerialNbr>(e.Row, e.OldRow))
				{
					RemoveKit(e.OldRow.LotSerialNbr, e.OldRow.InventoryID);
					AddKit(e.Row.LotSerialNbr, e.OldRow.InventoryID);
				}
			}
		}

		protected virtual void _(Events.RowDeleted<INKitTranSplit> e)
		{
			if (e.Row != null && e.Row.DocType == INDocType.Disassembly && IsWhenReceivedSerialNumbered(e.Row.InventoryID) && e.Row.Qty > 0)
				RemoveKit(e.Row.LotSerialNbr, e.Row.InventoryID);
		}
		#endregion
		#endregion

		public override int ExecuteUpdate(string viewName, IDictionary keys, IDictionary values, params object[] parameters)
		{
			if (IsCopyPasteContext)
			{
				bool componentsAllowInsert = Components.Cache.AllowInsert;
				bool componentsAllowUpdate = Components.Cache.AllowUpdate;
				bool componentsAllowDelete = Components.Cache.AllowDelete;
				bool overheadAllowInsert = Overhead.Cache.AllowInsert;
				bool overheadAllowUpdate = Overhead.Cache.AllowUpdate;
				bool overheadAllowDelete = Overhead.Cache.AllowDelete;

				try
				{
					Components.Cache.AllowInsert = true;
					Components.Cache.AllowUpdate = true;
					Components.Cache.AllowDelete = true;
					Overhead.Cache.AllowInsert = true;
					Overhead.Cache.AllowUpdate = true;
					Overhead.Cache.AllowDelete = true;

					return base.ExecuteUpdate(viewName, keys, values, parameters);
				}
				finally
				{
					Components.Cache.AllowInsert = componentsAllowInsert;
					Components.Cache.AllowUpdate = componentsAllowUpdate;
					Components.Cache.AllowDelete = componentsAllowDelete;
					Overhead.Cache.AllowInsert = overheadAllowInsert;
					Overhead.Cache.AllowUpdate = overheadAllowUpdate;
					Overhead.Cache.AllowDelete = overheadAllowDelete;
				}

			}
			else
			{
				return base.ExecuteUpdate(viewName, keys, values, parameters);
			}
		}

		public override void Persist()
		{
			if (Document.Current != null)
				DistributeParts();
			base.Persist();
		}

		public virtual void RebuildComponents(decimal numberOfKits)
		{
			if (this.IsCopyPasteContext)
				return;

			foreach (INComponentTran t in Components.Select())
			{
				Components.Delete(t);
			}

			foreach (INOverheadTran t in Overhead.Select())
			{
				Overhead.Delete(t);
			}

			if (Document.Current != null)
			{
				foreach (PXResult<INKitSpecStkDet, InventoryItem> res in SpecComponents.Select())
				{
					INKitSpecStkDet spec = (INKitSpecStkDet)res;
					InventoryItem item = (InventoryItem)res;

					INComponentTran tran = new INComponentTran();
					tran.DocType = Document.Current.DocType;
					tran.TranType = tran.DocType == INDocType.Disassembly ? INTranType.Disassembly : INTranType.Assembly;
					tran.InvtMult = GetInvtMult(tran);
					tran.InventoryID = spec.CompInventoryID;
					tran = PXCache<INComponentTran>.CreateCopy(Components.Insert(tran));

					tran.SubItemID = spec.CompSubItemID;
					if (tran.DocType == INDocType.Disassembly)
					{
						tran.Qty = spec.DfltCompQty * numberOfKits * spec.DisassemblyCoeff;
					}
					else
					{
						tran.Qty = spec.DfltCompQty * numberOfKits;
					}
					tran.UOM = spec.UOM;
					tran.SiteID = Document.Current.SiteID;

					if (Document.Current.DocType == INDocType.Disassembly)
					{
						tran.LocationID = Document.Current.LocationID;
					}

					tran = Components.Update(tran);
				}

				foreach (PXResult<INKitSpecNonStkDet, InventoryItem> res in SpecOverhead.Select())
				{
					INKitSpecNonStkDet spec = (INKitSpecNonStkDet)res;
					InventoryItem item = (InventoryItem)res;

					INOverheadTran tran = new INOverheadTran();
					tran.DocType = Document.Current.DocType;
					tran.TranType = tran.DocType == INDocType.Disassembly ? INTranType.Disassembly : INTranType.Assembly;
					tran.InvtMult = GetInvtMult(tran);
					tran.InventoryID = spec.CompInventoryID;
					tran.Qty = spec.DfltCompQty * numberOfKits;
					tran.UOM = spec.UOM;
					tran.SiteID = Document.Current.SiteID;

					Overhead.Insert(tran);
				}
			}


		}

		public virtual void RecountComponents(decimal numberOfKits, decimal oldNumberOfKits)
		{
			foreach (PXResult<INComponentTran, InventoryItem, INKitSpecStkDet> res in Components.Select())
			{
				INComponentTran tran = (INComponentTran)Components.Cache.CreateCopy((INComponentTran)res);
				INKitSpecStkDet spec = (INKitSpecStkDet)res;

				if (spec.DfltCompQty != null)
				{
					if (tran.DocType == INDocType.Disassembly)
					{
						tran.Qty = spec.DfltCompQty * numberOfKits * spec.DisassemblyCoeff;
					}
					else
					{
						tran.Qty = spec.DfltCompQty * numberOfKits;
					}
				}
				else
				{
					//Component not found in Specs. Prorate Qty:
					if (oldNumberOfKits > 0)
					{
						tran.Qty = tran.Qty * numberOfKits / oldNumberOfKits;
					}
					else
					{
						tran.Qty = numberOfKits;
					}
				}

				if (spec.UOM != null)
					tran.UOM = spec.UOM;
				Components.Update(tran);
			}

			foreach (PXResult<INOverheadTran, InventoryItem, INKitSpecNonStkDet> res in Overhead.Select())
			{
				INOverheadTran tran = (INOverheadTran)Overhead.Cache.CreateCopy((INOverheadTran)res); ;
				INKitSpecNonStkDet spec = (INKitSpecNonStkDet)res;

				if (spec.DfltCompQty != null)
				{
					tran.Qty = (spec.DfltCompQty ?? 1) * numberOfKits;
				}
				else
				{
					//Component not found in Specs. Prorate Qty:

					if (oldNumberOfKits > 0)
					{
						tran.Qty = tran.Qty * numberOfKits / oldNumberOfKits;
					}
					else
					{
						tran.Qty = numberOfKits;
					}
				}

				if (spec.UOM != null)
					tran.UOM = spec.UOM;
				Overhead.Update(tran);
			}

		}

		public virtual bool VerifyQtyVariance(PXCache sender, INTran row, INKitSpecStkDet spec, INKitRegister assembly)
		{
			if (Document.Current != null && row != null && spec != null && spec.AllowQtyVariation == false)
			{
				decimal inBase = INUnitAttribute.ConvertToBase(sender, row.InventoryID, row.UOM, (row.Qty ?? 0), INPrecision.QUANTITY);
				decimal UOMQtyCoef = INUnitAttribute.ConvertToBase(sender, assembly.KitInventoryID, assembly.UOM, (assembly.Qty ?? 0), INPrecision.QUANTITY);
				decimal value = (spec.DfltCompQty ?? 0) * UOMQtyCoef;
				if (IsSerialNumbered(row.InventoryID)) value = Math.Ceiling(value);
				decimal inBaseSpec = INUnitAttribute.ConvertToBase(sender, row.InventoryID, spec.UOM, value, INPrecision.QUANTITY);

				if (Document.Current.DocType != INDocType.Disassembly)
				{
					return inBase == inBaseSpec;
				}
			}

			return true;
		}

		public virtual bool VerifyQtyVariance(PXCache sender, INOverheadTran row, INKitSpecNonStkDet spec, INKitRegister assembly)
		{
			if (Document.Current != null && row != null && spec != null && spec.AllowQtyVariation == false)
			{
				decimal inBase = INUnitAttribute.ConvertToBase(sender, row.InventoryID, row.UOM, (row.Qty ?? 0), INPrecision.QUANTITY);
				decimal UOMQtyCoef = INUnitAttribute.ConvertToBase(sender, assembly.KitInventoryID, assembly.UOM, (assembly.Qty ?? 0), INPrecision.QUANTITY);
				decimal inBaseSpec = INUnitAttribute.ConvertToBase(sender, row.InventoryID, spec.UOM, (spec.DfltCompQty ?? 0) * UOMQtyCoef, INPrecision.QUANTITY);

				if (Document.Current.DocType != INDocType.Disassembly)
				{
					return inBase == inBaseSpec;
				}
			}

			return true;
		}

		public virtual bool VerifyQtyBounds(PXCache sender, INTran row, INKitSpecStkDet spec, INKitRegister assembly)
		{
			if (Document.Current != null && row != null && spec != null && spec.AllowQtyVariation == true && Document.Current.DocType != INDocType.Disassembly)
			{
				decimal inBase = INUnitAttribute.ConvertToBase(sender, row.InventoryID, row.UOM, row.Qty ?? 0, INPrecision.QUANTITY);
				decimal UOMQtyCoef = INUnitAttribute.ConvertToBase(sender, assembly.KitInventoryID, assembly.UOM, (assembly.Qty ?? 0), INPrecision.QUANTITY);

				if (spec.MinCompQty != null)
				{
					decimal inBaseSpec = INUnitAttribute.ConvertToBase(sender, row.InventoryID, spec.UOM, spec.MinCompQty.Value * UOMQtyCoef, INPrecision.QUANTITY);

					if (inBase < inBaseSpec)
						return false;
				}

				if (spec.MaxCompQty != null)
				{
					decimal inBaseSpec = INUnitAttribute.ConvertToBase(sender, row.InventoryID, spec.UOM, spec.MaxCompQty.Value * UOMQtyCoef, INPrecision.QUANTITY);

					if (inBase > inBaseSpec)
						return false;
				}
			}

			return true;
		}

		public virtual bool VerifyQtyBounds(PXCache sender, INOverheadTran row, INKitSpecNonStkDet spec, INKitRegister assembly)
		{
			if (Document.Current != null && row != null && spec != null && spec.AllowQtyVariation == true && Document.Current.DocType != INDocType.Disassembly)
			{
				decimal inBase = INUnitAttribute.ConvertToBase(sender, row.InventoryID, row.UOM, row.Qty ?? 0, INPrecision.QUANTITY);
				decimal UOMQtyCoef = INUnitAttribute.ConvertToBase(sender, assembly.KitInventoryID, assembly.UOM, (assembly.Qty ?? 0), INPrecision.QUANTITY);

				if (spec.MinCompQty != null)
				{
					decimal inBaseSpec = INUnitAttribute.ConvertToBase(sender, row.InventoryID, spec.UOM, spec.MinCompQty.Value * UOMQtyCoef, INPrecision.QUANTITY);

					if (inBase < inBaseSpec)
						return false;
				}

				if (spec.MaxCompQty != null)
				{
					decimal inBaseSpec = INUnitAttribute.ConvertToBase(sender, row.InventoryID, spec.UOM, spec.MaxCompQty.Value * UOMQtyCoef, INPrecision.QUANTITY);

					if (inBase > inBaseSpec)
						return false;
				}
			}

			return true;
		}

		public virtual short? GetInvtMult(INTran tran)
		{
			short? result = null;

			if (Document.Current != null)
			{
				if (Document.Current.DocType == INDocType.Disassembly)
				{
					if (tran.LineNbr != Document.Current.KitLineNbr)
					{
						result = 1;
					}
					else
					{
						result = -1;
					}
				}
				else
				{
					if (tran.LineNbr != Document.Current.KitLineNbr)
					{
						result = -1;
					}
					else
					{
						result = 1;
					}
				}
			}

			return result;
		}

		public virtual short? GetInvtMult(INOverheadTran tran)
		{
			short? result = null;

			if (Document.Current != null)
			{
				if (Document.Current.DocType == INDocType.Disassembly)
				{
					if (tran.LineNbr != Document.Current.KitLineNbr)
					{
						result = 1;
					}
					else
					{
						result = -1;
					}
				}
				else
				{
					if (tran.LineNbr != Document.Current.KitLineNbr)
					{
						result = -1;
					}
					else
					{
						result = 1;
					}
				}
			}

			return result;
		}

		protected virtual bool TryGetNonProject(out CT.Contract nonProject)
		{
			nonProject = PXSelect<CT.Contract, Where<CT.Contract.nonProject, Equal<True>>>.Select(this);
			return nonProject != null;
		}

		public virtual bool IsSerialNumbered(int? inventoryID)
		{
			bool result = false;
			var item = InventoryItem.PK.Find(this, inventoryID);
			var lotSerClass = INLotSerClass.PK.Find(this, item?.LotSerClassID);
			if (lotSerClass != null && lotSerClass.LotSerTrack == INLotSerTrack.SerialNumbered)
			{
				result = true;
			}

			return result;
		}

		public virtual bool IsWhenReceivedSerialNumbered(int? inventoryID)
		{
			bool result = false;
			var item = InventoryItem.PK.Find(this, inventoryID);
			var lotSerClass = INLotSerClass.PK.Find(this, item?.LotSerClassID);

			if (lotSerClass != null && lotSerClass.LotSerTrack == INLotSerTrack.SerialNumbered && lotSerClass.LotSerAssign == INLotSerAssign.WhenReceived)
			{
				result = true;
			}

			return result;
		}

		public virtual void DistributeParts()
		{
			Dictionary<string, INKitSerialPart> tracks = GetPartsDistribution();
			foreach (INKitSerialPart part in SerialNumberedParts.Select())
			{
				if (!tracks.ContainsKey(string.Format("{0}.{1}-{2}.{3}", part.KitLineNbr, part.KitSplitLineNbr, part.PartLineNbr, part.PartSplitLineNbr)))
				{
					SerialNumberedParts.Delete(part);
				}
			}

			foreach (INKitSerialPart track in tracks.Values)
			{
				INKitSerialPart part = SerialNumberedParts.Locate(track);

				if (part == null)
				{
					SerialNumberedParts.Insert(track);
				}
			}
		}

		public virtual Dictionary<string, INKitSerialPart> GetPartsDistribution()
		{
			Dictionary<string, INKitSerialPart> list = new Dictionary<string, INKitSerialPart>();
			if (IsSerialNumbered(Document.Current.KitInventoryID))
			{
				PXResultset<INKitTranSplit> kitSplits = MasterSplits.Select();

				PXSelectBase<INComponentTranSplit> CompSplits = new PXSelect<INComponentTranSplit,
				Where<INComponentTranSplit.docType, Equal<Required<INComponentTran.docType>>,
				And<INComponentTranSplit.refNbr, Equal<Required<INComponentTran.refNbr>>,
				And<INComponentTranSplit.lineNbr, Equal<Required<INComponentTran.lineNbr>>>>>>(this);

				for (int kitIndex = 0; kitIndex < kitSplits.Count; kitIndex++)
				{
					foreach (INComponentTran component in Components.Select())
					{
						if (IsWhenReceivedSerialNumbered(component.InventoryID))
						{
							PXResultset<INComponentTranSplit> compSplits = CompSplits.Select(component.DocType, component.RefNbr, component.LineNbr);

							if (compSplits.Count % kitSplits.Count != 0)
							{
								if (Components.Cache.GetStatus(component) == PXEntryStatus.Notchanged)
								{
									Components.Cache.SetStatus(component, PXEntryStatus.Modified);
								}

								if (Components.Cache.RaiseExceptionHandling<INComponentTran.qty>(component, component.Qty, new PXSetPropertyException(Messages.KitQtyNotEvenDistributed)))
								{
									throw new PXSetPropertyException(typeof(INComponentTran.qty).Name, null, Messages.KitQtyNotEvenDistributed);
								}
							}

							int partsInKit = compSplits.Count / kitSplits.Count;
							int startIndex = kitIndex * partsInKit;
							for (int partIndex = startIndex; partIndex < startIndex + partsInKit; partIndex++)
							{
								INKitTranSplit kitSplit = kitSplits[kitIndex];
								INComponentTranSplit partSplit = compSplits[partIndex];

								INKitSerialPart track = new INKitSerialPart();
								track.DocType = kitSplit.DocType;
								track.RefNbr = kitSplit.RefNbr;
								track.KitLineNbr = kitSplit.LineNbr;
								track.KitSplitLineNbr = kitSplit.SplitLineNbr;
								track.PartLineNbr = partSplit.LineNbr;
								track.PartSplitLineNbr = partSplit.SplitLineNbr;

								list.Add(string.Format("{0}.{1}-{2}.{3}", track.KitLineNbr, track.KitSplitLineNbr, track.PartLineNbr, track.PartSplitLineNbr), track);
							}
						}
					}
				}
			}
			return list;
		}

		public virtual void AddKit(string serialNumber, int? inventoryID)
		{
			INTranSplit originalMasterTranSplit;
			INTran originalMasterTran;
			INRegister originalDoc;

			SearchOriginalAssembySplitLine(serialNumber, inventoryID, out originalMasterTranSplit, out originalMasterTran, out originalDoc);
			if (originalMasterTranSplit != null)
			{
				decimal numberOfKits = originalMasterTran.Qty.Value;

				#region Stock Items / Components

				PXResultset<INTran> items = PXSelectJoin<INTran,
							InnerJoin<InventoryItem, On2<INTran.FK.InventoryItem,
								And<InventoryItem.stkItem, Equal<True>>>,
							LeftJoin<INKitSpecStkDet, On<INTran.inventoryID, Equal<INKitSpecStkDet.compInventoryID>,
								And<INKitSpecStkDet.kitInventoryID, Equal<Required<INKitSpecStkDet.kitInventoryID>>,
								And<INKitSpecStkDet.revisionID, Equal<Required<INKitSpecStkDet.revisionID>>>>>>>,
							Where<INTran.docType, Equal<INDocType.production>,
							And<INTran.refNbr, Equal<Required<INTran.refNbr>>,
							And<INTran.invtMult, Equal<shortMinus1>>>>>.Select(this, originalDoc.KitInventoryID, originalDoc.KitRevisionID, originalDoc.RefNbr);

				foreach (PXResult<INTran, InventoryItem, INKitSpecStkDet> res in items)
				{
					InventoryItem item = (InventoryItem)res;
					INTran origTran = (INTran)res;
					INKitSpecStkDet spec = (INKitSpecStkDet)res;

					INComponentTran tran = GetComponentByInventoryID(item.InventoryID);

					if (tran != null)
					{
						Components.Current = tran;

						if (IsWhenReceivedSerialNumbered(item.InventoryID))
						{
							#region Add Splits
							PXResultset<INTranSplit> parts = GetComponentSplits(originalMasterTranSplit, origTran);
							foreach (INTranSplit part in parts)
							{
								INComponentTranSplit split = PXCache<INComponentTranSplit>.CreateCopy(ComponentSplits.Insert(new INComponentTranSplit()));
								split.LotSerialNbr = part.LotSerialNbr;
								split.Qty = part.Qty;
								ComponentSplits.Update(split);
							}
							#endregion
						}
						else
						{
							INComponentTran copy = PXCache<INComponentTran>.CreateCopy(tran);
							tran.Qty += (origTran.Qty / numberOfKits) * (spec.DisassemblyCoeff ?? 1);
							Components.Cache.SetValueExt<INComponentTran.qty>(tran, tran.Qty);
							Components.Cache.MarkUpdated(tran);
							Components.Cache.RaiseRowUpdated(tran, copy);
						}
					}
					else
					{
						#region Insert New Component Tran

						tran = new INComponentTran();
						tran.DocType = Document.Current.DocType;
						tran.TranType = tran.DocType == INDocType.Disassembly ? INTranType.Disassembly : INTranType.Assembly;
						tran.InvtMult = GetInvtMult(tran);
						tran.InventoryID = origTran.InventoryID;
						tran.SubItemID = origTran.SubItemID;
						tran.UOM = origTran.UOM;


						if (IsWhenReceivedSerialNumbered(item.InventoryID))
						{
							Components.Insert(tran);

							#region Add Splits

							PXResultset<INTranSplit> parts = GetComponentSplits(originalMasterTranSplit, origTran);
							foreach (INTranSplit part in parts)
							{
								INComponentTranSplit split = PXCache<INComponentTranSplit>.CreateCopy(ComponentSplits.Insert(new INComponentTranSplit()));
								split.LotSerialNbr = part.LotSerialNbr;
								split.Qty = part.Qty;
								ComponentSplits.Update(split);
							}

							#endregion

						}
						else
						{
							tran.Qty = (origTran.Qty / numberOfKits) * (spec.DisassemblyCoeff ?? 1);
							Components.Insert(tran);
						}

						#endregion
					}
				}

				#endregion

				#region Non Stock Items /Overhead

				PXResultset<INTran> overheadItems = PXSelectJoin<INTran,
							InnerJoin<InventoryItem,
								On2<INTran.FK.InventoryItem,
								And<InventoryItem.stkItem, Equal<boolFalse>>>,
							LeftJoin<INKitSpecNonStkDet, On<INTran.inventoryID, Equal<INKitSpecNonStkDet.compInventoryID>,
								And<INKitSpecNonStkDet.kitInventoryID, Equal<Required<INKitSpecNonStkDet.kitInventoryID>>,
								And<INKitSpecNonStkDet.revisionID, Equal<Required<INKitSpecNonStkDet.revisionID>>>>>>>,
							Where<INTran.docType, Equal<INDocType.production>,
							And<INTran.refNbr, Equal<Required<INTran.refNbr>>,
							And<INTran.invtMult, Equal<shortMinus1>>>>>.Select(this, originalDoc.KitInventoryID, originalDoc.KitRevisionID, originalDoc.RefNbr);


				foreach (PXResult<INTran, InventoryItem, INKitSpecNonStkDet> res in overheadItems)
				{
					InventoryItem item = (InventoryItem)res;
					INTran origTran = (INTran)res;
					INKitSpecNonStkDet spec = (INKitSpecNonStkDet)res;

					INOverheadTran tran = GetOverheadByInventoryID(item.InventoryID);

					if (tran != null)
					{
						tran.Qty += origTran.Qty / numberOfKits;
						Overhead.Update(tran);
					}
					else
					{
						#region Insert New Overhead Tran

						tran = new INOverheadTran();
						tran.DocType = Document.Current.DocType;
						tran.TranType = tran.DocType == INDocType.Disassembly ? INTranType.Disassembly : INTranType.Assembly;
						tran.InvtMult = GetInvtMult(tran);
						tran.InventoryID = origTran.InventoryID;
						tran.Qty = origTran.Qty / numberOfKits;
						tran.UOM = origTran.UOM;
						tran.SiteID = origTran.SiteID;
						//location for disassembled components will be default from default receipt location
						//tran.LocationID = origTran.LocationID;

						Overhead.Insert(tran);

						#endregion
					}
				}

				#endregion

			}
			else
			{
				#region Original Kit Assembly was not found - Use Specification
				foreach (PXResult<INKitSpecStkDet, InventoryItem> res in SpecComponents.Select())
				{
					INKitSpecStkDet spec = (INKitSpecStkDet)res;
					InventoryItem item = (InventoryItem)res;

					INComponentTran tran = GetComponentByInventoryID(item.InventoryID);

					if (tran != null)
					{
						INComponentTran copy = PXCache<INComponentTran>.CreateCopy(tran);
						tran.Qty += spec.DfltCompQty * (spec.DisassemblyCoeff ?? 1);
						Components.Cache.SetValueExt<INComponentTran.qty>(tran, tran.Qty);
						Components.Cache.MarkUpdated(tran);
						Components.Cache.RaiseRowUpdated(tran, copy);
					}
					else
					{
						#region Insert New Component Tran

						tran = new INComponentTran();
						tran.DocType = Document.Current.DocType;
						tran.TranType = tran.DocType == INDocType.Disassembly ? INTranType.Disassembly : INTranType.Assembly;
						tran.InvtMult = GetInvtMult(tran);
						tran.InventoryID = spec.CompInventoryID;
						tran.SubItemID = spec.CompSubItemID;
						tran.UOM = spec.UOM;
						tran.SiteID = Document.Current.SiteID;
						//location for disassembled components will be default from default receipt location
						//tran.LocationID = Document.Current.LocationID;
						tran.Qty = spec.DfltCompQty * (spec.DisassemblyCoeff ?? 1);
						Components.Insert(tran);

						#endregion
					}

				}

				foreach (PXResult<INKitSpecNonStkDet, InventoryItem> res in SpecOverhead.Select())
				{
					INKitSpecNonStkDet spec = (INKitSpecNonStkDet)res;
					InventoryItem item = (InventoryItem)res;

					INOverheadTran tran = GetOverheadByInventoryID(item.InventoryID);

					if (tran != null)
					{
						tran.Qty += spec.DfltCompQty;
						Overhead.Update(tran);
					}
					else
					{
						#region Insert New Overhad Tran
						tran = new INOverheadTran();
						tran.DocType = Document.Current.DocType;
						tran.TranType = tran.DocType == INDocType.Disassembly ? INTranType.Disassembly : INTranType.Assembly;
						tran.InvtMult = GetInvtMult(tran);
						tran.InventoryID = spec.CompInventoryID;
						tran.Qty = spec.DfltCompQty;
						tran.UOM = spec.UOM;
						tran.SiteID = Document.Current.SiteID;
						//location for disassembled components will be default from default receipt location
						//tran.LocationID = Document.Current.LocationID;

						Overhead.Insert(tran);
						#endregion
					}
				}
				#endregion
			}
		}

		public virtual void RemoveKit(string serialNumber, int? inventoryID)
		{
			INTranSplit originalMasterTranSplit;
			INTran originalMasterTran;
			INRegister originalDoc;

			SearchOriginalAssembySplitLine(serialNumber, inventoryID, out originalMasterTranSplit, out originalMasterTran, out originalDoc);
			if (originalMasterTranSplit != null)
			{
				decimal numberOfKits = originalMasterTran.Qty.Value;

				#region Stock Items / Components
				PXResultset<INTran> items = PXSelectJoin<INTran,
							InnerJoin<InventoryItem, On<INTran.FK.InventoryItem>,
							LeftJoin<INKitSpecStkDet, On<INTran.inventoryID, Equal<INKitSpecStkDet.compInventoryID>>>>,
							Where<INTran.docType, Equal<INDocType.production>,
							And<INTran.refNbr, Equal<Required<INTran.refNbr>>,
							And<INTran.invtMult, Equal<shortMinus1>,
							And<INKitSpecStkDet.kitInventoryID, Equal<Required<INKitSpecStkDet.kitInventoryID>>,
							And<INKitSpecStkDet.revisionID, Equal<Required<INKitSpecStkDet.revisionID>>>>>>>>.Select(this, originalDoc.RefNbr, originalDoc.KitInventoryID, originalDoc.KitRevisionID);

				foreach (PXResult<INTran, InventoryItem, INKitSpecStkDet> res in items)
				{
					InventoryItem item = (InventoryItem)res;
					INTran origTran = (INTran)res;
					INKitSpecStkDet spec = (INKitSpecStkDet)res;

					INComponentTran tran = GetComponentByInventoryID(item.InventoryID);

					if (tran != null)
					{
						Components.Current = tran;

						if (IsSerialNumbered(item.InventoryID))
						{
							#region Remove Splits
							PXResultset<INTranSplit> parts = GetComponentSplits(originalMasterTranSplit, origTran);
							foreach (INTranSplit part in parts)
							{
								INComponentTranSplit split = PXSelect<INComponentTranSplit,
									Where<INComponentTranSplit.docType, Equal<Current<INKitRegister.docType>>,
									And<INComponentTranSplit.refNbr, Equal<Current<INKitRegister.refNbr>>,
									And<INComponentTranSplit.lineNbr, Equal<Required<INComponentTranSplit.lineNbr>>,
									And<INComponentTranSplit.lotSerialNbr, Equal<Required<INComponentTranSplit.lotSerialNbr>>>>>>>.Select(this, tran.LineNbr, part.LotSerialNbr);

								INComponentTran parent = (INComponentTran)LSParentAttribute.SelectParent(ComponentSplits.Cache, split, typeof(INComponentTran));

								ComponentSplits.Delete(split);

								INComponentTran copy = PXCache<INComponentTran>.CreateCopy(parent);
								copy.Qty--;
								//copy.UnassignedQty--;
								Components.Cache.Update(copy);
							}
							#endregion
						}
						else
						{
							INComponentTran copy = PXCache<INComponentTran>.CreateCopy(tran);
							tran.Qty -= (origTran.Qty / numberOfKits) * spec.DisassemblyCoeff ?? 1;
							if (tran.Qty < 0)
								tran.Qty = 0;
							Components.Cache.SetValueExt<INComponentTran.qty>(tran, tran.Qty);
							Components.Cache.MarkUpdated(tran);
							Components.Cache.RaiseRowUpdated(tran, copy);
						}
					}
				}
				#endregion

				#region NonStcok Items / Overhead
				PXResultset<INTran> overheadItems = PXSelectJoin<INTran,
							InnerJoin<InventoryItem, On<INTran.FK.InventoryItem>,
							LeftJoin<INKitSpecNonStkDet, On<INTran.inventoryID, Equal<INKitSpecNonStkDet.compInventoryID>>>>,
							Where<INTran.docType, Equal<INDocType.production>,
							And<INTran.refNbr, Equal<Required<INTran.refNbr>>,
							And<INTran.invtMult, Equal<shortMinus1>,
							And<INKitSpecNonStkDet.kitInventoryID, Equal<Required<INKitSpecNonStkDet.kitInventoryID>>,
							And<INKitSpecNonStkDet.revisionID, Equal<Required<INKitSpecNonStkDet.revisionID>>>>>>>>.Select(this, originalDoc.RefNbr, originalDoc.KitInventoryID, originalDoc.KitRevisionID);

				foreach (PXResult<INTran, InventoryItem, INKitSpecNonStkDet> res in overheadItems)
				{
					InventoryItem item = (InventoryItem)res;
					INTran origTran = (INTran)res;
					INKitSpecNonStkDet spec = (INKitSpecNonStkDet)res;

					INOverheadTran tran = GetOverheadByInventoryID(item.InventoryID);

					if (tran != null)
					{
						tran.Qty -= origTran.Qty / numberOfKits;

						if (tran.Qty < 0)
							tran.Qty = 0;

						Overhead.Update(tran);
					}
				}
				#endregion
			}
			else
			{
				#region Original Kit Assembly was not found - Use Specification
				foreach (PXResult<INKitSpecStkDet, InventoryItem> res in SpecComponents.Select())
				{
					INKitSpecStkDet spec = (INKitSpecStkDet)res;
					InventoryItem item = (InventoryItem)res;

					INComponentTran tran = GetComponentByInventoryID(item.InventoryID);

					if (tran != null)
					{
						INComponentTran copy = PXCache<INComponentTran>.CreateCopy(tran);
						tran.Qty -= spec.DfltCompQty * spec.DisassemblyCoeff ?? 1;

						if (tran.Qty < 0)
							tran.Qty = 0;
						Components.Cache.SetValueExt<INComponentTran.qty>(tran, tran.Qty);
						Components.Cache.MarkUpdated(tran);
						Components.Cache.RaiseRowUpdated(tran, copy);
					}
				}

				foreach (PXResult<INKitSpecNonStkDet, InventoryItem> res in SpecOverhead.Select())
				{
					INKitSpecNonStkDet spec = (INKitSpecNonStkDet)res;
					InventoryItem item = (InventoryItem)res;

					INOverheadTran tran = GetOverheadByInventoryID(item.InventoryID);

					if (tran != null)
					{
						tran.Qty -= spec.DfltCompQty;
						if (tran.Qty < 0)
							tran.Qty = 0;

						Overhead.Update(tran);
					}
				}
				#endregion
			}
		}

		public virtual void SearchOriginalAssembySplitLine(string serialNumber, int? inventoryID, out INTranSplit originalSplit, out INTran originalMasterTran, out INRegister originalDoc)
		{
			originalSplit = null;
			originalMasterTran = null;
			originalDoc = null;

			if (!string.IsNullOrEmpty(serialNumber))
			{
				PXSelectBase<INTranSplit> split =
				 new PXSelectJoin<INTranSplit,
				 InnerJoin<INTran, On<INTranSplit.FK.Tran>,
				 InnerJoin<INRegister, On<INRegister.FK.KitTran>>>,
				 Where<INRegister.docType, Equal<INDocType.production>,
					 And<INTranSplit.lotSerialNbr, Equal<Required<INTranSplit.lotSerialNbr>>,
					 And<INTranSplit.inventoryID, Equal<Required<INTranSplit.inventoryID>>,
					 And<INTran.qty, NotEqual<decimal0>>>>>>(this);

				PXResultset<INTranSplit> set = split.Select(serialNumber, inventoryID);
				if (set != null && set.Count > 0)
				{
					PXResult<INTranSplit, INTran, INRegister> res = (PXResult<INTranSplit, INTran, INRegister>)set[0];
					originalSplit = (INTranSplit)res;
					originalMasterTran = (INTran)res;
					originalDoc = (INRegister)res;
				}
			}
		}

		public virtual INKitSpecStkDet GetComponentSpecByID(int? inventoryID, int? subItemID)
		{
			return PXSelect<INKitSpecStkDet, Where<INKitSpecStkDet.kitInventoryID, Equal<Current<INKitRegister.kitInventoryID>>,
				And<INKitSpecStkDet.revisionID, Equal<Current<INKitRegister.kitRevisionID>>,
				And<INKitSpecStkDet.compInventoryID, Equal<Required<INKitSpecStkDet.compInventoryID>>,
				And<Where<INKitSpecStkDet.compSubItemID, Equal<Required<INKitSpecStkDet.compSubItemID>>,
					Or<Required<INKitSpecStkDet.compSubItemID>, IsNull>>>>>>>.Select(this, inventoryID, subItemID, subItemID);
		}

		public virtual INKitSpecNonStkDet GetNonStockComponentSpecByID(int? inventoryID)
		{
			return PXSelect<INKitSpecNonStkDet, Where<INKitSpecNonStkDet.kitInventoryID, Equal<Current<INKitRegister.kitInventoryID>>,
				And<INKitSpecNonStkDet.revisionID, Equal<Current<INKitRegister.kitRevisionID>>,
				And<INKitSpecNonStkDet.compInventoryID, Equal<Required<INKitSpecStkDet.compInventoryID>>>>>>.Select(this, inventoryID);
		}

		public virtual InventoryItem GetInventoryItemByID(int? inventoryID)
		{
			return InventoryItem.PK.Find(this, inventoryID);
		}

		public virtual INComponentTran GetComponentByInventoryID(int? inventoryID)
		{
			return PXSelect<INComponentTran,
							Where<INComponentTran.docType, Equal<Current<INKitRegister.docType>>,
							And<INComponentTran.refNbr, Equal<Current<INKitRegister.refNbr>>,
							And<INComponentTran.inventoryID, Equal<Required<INComponentTran.inventoryID>>>>>>.Select(this, inventoryID);

		}

		public virtual INOverheadTran GetOverheadByInventoryID(int? inventoryID)
		{
			return PXSelect<INOverheadTran,
							Where<INOverheadTran.docType, Equal<Current<INKitRegister.docType>>,
							And<INOverheadTran.refNbr, Equal<Current<INKitRegister.refNbr>>,
							And<INOverheadTran.inventoryID, Equal<Required<INOverheadTran.inventoryID>>>>>>.Select(this, inventoryID);

		}

		public virtual PXResultset<INTranSplit> GetComponentSplits(INTranSplit originalKitSplit, INTran originalComponent)
		{
			return PXSelectJoin<INTranSplit,
				InnerJoin<INKitSerialPart, On<INKitSerialPart.docType, Equal<INTranSplit.docType>,
					And<INKitSerialPart.refNbr, Equal<INTranSplit.refNbr>,
					And<INKitSerialPart.partLineNbr, Equal<INTranSplit.lineNbr>,
					And<INKitSerialPart.partSplitLineNbr, Equal<INTranSplit.splitLineNbr>>>>>>,
				Where<INKitSerialPart.docType, Equal<Required<INKitSerialPart.docType>>,
				And<INKitSerialPart.refNbr, Equal<Required<INKitSerialPart.refNbr>>,
				And<INKitSerialPart.kitLineNbr, Equal<Required<INKitSerialPart.kitLineNbr>>,
				And<INKitSerialPart.kitSplitLineNbr, Equal<Required<INKitSerialPart.kitSplitLineNbr>>,
				And<INKitSerialPart.partLineNbr, Equal<Required<INKitSerialPart.partLineNbr>>>>>>>>.Select(this, originalKitSplit.DocType, originalKitSplit.RefNbr, originalKitSplit.LineNbr, originalKitSplit.SplitLineNbr, originalComponent.LineNbr);
		}
	}

	#region DAC Extensions
	[PXProjection(typeof(Select<INTran, Where<INTran.assyType, Equal<INAssyType.compTran>>>), Persistent = true)]
	[PXCacheName(Messages.INComponentTran)]
	public partial class INComponentTran : INTran
	{
		#region Keys
		public new class PK : PrimaryKeyOf<INComponentTran>.By<docType, refNbr, lineNbr>
		{
			public static INComponentTran Find(PXGraph graph, string docType, string refNbr, int? lineNbr) => FindBy(graph, docType, refNbr, lineNbr);
		}
		public new static class FK
		{
			public class Branch : GL.Branch.PK.ForeignKeyOf<INComponentTran>.By<branchID> { }
			public class KitRegister : INKitRegister.PK.ForeignKeyOf<INComponentTran>.By<docType, refNbr> { }
			public class Project : PMProject.PK.ForeignKeyOf<INComponentTran>.By<projectID> { }
			public class InventoryItem : IN.InventoryItem.PK.ForeignKeyOf<INComponentTran>.By<inventoryID> { }
			public class SubItem : INSubItem.PK.ForeignKeyOf<INComponentTran>.By<subItemID> { }
			public class Site : INSite.PK.ForeignKeyOf<INComponentTran>.By<siteID> { }
			public class Location : INLocation.PK.ForeignKeyOf<INComponentTran>.By<locationID> { }
			public class ReasonCode : CS.ReasonCode.PK.ForeignKeyOf<INComponentTran>.By<reasonCode> { }
			//todo public class UnitOfMeasure : INUnit.UK.ByInventory.ForeignKeyOf<INComponentTran>.By<inventoryID, uOM> { }
			//todo public class FinancialPeriod : GL.FinPeriods.TableDefinition.FinPeriod.PK.ForeignKeyOf<INComponentTran>.By<finPeriodID> { }
			//todo public class MasterFinancialPeriod : GL.FinPeriods.TableDefinition.FinPeriod.PK.ForeignKeyOf<INComponentTran>.By<tranPeriodID> { }
		}
		#endregion
		#region BranchID
		[Branch(typeof(INKitRegister.branchID), Visibility = PXUIVisibility.Invisible, Visible = false, Enabled = false)]
		public override Int32? BranchID
		{
			get => this._BranchID;
			set => this._BranchID = value;
		}
		public new abstract class branchID : BqlInt.Field<branchID> { }
		#endregion
		#region DocType
		[PXDBString(1, IsFixed = true, IsKey = true)]
		[PXDefault(typeof(INKitRegister.docType))]
		public override String DocType
		{
			get => this._DocType;
			set => this._DocType = value;
		}
		public new abstract class docType : BqlString.Field<docType> { }
		#endregion
		#region TranType
		[PXDBString(3, IsFixed = true)]
		[PXDefault]
		[PXFormula(typeof(Switch<Case<Where<docType, Equal<INDocType.disassembly>>, INTranType.disassembly>, INTranType.assembly>))]
		public override String TranType
		{
			get => this._TranType;
			set => this._TranType = value;
		}
		public new abstract class tranType : BqlString.Field<tranType> { }
		#endregion
		#region RefNbr
		[PXDBString(15, IsUnicode = true, IsKey = true)]
		[PXDBDefault(typeof(INKitRegister.refNbr))]
		[PXParent(typeof(Select<INKitRegister,
			Where<INKitRegister.docType, Equal<Current<docType>>,
				And<INKitRegister.refNbr, Equal<Current<refNbr>>>>>))]
		public override String RefNbr
		{
			get => this._RefNbr;
			set => this._RefNbr = value;
		}
		public new abstract class refNbr : BqlString.Field<refNbr> { }
		#endregion
		#region LineNbr
		[PXDBInt(IsKey = true)]
		[PXDefault]
		[PXLineNbr(typeof(INKitRegister.lineCntr))]
		public override Int32? LineNbr
		{
			get => this._LineNbr;
			set => this._LineNbr = value;
		}
		public new abstract class lineNbr : BqlInt.Field<lineNbr> { }
		#endregion
		#region AssyType
		[PXDBString(1, IsFixed = true)]
		[PXDefault(INAssyType.CompTran)]
		public override String AssyType
		{
			get => this._AssyType;
			set => this._AssyType = value;
		}
		public new abstract class assyType : BqlString.Field<assyType> { }
		#endregion
		#region ProjectID
		[PM.ProjectDefault]
		[PXDBInt(BqlField = typeof(INTran.projectID))]
		public override Int32? ProjectID
		{
			get => this._ProjectID;
			set => this._ProjectID = value;
		}
		public new abstract class projectID : BqlInt.Field<projectID> { }
		#endregion
		#region TranDate
		[PXDBDate]
		[PXDBDefault(typeof(INKitRegister.tranDate))]
		public override DateTime? TranDate
		{
			get => this._TranDate;
			set => this._TranDate = value;
		}
		public new abstract class tranDate : BqlDateTime.Field<tranDate> { }
		#endregion
		#region FinPeriodID
		[FinPeriodID(
			branchSourceType: typeof(branchID),
			masterFinPeriodIDType: typeof(tranPeriodID),
			headerMasterFinPeriodIDType: typeof(INKitRegister.tranPeriodID))]
		public override String FinPeriodID
		{
			get => this._FinPeriodID;
			set => this._FinPeriodID = value;
		}
		public new abstract class finPeriodID : BqlString.Field<finPeriodID> { }
		#endregion
		#region TranPeriodID
		[PeriodID(BqlField = typeof(INTran.tranPeriodID))]
		public override String TranPeriodID
		{
			get => this._TranPeriodID;
			set => this._TranPeriodID = value;
		}
		public new abstract class tranPeriodID : BqlString.Field<tranPeriodID> { }
		#endregion
		#region InvtMult
		[PXDBShort]
		[PXDefault]
		public override Int16? InvtMult
		{
			get => this._InvtMult;
			set => this._InvtMult = value;
		}
		public new abstract class invtMult : BqlShort.Field<invtMult> { }
		#endregion
		#region UOM
		[PXDefault(typeof(Search<InventoryItem.baseUnit, Where<InventoryItem.inventoryID, Equal<Current<inventoryID>>>>))]
		[INUnit(typeof(inventoryID))]
		public override String UOM
		{
			get => this._UOM;
			set => this._UOM = value;
		}
		public new abstract class uOM : BqlString.Field<uOM> { }
		#endregion
		#region Qty
		[PXDBQuantity(typeof(uOM), typeof(baseQty), InventoryUnitType.BaseUnit)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Quantity")]
		public override Decimal? Qty
		{
			get => this._Qty;
			set => this._Qty = value;
		}
		public new abstract class qty : BqlDecimal.Field<qty> { }
		#endregion
		#region InventoryID
		[PXDefault]
		[Inventory(typeof(Search2<InventoryItem.inventoryID,
			InnerJoin<INLotSerClass, On<InventoryItem.FK.LotSerialClass>>,
			Where2<Match<Current<AccessInfo.userName>>, And<InventoryItem.stkItem, Equal<True>>>>), typeof(InventoryItem.inventoryCD), typeof(InventoryItem.descr), DisplayName = "Inventory ID")]
		[PXRestrictor(typeof(Where<INLotSerTrack.serialNumbered, Equal<Current<INKitRegister.lotSerTrack>>,
									Or<INLotSerClass.lotSerTrack, Equal<INLotSerTrack.lotNumbered>,
									Or<INLotSerClass.lotSerTrack, Equal<INLotSerTrack.notNumbered>>>
							>), Messages.SNComponentInSNKit)]
		public override Int32? InventoryID
		{
			get => _InventoryID;
			set => _InventoryID = value;
		}
		public new abstract class inventoryID : BqlInt.Field<inventoryID> { }
		#endregion
		#region SubItemID
		[PXDefault(typeof(Search<InventoryItem.defaultSubItemID,
			Where<InventoryItem.inventoryID, Equal<Current<inventoryID>>,
			And<InventoryItem.defaultSubItemOnEntry, Equal<boolTrue>>>>),
			PersistingCheck = PXPersistingCheck.Nothing)]
		[SubItem(
			typeof(inventoryID),
			typeof(LeftJoin<INSiteStatus,
				On2<INSiteStatus.FK.SubItem,
				And<INSiteStatus.inventoryID, Equal<Optional<inventoryID>>,
				And<INSiteStatus.siteID, Equal<Optional<siteID>>>>>>))]
		[PXFormula(typeof(Default<inventoryID>))]
		public override Int32? SubItemID
		{
			get => this._SubItemID;
			set => this._SubItemID = value;
		}
		public new abstract class subItemID : BqlInt.Field<subItemID> { }
		#endregion
		#region SiteID
		[PXDefault(typeof(INKitRegister.siteID))]
		[PXDBInt]
		[PXSelector(typeof(Search<INSite.siteID>), SubstituteKey = typeof(INSite.siteCD))]
		public override Int32? SiteID
		{
			get => this._SiteID;
			set => this._SiteID = value;
		}
		public new abstract class siteID : BqlInt.Field<siteID> { }
		#endregion
		#region LocationID
		[LocationAvail(typeof(inventoryID),
					  typeof(subItemID),
					  typeof(siteID),
					  typeof(Where2<Where<tranType, Equal<INTranType.assembly>, Or<tranType, Equal<INTranType.disassembly>>>, And<invtMult, Equal<shortMinus1>>>),
					  typeof(Where2<Where<tranType, Equal<INTranType.assembly>, Or<tranType, Equal<INTranType.disassembly>>>, And<invtMult, Equal<short1>>>),
					  typeof(Where<False, Equal<True>>))]
		[PXRestrictor(typeof(Where<True, Equal<True>>), null, ReplaceInherited = true)]
		public override Int32? LocationID
		{
			get => this._LocationID;
			set => this._LocationID = value;
		}
		public new abstract class locationID : BqlInt.Field<locationID> { }
		#endregion
		#region UnitCost
		[PXDBPriceCost]
		[PXUIField(DisplayName = "Unit Cost")]
		[PXFormula(typeof(Default<uOM>))]
		public override Decimal? UnitCost
		{
			get => this._UnitCost;
			set => this._UnitCost = value;
		}
		public new abstract class unitCost : BqlDecimal.Field<unitCost> { }
		#endregion
		#region TranCost
		[PXDBBaseCury(BqlField = typeof(INTran.tranCost))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Ext. Cost")]
		[PXFormula(typeof(Mult<qty, unitCost>), typeof(SumCalc<INKitRegister.totalCostStock>))]
		public override Decimal? TranCost { get; set; }
		public new abstract class tranCost : BqlDecimal.Field<tranCost> { }
		#endregion
		#region ReasonCode
		[PXDBString(CS.ReasonCode.reasonCodeID.Length, IsUnicode = true, BqlField = typeof(INTran.reasonCode))]
		[PXSelector(typeof(Search<ReasonCode.reasonCodeID, Where<ReasonCode.usage, Equal<Optional<docType>>>>))]
		[PXUIField(DisplayName = "Reason Code")]
		public override String ReasonCode
		{
			get => this._ReasonCode;
			set => this._ReasonCode = value;
		}
		public new abstract class reasonCode : BqlString.Field<reasonCode> { }
		#endregion
		#region TranDesc
		[PXDBString(256, IsUnicode = true, BqlField = typeof(INTran.tranDesc))]
		[PXUIField(DisplayName = "Description")]
		[PXDefault(typeof(Search<InventoryItem.descr, Where<InventoryItem.inventoryID, Equal<Current<inventoryID>>>>),
			CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		public override String TranDesc
		{
			get => this._TranDesc;
			set => this._TranDesc = value;
		}
		public new abstract class tranDesc : BqlString.Field<tranDesc> { }
		#endregion

		#region Methods

		//This is a bad idea... but still
		public static implicit operator INComponentTranSplit(INComponentTran item)
		{
			INComponentTranSplit ret = new INComponentTranSplit();
			ret.DocType = item.DocType;
			ret.TranType = item.TranType;
			ret.RefNbr = item.RefNbr;
			ret.LineNbr = item.LineNbr;
			ret.SplitLineNbr = (short)1;
			ret.InventoryID = item.InventoryID;
			ret.SiteID = item.SiteID;
			ret.SubItemID = item.SubItemID;
			ret.LocationID = item.LocationID;
			ret.LotSerialNbr = item.LotSerialNbr;
			ret.ExpireDate = item.ExpireDate;
			ret.Qty = item.Qty;
			ret.UOM = item.UOM;
			ret.TranDate = item.TranDate;
			ret.BaseQty = item.BaseQty;
			ret.InvtMult = item.InvtMult;
			ret.Released = item.Released;

			return ret;
		}

		//This is a bad idea... but still
		public static implicit operator INComponentTran(INComponentTranSplit item)
		{
			INComponentTran ret = new INComponentTran();
			ret.DocType = item.DocType;
			ret.TranType = item.TranType;
			ret.RefNbr = item.RefNbr;
			ret.LineNbr = item.LineNbr;
			ret.InventoryID = item.InventoryID;
			ret.SiteID = item.SiteID;
			ret.SubItemID = item.SubItemID;
			ret.LocationID = item.LocationID;
			ret.LotSerialNbr = item.LotSerialNbr;
			ret.Qty = item.Qty;
			ret.UOM = item.UOM;
			ret.TranDate = item.TranDate;
			ret.BaseQty = item.BaseQty;
			ret.InvtMult = item.InvtMult;
			ret.Released = item.Released;

			return ret;
		}
		#endregion
	}

	[PXProjection(typeof(Select<INTran, Where<INTran.assyType, Equal<INAssyType.overheadTran>>>), Persistent = true)]
	[PXCacheName(Messages.INOverheadTran)]
	public partial class INOverheadTran : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<INOverheadTran>.By<docType, refNbr, lineNbr>
		{
			public static INOverheadTran Find(PXGraph graph, string docType, string refNbr, int? lineNbr) => FindBy(graph, docType, refNbr, lineNbr);
		}
		public static class FK
		{
			public class Branch : GL.Branch.PK.ForeignKeyOf<INOverheadTran>.By<branchID> { }
			public class KitRegister : INKitRegister.PK.ForeignKeyOf<INOverheadTran>.By<docType, refNbr> { }
			public class Project : PMProject.PK.ForeignKeyOf<INOverheadTran>.By<projectID> { }
			public class InventoryItem : IN.InventoryItem.PK.ForeignKeyOf<INOverheadTran>.By<inventoryID> { }
			public class SubItem : INSubItem.PK.ForeignKeyOf<INOverheadTran>.By<subItemID> { }
			public class Site : INSite.PK.ForeignKeyOf<INOverheadTran>.By<siteID> { }
			public class Location : INLocation.PK.ForeignKeyOf<INOverheadTran>.By<locationID> { }
			public class ReasonCode : CS.ReasonCode.PK.ForeignKeyOf<INOverheadTran>.By<reasonCode> { }
			//todo public class UnitOfMeasure : INUnit.UK.ByInventory.ForeignKeyOf<INOverheadTran>.By<inventoryID, uOM> { }
			//todo public class FinancialPeriod : GL.FinPeriods.TableDefinition.FinPeriod.PK.ForeignKeyOf<INOverheadTran>.By<finPeriodID> { }
			//todo public class MasterFinancialPeriod : GL.FinPeriods.TableDefinition.FinPeriod.PK.ForeignKeyOf<INOverheadTran>.By<tranPeriodID> { }
		}
		#endregion
		#region BranchID
		[Branch(typeof(INKitRegister.branchID), BqlField = typeof(INTran.branchID), Visibility = PXUIVisibility.Invisible, Visible = false, Enabled = false)]
		public virtual Int32? BranchID { get; set; }
		public abstract class branchID : BqlInt.Field<branchID> { }
		#endregion
		#region DocType
		[PXDBString(1, IsFixed = true, IsKey = true, BqlField = typeof(INTran.docType))]
		[PXDefault(typeof(INKitRegister.docType))]
		public virtual String DocType { get; set; }
		public abstract class docType : BqlString.Field<docType> { }
		#endregion
		#region TranType
		[PXDBString(3, IsFixed = true, BqlField = typeof(INTran.tranType))]
		[PXDefault]
		[PXFormula(typeof(Switch<Case<Where<docType, Equal<INDocType.disassembly>>, INTranType.disassembly>, INTranType.assembly>))]
		public virtual String TranType { get; set; }
		public abstract class tranType : BqlString.Field<tranType> { }
		#endregion
		#region RefNbr
		[PXDBString(15, IsUnicode = true, IsKey = true, BqlField = typeof(INTran.refNbr))]
		[PXDBDefault(typeof(INKitRegister.refNbr))]
		[PXParent(typeof(Select<INKitRegister,
			Where<INKitRegister.docType, Equal<Current<docType>>,
			And<INKitRegister.refNbr, Equal<Current<refNbr>>>>>))]
		public virtual String RefNbr { get; set; }
		public abstract class refNbr : BqlString.Field<refNbr> { }
		#endregion
		#region LineNbr
		[PXDBInt(IsKey = true, BqlField = typeof(INTran.lineNbr))]
		[PXDefault]
		[PXLineNbr(typeof(INKitRegister.lineCntr))]
		public virtual Int32? LineNbr { get; set; }
		public abstract class lineNbr : BqlInt.Field<lineNbr> { }
		#endregion
		#region AssyType
		[PXDefault(INAssyType.OverheadTran)]
		[PXDBString(1, IsFixed = true, BqlField = typeof(INTran.assyType))]
		public virtual String AssyType { get; set; }
		public abstract class assyType : BqlString.Field<assyType> { }
		#endregion
		#region ProjectID
		[PM.ProjectDefault]
		[PXDBInt(BqlField = typeof(INTran.projectID))]
		public virtual Int32? ProjectID { get; set; }
		public abstract class projectID : BqlInt.Field<projectID> { }
		#endregion
		#region TranDate
		[PXDBDate(BqlField = typeof(INTran.tranDate))]
		[PXDBDefault(typeof(INKitRegister.tranDate))]
		public virtual DateTime? TranDate { get; set; }
		public abstract class tranDate : BqlDateTime.Field<tranDate> { }
		#endregion
		#region InventoryID
		[NonStockItem(BqlField = typeof(INTran.inventoryID), DisplayName = "Inventory ID")]
		public virtual Int32? InventoryID { get; set; }
		public abstract class inventoryID : BqlInt.Field<inventoryID> { }
		#endregion
		#region SubItemID
		[SubItem(typeof(inventoryID), BqlField = typeof(INTran.subItemID))]
		public virtual Int32? SubItemID { get; set; }
		public abstract class subItemID : BqlInt.Field<subItemID> { }
		#endregion
		#region SiteID
		[PXDefault(typeof(INKitRegister.siteID))]
		[PXDBInt(BqlField = typeof(INTran.siteID))]
		public virtual Int32? SiteID { get; set; }
		public abstract class siteID : BqlInt.Field<siteID> { }
		#endregion
		#region LocationID
		[LocationAvail(typeof(inventoryID), typeof(subItemID), typeof(siteID), typeof(tranType), typeof(invtMult), BqlField = typeof(INTran.locationID))]
		public virtual Int32? LocationID { get; set; }
		public abstract class locationID : BqlInt.Field<locationID> { }
		#endregion
		#region InvtMult
		[PXDBShort(BqlField = typeof(INTran.invtMult))]
		[PXDefault]
		public virtual Int16? InvtMult { get; set; }
		public abstract class invtMult : BqlShort.Field<invtMult> { }
		#endregion
		#region UOM
		[PXDefault(typeof(Search<InventoryItem.baseUnit, Where<InventoryItem.inventoryID, Equal<Current<inventoryID>>>>))]
		[INUnit(typeof(inventoryID), BqlField = typeof(INTran.uOM))]
		public virtual String UOM { get; set; }
		public abstract class uOM : BqlString.Field<uOM> { }
		#endregion
		#region Qty
		[PXDBQuantity(typeof(uOM), typeof(baseQty), InventoryUnitType.BaseUnit, BqlField = typeof(INTran.qty))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Quantity")]
		public virtual Decimal? Qty { get; set; }
		public abstract class qty : BqlDecimal.Field<qty> { }
		#endregion
		#region FinPeriodID
		[PXDBDefault(typeof(INKitRegister.finPeriodID))]
		[FinPeriodID(
			branchSourceType: typeof(branchID),
			masterFinPeriodIDType: typeof(tranPeriodID),
			headerMasterFinPeriodIDType: typeof(INKitRegister.tranPeriodID),
			BqlField = typeof(INTran.finPeriodID))]
		public virtual String FinPeriodID { get; set; }
		public abstract class finPeriodID : BqlString.Field<finPeriodID> { }
		#endregion
		#region TranDesc
		[PXDBString(256, IsUnicode = true, BqlField = typeof(INTran.tranDesc))]
		[PXUIField(DisplayName = "Description")]
		[PXDefault(typeof(Search<InventoryItem.descr, Where<InventoryItem.inventoryID, Equal<Current<inventoryID>>>>),
			CacheGlobal = true, PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual String TranDesc { get; set; }
		public abstract class tranDesc : BqlString.Field<tranDesc> { }
		#endregion
		#region ReasonCode
		[PXDBString(CS.ReasonCode.reasonCodeID.Length, IsUnicode = true, BqlField = typeof(INTran.reasonCode))]
		[PXSelector(typeof(Search<ReasonCode.reasonCodeID, Where<ReasonCode.usage, Equal<Optional<docType>>>>))]
		[PXUIField(DisplayName = "Reason Code")]
		public virtual String ReasonCode { get; set; }
		public abstract class reasonCode : BqlString.Field<reasonCode> { }
		#endregion
		#region BaseQty
		[PXDBQuantity(BqlField = typeof(INTran.baseQty))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? BaseQty { get; set; }
		public abstract class baseQty : BqlDecimal.Field<baseQty> { }
		#endregion
		#region UnassignedQty
		[PXDBDecimal(6, BqlField = typeof(INTran.unassignedQty))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? UnassignedQty { get; set; }
		public abstract class unassignedQty : BqlDecimal.Field<unassignedQty> { }
		#endregion
		#region Released
		[PXDBBool(BqlField = typeof(INTran.released))]
		[PXDefault(false)]
		public virtual Boolean? Released { get; set; }
		public abstract class released : BqlBool.Field<released> { }
		#endregion
		#region TranPeriodID
		[PXDBDefault(typeof(INKitRegister.tranPeriodID))]
		[PeriodID(BqlField = typeof(INTran.tranPeriodID))]
		public virtual String TranPeriodID { get; set; }
		public abstract class tranPeriodID : BqlString.Field<tranPeriodID> { }
		#endregion
		#region UnitPrice
		[PXDBPriceCost(BqlField = typeof(INTran.unitPrice))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Unit Price")]
		public virtual Decimal? UnitPrice { get; set; }
		public abstract class unitPrice : BqlDecimal.Field<unitPrice> { }
		#endregion
		#region TranAmt
		[PXDBBaseCury(BqlField = typeof(INTran.tranAmt))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Ext. Price")]
		public virtual Decimal? TranAmt { get; set; }
		public abstract class tranAmt : BqlDecimal.Field<tranAmt> { }
		#endregion
		#region UnitCost
		[PXDBPriceCost(BqlField = typeof(INTran.unitCost))]
		[PXUIField(DisplayName = "Unit Cost")]
		[PXFormula(typeof(Default<uOM>))]
		public virtual Decimal? UnitCost { get; set; }
		public abstract class unitCost : BqlDecimal.Field<unitCost> { }
		#endregion
		#region TranCost
		[PXDBBaseCury(BqlField = typeof(INTran.tranCost))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Ext. Cost")]
		[PXFormula(typeof(Mult<qty, unitCost>), typeof(SumCalc<INKitRegister.totalCostNonStock>))]
		public virtual Decimal? TranCost { get; set; }
		public abstract class tranCost : BqlDecimal.Field<tranCost> { }
		#endregion
		#region UpdateShippedNotInvoiced
		[PXDBBool(BqlField = typeof(INTran.updateShippedNotInvoiced))]
		[PXDefault(false)]
		public virtual Boolean? UpdateShippedNotInvoiced { get; set; }
		public abstract class updateShippedNotInvoiced : BqlBool.Field<updateShippedNotInvoiced> { }
		#endregion

		#region CreatedByID
		[PXDBCreatedByID(BqlField = typeof(INTran.createdByID))]
		public virtual Guid? CreatedByID { get; set; }
		public abstract class createdByID : BqlGuid.Field<createdByID> { }
		#endregion
		#region CreatedByScreenID
		[PXDBCreatedByScreenID(BqlField = typeof(INTran.createdByScreenID))]
		public virtual String CreatedByScreenID { get; set; }
		public abstract class createdByScreenID : BqlString.Field<createdByScreenID> { }
		#endregion
		#region CreatedDateTime
		[PXDBCreatedDateTime(BqlField = typeof(INTran.createdDateTime))]
		public virtual DateTime? CreatedDateTime { get; set; }
		public abstract class createdDateTime : BqlDateTime.Field<createdDateTime> { }
		#endregion
		#region LastModifiedByID
		[PXDBLastModifiedByID(BqlField = typeof(INTran.lastModifiedByID))]
		public virtual Guid? LastModifiedByID { get; set; }
		public abstract class lastModifiedByID : BqlGuid.Field<lastModifiedByID> { }
		#endregion
		#region LastModifiedByScreenID
		[PXDBLastModifiedByScreenID(BqlField = typeof(INTran.lastModifiedByScreenID))]
		public virtual String LastModifiedByScreenID { get; set; }
		public abstract class lastModifiedByScreenID : BqlString.Field<lastModifiedByScreenID> { }
		#endregion
		#region LastModifiedDateTime
		[PXDBLastModifiedDateTime(BqlField = typeof(INTran.lastModifiedDateTime))]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		public abstract class lastModifiedDateTime : BqlDateTime.Field<lastModifiedDateTime> { }
		#endregion
		#region tstamp
		[PXDBTimestamp(BqlField = typeof(INTran.Tstamp))]
		public virtual Byte[] Tstamp { get; set; }
		public abstract class tstamp : BqlByteArray.Field<tstamp> { }
		#endregion
	}

	[DebuggerDisplay("SerialNbr={LotSerialNbr}")]
	[PXCacheName(Messages.INComponentTranSplit)]
	public partial class INComponentTranSplit : INTranSplit
	{
		#region Keys
		public new class PK : PrimaryKeyOf<INComponentTranSplit>.By<docType, refNbr,lineNbr, splitLineNbr>
		{
			public static INComponentTranSplit Find(PXGraph graph, string docType, string refNbr, int? lineNbr, int? splitLineNbr)
				=> FindBy(graph, docType, refNbr, lineNbr, splitLineNbr);
		}
		public new static class FK
		{
			public class KitRegister : INKitRegister.PK.ForeignKeyOf<INComponentTranSplit>.By<docType, refNbr> { }
			public class ComponentTran : INComponentTran.PK.ForeignKeyOf<INComponentTranSplit>.By<docType, refNbr, lineNbr> { } //+ lineNbr != INKitRegister.kitLineNbr.FromCurrent
			public class InventoryItem : IN.InventoryItem.PK.ForeignKeyOf<INComponentTranSplit>.By<inventoryID> { }
			public class SubItem : INSubItem.PK.ForeignKeyOf<INComponentTranSplit>.By<subItemID> { }
			public class Site : INSite.PK.ForeignKeyOf<INComponentTranSplit>.By<siteID> { }
			public class Location : INLocation.PK.ForeignKeyOf<INComponentTranSplit>.By<locationID> { }
			public class ItemPlan : INItemPlan.PK.ForeignKeyOf<INComponentTranSplit>.By<planID> { }
		}
		#endregion
		#region DocType
		[PXDBString(1, IsFixed = true, IsKey = true)]
		[PXDefault(typeof(INComponentTran.docType))]
		public override String DocType
		{
			get => this._DocType;
			set => this._DocType = value;
		}
		public new abstract class docType : BqlString.Field<docType> { }
		#endregion
		#region TranType
		[PXDBString(3)]
		[PXDefault]
		[PXFormula(typeof(Switch<Case<Where<docType, Equal<INDocType.disassembly>>, INTranType.disassembly>, INTranType.assembly>))]
		public override String TranType
		{
			get => this._TranType;
			set => this._TranType = value;
		}
		public new abstract class tranType : BqlString.Field<tranType> { }
		#endregion
		#region RefNbr
		[PXDBString(15, IsUnicode = true, IsKey = true)]
		[PXDBDefault(typeof(INKitRegister.refNbr))]
		[PXParent(typeof(FK.ComponentTran), typeof(Where<INComponentTran.lineNbr.IsNotEqual<INKitRegister.kitLineNbr.FromCurrent>>))]
		public override String RefNbr
		{
			get => this._RefNbr;
			set => this._RefNbr = value;
		}
		public new abstract class refNbr : BqlString.Field<refNbr> { }
		#endregion
		#region LineNbr
		[PXDBInt(IsKey = true)]
		[PXDefault(typeof(INComponentTran.lineNbr))]
		public override Int32? LineNbr
		{
			get => this._LineNbr;
			set => this._LineNbr = value;
		}
		public new abstract class lineNbr : BqlInt.Field<lineNbr> { }
		#endregion
		#region SplitLineNbr
		[PXDBInt(IsKey = true)]
		[PXLineNbr(typeof(INKitRegister.lineCntr))]
		public override Int32? SplitLineNbr
		{
			get => this._SplitLineNbr;
			set => this._SplitLineNbr = value;
		}
		public new abstract class splitLineNbr : BqlInt.Field<splitLineNbr> { }
		#endregion
		#region TranDate
		[PXDBDate]
		[PXDBDefault(typeof(INKitRegister.tranDate))]
		public override DateTime? TranDate
		{
			get => this._TranDate;
			set => this._TranDate = value;
		}
		public new abstract class tranDate : BqlDateTime.Field<tranDate> { }
		#endregion
		#region InvtMult
		[PXDBShort]
		[PXDefault(typeof(INComponentTran.invtMult))]
		public override Int16? InvtMult
		{
			get => this._InvtMult;
			set => this._InvtMult = value;
		}
		public new abstract class invtMult : BqlShort.Field<invtMult> { }
		#endregion
		#region InventoryID
		[StockItem(Visible = false)]
		[PXDefault(typeof(INComponentTran.inventoryID))]
		public override Int32? InventoryID
		{
			get => this._InventoryID;
			set => this._InventoryID = value;
		}
		public new abstract class inventoryID : BqlInt.Field<inventoryID> { }
		#endregion
		#region SubItemID
		[SubItem(typeof(inventoryID))]
		[PXDefault]
		public override Int32? SubItemID
		{
			get => this._SubItemID;
			set => this._SubItemID = value;
		}
		public new abstract class subItemID : BqlInt.Field<subItemID> { }
		#endregion
		#region SiteID
		[Site]
		[PXDefault(typeof(INComponentTran.siteID))]
		public override Int32? SiteID
		{
			get => this._SiteID;
			set => this._SiteID = value;
		}
		public new abstract class siteID : BqlInt.Field<siteID> { }
		#endregion
		#region LocationID
		[LocationAvail(typeof(inventoryID),
						typeof(subItemID),
						typeof(siteID),
						typeof(Where2<Where<tranType, Equal<INTranType.assembly>, Or<tranType, Equal<INTranType.disassembly>>>, And<invtMult, Equal<shortMinus1>>>),
						typeof(Where2<Where<tranType, Equal<INTranType.assembly>, Or<tranType, Equal<INTranType.disassembly>>>, And<invtMult, Equal<short1>>>),
						typeof(Where<False, Equal<True>>))]
		[PXRestrictor(typeof(Where<True, Equal<True>>), null, ReplaceInherited = true)]
		[PXDefault]
		public override Int32? LocationID
		{
			get => this._LocationID;
			set => this._LocationID = value;
		}
		public new abstract class locationID : BqlInt.Field<locationID> { }
		#endregion
		#region LotSerialNbr
		[INLotSerialNbr(typeof(inventoryID), typeof(subItemID), typeof(locationID), typeof(INTran.lotSerialNbr))]
		public override String LotSerialNbr
		{
			get => this._LotSerialNbr;
			set => this._LotSerialNbr = value;
		}
		public new abstract class lotSerialNbr : BqlString.Field<lotSerialNbr> { }
		#endregion
		#region UOM
		[INUnit(typeof(inventoryID), DisplayName = "UOM", Enabled = false)]
		[PXDefault]
		public override String UOM
		{
			get => this._UOM;
			set => this._UOM = value;
		}
		public new abstract class uOM : BqlString.Field<uOM> { }
		#endregion
		#region Qty
		[PXDBQuantity(typeof(uOM), typeof(baseQty), InventoryUnitType.BaseUnit)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Quantity")]
		public override Decimal? Qty
		{
			get => this._Qty;
			set => this._Qty = value;
		}
		public new abstract class qty : BqlDecimal.Field<qty> { }
		#endregion
		#region BaseQty
		public new abstract class baseQty : BqlDecimal.Field<baseQty> { }
		#endregion
		#region PlanID
		[PXDBLong(IsImmutable = true)]
		public override Int64? PlanID
		{
			get => this._PlanID;
			set => this._PlanID = value;
		}
		public new abstract class planID : BqlLong.Field<planID> { }
		#endregion
	}

	[PXProjection(typeof(Select<INTranSplit>), Persistent = true)]
	[PXCacheName(Messages.INKitTranSplit)]
	public partial class INKitTranSplit : IBqlTable, ILSDetail
	{
		#region Keys
		public class PK : PrimaryKeyOf<INKitTranSplit>.By<docType, refNbr, lineNbr, splitLineNbr>
		{
			public static INKitTranSplit Find(PXGraph graph, string docType, string refNbr, int? lineNbr, int? splitLineNbr) =>
				FindBy(graph, docType, refNbr, lineNbr, splitLineNbr);
		}
		public static class FK
		{
			public class Register : INRegister.PK.ForeignKeyOf<INTranSplit>.By<docType, refNbr> { }
			public class Tran : INTran.PK.ForeignKeyOf<INTranSplit>.By<docType, refNbr, lineNbr> { }
			public class InventoryItem : IN.InventoryItem.PK.ForeignKeyOf<INTranSplit>.By<inventoryID> { }
			public class SubItem : INSubItem.PK.ForeignKeyOf<INTranSplit>.By<subItemID> { }
			public class Site : INSite.PK.ForeignKeyOf<INTranSplit>.By<siteID> { }
			public class Location : INLocation.PK.ForeignKeyOf<INTranSplit>.By<locationID> { }
			public class ItemPlan : INItemPlan.PK.ForeignKeyOf<INTranSplit>.By<planID> { }
			public class LotSerialStatus : INLotSerialStatus.PK.ForeignKeyOf<INTranSplit>.By<inventoryID, subItemID, siteID, locationID, lotSerialNbr> { }
			//todo public class UnitOfMeasure : INUnit.UK.ByInventory.ForeignKeyOf<INTranSplit>.By<inventoryID, uOM> { }
		}
		#endregion

		#region Selected
		[PXBool]
		[PXUnboundDefault(false)]
		[PXUIField(DisplayName = "Selected")]
		public virtual bool? Selected { get; set; } = false;
		public abstract class selected : BqlBool.Field<selected> { }
		#endregion
		#region DocType
		[PXDBString(1, IsFixed = true, IsKey = true, BqlField = typeof(INTranSplit.docType))]
		[PXDefault(typeof(INKitRegister.docType))]
		public virtual String DocType { get; set; }
		public abstract class docType : BqlString.Field<docType> { }
		#endregion
		#region TranType
		[PXDBString(3, BqlField = typeof(INTranSplit.tranType))]
		[PXDefault]
		[PXFormula(typeof(Switch<Case<Where<docType, Equal<INDocType.disassembly>>, INTranType.disassembly>, INTranType.assembly>))]
		public virtual String TranType { get; set; }
		public abstract class tranType : BqlString.Field<tranType> { }
		#endregion
		#region RefNbr
		[PXDBString(15, IsUnicode = true, IsKey = true, BqlField = typeof(INTranSplit.refNbr))]
		[PXDBDefault(typeof(INKitRegister.refNbr))]
		[PXParent(typeof(Select<INKitRegister,
			Where<INKitRegister.docType, Equal<Current<docType>>,
			And<INKitRegister.refNbr, Equal<Current<refNbr>>,
			And<INKitRegister.kitLineNbr, Equal<Current<lineNbr>>>>>>))]
		public virtual String RefNbr { get; set; }
		public abstract class refNbr : BqlString.Field<refNbr> { }
		#endregion
		#region LineNbr
		[PXDBInt(IsKey = true, BqlField = typeof(INTranSplit.lineNbr))]
		[PXDefault(typeof(INKitRegister.kitLineNbr))]
		public virtual Int32? LineNbr { get; set; }
		public abstract class lineNbr : BqlInt.Field<lineNbr> { }
		#endregion
		#region SplitLineNbr
		[PXDBInt(IsKey = true, BqlField = typeof(INTranSplit.splitLineNbr))]
		[PXLineNbr(typeof(INKitRegister.lineCntr))]
		public virtual Int32? SplitLineNbr { get; set; }
		public abstract class splitLineNbr : BqlInt.Field<splitLineNbr> { }
		#endregion
		#region TranDate
		[PXDBDate(BqlField = typeof(INTranSplit.tranDate))]
		[PXDBDefault(typeof(INKitRegister.tranDate))]
		public virtual DateTime? TranDate { get; set; }
		public abstract class tranDate : BqlDateTime.Field<tranDate> { }
		#endregion
		#region InvtMult
		[PXDBShort(BqlField = typeof(INTranSplit.invtMult))]
		[PXDefault(typeof(INKitRegister.invtMult))]
		public virtual Int16? InvtMult { get; set; }
		public abstract class invtMult : BqlShort.Field<invtMult> { }
		#endregion
		#region InventoryID
		[StockItem(Visible = false, BqlField = typeof(INTranSplit.inventoryID))]
		[PXDefault(typeof(INKitRegister.kitInventoryID))]
		public virtual Int32? InventoryID { get; set; }
		public abstract class inventoryID : BqlInt.Field<inventoryID> { }
		#endregion
		#region IsStockItem
		public bool? IsStockItem
		{
			get => true;
			set { }
		}
		#endregion
		#region SubItemID
		[SubItem(typeof(inventoryID), BqlField = typeof(INTranSplit.subItemID))]
		[PXDefault]
		public virtual Int32? SubItemID { get; set; }
		public abstract class subItemID : BqlInt.Field<subItemID> { }
		#endregion
		#region SiteID
		[Site(BqlField = typeof(INTranSplit.siteID))]
		[PXDefault(typeof(INKitRegister.siteID))]
		public virtual Int32? SiteID { get; set; }
		public abstract class siteID : BqlInt.Field<siteID> { }
		#endregion
		#region LocationID
		[LocationAvail(typeof(inventoryID), typeof(subItemID), typeof(siteID), typeof(tranType), typeof(invtMult), BqlField = typeof(INTranSplit.locationID))]
		[PXDefault]
		public virtual Int32? LocationID { get; set; }
		public abstract class locationID : BqlInt.Field<locationID> { }
		#endregion
		#region LotSerialNbr
		[INLotSerialNbr(typeof(inventoryID), typeof(subItemID), typeof(locationID), typeof(INKitRegister.lotSerialNbr), BqlField = typeof(INTranSplit.lotSerialNbr))]
		public virtual String LotSerialNbr { get; set; }
		public abstract class lotSerialNbr : BqlString.Field<lotSerialNbr> { }
		#endregion
		#region LotSerClassID
		[PXString(10, IsUnicode = true)]
		public virtual String LotSerClassID { get; set; }
		public abstract class lotSerClassID : BqlString.Field<lotSerClassID> { }
		#endregion
		#region AssignedNbr
		[PXString(30, IsUnicode = true)]
		public virtual String AssignedNbr { get; set; }
		public abstract class assignedNbr : BqlString.Field<assignedNbr> { }
		#endregion
		#region ExpireDate
		[INExpireDate(typeof(inventoryID), BqlField = typeof(INTranSplit.expireDate))]
		public virtual DateTime? ExpireDate { get; set; }
		public abstract class expireDate : BqlDateTime.Field<expireDate> { }
		#endregion
		#region Released
		[PXDBBool(BqlField = typeof(INTranSplit.released))]
		[PXDefault(false)]
		public virtual Boolean? Released { get; set; }
		public abstract class released : BqlBool.Field<released> { }
		#endregion
		#region UOM
		[INUnit(typeof(inventoryID), DisplayName = "UOM", Enabled = false, BqlField = typeof(INTranSplit.uOM))]
		[PXDefault]
		public virtual String UOM { get; set; }
		public abstract class uOM : BqlString.Field<uOM> { }
		#endregion
		#region Qty
		[PXDBQuantity(typeof(uOM), typeof(baseQty), InventoryUnitType.BaseUnit, BqlField = typeof(INTranSplit.qty))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Quantity")]
		public virtual Decimal? Qty { get; set; }
		public abstract class qty : BqlDecimal.Field<qty> { }
		#endregion
		#region BaseQty
		[PXDBQuantity(BqlField = typeof(INTranSplit.baseQty))]
		public virtual Decimal? BaseQty { get; set; }
		public abstract class baseQty : BqlDecimal.Field<baseQty> { }
		#endregion
		#region PlanID
		[PXDBLong(BqlField = typeof(INTranSplit.planID), IsImmutable = true)]
		public virtual Int64? PlanID { get; set; }
		public abstract class planID : BqlLong.Field<planID> { }
		#endregion

		#region ProjectID
		[PXFormula(typeof(Selector<locationID, INLocation.projectID>))]
		[PXInt]
		public virtual Int32? ProjectID { get; set; }
		public abstract class projectID : BqlInt.Field<projectID> { }
		#endregion
		#region TaskID
		[PXFormula(typeof(Selector<locationID, INLocation.taskID>))]
		[PXInt]
		public virtual Int32? TaskID { get; set; }
		public abstract class taskID : BqlInt.Field<taskID> { }
		#endregion

		#region IsIntercompany
		public abstract class isIntercompany : Data.BQL.BqlBool.Field<isIntercompany> { }
		[PXDBBool(BqlField = typeof(INTranSplit.isIntercompany))]
		[PXDefault(false)]
		public virtual bool? IsIntercompany
		{
			get;
			set;
		}
		#endregion

		#region CreatedByID
		[PXDBCreatedByID(BqlField = typeof(INTranSplit.createdByID))]
		public virtual Guid? CreatedByID { get; set; }
		public abstract class createdByID : BqlGuid.Field<createdByID> { }
		#endregion
		#region CreatedByScreenID
		[PXDBCreatedByScreenID(BqlField = typeof(INTranSplit.createdByScreenID))]
		public virtual String CreatedByScreenID { get; set; }
		public abstract class createdByScreenID : BqlString.Field<createdByScreenID> { }
		#endregion
		#region CreatedDateTime
		[PXDBCreatedDateTime(BqlField = typeof(INTranSplit.createdDateTime))]
		public virtual DateTime? CreatedDateTime { get; set; }
		public abstract class createdDateTime : BqlDateTime.Field<createdDateTime> { }
		#endregion
		#region LastModifiedByID
		[PXDBLastModifiedByID(BqlField = typeof(INTranSplit.lastModifiedByID))]
		public virtual Guid? LastModifiedByID { get; set; }
		public abstract class lastModifiedByID : BqlGuid.Field<lastModifiedByID> { }
		#endregion
		#region LastModifiedByScreenID
		[PXDBLastModifiedByScreenID(BqlField = typeof(INTranSplit.lastModifiedByScreenID))]
		public virtual String LastModifiedByScreenID { get; set; }
		public abstract class lastModifiedByScreenID : BqlString.Field<lastModifiedByScreenID> { }
		#endregion
		#region LastModifiedDateTime
		[PXDBLastModifiedDateTime(BqlField = typeof(INTranSplit.lastModifiedDateTime))]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		public abstract class lastModifiedDateTime : BqlDateTime.Field<lastModifiedDateTime> { }
		#endregion
		#region tstamp
		[PXDBTimestamp(BqlField = typeof(INTranSplit.Tstamp))]
		public virtual Byte[] tstamp { get; set; }
		public abstract class Tstamp : BqlByteArray.Field<Tstamp> { }
		#endregion

		#region Methods
		public INKitTranSplit() { }
		public INKitTranSplit(string LotSerialNbr, string AssignedNbr, string LotSerClassID)
			: this()
		{
			this.LotSerialNbr = LotSerialNbr;
			this.AssignedNbr = AssignedNbr;
			this.LotSerClassID = LotSerClassID;
		}
		public static implicit operator INLotSerialStatus(INKitTranSplit item)
		{
			INLotSerialStatus ret = new INLotSerialStatus();
			ret.InventoryID = item.InventoryID;
			ret.SiteID = item.SiteID;
			ret.LocationID = item.LocationID;
			ret.SubItemID = item.SubItemID;
			ret.LotSerialNbr = item.LotSerialNbr;

			return ret;
		}
		#endregion
	}

	[PXPrimaryGraph(typeof(KitAssemblyEntry))]
	[PXCacheName(Messages.INKit)]
	[PXProjection(typeof(Select2<INRegister, InnerJoin<INTran, On<INRegister.FK.KitTran>>>), Persistent = true)]
	public partial class INKitRegister : IBqlTable, ILSPrimary
	{
		#region Keys
		public class PK : PrimaryKeyOf<INKitRegister>.By<docType, refNbr> 
		{
			public static INKitRegister Find(PXGraph graph, string docType, string refNbr) => FindBy(graph, docType, refNbr);
		}
		public static class FK
		{
			public class Register : INRegister.PK.ForeignKeyOf<INRegister>.By<docType, refNbr> { }
			public class Tran : INTran.PK.ForeignKeyOf<INTran>.By<tranDocType, tranRefNbr, lineNbr> { }
			public class Site : INSite.PK.ForeignKeyOf<INKitRegister>.By<siteID> { }
			public class Location : INLocation.PK.ForeignKeyOf<INKitRegister>.By<locationID> { }
			public class Branch : GL.Branch.PK.ForeignKeyOf<INKitRegister>.By<branchID> { }
			public class KitInventoryItem : IN.InventoryItem.PK.ForeignKeyOf<INKitRegister>.By<kitInventoryID> { }
			public class KitTran : INTran.PK.ForeignKeyOf<INKitRegister>.By<docType, refNbr, kitLineNbr> { }
			public class KitSpecification : INKitSpecHdr.PK.ForeignKeyOf<INKitRegister>.By<kitInventoryID, kitRevisionID> { }
			public class Project : PMProject.PK.ForeignKeyOf<INKitRegister>.By<projectID> { }
			public class Task : PMTask.PK.ForeignKeyOf<INKitRegister>.By<taskID> { }
			public class InventoryItem : IN.InventoryItem.PK.ForeignKeyOf<INKitRegister>.By<inventoryID> { }
			public class SubItem : INSubItem.PK.ForeignKeyOf<INKitRegister>.By<subItemID> { }
			public class ReasonCode : CS.ReasonCode.PK.ForeignKeyOf<INKitRegister>.By<reasonCode> { }
			//todo public class UnitOfMeasure : INUnit.UK.ByInventory.ForeignKeyOf<INKitRegister>.By<inventoryID, uOM> { }
			//todo public class FinancialPeriod : GL.FinPeriods.TableDefinition.FinPeriod.PK.ForeignKeyOf<INKitRegister>.By<finPeriodID> { }
			//todo public class MasterFinancialPeriod : GL.FinPeriods.TableDefinition.FinPeriod.PK.ForeignKeyOf<INKitRegister>.By<tranPeriodID> { }
		}
		#endregion

		#region INRegister Fields
		#region BranchID
		[Branch(typeof(Search<INSite.branchID, Where<INSite.siteID, Equal<Current<siteID>>>>), IsDetail = false, BqlField = typeof(INRegister.branchID), Enabled = false)]
		[PXDependsOnFields(typeof(tranBranchID))]
		public virtual Int32? BranchID { get; set; }
		public abstract class branchID : BqlInt.Field<branchID> { }
		#endregion
		#region DocType
		[PXDBString(1, IsKey = true, IsFixed = true, BqlField = typeof(INRegister.docType))]
		[PXDefault(INDocType.Production)]
		[INDocType.KitList]
		[PXUIField(DisplayName = "Type", Visibility = PXUIVisibility.SelectorVisible)]
		[PXDependsOnFields(typeof(tranDocType))]
		public virtual String DocType { get; set; }
		public abstract class docType : BqlString.Field<docType> { }
		#endregion
		#region RefNbr
		[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = ">CCCCCCCCCCCCCCC", BqlField = typeof(INRegister.refNbr))]
		[PXDefault]
		[PXUIField(DisplayName = "Reference Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
		[PXSelector(typeof(Search<refNbr, Where<docType, Equal<Optional<docType>>>, OrderBy<Desc<refNbr>>>), Filterable = true)]
		[AutoNumber(typeof(docType), typeof(tranDate),
					new string[] { INDocType.Production, INDocType.Change, INDocType.Disassembly },
					new Type[] { typeof(INSetup.kitAssemblyNumberingID), typeof(INSetup.kitAssemblyNumberingID), typeof(INSetup.kitAssemblyNumberingID) })]
		[PXDependsOnFields(typeof(tranRefNbr))]
		public virtual String RefNbr { get; set; }
		public abstract class refNbr : BqlString.Field<refNbr> { }
		#endregion
		#region OrigModule
		[PXDBString(2, IsFixed = true, BqlField = typeof(INRegister.origModule))]
		[PXDefault(BatchModule.IN)]
		public virtual String OrigModule { get; set; }
		public abstract class origModule : BqlString.Field<origModule> { }
		#endregion
		#region TranDesc
		[PXDBString(256, IsUnicode = true, BqlField = typeof(INRegister.tranDesc))]
		[PXUIField(DisplayName = "Description")]
		[PXFormula(typeof(tranTranDesc))]
		public virtual String TranDesc { get; set; }
		public abstract class tranDesc : BqlString.Field<tranDesc> { }
		#endregion
		#region Released
		[PXDBBool(BqlField = typeof(INRegister.released))]
		[PXDefault(false)]
		[NoUpdateDBField(NoInsert = true)]
		public virtual Boolean? Released { get; set; }
		public abstract class released : BqlBool.Field<released> { }
		#endregion
		#region Hold
		[PXDBBool(BqlField = typeof(INRegister.hold))]
		[PXDefault(typeof(INSetup.holdEntry))]
		[PXUIField(DisplayName = "Hold", Enabled = false)]
		public virtual Boolean? Hold { get; set; }
		public abstract class hold : BqlBool.Field<hold> { }
		#endregion
		#region Status
		[PXDBString(1, IsFixed = true, BqlField = typeof(INRegister.status))]
		[PXDefault]
		[PXUIField(DisplayName = "Status", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[INDocStatus.List]
		public virtual String Status { get; set; }
		public abstract class status : BqlString.Field<status> { }
		#endregion
		#region TranDate
		[PXDBDate(BqlField = typeof(INRegister.tranDate))]
		[PXDefault(typeof(AccessInfo.businessDate))]
		[PXUIField(DisplayName = "Date", Visibility = PXUIVisibility.SelectorVisible)]
		[PXDependsOnFields(typeof(tranTranDate))]
		public virtual DateTime? TranDate { get; set; }
		public abstract class tranDate : BqlDateTime.Field<tranDate> { }
		#endregion
		#region TransferType
		[PXDBString(1, IsFixed = true, BqlField = typeof(INRegister.transferType))]
		[PXDefault(INTransferType.OneStep)]
		[INTransferType.List]
		[PXUIField(DisplayName = "Transfer Type")]
		public virtual String TransferType { get; set; }
		public abstract class transferType : BqlString.Field<transferType> { }
		#endregion
		#region FinPeriodID
		[INOpenPeriod(typeof(tranDate),
			branchSourceType: typeof(siteID),
			branchSourceFormulaType: typeof(Selector<siteID, INSite.branchID>),
			masterFinPeriodIDType: typeof(tranPeriodID),
			IsHeader = true,
			BqlField = typeof(INRegister.finPeriodID))]
		[PXDefault]
		[PXUIField(DisplayName = "Post Period", Visibility = PXUIVisibility.SelectorVisible)]
		[PXDependsOnFields(typeof(tranFinPeriodID))]
		public virtual String FinPeriodID { get; set; }
		public abstract class finPeriodID : BqlString.Field<finPeriodID> { }
		#endregion
		#region TranPeriodID
		[PeriodID(BqlField = typeof(INRegister.tranPeriodID))]
		public virtual String TranPeriodID { get; set; }
		public abstract class tranPeriodID : BqlString.Field<tranPeriodID> { }
		#endregion
		#region LineCntr
		[PXDBInt(BqlField = typeof(INRegister.lineCntr))]
		[PXDefault(1)]
		public virtual Int32? LineCntr { get; set; }
		public abstract class lineCntr : BqlInt.Field<lineCntr> { }
		#endregion
		#region TotalQty
		[PXDBQuantity(BqlField = typeof(INRegister.totalQty))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Total Qty.", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[PXFormula(typeof(qty))]
		public virtual Decimal? TotalQty { get; set; }
		public abstract class totalQty : BqlDecimal.Field<totalQty> { }
		#endregion
		#region TotalAmount
		[PXDBBaseCury(BqlField = typeof(INRegister.totalAmount))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Total Amount", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[PXFormula(typeof(tranAmt))]
		public virtual Decimal? TotalAmount { get; set; }
		public abstract class totalAmount : BqlDecimal.Field<totalAmount> { }
		#endregion
		#region TotalCost
		[PXDBBaseCury(BqlField = typeof(INRegister.totalCost))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Total Cost", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[PXFormula(typeof(tranCost))]
		public virtual Decimal? TotalCost { get; set; }
		public abstract class totalCost : BqlDecimal.Field<totalCost> { }
		#endregion
		#region TotalCostStock
		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Stock Total Cost", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual Decimal? TotalCostStock { get; set; }
		public abstract class totalCostStock : BqlDecimal.Field<totalCostStock> { }
		#endregion
		#region TotalCostNonStock
		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Non-Stock Total Cost", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		public virtual Decimal? TotalCostNonStock { get; set; }
		public abstract class totalCostNonStock : BqlDecimal.Field<totalCostNonStock> { }
		#endregion
		#region ControlQty
		[PXDBQuantity(BqlField = typeof(INRegister.controlQty))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Control Qty.")]
		public virtual Decimal? ControlQty { get; set; }
		public abstract class controlQty : BqlDecimal.Field<controlQty> { }
		#endregion
		#region ControlAmount
		[PXDBBaseCury(BqlField = typeof(INRegister.controlAmount))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Control Amount")]
		public virtual Decimal? ControlAmount { get; set; }
		public abstract class controlAmount : BqlDecimal.Field<controlAmount> { }
		#endregion
		#region ControlCost
		[PXDBBaseCury(BqlField = typeof(INRegister.controlCost))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Control Cost")]
		public virtual Decimal? ControlCost { get; set; }
		public abstract class controlCost : BqlDecimal.Field<controlCost> { }
		#endregion
		#region KitInventoryID
		[PXDefault]
		[PXDBInt(BqlField = typeof(INRegister.kitInventoryID))]
		[PXUIField(DisplayName = "Inventory ID", Visibility = PXUIVisibility.Visible)]
		[PXDimensionSelector(InventoryAttribute.DimensionName,
			typeof(Search2<InventoryItem.inventoryID,
				InnerJoin<INKitSpecHdrDistinct, On<InventoryItem.inventoryID, Equal<INKitSpecHdrDistinct.kitInventoryID>>>,
				Where<InventoryItem.stkItem, Equal<boolTrue>, And<Match<Current<AccessInfo.userName>>>>>),
			typeof(InventoryItem.inventoryCD), DescriptionField = typeof(InventoryItem.descr))]
		[PXDependsOnFields(typeof(inventoryID))]
		public virtual Int32? KitInventoryID { get; set; }
		public abstract class kitInventoryID : BqlInt.Field<kitInventoryID> { }
		#endregion
		#region KitRevisionID
		[PXDefault]
		[PXDBString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa", BqlField = typeof(INRegister.kitRevisionID))]
		[PXUIField(DisplayName = "Revision", Visibility = PXUIVisibility.Visible)]
		[PXRestrictor(typeof(Where<INKitSpecHdr.isActive, Equal<True>>), Messages.InactiveKitRevision, typeof(INKitSpecHdr.revisionID))]
		[PXSelector(typeof(Search<INKitSpecHdr.revisionID,
			Where<INKitSpecHdr.kitInventoryID, Equal<Current<kitInventoryID>>>>), typeof(INKitSpecHdr.kitInventoryID), typeof(INKitSpecHdr.descr))]
		public virtual String KitRevisionID { get; set; }
		public abstract class kitRevisionID : BqlString.Field<kitRevisionID> { }
		#endregion
		#region KitLineNbr
		[PXDBInt(BqlField = typeof(INRegister.kitLineNbr))]
		[PXDefault(0)]
		[PXDependsOnFields(typeof(lineNbr))]
		public virtual Int32? KitLineNbr { get; set; }
		public abstract class kitLineNbr : BqlInt.Field<kitLineNbr> { }
		#endregion
		#region BatchNbr
		[PXDBString(15, IsUnicode = true, BqlField = typeof(INRegister.batchNbr))]
		[PXUIField(DisplayName = "Batch Nbr.", Visibility = PXUIVisibility.Visible, Enabled = false)]
		[PXSelector(typeof(Search<Batch.batchNbr, Where<Batch.module, Equal<BatchModule.moduleIN>>>))]
		public virtual String BatchNbr { get; set; }
		public abstract class batchNbr : BqlString.Field<batchNbr> { }
		#endregion
		#region LotSerTrack
		[PXString(1)]
		[PXFormula(typeof(Selector<kitInventoryID, Selector<InventoryItem.lotSerClassID, INLotSerClass.lotSerTrack>>))]
		public virtual String LotSerTrack { get; set; }
		public abstract class lotSerTrack : BqlString.Field<lotSerTrack> { }
		#endregion
		#region CreatedByID
		[PXDBCreatedByID(BqlField = typeof(INRegister.createdByID))]
		public virtual Guid? CreatedByID { get; set; }
		public abstract class createdByID : BqlGuid.Field<createdByID> { }
		#endregion
		#region CreatedByScreenID
		[PXDBCreatedByScreenID(BqlField = typeof(INRegister.createdByScreenID))]
		public virtual String CreatedByScreenID { get; set; }
		public abstract class createdByScreenID : BqlString.Field<createdByScreenID> { }
		#endregion
		#region CreatedDateTime
		[PXDBCreatedDateTime(BqlField = typeof(INRegister.createdDateTime))]
		[PXUIField(DisplayName = PXDBLastModifiedByIDAttribute.DisplayFieldNames.CreatedDateTime, Enabled = false)]
		public virtual DateTime? CreatedDateTime { get; set; }
		public abstract class createdDateTime : BqlDateTime.Field<createdDateTime> { }
		#endregion
		#region LastModifiedByID
		[PXDBLastModifiedByID(BqlField = typeof(INRegister.lastModifiedByID))]
		public virtual Guid? LastModifiedByID { get; set; }
		public abstract class lastModifiedByID : BqlGuid.Field<lastModifiedByID> { }
		#endregion
		#region LastModifiedByScreenID
		[PXDBLastModifiedByScreenID(BqlField = typeof(INRegister.lastModifiedByScreenID))]
		public virtual String LastModifiedByScreenID { get; set; }
		public abstract class lastModifiedByScreenID : BqlString.Field<lastModifiedByScreenID> { }
		#endregion
		#region LastModifiedDateTime
		[PXDBLastModifiedDateTime(BqlField = typeof(INRegister.lastModifiedDateTime))]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		public abstract class lastModifiedDateTime : BqlDateTime.Field<lastModifiedDateTime> { }
		#endregion
		#region NoteID
		[PXSearchable(SM.SearchCategory.IN, Messages.SearchableTitleKit, new Type[] { typeof(docType), typeof(refNbr), typeof(tranDocType), typeof(tranType), typeof(tranRefNbr) },//excessive fields are required because of _DocType that is referensed in 2 property get/set
			new Type[] { typeof(tranDesc), typeof(tranTranDesc) },
			NumberFields = new Type[] { typeof(refNbr) },
			Line1Format = "{1}{2}{3:d}{4}", Line1Fields = new Type[] { typeof(kitInventoryID), typeof(InventoryItem.inventoryCD), typeof(kitRevisionID), typeof(tranDate), typeof(status) },
			Line2Format = "{0}", Line2Fields = new Type[] { typeof(tranTranDesc) },
			SelectForFastIndexing = typeof(Select<INKitRegister, Where<docType, Equal<INDocType.production>, And<docType, Equal<INDocType.disassembly>>>>)
		)]
		[PXNote(BqlField = typeof(INRegister.noteID))]
		public virtual Guid? NoteID { get; set; }
		public abstract class noteID : BqlGuid.Field<noteID> { }
		#endregion
		#region tstamp
		[PXDBTimestamp(BqlField = typeof(INRegister.Tstamp))]
		public virtual Byte[] tstamp { get; set; }
		public abstract class Tstamp : BqlByteArray.Field<Tstamp> { }
		#endregion
		#endregion

		#region INTran Fields
		#region TranDocType
		[PXDBString(1, IsFixed = true, BqlField = typeof(INTran.docType))]
		[PXDefault]
		[PXRestriction]
		public virtual String TranDocType
		{
			get => this.DocType;
			set => this.DocType = value;
		}
		public abstract class tranDocType : BqlString.Field<tranDocType> { }
		#endregion
		#region TranType
		[PXDBString(3, IsFixed = true, BqlField = typeof(INTran.tranType))]
		[PXDefault]
		[PXFormula(typeof(Switch<Case<Where<tranDocType, Equal<INDocType.disassembly>>, INTranType.disassembly>, INTranType.assembly>))]
		[INTranType.List]
		[PXUIField(DisplayName = "Tran. Type")]
		public virtual String TranType { get; set; }
		public abstract class tranType : BqlString.Field<tranType> { }
		#endregion
		#region TranRefNbr
		[PXDBString(15, IsUnicode = true, BqlField = typeof(INTran.refNbr))]
		[PXDefault]
		[PXRestriction]
		public virtual String TranRefNbr
		{
			get => this.RefNbr;
			set => this.RefNbr = value;
		}
		public abstract class tranRefNbr : BqlString.Field<tranRefNbr> { }
		#endregion
		#region TranBranchID
		[PXDBInt(BqlField = typeof(INTran.branchID))]
		public virtual Int32? TranBranchID
		{
			get => this.BranchID;
			set => this.BranchID = value;
		}
		public abstract class tranBranchID : BqlInt.Field<tranBranchID> { }
		#endregion
		#region LineNbr
		[PXDBInt(BqlField = typeof(INTran.lineNbr))]
		[PXDefault]
		[PXRestriction]
		public virtual Int32? LineNbr
		{
			get => KitLineNbr;
			set => KitLineNbr = value;
		}
		public abstract class lineNbr : BqlInt.Field<lineNbr> { }
		#endregion
		#region AssyType
		[PXDefault(INAssyType.KitTran)]
		[PXDBString(1, IsFixed = true, BqlField = typeof(INTran.assyType))]
		public virtual String AssyType { get; set; }
		public abstract class assyType : BqlString.Field<assyType> { }
		#endregion
		#region ProjectID
		[PM.ProjectDefault]
		[PXDBInt(BqlField = typeof(INTran.projectID))]
		public virtual Int32? ProjectID { get; set; }
		public abstract class projectID : BqlInt.Field<projectID> { }
		#endregion
		#region TaskID
		[PXDBInt(BqlField = typeof(INTran.taskID))]
		public virtual Int32? TaskID { get; set; }
		public abstract class taskID : BqlInt.Field<taskID> { }
		#endregion
		#region TranTranDate
		[PXDBDate(BqlField = typeof(INTran.tranDate))]
		public virtual DateTime? TranTranDate
		{
			get => this.TranDate;
			set => this.TranDate = value;
		}
		public abstract class tranTranDate : BqlDateTime.Field<tranTranDate> { }
		#endregion
		#region InvtMult
		[PXDBShort(BqlField = typeof(INTran.invtMult))]
		[PXDefault((short)1)]
		public virtual Int16? InvtMult { get; set; }
		public abstract class invtMult : BqlShort.Field<invtMult> { }
		#endregion
		#region InventoryID
		/// <summary>
		/// This field declaration is for population of <see cref="INTran.InventoryID"/> within projection
		/// </summary>
		[PXDBInt(BqlField = typeof(INTran.inventoryID))]
		public virtual Int32? InventoryID
		{
			get => this.KitInventoryID;
			set => this.KitInventoryID = value;
		}
		public abstract class inventoryID : BqlInt.Field<inventoryID> { }
		#endregion
		#region SubItemID
		[SubItem(typeof(kitInventoryID), BqlField = typeof(INTran.subItemID))]
		public virtual Int32? SubItemID { get; set; }
		public abstract class subItemID : BqlInt.Field<subItemID> { }
		#endregion
		#region SiteID
		[PXDefault]
		[SiteAvail(typeof(kitInventoryID), typeof(subItemID), BqlField = typeof(INTran.siteID))]
		[InterBranchRestrictor(typeof(Where<SameOrganizationBranch<INSite.branchID, Current<AccessInfo.branchID>>>))]
		public virtual Int32? SiteID { get; set; }
		public abstract class siteID : BqlInt.Field<siteID> { }
		#endregion
		#region LocationID
		[LocationAvail(typeof(kitInventoryID),
					  typeof(subItemID),
					  typeof(siteID),
					  typeof(Where<False>),
					  typeof(Where2<Where<tranType, Equal<INTranType.assembly>, Or<tranType, Equal<INTranType.disassembly>>>, And<invtMult, Equal<short1>>>),
					  typeof(Where<False, Equal<True>>), BqlField = typeof(INTran.locationID))]
		public virtual Int32? LocationID { get; set; }
		public abstract class locationID : BqlInt.Field<locationID> { }
		#endregion
		#region UOM
		[PXDefault(typeof(Search<InventoryItem.baseUnit, Where<InventoryItem.inventoryID, Equal<Current<kitInventoryID>>>>))]
		[INUnit(typeof(kitInventoryID), BqlField = typeof(INTran.uOM))]
		public virtual String UOM { get; set; }
		public abstract class uOM : BqlString.Field<uOM> { }
		#endregion
		#region Qty
		[PXDBQuantity(typeof(uOM), typeof(baseQty), InventoryUnitType.BaseUnit, BqlField = typeof(INTran.qty))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Quantity")]
		public virtual Decimal? Qty { get; set; }
		public abstract class qty : BqlDecimal.Field<qty> { }
		#endregion
		#region BaseQty
		[PXDBQuantity(BqlField = typeof(INTran.baseQty))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? BaseQty { get; set; }
		public abstract class baseQty : BqlDecimal.Field<baseQty> { }
		#endregion
		#region UnassignedQty
		[PXDBDecimal(6, BqlField = typeof(INTran.unassignedQty))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? UnassignedQty { get; set; }
		public abstract class unassignedQty : BqlDecimal.Field<unassignedQty> { }
		#endregion
		#region TranReleased
		[PXDBBool(BqlField = typeof(INTran.released))]
		[PXDefault(false)]
		public virtual Boolean? TranReleased { get; set; }
		public abstract class tranReleased : BqlBool.Field<tranReleased> { }
		#endregion
		#region TranFinPeriodID
		[PXDefault]
		[FinPeriodID(
			sourceType: typeof(tranTranDate),
			branchSourceType: typeof(tranBranchID),
			masterFinPeriodIDType: typeof(tranTranPeriodID),
			IsHeader = true,
			BqlField = typeof(INTran.finPeriodID))]
		public virtual String TranFinPeriodID
		{
			get => this.FinPeriodID;
			set => this.FinPeriodID = value;
		}
		public abstract class tranFinPeriodID : BqlString.Field<tranFinPeriodID> { }
		#endregion
		#region TranTranPeriodID
		[FinPeriodID(BqlField = typeof(INTran.tranPeriodID))]
		public virtual String TranTranPeriodID
		{
			get => this.TranPeriodID;
			set => this.TranPeriodID = value;
		}
		public abstract class tranTranPeriodID : BqlString.Field<tranTranPeriodID> { }
		#endregion
		#region UnitPrice
		[PXDBPriceCost(BqlField = typeof(INTran.unitPrice))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Unit Price")]
		public virtual Decimal? UnitPrice { get; set; }
		public abstract class unitPrice : BqlDecimal.Field<unitPrice> { }
		#endregion
		#region TranAmt
		[PXDBBaseCury(BqlField = typeof(INTran.tranAmt))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Ext. Price")]
		public virtual Decimal? TranAmt { get; set; }
		public abstract class tranAmt : BqlDecimal.Field<tranAmt> { }
		#endregion
		#region UnitCost
		[PXDBPriceCost(BqlField = typeof(INTran.unitCost))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Unit Cost")]
		public virtual Decimal? UnitCost { get; set; }
		public abstract class unitCost : BqlDecimal.Field<unitCost> { }
		#endregion
		#region TranCost
		[PXDBBaseCury(BqlField = typeof(INTran.tranCost))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Ext. Cost")]
		[PXFormula(typeof(Add<totalCostStock, totalCostNonStock>))]
		public virtual Decimal? TranCost { get; set; }
		public abstract class tranCost : BqlDecimal.Field<tranCost> { }
		#endregion
		#region TranTranDesc
		[PXDBString(60, IsUnicode = true, BqlField = typeof(INTran.tranDesc))]
		[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
		[PXFormula(typeof(Selector<kitInventoryID, InventoryItem.descr>))]
		public virtual String TranTranDesc { get; set; }
		public abstract class tranTranDesc : BqlString.Field<tranTranDesc> { }
		#endregion
		#region ReasonCode
		[PXDBString(CS.ReasonCode.reasonCodeID.Length, IsUnicode = true, BqlField = typeof(INTran.reasonCode))]
		[PXSelector(typeof(Search<ReasonCode.reasonCodeID, Where<ReasonCode.usage, Equal<Optional<docType>>>>))]
		[PXUIField(DisplayName = "Reason Code")]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual String ReasonCode { get; set; }
		public abstract class reasonCode : BqlString.Field<reasonCode> { }
		#endregion
		#region LotSerialNbr
		[INLotSerialNbr(typeof(kitInventoryID), typeof(subItemID), typeof(locationID), PersistingCheck = PXPersistingCheck.Nothing, BqlField = typeof(INTran.lotSerialNbr))]
		public virtual String LotSerialNbr { get; set; }
		public abstract class lotSerialNbr : BqlString.Field<lotSerialNbr> { }
		#endregion
		#region ExpireDate
		[INExpireDate(typeof(kitInventoryID), PersistingCheck = PXPersistingCheck.Nothing, BqlField = typeof(INTran.expireDate))]
		public virtual DateTime? ExpireDate { get; set; }
		public abstract class expireDate : BqlDateTime.Field<expireDate> { }
		#endregion
		#region UpdateShippedNotInvoiced
		[PXDBBool(BqlField = typeof(INTran.updateShippedNotInvoiced))]
		[PXDefault(false)]
		public virtual Boolean? UpdateShippedNotInvoiced { get; set; }
		public abstract class updateShippedNotInvoiced : BqlBool.Field<updateShippedNotInvoiced> { }
		#endregion
		#region HasMixedProjectTasks
		/// <summary>
		/// Returns true if the splits associated with the line has mixed ProjectTask values.
		/// This field is used to validate the record on persist. 
		/// </summary>
		[PXBool]
		[PXFormula(typeof(False))]
		public virtual bool? HasMixedProjectTasks { get; set; }
		public abstract class hasMixedProjectTasks : BqlBool.Field<hasMixedProjectTasks> { }
		#endregion
		#region IsIntercompany
		public abstract class isIntercompany : Data.BQL.BqlBool.Field<isIntercompany> { }
		[PXDBBool(BqlField = typeof(INTran.isIntercompany))]
		[PXDefault(false)]
		public virtual bool? IsIntercompany
		{
			get;
			set;
		}
		#endregion

		#region TranCreatedByID
		[PXDBCreatedByID(BqlField = typeof(INTran.createdByID))]
		public virtual Guid? TranCreatedByID { get; set; }
		public abstract class trancreatedByID : BqlGuid.Field<trancreatedByID> { }
		#endregion
		#region TranCreatedByScreenID
		[PXDBCreatedByScreenID(BqlField = typeof(INTran.createdByScreenID))]
		public virtual String TranCreatedByScreenID { get; set; }
		public abstract class trancreatedByScreenID : BqlString.Field<trancreatedByScreenID> { }
		#endregion
		#region TranCreatedDateTime
		[PXDBCreatedDateTime(BqlField = typeof(INTran.createdDateTime))]
		public virtual DateTime? TranCreatedDateTime { get; set; }
		public abstract class trancreatedDateTime : BqlDateTime.Field<trancreatedDateTime> { }
		#endregion
		#region TranLastModifiedByID
		[PXDBLastModifiedByID(BqlField = typeof(INTran.lastModifiedByID))]
		public virtual Guid? TranLastModifiedByID { get; set; }
		public abstract class tranlastModifiedByID : BqlGuid.Field<tranlastModifiedByID> { }
		#endregion
		#region TranLastModifiedByScreenID
		[PXDBLastModifiedByScreenID(BqlField = typeof(INTran.lastModifiedByScreenID))]
		public virtual String TranLastModifiedByScreenID { get; set; }
		public abstract class tranlastModifiedByScreenID : BqlString.Field<tranlastModifiedByScreenID> { }
		#endregion
		#region TranLastModifiedDateTime
		[PXDBLastModifiedDateTime(BqlField = typeof(INTran.lastModifiedDateTime))]
		public virtual DateTime? TranLastModifiedDateTime { get; set; }
		public abstract class tranlastModifiedDateTime : BqlDateTime.Field<tranlastModifiedDateTime> { }
		#endregion
		#region tstamp
		[PXDBTimestamp(BqlField = typeof(INTran.Tstamp))]
		public virtual Byte[] Trantstamp { get; set; }
		public abstract class tranTstamp : BqlByteArray.Field<tranTstamp> { }
		#endregion
		#endregion

		#region Methods
		//This is a bad idea... but still
		public static implicit operator INKitTranSplit(INKitRegister item)
		{
			INKitTranSplit ret = new INKitTranSplit();
			ret.DocType = item.DocType;
			ret.TranType = item.TranType;
			ret.RefNbr = item.RefNbr;
			ret.LineNbr = item.LineNbr;
			ret.SplitLineNbr = 1;
			ret.InventoryID = item.KitInventoryID;
			ret.SiteID = item.SiteID;
			ret.SubItemID = item.SubItemID;
			ret.LocationID = item.LocationID;
			ret.LotSerialNbr = item.LotSerialNbr;
			ret.ExpireDate = item.ExpireDate;
			ret.Qty = item.Qty;
			ret.UOM = item.UOM;
			ret.TranDate = item.TranDate;
			ret.BaseQty = item.BaseQty;
			ret.InvtMult = item.InvtMult;
			ret.Released = item.Released;

			return ret;
		}

		public static explicit operator INRegister(INKitRegister inKitDoc)
		{
			INRegister indoc = new INRegister
			{
				BatchNbr = inKitDoc.BatchNbr,
				ControlAmount = inKitDoc.ControlAmount,
				ControlCost = inKitDoc.ControlCost,
				ControlQty = inKitDoc.ControlQty,
				CreatedByID = inKitDoc.CreatedByID,
				CreatedByScreenID = inKitDoc.CreatedByScreenID,
				CreatedDateTime = inKitDoc.CreatedDateTime,
				DocType = inKitDoc.DocType,
				FinPeriodID = inKitDoc.FinPeriodID,
				Hold = inKitDoc.Hold,
				KitInventoryID = inKitDoc.KitInventoryID,
				KitLineNbr = inKitDoc.KitLineNbr,
				KitRevisionID = inKitDoc.KitRevisionID,
				LastModifiedByID = inKitDoc.LastModifiedByID,
				LastModifiedByScreenID = inKitDoc.LastModifiedByScreenID,
				LastModifiedDateTime = inKitDoc.LastModifiedDateTime,
				LineCntr = inKitDoc.LineCntr,
				NoteID = inKitDoc.NoteID,
				OrigModule = inKitDoc.OrigModule,
				RefNbr = inKitDoc.RefNbr,
				Released = inKitDoc.Released,
				SiteID = inKitDoc.SiteID,
				Status = inKitDoc.Status,
				TotalAmount = inKitDoc.TotalAmount,
				TotalCost = inKitDoc.TotalCost,
				TotalQty = inKitDoc.TotalQty,
				TranDate = inKitDoc.TranDate,
				TranDesc = inKitDoc.TranDesc,
				TranPeriodID = inKitDoc.TranPeriodID,
				TransferType = inKitDoc.TransferType,
				tstamp = inKitDoc.tstamp
			};
			return indoc;
		}
		#endregion
	}

	[PXProjection(typeof(Select4<INKitSpecHdr, Where<INKitSpecHdr.isActive, Equal<boolTrue>>, Aggregate<GroupBy<INKitSpecHdr.kitInventoryID>>>))]
	[PXHidden]
	public partial class INKitSpecHdrDistinct : IBqlTable
	{
		#region KitInventoryID
		[PXDBInt(BqlField = typeof(INKitSpecHdr.kitInventoryID))]
		public virtual Int32? KitInventoryID { get; set; }
		public abstract class kitInventoryID : BqlInt.Field<kitInventoryID> { }
		#endregion
	}
	#endregion

	#region Attribute Extensions
	public class INKitTranSplitPlanIDAttribute : INTranSplitPlanIDAttribute
	{
		#region Ctor
		public INKitTranSplitPlanIDAttribute(Type ParentNoteID, Type ParentHoldEntry, Type ParentTransferType)
			: base(ParentNoteID, ParentHoldEntry, ParentTransferType)
		{
			_ParentTransferType = ParentTransferType;
		}
		#endregion
		#region Initialization
		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);

			if (!sender.Graph.IsImport && !sender.Graph.UnattendedMode)
			{
				//plan source should go before plan
				//to bind errors from INItemPlan.SubItemID -> SOLine.SubItemID or SOLineSplit.SubItemID
				if (!sender.Graph.Views.Caches.Contains(_ItemType))
					sender.Graph.Views.Caches.Add(_ItemType);
				if (sender.Graph.Views.Caches.Contains(typeof(INItemPlan)))
					sender.Graph.Views.Caches.Remove(typeof(INItemPlan));
				sender.Graph.Views.Caches.Add(typeof(INItemPlan));
			}

			if (sender.Graph.Views.Caches.Contains(typeof(LotSerialStatus)))
				sender.Graph.Views.Caches.Remove(typeof(LotSerialStatus));
			sender.Graph.Views.Caches.Add(typeof(LotSerialStatus));
			if (sender.Graph.Views.Caches.Contains(typeof(ItemLotSerial)))
				sender.Graph.Views.Caches.Remove(typeof(ItemLotSerial));
			sender.Graph.Views.Caches.Add(typeof(ItemLotSerial));
			if (sender.Graph.Views.Caches.Contains(typeof(SiteLotSerial)))
				sender.Graph.Views.Caches.Remove(typeof(SiteLotSerial));
			sender.Graph.Views.Caches.Add(typeof(SiteLotSerial));
			if (sender.Graph.Views.Caches.Contains(typeof(LocationStatus)))
				sender.Graph.Views.Caches.Remove(typeof(LocationStatus));
			sender.Graph.Views.Caches.Add(typeof(LocationStatus));
			if (sender.Graph.Views.Caches.Contains(typeof(SiteStatus)))
				sender.Graph.Views.Caches.Remove(typeof(SiteStatus));
			sender.Graph.Views.Caches.Add(typeof(SiteStatus));

		}
		#endregion
		#region Implementation
		public override void Parent_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			base.Parent_RowUpdated(sender, e);

			if (!sender.ObjectsEqual<INKitRegister.hold>(e.Row, e.OldRow))
			{
				PXCache plancache = sender.Graph.Caches[typeof(INItemPlan)];

				//preload plans in cache first
				var not_inserted = PXSelect<INItemPlan, Where<INItemPlan.refNoteID, Equal<Current<INKitRegister.noteID>>>>.Select(sender.Graph).ToList();

				foreach (INTranSplit split in PXSelect<INTranSplit, Where<INTranSplit.docType, Equal<Current<INKitRegister.docType>>, And<INTranSplit.refNbr, Equal<Current<INKitRegister.refNbr>>>>>.Select(sender.Graph))
				{
					foreach (INItemPlan plan in plancache.Cached)
					{
						if (object.Equals(plan.PlanID, split.PlanID) && plancache.GetStatus(plan) != PXEntryStatus.Deleted && plancache.GetStatus(plan) != PXEntryStatus.InsertedDeleted)
						{
							plan.Hold = (bool?)sender.GetValue<INKitRegister.hold>(e.Row);
							if (!GetAllocateDocumentsOnHold(sender.Graph))
							{
								plancache.Update(plan);
							}
							else
							{
								plancache.MarkUpdated(plan);
							}
						}
					}
				}

				// PXSelect<INTranSplit> doesn't merge result with INKitTranSplit because Caches[typeof(INTranSplit)] returns INComponentTranSplit cache
				var kitTranSplitCache = sender.Graph.Caches[typeof(INKitTranSplit)];

				var kitSplits = kitTranSplitCache.Cached.Cast<INKitTranSplit>()
					.Where(s => kitTranSplitCache.GetStatus(s).IsIn(PXEntryStatus.Inserted, PXEntryStatus.Updated));

				foreach (INKitTranSplit split in kitSplits)
				{
					foreach (INItemPlan plan in plancache.Cached)
					{
						if (object.Equals(plan.PlanID, split.PlanID) && plancache.GetStatus(plan) != PXEntryStatus.Deleted && plancache.GetStatus(plan) != PXEntryStatus.InsertedDeleted)
						{
							plan.Hold = (bool?)sender.GetValue<INKitRegister.hold>(e.Row);
							if (!GetAllocateDocumentsOnHold(sender.Graph))
							{
								plancache.Update(plan);
							}
							else
							{
								plancache.MarkUpdated(plan);
							}
						}
					}
				}
			}
		}

		public override INItemPlan DefaultValues(PXCache sender, INItemPlan plan_Row, object orig_Row)
		{
			if (orig_Row is INKitTranSplit)
			{
				INKitTranSplit row = (INKitTranSplit)orig_Row;

				INTranSplit properties = new INTranSplit();
				properties.InventoryID = row.InventoryID;
				properties.SubItemID = row.SubItemID;
				properties.SiteID = row.SiteID;
				properties.LocationID = row.LocationID;
				properties.LotSerialNbr = row.LotSerialNbr;
				properties.AssignedNbr = row.AssignedNbr;
				properties.BaseQty = row.BaseQty;
				properties.TranType = row.TranType;
				properties.InvtMult = row.InvtMult;

				return base.DefaultValues(sender, plan_Row, properties);

			}
			else
			{
				return base.DefaultValues(sender, plan_Row, orig_Row);
			}
		}
		#endregion
	}
	#endregion

	#region LSSelect Extensions
	public class LSINComponentTran : LSSelect<INComponentTran, INComponentTranSplit,
		Where<INComponentTranSplit.docType, Equal<Current<INKitRegister.docType>>,
		And<INComponentTranSplit.refNbr, Equal<Current<INKitRegister.refNbr>>>>>
	{
		#region Ctor
		public LSINComponentTran(PXGraph graph)
			: base(graph)
		{
			graph.FieldDefaulting.AddHandler<INComponentTranSplit.subItemID>(INTranSplit_SubItemID_FieldDefaulting);
			graph.FieldDefaulting.AddHandler<INComponentTranSplit.locationID>(INTranSplit_LocationID_FieldDefaulting);
			graph.FieldDefaulting.AddHandler<INComponentTranSplit.invtMult>(INTranSplit_InvtMult_FieldDefaulting);
			graph.FieldDefaulting.AddHandler<INComponentTranSplit.lotSerialNbr>(INTranSplit_LotSerialNbr_FieldDefaulting);
			graph.FieldVerifying.AddHandler<INComponentTranSplit.qty>(INTranSplit_Qty_FieldVerifying);
			graph.FieldVerifying.AddHandler<INComponentTran.qty>(INTran_Qty_FieldVerifying);
			graph.RowUpdated.AddHandler<INKitRegister>(INKitRegister_RowUpdated);
			graph.RowUpdated.AddHandler<INComponentTran>(INComponentTran_RowUpdated);
		}
		#endregion

		#region Implementation

		protected override void SetEditMode()
		{
			if (!Initialized)
			{
				PXUIFieldAttribute.SetEnabled(MasterCache, null, true);
				PXUIFieldAttribute.SetEnabled(DetailCache, null, true);
				PXUIFieldAttribute.SetEnabled(DetailCache, null, "UOM", false);
				Initialized = true;
			}
		}

		protected virtual void INKitRegister_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			if (!sender.ObjectsEqual<INKitRegister.hold>(e.Row, e.OldRow) && (bool?)sender.GetValue<INKitRegister.hold>(e.Row) == false)
			{
				PXCache cache = sender.Graph.Caches[typeof(INComponentTran)];

				foreach (INComponentTran item in PXParentAttribute.SelectSiblings(cache, null, typeof(INKitRegister)))
				{
					if (Math.Abs((decimal)item.BaseQty) >= 0.0000005m && (item.UnassignedQty >= 0.0000005m || item.UnassignedQty <= -0.0000005m))
					{
						cache.RaiseExceptionHandling<INComponentTran.qty>(item, item.Qty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned));

						cache.MarkUpdated(item);
					}
				}
			}
		}

		protected override void Master_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert || (e.Operation & PXDBOperation.Command) == PXDBOperation.Update)
			{
				PXCache cache = sender.Graph.Caches[typeof(INKitRegister)];
				object doc = PXParentAttribute.SelectParent(sender, e.Row, typeof(INKitRegister)) ?? cache.Current;

				bool? OnHold = (bool?)cache.GetValue<INKitRegister.hold>(doc);

				if (OnHold == false && Math.Abs((decimal)((INComponentTran)e.Row).BaseQty) >= 0.0000005m && (((INComponentTran)e.Row).UnassignedQty >= 0.0000005m || ((INComponentTran)e.Row).UnassignedQty <= -0.0000005m))
				{
					if (sender.RaiseExceptionHandling<INComponentTran.qty>(e.Row, ((INComponentTran)e.Row).Qty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned)))
					{
						throw new PXRowPersistingException(typeof(INComponentTran.qty).Name, ((INComponentTran)e.Row).Qty, Messages.BinLotSerialNotAssigned);
					}
				}
			}
			base.Master_RowPersisting(sender, e);
		}

		protected virtual void INComponentTran_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			INComponentTran row = (INComponentTran)e.Row;
			if (row == null) return;
			if (!PXLongOperation.Exists(sender.Graph.UID))
			{
				IQtyAllocated availability = AvailabilityFetch(sender, (INComponentTran)e.Row, AvailabilityFetchMode.ExcludeCurrent);

				if (availability != null)
				{
					PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, ((INComponentTran)e.Row).InventoryID);

					availability.QtyOnHand = INUnitAttribute.ConvertFromBase<INComponentTran.inventoryID, INComponentTran.uOM>(sender, e.Row, (decimal)availability.QtyOnHand, INPrecision.QUANTITY);
					availability.QtyAvail = INUnitAttribute.ConvertFromBase<INComponentTran.inventoryID, INComponentTran.uOM>(sender, e.Row, (decimal)availability.QtyAvail, INPrecision.QUANTITY);
					availability.QtyNotAvail = INUnitAttribute.ConvertFromBase<INComponentTran.inventoryID, INComponentTran.uOM>(sender, e.Row, (decimal)availability.QtyNotAvail, INPrecision.QUANTITY);
					availability.QtyHardAvail = INUnitAttribute.ConvertFromBase<INComponentTran.inventoryID, INComponentTran.uOM>(sender, e.Row, (decimal)availability.QtyHardAvail, INPrecision.QUANTITY);

					AvailabilityCheck(sender, (INComponentTran)e.Row, availability);
				}
			}
		}
		protected override DateTime? ExpireDateByLot(PXCache sender, ILSMaster item, ILSMaster master)
		{
			if (master != null && master.InvtMult > 0)
			{
				item.ExpireDate = null;
				return base.ExpireDateByLot(sender, item, null);
			}
			else return base.ExpireDateByLot(sender, item, master);
		}

		public override void Availability_FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			if (!PXLongOperation.Exists(sender.Graph.UID))
			{
				IQtyAllocated availability = AvailabilityFetch(sender, (INComponentTran)e.Row, ((INComponentTran)e.Row)?.Released == true ? AvailabilityFetchMode.None : AvailabilityFetchMode.ExcludeCurrent);

				if (availability != null)
				{
					PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, ((INComponentTran)e.Row).InventoryID);

					availability.QtyOnHand = INUnitAttribute.ConvertFromBase<INComponentTran.inventoryID, INComponentTran.uOM>(sender, e.Row, (decimal)availability.QtyOnHand, INPrecision.QUANTITY);
					availability.QtyAvail = INUnitAttribute.ConvertFromBase<INComponentTran.inventoryID, INComponentTran.uOM>(sender, e.Row, (decimal)availability.QtyAvail, INPrecision.QUANTITY);
					availability.QtyNotAvail = INUnitAttribute.ConvertFromBase<INComponentTran.inventoryID, INComponentTran.uOM>(sender, e.Row, (decimal)availability.QtyNotAvail, INPrecision.QUANTITY);
					availability.QtyHardAvail = INUnitAttribute.ConvertFromBase<INComponentTran.inventoryID, INComponentTran.uOM>(sender, e.Row, (decimal)availability.QtyHardAvail, INPrecision.QUANTITY);
					availability.QtyActual = INUnitAttribute.ConvertFromBase<INComponentTran.inventoryID, INComponentTran.uOM>(sender, e.Row, (decimal)availability.QtyActual, INPrecision.QUANTITY);

					e.ReturnValue = PXMessages.LocalizeFormatNoPrefix(Messages.Availability_ActualInfo,
						sender.GetValue<INTran.uOM>(e.Row),
						FormatQty(availability.QtyOnHand),
						FormatQty(availability.QtyAvail),
						FormatQty(availability.QtyHardAvail),
						FormatQty(availability.QtyActual));
				}
				else
				{
					e.ReturnValue = string.Empty;
				}
			}
			else
			{
				e.ReturnValue = string.Empty;
			}

			base.Availability_FieldSelecting(sender, e);
		}

		protected override IEnumerable<PXExceptionInfo> GetAvailabilityCheckErrors(PXCache sender, ILSMaster Row, IQtyAllocated availability)
		{
			foreach (var errorInfo in base.GetAvailabilityCheckErrors(sender, Row, availability))
				yield return errorInfo;
			if (Row.InvtMult == -1 && Row.BaseQty > 0m)
			{
				if (availability != null && availability.QtyAvail < Row.Qty)
				{
					if (availability is LotSerialStatus)
						yield return new PXExceptionInfo(Messages.StatusCheck_QtyLotSerialNegative);
					else if (availability is LocationStatus)
						yield return new PXExceptionInfo(Messages.StatusCheck_QtyLocationNegative);
					else if (availability is SiteStatus)
						yield return new PXExceptionInfo(Messages.StatusCheck_QtyNegative);
				}
			}
		}

		public override INComponentTranSplit Convert(INComponentTran item)
		{
			using (InvtMultScope<INComponentTran> ms = new InvtMultScope<INComponentTran>(item))
			{
				INComponentTranSplit ret = item;
				//baseqty will be overriden in all cases but AvailabilityFetch
				ret.BaseQty = item.BaseQty - item.UnassignedQty;
				return ret;
			}
		}

		public void ThrowFieldIsEmpty<Field>(PXCache sender, object data)
			where Field : IBqlField
		{
			if (sender.RaiseExceptionHandling<Field>(data, null, new PXSetPropertyException(ErrorMessages.FieldIsEmpty, $"[{typeof(Field).Name}]")))
			{
				throw new PXRowPersistingException(typeof(Field).Name, null, ErrorMessages.FieldIsEmpty, typeof(Field).Name);
			}
		}

		public virtual void INTranSplit_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if (e.Row != null && ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert || (e.Operation & PXDBOperation.Command) == PXDBOperation.Update))
			{
				if (((INComponentTranSplit)e.Row).BaseQty != 0m && ((INComponentTranSplit)e.Row).LocationID == null)
				{
					ThrowFieldIsEmpty<INComponentTranSplit.locationID>(sender, e.Row);
				}
			}
		}

		public virtual void INTranSplit_SubItemID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXCache cache = sender.Graph.Caches[typeof(INComponentTran)];
			if (cache.Current != null && (e.Row == null || ((INComponentTran)cache.Current).LineNbr == ((INComponentTranSplit)e.Row).LineNbr))
			{
				e.NewValue = ((INComponentTran)cache.Current).SubItemID;
				e.Cancel = true;
			}
		}

		public virtual void INTranSplit_LocationID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXCache cache = sender.Graph.Caches[typeof(INComponentTran)];
			if (cache.Current != null && (e.Row == null || ((INComponentTran)cache.Current).LineNbr == ((INComponentTranSplit)e.Row).LineNbr))
			{
				e.NewValue = ((INComponentTran)cache.Current).LocationID;
				e.Cancel = true;
			}
		}

		public virtual void INTranSplit_InvtMult_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXCache cache = sender.Graph.Caches[typeof(INComponentTran)];
			if (cache.Current != null && (e.Row == null || ((INComponentTran)cache.Current).LineNbr == ((INComponentTranSplit)e.Row).LineNbr))
			{
				using (InvtMultScope<INComponentTran> ms = new InvtMultScope<INComponentTran>((INComponentTran)cache.Current))
				{
					e.NewValue = ((INComponentTran)cache.Current).InvtMult;
					e.Cancel = true;
				}
			}
		}

		public virtual void INTranSplit_LotSerialNbr_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, ((INComponentTranSplit)e.Row).InventoryID);

			if (item != null)
			{
				object InvtMult = ((INComponentTranSplit)e.Row).InvtMult;
				if (InvtMult == null)
				{
					sender.RaiseFieldDefaulting<INComponentTranSplit.invtMult>(e.Row, out InvtMult);
				}

				object TranType = ((INComponentTranSplit)e.Row).TranType;
				if (TranType == null)
				{
					sender.RaiseFieldDefaulting<INComponentTranSplit.tranType>(e.Row, out TranType);
				}

				INLotSerTrack.Mode mode = INLotSerialNbrAttribute.TranTrackMode(item, (string)TranType, (short?)InvtMult);
				if (mode == INLotSerTrack.Mode.None || (mode & INLotSerTrack.Mode.Create) > 0)
				{
					ILotSerNumVal lotSerNum = ReadLotSerNumVal(sender, item);
					foreach (INComponentTranSplit lssplit in INLotSerialNbrAttribute.CreateNumbers<INComponentTranSplit>(sender, item, lotSerNum, mode, 1m))
					{
						e.NewValue = lssplit.LotSerialNbr;
						e.Cancel = true;
					}
				}
				//otherwise default via attribute
			}
		}

		public virtual void INTranSplit_Qty_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, ((INComponentTranSplit)e.Row).InventoryID);

			if (item != null && INLotSerialNbrAttribute.IsTrackSerial(item, ((INComponentTranSplit)e.Row).TranType, ((INComponentTranSplit)e.Row).InvtMult))
			{
				if (e.NewValue != null && e.NewValue is decimal && (decimal)e.NewValue != 0m && (decimal)e.NewValue != 1m)
				{
					e.NewValue = 1m;
				}
			}
		}

		public virtual void INTran_Qty_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if ((decimal?)e.NewValue < 0m)
			{
				throw new PXSetPropertyException(CS.Messages.Entry_GE, PXErrorLevel.Error, (int)0);
			}
		}

		protected override void RaiseQtyExceptionHandling(PXCache sender, object row, object newValue, PXExceptionInfo ei)
		{
			if (row is INComponentTran)
			{
				sender.RaiseExceptionHandling<INComponentTran.qty>(row, null, new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning, sender.GetStateExt<INComponentTran.inventoryID>(row), sender.GetStateExt<INComponentTran.subItemID>(row), sender.GetStateExt<INComponentTran.siteID>(row), sender.GetStateExt<INComponentTran.locationID>(row), sender.GetValue<INTran.lotSerialNbr>(row)));
			}
			else
			{
				sender.RaiseExceptionHandling<INComponentTranSplit.qty>(row, null, new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning, sender.GetStateExt<INComponentTranSplit.inventoryID>(row), sender.GetStateExt<INComponentTranSplit.subItemID>(row), sender.GetStateExt<INComponentTranSplit.siteID>(row), sender.GetStateExt<INComponentTranSplit.locationID>(row), sender.GetValue<INComponentTranSplit.lotSerialNbr>(row)));
			}
		}


		#endregion
	}

	public class LSINComponentMasterTran : LSSelect<INKitRegister, INKitTranSplit,
		Where<INKitTranSplit.docType, Equal<Current<INKitRegister.docType>>,
		And<INKitTranSplit.refNbr, Equal<Current<INKitRegister.refNbr>>>>>
	{
		#region Ctor
		public LSINComponentMasterTran(PXGraph graph)
			: base(graph)
		{
			graph.FieldDefaulting.AddHandler<INKitTranSplit.subItemID>(INTranSplit_SubItemID_FieldDefaulting);
			graph.FieldDefaulting.AddHandler<INKitTranSplit.locationID>(INTranSplit_LocationID_FieldDefaulting);
			graph.FieldDefaulting.AddHandler<INKitTranSplit.invtMult>(INTranSplit_InvtMult_FieldDefaulting);
			graph.FieldDefaulting.AddHandler<INKitTranSplit.lotSerialNbr>(INTranSplit_LotSerialNbr_FieldDefaulting);
			graph.FieldVerifying.AddHandler<INKitTranSplit.qty>(INTranSplit_Qty_FieldVerifying);
			graph.FieldVerifying.AddHandler<INKitRegister.qty>(INTran_Qty_FieldVerifying);
			graph.RowUpdated.AddHandler<INKitRegister>(INKitRegister_RowUpdated);
		}
		#endregion

		#region Implementation
		public override void Availability_FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			if (!PXLongOperation.Exists(sender.Graph.UID))
			{
				IQtyAllocated availability = AvailabilityFetch(sender, (INKitRegister)e.Row, ((INKitRegister)e.Row)?.Released == true ? AvailabilityFetchMode.None : AvailabilityFetchMode.ExcludeCurrent);

				if (availability != null)
				{
					PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, ((INKitRegister)e.Row).KitInventoryID);

					availability.QtyOnHand = INUnitAttribute.ConvertFromBase<INKitRegister.kitInventoryID, INKitRegister.uOM>(sender, e.Row, (decimal)availability.QtyOnHand, INPrecision.QUANTITY);
					availability.QtyAvail = INUnitAttribute.ConvertFromBase<INKitRegister.kitInventoryID, INKitRegister.uOM>(sender, e.Row, (decimal)availability.QtyAvail, INPrecision.QUANTITY);
					availability.QtyNotAvail = INUnitAttribute.ConvertFromBase<INKitRegister.kitInventoryID, INKitRegister.uOM>(sender, e.Row, (decimal)availability.QtyNotAvail, INPrecision.QUANTITY);
					availability.QtyHardAvail = INUnitAttribute.ConvertFromBase<INKitRegister.kitInventoryID, INKitRegister.uOM>(sender, e.Row, (decimal)availability.QtyHardAvail, INPrecision.QUANTITY);
					availability.QtyActual = INUnitAttribute.ConvertFromBase<INKitRegister.inventoryID, INKitRegister.uOM>(sender, e.Row, (decimal)availability.QtyActual, INPrecision.QUANTITY);

					e.ReturnValue = PXMessages.LocalizeFormatNoPrefix(Messages.Availability_ActualInfo,
						sender.GetValue<INTran.uOM>(e.Row),
						FormatQty(availability.QtyOnHand),
						FormatQty(availability.QtyAvail),
						FormatQty(availability.QtyHardAvail),
						FormatQty(availability.QtyActual));

					AvailabilityCheck(sender, (INKitRegister)e.Row, availability);
				}
				else
				{
					e.ReturnValue = string.Empty;
				}
			}
			else
			{
				e.ReturnValue = string.Empty;
			}

			base.Availability_FieldSelecting(sender, e);
		}

		public override INKitTranSplit Convert(INKitRegister item)
		{
			using (InvtMultScope<INKitRegister> ms = new InvtMultScope<INKitRegister>(item))
			{
				INKitTranSplit ret = item;
				//baseqty will be overriden in all cases but AvailabilityFetch
				ret.BaseQty = item.BaseQty - item.UnassignedQty;
				return ret;
			}
		}

		public void ThrowFieldIsEmpty<Field>(PXCache sender, object data)
			where Field : IBqlField
		{
			if (sender.RaiseExceptionHandling<Field>(data, null, new PXSetPropertyException(ErrorMessages.FieldIsEmpty, $"[{typeof(Field).Name}]")))
			{
				throw new PXRowPersistingException(typeof(Field).Name, null, ErrorMessages.FieldIsEmpty, typeof(Field).Name);
			}
		}

		public virtual void INTranSplit_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if (e.Row != null && ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert || (e.Operation & PXDBOperation.Command) == PXDBOperation.Update))
			{
				if (((INKitTranSplit)e.Row).BaseQty != 0m && ((INKitTranSplit)e.Row).LocationID == null)
				{
					ThrowFieldIsEmpty<INKitTranSplit.locationID>(sender, e.Row);
				}
			}
		}
		protected override void Master_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert || (e.Operation & PXDBOperation.Command) == PXDBOperation.Update)
			{
				INKitRegister row = e.Row as INKitRegister;
				if (row != null && row.Hold == false && Math.Abs((decimal)row.BaseQty) >= 0.0000005m && (row.UnassignedQty >= 0.0000005m || row.UnassignedQty <= -0.0000005m))
				{
					if (sender.RaiseExceptionHandling<INKitRegister.qty>(e.Row, row.Qty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned)))
					{
						throw new PXRowPersistingException(typeof(INKitRegister.qty).Name, row.Qty, Messages.BinLotSerialNotAssigned);
					}
				}
			}
			base.Master_RowPersisting(sender, e);
		}
		protected virtual void INKitRegister_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			if (!sender.ObjectsEqual<INKitRegister.hold>(e.Row, e.OldRow) && (bool?)sender.GetValue<INKitRegister.hold>(e.Row) == false)
			{
				if (((INKitRegister)e.Row).UnassignedQty != 0)
				{
					sender.RaiseExceptionHandling<INKitRegister.qty>(e.Row, ((INKitRegister)e.Row).Qty, new PXSetPropertyException(Messages.BinLotSerialNotAssigned));
				}
			}
		}

		public virtual void INTranSplit_SubItemID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXCache cache = sender.Graph.Caches[typeof(INKitRegister)];
			if (cache.Current != null && (e.Row == null || ((INKitRegister)cache.Current).LineNbr == ((INKitTranSplit)e.Row).LineNbr))
			{
				e.NewValue = ((INKitRegister)cache.Current).SubItemID;
				e.Cancel = true;
			}
		}

		public virtual void INTranSplit_LocationID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXCache cache = sender.Graph.Caches[typeof(INKitRegister)];
			if (cache.Current != null && (e.Row == null || ((INKitRegister)cache.Current).LineNbr == ((INKitTranSplit)e.Row).LineNbr))
			{
				e.NewValue = ((INKitRegister)cache.Current).LocationID;
				e.Cancel = true;
			}
		}

		public virtual void INTranSplit_InvtMult_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXCache cache = sender.Graph.Caches[typeof(INKitRegister)];
			if (cache.Current != null && (e.Row == null || ((INKitRegister)cache.Current).LineNbr == ((INKitTranSplit)e.Row).LineNbr))
			{
				using (InvtMultScope<INKitRegister> ms = new InvtMultScope<INKitRegister>((INKitRegister)cache.Current))
				{
					e.NewValue = ((INKitRegister)cache.Current).InvtMult;
					e.Cancel = true;
				}
			}
		}

		public virtual void INTranSplit_LotSerialNbr_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, ((INKitTranSplit)e.Row).InventoryID);

			if (item != null)
			{
				object InvtMult = ((INKitTranSplit)e.Row).InvtMult;
				if (InvtMult == null)
				{
					sender.RaiseFieldDefaulting<INKitTranSplit.invtMult>(e.Row, out InvtMult);
				}

				object TranType = ((INKitTranSplit)e.Row).TranType;
				if (TranType == null)
				{
					sender.RaiseFieldDefaulting<INKitTranSplit.tranType>(e.Row, out TranType);
				}

				INLotSerTrack.Mode mode = INLotSerialNbrAttribute.TranTrackMode(item, (string)TranType, (short?)InvtMult);
				if (mode == INLotSerTrack.Mode.None || (mode & INLotSerTrack.Mode.Create) > 0)
				{
					ILotSerNumVal lotSerNum = ReadLotSerNumVal(sender, item);
					foreach (INKitTranSplit lssplit in INLotSerialNbrAttribute.CreateNumbers<INKitTranSplit>(sender, item, lotSerNum, mode, 1m))
					{
						e.NewValue = lssplit.LotSerialNbr;
						e.Cancel = true;
					}
				}
				//otherwise default via attribute
			}
		}

		public virtual void INTranSplit_Qty_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, ((INKitTranSplit)e.Row).InventoryID);

			if (item != null && INLotSerialNbrAttribute.IsTrackSerial(item, ((INKitTranSplit)e.Row).TranType, ((INKitTranSplit)e.Row).InvtMult))
			{
				if (e.NewValue != null && e.NewValue is decimal && (decimal)e.NewValue != 0m && (decimal)e.NewValue != 1m)
				{
					e.NewValue = 1m;
				}
			}
		}

		public virtual void INTran_Qty_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if ((decimal?)e.NewValue < 0m)
			{
				throw new PXSetPropertyException(CS.Messages.Entry_GE, PXErrorLevel.Error, (int)0);
			}
		}

		protected override void RaiseQtyExceptionHandling(PXCache sender, object row, object newValue, PXExceptionInfo ei)
		{
			if (row is INKitRegister)
			{
				sender.RaiseExceptionHandling<INKitRegister.qty>(row, null, new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning, sender.GetStateExt<INKitRegister.kitInventoryID>(row), sender.GetStateExt<INKitRegister.subItemID>(row), sender.GetStateExt<INKitRegister.siteID>(row), sender.GetStateExt<INKitRegister.locationID>(row), sender.GetValue<INKitRegister.lotSerialNbr>(row)));
			}
			else
			{
				sender.RaiseExceptionHandling<INKitTranSplit.qty>(row, null, new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning, sender.GetStateExt<INKitTranSplit.inventoryID>(row), sender.GetStateExt<INKitTranSplit.subItemID>(row), sender.GetStateExt<INKitTranSplit.siteID>(row), sender.GetStateExt<INKitTranSplit.locationID>(row), sender.GetValue<INKitTranSplit.lotSerialNbr>(row)));
			}
		}

		protected override object[] SelectDetail(PXCache sender, INKitRegister row)
		{
			PXSelectBase<INKitTranSplit> select = new PXSelect<INKitTranSplit,
			Where<INKitTranSplit.docType, Equal<Required<INKitRegister.docType>>,
			And<INKitTranSplit.refNbr, Equal<Required<INKitRegister.refNbr>>,
			And<INKitTranSplit.lineNbr, Equal<Required<INKitRegister.kitLineNbr>>>>>>(_Graph);

			PXResultset<INKitTranSplit> res = select.Select(row.DocType, row.RefNbr, row.KitLineNbr);

			List<object> list = new List<object>(res.Count);

			foreach (INKitTranSplit detail in res)
			{
				list.Add(detail);
			}

			return list.ToArray();
		}

		protected override object[] SelectDetail(PXCache sender, INKitTranSplit row)
		{
			INKitRegister kitRow = (INKitRegister)PXParentAttribute.SelectParent(sender, row, typeof(INKitRegister));

			return SelectDetail(sender, kitRow);
		}

		#endregion
	}
	#endregion
}