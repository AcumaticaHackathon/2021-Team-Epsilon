using System;
using PX.Common;
using PX.Objects.Common;
using PX.Data;
using PX.Objects.CS;
using System.Collections;
using System.Collections.Generic;

namespace PX.Objects.AM
{
    public class MPSMaint : PXGraph<MPSMaint>, PXImportAttribute.IPXPrepareItems
    {
        public PXSave<AMMPS> Save;
        public PXCancel<AMMPS> Cancel;
        public PXCopyPasteAction<AMMPS> CopyPaste;

        [PXFilterable]
        [PXImport(typeof(AMMPS))]
        public PXSelect<AMMPS> AMMPSRecords;
        public PXSetup<AMRPSetup> setup;

        public MPSMaint()
        {
            PXUIFieldAttribute.SetDisplayName<AMBomItemActive.descr>(this.Caches<AMBomItemActive>(), "BOM Description");
        }

        protected virtual void _(Events.FieldVerifying<AMMPS, AMMPS.planDate> e)
        {
            if(e.Row == null)
            {
                return;
            }

            var mpsDate = Common.Current.BusinessDate(this).AddDays(setup.Current.MPSFence.GetValueOrDefault());
 
            if ((DateTime)e.NewValue < mpsDate)
            {
                e.Cache.RaiseExceptionHandling<AMMPS.planDate>(e.Row, e.Row.PlanDate, new PXSetPropertyException(AM.Messages.GetLocal(AM.Messages.MpsMaintPlanDateWarning), PXErrorLevel.Warning));
            }
        }

        protected virtual void _(Events.RowPersisting<AMMPS> e)
        {
            if (e.Row != null && e.Row.Qty.GetValueOrDefault() <= 0)
            {
                //prevents the records from saving with a quantity default of zero
                object qty = e.Row.Qty.GetValueOrDefault();
                e.Cache.RaiseFieldVerifying<AMMPS.qty>(e.Row, ref qty);
            }
        }

        protected virtual void _(Events.FieldVerifying<AMMPS, AMMPS.qty> e)
        {
            if(e.Row == null)
            {
                return;
            }

            if ((decimal)e.NewValue <= 0)
            {
                e.Cache.RaiseExceptionHandling<AMMPS.qty>(e.Row, e.Row.Qty, new PXSetPropertyException(AM.Messages.GetLocal(AM.Messages.QuantityGreaterThanZero)));
            }
        }

        protected virtual void _(Events.FieldDefaulting<AMMPS, AMMPS.bOMID> e)
        {
            if (IsImport || IsContractBasedAPI)
            {
                return;
            }

            e.NewValue = GetBomID(e.Row);
        }

        protected virtual string GetBomID(AMMPS mps)
        {
            if (mps?.InventoryID == null || mps.SiteID == null)
            {
                return null;
            }

            var id = new PrimaryBomIDManager(this)
            {
                BOMIDType = PrimaryBomIDManager.BomIDType.Planning
            };

            var planBomId = id.GetPrimaryAllLevels(mps.InventoryID, mps.SiteID, mps.SubItemID);
            if (!string.IsNullOrWhiteSpace(planBomId))
            {
                return planBomId;
            }

            id.BOMIDType = PrimaryBomIDManager.BomIDType.Default;
            return id.GetPrimaryAllLevels(mps.InventoryID, mps.SiteID, mps.SubItemID);
        }

        protected virtual void _(Events.RowSelected<AMMPS> e)
        {
            if (e.Row == null)
            {
                return;
            }

            // Get the Numbering for the Row
            Numbering numbering = PXSelectJoin<Numbering, InnerJoin<AMMPSType, On<AMMPSType.mpsNumberingID, Equal<Numbering.numberingID>>>,
                Where<AMMPSType.mPSTypeID, Equal<Required<AMMPSType.mPSTypeID>>>>.Select(this, e.Row.MPSTypeID);

            var userNumbering = numbering?.UserNumbering == true;

            PXUIFieldAttribute.SetVisible<AMMPS.mPSID>(e.Cache, e.Row, userNumbering);
            PXUIFieldAttribute.SetEnabled<AMMPS.mPSID>(e.Cache, e.Row, userNumbering);
        }

        protected virtual void _(Events.RowInserting<AMMPS> e)
        {
            // Temp key to allow multiple inserts before persisting. When persisting and auto number it will swap the value for us.
            var insertedCounter = e.Cache.Inserted.Count() + 1;
            e.Row.MPSID = $"-{insertedCounter}";
        }

        public PXAction<AMWC> ViewBOM;
        [PXLookupButton]
        [PXUIField(DisplayName = "View BOM")]
        protected virtual void viewBOM()
        {
            if (AMMPSRecords.Current == null)
            {
                return;
            }

            var graphBOM = PXGraph.CreateInstance<BOMMaint>();

            AMBomItem bomItem = PXSelect<AMBomItem,
                Where<AMBomItem.bOMID, Equal<Required<AMBomItem.bOMID>>>,
                OrderBy<Desc<AMBomItem.revisionID>>>.SelectWindowed(this, 0, 1, AMMPSRecords.Current.BOMID);

            if (bomItem != null)
            {
                graphBOM.Documents.Current = bomItem;
            }

            if (graphBOM.Documents.Current != null)
            {
                throw new PXRedirectRequiredException(graphBOM, true, string.Empty);
            }
        }

        #region Implementation of IPXPrepareItems

        public MultiDuplicatesSearchEngine<AMMPS> DuplicateFinder { get; set; }

        private bool CanUpdateExistingRecords
        {
            get
            {
                return IsImportFromExcel && PXExecutionContext.Current.Bag.TryGetValue(PXImportAttribute._DONT_UPDATE_EXIST_RECORDS, out var dontUpdateExistRecords) &&
                    false.Equals(dontUpdateExistRecords);
            }
        }

        protected virtual Type[] GetImportAlternativeKeyFields()
        {
            var keys = new List<Type>()
            {
                typeof(AMMPS.inventoryID),
                typeof(AMMPS.siteID),
                typeof(AMMPS.mPSTypeID),
                typeof(AMMPS.planDate)
            };

            if (PXAccess.FeatureInstalled<FeaturesSet.subItem>())
            {
                keys.Add(typeof(AMMPS.subItemID));
            }

            return keys.ToArray();
        }

        public bool PrepareImportRow(string viewName, IDictionary keys, IDictionary values)
        {
            if (string.Compare(viewName, nameof(AMMPSRecords), true) != 0 || !CanUpdateExistingRecords || keys == null)
            {
                return true;
            }

            if (DuplicateFinder == null)
            {
                DuplicateFinder = new MultiDuplicatesSearchEngine<AMMPS>(AMMPSRecords.Cache, GetImportAlternativeKeyFields(), AMMPSRecords.SelectMain());
            }

            var duplicate = DuplicateFinder.Find(values);
            var containsForecastId = keys.Contains(nameof(AMMPS.mPSID));
            if (duplicate != null)
            {
                DuplicateFinder.RemoveItem(duplicate);

                if (containsForecastId)
                {
                    keys[nameof(AMMPS.mPSID)] = duplicate.MPSID;
                }
                else
                {
                    keys.Add(nameof(AMMPS.mPSID), duplicate.MPSID);
                }
            }
            else if (containsForecastId)
            {
                var value = keys[nameof(AMMPS.mPSID)] as string;
                var lineExists = !string.IsNullOrWhiteSpace(value) && AMMPSRecords.Cache.Locate(new AMMPS { MPSID = value }) != null;

                if (lineExists)
                {
                    keys.Remove(nameof(AMMPS.mPSID));
                }
            }

            return true;
        }

        public bool RowImporting(string viewName, object row)
        {
            return row == null;
        }

        public bool RowImported(string viewName, object row, object oldRow)
        {
            return oldRow == null;
        }

        public void PrepareItems(string viewName, IEnumerable items)
        {
        }

        #endregion
    }
}