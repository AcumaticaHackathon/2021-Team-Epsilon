using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.AP;
using PX.Objects.CN.Common.Descriptor;
using PX.Objects.CN.ProjectAccounting.AP.CacheExtensions;
using PX.Objects.CN.ProjectAccounting.Descriptor;
using PX.Objects.CN.ProjectAccounting.PM.CacheExtensions;
using PX.Objects.CN.ProjectAccounting.PM.Descriptor;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.PM;
using PX.Objects.PO;
using PX.Objects.SO;
using PX.Objects.IN;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Objects.CN.Common.Extensions;
using PoMessages = PX.Objects.PO.Messages;
using ScMessages = PX.Objects.CN.Subcontracts.SC.Descriptor.Messages;
using PX.Objects.Common.Extensions;
using System.Resources;
using AutoMapper.Mappers;
using System.Diagnostics;

namespace PX.Objects.CN.ProjectAccounting.PO.GraphExtensions
{
    public class PoOrderEntryExt : PXGraphExtension<POOrderEntry>
    {
        #region DAC Attributes Override

        [PXMergeAttributes(Method = MergeMethod.Append)]
        [PXRestrictor(typeof(Where<PMTask.type, NotEqual<ProjectTaskType.revenue>>),
            ProjectAccountingMessages.TaskTypeIsNotAvailable, typeof(PMTask.type))]
        [PXFormula(typeof(Validate<POLine.projectID, POLine.costCodeID, POLine.inventoryID, POLine.siteID>))]
        [PXRemoveBaseAttribute(typeof(PXDefaultAttribute))]
        [PXDefault(typeof(Search<PMTask.taskID,
            Where<PMTask.projectID, Equal<Current<POLine.projectID>>,
                And<PMTask.isDefault, Equal<True>,
                And<PMTask.type, NotEqual<ProjectTaskType.revenue>>>>>),
            PersistingCheck = PXPersistingCheck.Nothing)]
        protected virtual void _(Events.CacheAttached<POLine.taskID> e)
        {
        }

        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDefault(typeof(Search<PMCostCode.costCodeID,
            Where<PMCostCode.costCodeID, Equal<Current<VendorExt.vendorDefaultCostCodeId>>>>),
            PersistingCheck = PXPersistingCheck.Nothing)]
        protected virtual void _(Events.CacheAttached<POLine.costCodeID> e)
        {
        } 

        #endregion

        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.construction>();
        }

        public override void Initialize()
        {
            Base.OnBeforePersist += OnBeforeGraphPersist;
        }

        #region Views/Selects

        [PXCopyPasteHiddenView]
        public PXFilter<CostBudgetFilter> ProjectItemFilter;

        [PXCopyPasteHiddenView]
        public PXSelect<PMCostBudget> AvailableProjectItems;
        public virtual IEnumerable availableProjectItems()
        {
            HashSet<BudgetKeyTuple> existing = new HashSet<BudgetKeyTuple>();
            foreach (POLine line in Base.Transactions.Select())
            {
                existing.Add(GetBudgetKeyFromLine(line));
            }

            var select = new PXSelectJoin<PMCostBudget, 
                InnerJoin<PMTask, On<PMCostBudget.projectTaskID, Equal<PMTask.taskID>, And<PMTask.isActive, Equal<True>, And<PMTask.visibleInPO, Equal<True>>>>>,
                Where<PMCostBudget.projectID, Equal<Current<CostBudgetFilter.projectID>>,
                And<PMCostBudget.type, Equal<AccountType.expense>,
                And<Current<CostBudgetFilter.projectID>, IsNotNull,
                And<Current<CostBudgetFilter.projectID>, NotEqual<Required<PMCostBudget.projectID>>>>>>>(Base);

            foreach (PXResult<PMCostBudget, PMTask> res in select.Select(ProjectDefaultAttribute.NonProject()))
            {
                PMCostBudget budget = (PMCostBudget) res;
                if (existing.Contains(GetBudgetKeyFromCostBudget(budget)))
                    budget.Selected = true;

                yield return budget;
            }
        } 

        #endregion

        #region Actions

        public PXAction<POOrder> addProjectItem;
        [PXUIField(DisplayName = "Add Project Item")]
        [PXButton(VisibleOnDataSource = false, VisibleOnProcessingResults = false)]
        public IEnumerable AddProjectItem(PXAdapter adapter)
        {
            if (AvailableProjectItems.View.Answer != WebDialogResult.OK)
            {
                ClearSelectionForProjectItems();
            }

            if (AvailableProjectItems.View.AskExt() == WebDialogResult.OK)
            {
                AddSelectedProjectItems();               
            }

            return adapter.Get();
        }

        public PXAction<POOrder> appendSelectedProjectItems;
        [PXUIField(DisplayName = "Add Lines")]
        [PXButton(VisibleOnDataSource = false, VisibleOnProcessingResults = false)]
        public IEnumerable AppendSelectedProjectItems(PXAdapter adapter)
        {
            AddSelectedProjectItems();

            return adapter.Get();
        }

        #endregion

        #region Event Handlers

        protected virtual void _(Events.RowSelected<POOrder> e)
        {
            if (e.Row != null)
            {
            addProjectItem.SetVisible(ProjectAttribute.IsPMVisible(BatchModule.PO));
            addProjectItem.SetEnabled(e.Row.VendorLocationID != null && e.Row.Hold == true);
            PXUIFieldAttribute.SetVisible<CostBudgetFilter.projectID>(ProjectItemFilter.Cache, null, Base.apsetup.Current.RequireSingleProjectPerDocument != true);
        }
        }

        protected virtual void _(Events.FieldUpdated<POOrder, POOrder.projectID> e)
        {
            // Acuminator disable once PX1074 SetupNotEnteredExceptionInEventHandlers [False positive.]
            if (Base.apsetup.Current.RequireSingleProjectPerDocument == true)
                ProjectItemFilter.Cache.SetDefaultExt<CostBudgetFilter.projectID>(ProjectItemFilter.Current);
        }

        [Obsolete]
        protected virtual void _(Events.RowSelected<POLine> e)
        {
            if (e.Row != null)
            {
                var error = PXUIFieldAttribute.GetErrorOnly<POLine.inventoryID>(e.Cache, e.Row);
                if (error == null)
                {
                    ValidateInventoryItemAndSetWarning(e.Row);
                }
            }
        }

        protected virtual void _(Events.FieldDefaulting<POLine, POLine.inventoryID> e)
        {
            if (Base.Document.Current?.VendorID != null)
            {
                e.NewValue = GetVendorDefaultInventoryId();
            }
        }

        protected virtual void _(Events.FieldDefaulting<CostBudgetFilter, CostBudgetFilter.projectID> e)
        {
            // Acuminator disable once PX1074 SetupNotEnteredExceptionInEventHandlers [False positive.]
            if (Base.apsetup.Current.RequireSingleProjectPerDocument == true && ProjectDefaultAttribute.IsProject(Base, Base.Document.Current?.ProjectID))
            {
                e.NewValue = Base.Document.Current.ProjectID;
            }
        }

        protected virtual void _(Events.FieldUpdated<CostBudgetFilter, CostBudgetFilter.projectID> e)
        {
            ClearSelectionForProjectItems();
        }

        protected virtual void _(Events.FieldVerifying<PMCostBudget, PMCostBudget.selected> e)
        {
            if (Base.Document.Current != null && Base.Document.Current.OrderType == POOrderType.RegularSubcontract)
                RaiseErrorIfReceiptIsRequired(e.Row.InventoryID);
        }

        #endregion

        protected virtual void AddSelectedProjectItems()
        {
            HashSet<BudgetKeyTuple> existingLines = GetExistingLines();
            AddNewLinesSkippingExisting(existingLines);
            foreach (PMCostBudget item in AvailableProjectItems.Cache.Updated)
            {
                AvailableProjectItems.Cache.SetStatus(item, PXEntryStatus.Notchanged);
            }
        }

        protected virtual void AddLine(PMCostBudget item)
        {
            POLine line = new POLine() { InventoryID = NormalizeInventoryID(item.InventoryID), ProjectID = item.ProjectID, TaskID = item.ProjectTaskID, CostCodeID = item.CostCodeID };
            line = Base.Transactions.Insert(line);
        }

        private int? NormalizeInventoryID(int? inventoryID)
        {
            if (inventoryID == null)
            {
                return null;
            }
            else if (inventoryID == PMInventorySelectorAttribute.EmptyInventoryID)
            {
                return null;
            }
            else
            {
                return inventoryID;
            }
        }

        private void ClearSelectionForProjectItems()
        {
            foreach (PMCostBudget item in AvailableProjectItems.Cache.Updated)
            {
                item.Selected = false;
            }
        }

        private HashSet<BudgetKeyTuple> GetExistingLines()
        {
            HashSet<BudgetKeyTuple> existing = new HashSet<BudgetKeyTuple>();

            foreach (POLine line in Base.Transactions.Select())
            {
                if (line.TaskID != null)
                {
                    existing.Add(GetBudgetKeyFromLine(line));
                }
            }

            return existing;
        }

        private BudgetKeyTuple GetBudgetKeyFromLine(POLine line)
        {
            BudgetKeyTuple key = new BudgetKeyTuple(line.ProjectID.GetValueOrDefault(), line.TaskID.GetValueOrDefault(), 0, line.InventoryID.GetValueOrDefault(PMInventorySelectorAttribute.EmptyInventoryID), line.CostCodeID.GetValueOrDefault());
            return key;
        }

        private BudgetKeyTuple GetBudgetKeyFromCostBudget(PMCostBudget item)
        {
            BudgetKeyTuple key = new BudgetKeyTuple(item.ProjectID.GetValueOrDefault(), item.TaskID.GetValueOrDefault(), 0, item.InventoryID.GetValueOrDefault(), item.CostCodeID.GetValueOrDefault());
            return key;
        }

        private void AddNewLinesSkippingExisting(HashSet<BudgetKeyTuple> existing)
        {
            foreach (PMCostBudget item in AvailableProjectItems.Cache.Updated)
            {
                if (item.Selected != true) continue;
                
                if (!existing.Contains(GetBudgetKeyFromCostBudget(item)))
                {
                    AddLine(item);
                }
            }
        }
        
        private int? GetVendorDefaultInventoryId()
        {
            var vendor = GetVendor();
            return vendor.GetExtension<VendorExt>().VendorDefaultInventoryId;
        }

        private Vendor GetVendor()
        {
            return new PXSelect<Vendor,
                    Where<Vendor.bAccountID, Equal<Required<Vendor.bAccountID>>>>(Base)
                .Select(Base.Document.Current.VendorID);
        }

        private void ValidateInventoryItemAndSetWarning(POLine line)
        {
            if (!IsInventoryItemValid(line))
            {
                var inventoryItem = InventoryItem.PK.Find(Base, line.InventoryID);
                Base.Transactions.Cache.RaiseException<POLine.inventoryID>(
                    line,
                    ScMessages.ItemIsNotPresentedInTheProjectBudget,
                    inventoryItem.InventoryCD, PXErrorLevel.RowWarning);
            }
            else
            {
                Base.Transactions.Cache.ClearFieldErrors<POLine.inventoryID>(line);
            }
        }

        private bool IsInventoryItemValid(POLine line)
        {
            if (line.InventoryID == null || !ShouldValidateInventoryItem() ||
                line.ProjectID == null || ProjectDefaultAttribute.IsNonProject(line.ProjectID)) return true;

            var project = PMProject.PK.Find(Base, line.ProjectID);
            if (IsNonProjectAccountGroupsAllowed(project) || !IsItemLevel(project)) return true;
            var result = IsInventoryItemUsedInProject(line, line.InventoryID);
            return result;
        }

        private bool IsItemLevel(PMProject project)
        {
            var result = project.CostBudgetLevel == BudgetLevels.Item || project.CostBudgetLevel == BudgetLevels.Detail;
            return result;
        }

        private bool IsInventoryItemUsedInProject(POLine line, int? inventoryID)
        {
            if (line.ProjectID == null || ProjectDefaultAttribute.IsNonProject(line.ProjectID)) return false;

            var project = PMProject.PK.Find(Base, line.ProjectID);
            if (!IsItemLevel(project)) return false;

            var query = new PXSelect<PMCostBudget, Where<PMCostBudget.projectID, Equal<Required<PMCostBudget.projectID>>>>(Base);
            var parameters = new List<object>();
            parameters.Add(line.ProjectID);

            if (line.TaskID.HasValue)
            {
                query.WhereAnd<Where<PMCostBudget.projectTaskID, Equal<Required<PMCostBudget.projectTaskID>>>>();
                parameters.Add(line.TaskID);
            }
            
            if (line.CostCodeID.HasValue)
            {
                query.WhereAnd<Where<PMCostBudget.costCodeID, Equal<Required<PMCostBudget.costCodeID>>>>();
                parameters.Add(line.CostCodeID);
            }

            Debug.Print("IsInventoryItemUsedInProject {0},{1},{2}", line.ProjectID, line.TaskID, line.CostCodeID);
            var result = query.Select(parameters.ToArray()).AsEnumerable()
                .Any(b => ((PMCostBudget)b).InventoryID == inventoryID);
            return result;
        }
               

        private bool ShouldValidateInventoryItem()
        {
            var originalSubcontract = (POOrder)Base.Document.Cache.GetOriginal(Base.Document.Current);
            if (originalSubcontract == null)
            {
                return true;
            }
            return (originalSubcontract.Status != POOrderStatus.PendingApproval || Base.Document.Current.Status != POOrderStatus.Hold) &&
                (originalSubcontract.Status == POOrderStatus.Hold || originalSubcontract.Status == POOrderStatus.PendingApproval);
        }

        private bool IsNonProjectAccountGroupsAllowed(PMProject project)
        {
            var projectExt = PXCache<PX.Objects.CT.Contract>.GetExtension<CacheExtensions.ContractExt>(project);
            return projectExt.AllowNonProjectAccountGroups == true;
        }

        private void RaiseErrorIfReceiptIsRequired(int? inventoryID)
        {
            InventoryItem item = InventoryItem.PK.Find(Base, inventoryID);
            if (item != null)
            {
                if (item.StkItem == true || item.NonStockReceipt == true)
                {
                    var ex = new PXSetPropertyException(PX.Objects.CN.Subcontracts.SC.Descriptor.Messages.ItemRequiringReceiptIsNotSupported, PXErrorLevel.RowError);
                    
                    throw ex;
                }
            }
        }

        public virtual void OnBeforeGraphPersist(PXGraph obj)
        {
           if (Base.Document.Current != null)
            {
                ValidateBudgetExistance(Base.Document.Current);
            }
        }

        private void ValidateBudgetExistance(POOrder order)
        {
            if (order != null)
            {
                if (order.Hold != true && (bool?)Base.Document.Cache.GetValueOriginal<POOrder.hold>(order) == true)
                {
                    bool raiseError = false;
                    foreach (POLine line in Base.Transactions.Select())
                    {
                        if (!IsInventoryItemValid(line))
                        {
                            raiseError = true;
                            Base.Transactions.Cache.RaiseException<POLine.inventoryID>(line, ScMessages.ItemIsNotPresentedInTheProjectBudget, null, PXErrorLevel.RowError);
                        }
                    }

                    if (raiseError)
                    {
                        throw new PXException(PX.Objects.PM.Messages.OneOrMoreLinesFailedAddOnTheFlyValidation);
                    }
                }
            }
        }

        #region Local Types
        [PXHidden]
        [Serializable]
        public class CostBudgetFilter : IBqlTable
        {
            #region ProjectID
            public abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }

            [PXRestrictor(typeof(Where<PMProject.isActive, Equal<True>>), PX.Objects.PM.Messages.InactiveContract, typeof(PMProject.contractCD))]
            [PXRestrictor(typeof(Where<PMProject.visibleInPO, Equal<True>>), PX.Objects.PM.Messages.ProjectInvisibleInModule, typeof(PMProject.contractCD))]
            [ProjectBase]
            public virtual Int32? ProjectID
            {
                get;
                set;
            }
            #endregion
        } 
        #endregion
    }
}
