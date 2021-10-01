using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CN.Compliance.CL.DAC;
using PX.Objects.PM;
using System.Collections;
using PX.Objects.CN.Common.Extensions;

namespace PX.Objects.CN.Compliance
{
    public class ComplianceViewEntityExtension<Graph, PrimaryDac> : PXGraphExtension<Graph> 
        where Graph : PXGraph
        where PrimaryDac : class, IBqlTable, new()
    {
        public PXAction<PrimaryDac> complianceViewCustomer;
        [PXUIField(DisplayName = "View Customer", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(VisibleOnDataSource = false, VisibleOnProcessingResults = false, PopupVisible = false)]
        public virtual IEnumerable ComplianceViewCustomer(PXAdapter adapter)
        {
            CustomerMaint target = PXGraph.CreateInstance<CustomerMaint>();
            ComplianceDocument complianceDocument = (ComplianceDocument)Base.Caches<ComplianceDocument>().Current;
            
            if (complianceDocument == null)
                return adapter.Get();
            
            target.BAccount.Current = target.BAccount.Search<Customer.bAccountID>(complianceDocument.CustomerID);
            if (target.BAccount.Current != null)
            {
                throw new PXRedirectRequiredException(target, true, "redirect")
                    {Mode = PXBaseRedirectException.WindowMode.NewWindow};
            }
            else
            {
                Customer customer = PXSelect<Customer, Where<Customer.bAccountID, Equal<Current<ComplianceDocument.customerID>>>>.Select(Base);
                throw new PXException(PXMessages.LocalizeFormat(ErrorMessages.ElementDoesntExistOrNoRights, customer.AcctCD));
            }
        }

        public PXAction<PrimaryDac> complianceViewProject;
        [PXUIField(DisplayName = "View Project", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(VisibleOnDataSource = false, VisibleOnProcessingResults = false, PopupVisible = false)]
        public virtual IEnumerable ComplianceViewProject(PXAdapter adapter)
        {
            PMProject entity = PXSelect<PMProject, Where<PMProject.contractID, Equal<Current<ComplianceDocument.projectID>>>>.Select(Base);
            if (entity != null)
            {
                var target = PXGraph.CreateInstance<ProjectEntry>();
                target.Project.Current = entity;
                throw new PXRedirectRequiredException(target, true, "redirect") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
            }

            return adapter.Get();
        }

        public PXAction<PrimaryDac> complianceViewCostTask;
        [PXUIField(DisplayName = "View Cost Task", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(VisibleOnDataSource = false, VisibleOnProcessingResults = false, PopupVisible = false)]
        public virtual IEnumerable ComplianceViewCostTask(PXAdapter adapter)
        {
            PMTask entity = PXSelect<PMTask, Where<PMTask.taskID, Equal<Current<ComplianceDocument.costTaskID>>>>.Select(Base);
            if (entity != null)
            {
                var target = PXGraph.CreateInstance<ProjectTaskEntry>();
                target.Task.Current = entity;
                throw new PXRedirectRequiredException(target, true, "redirect") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
            }

            return adapter.Get();
        }

        public PXAction<PrimaryDac> complianceViewRevenueTask;
        [PXUIField(DisplayName = "View Revenue Task", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(VisibleOnDataSource = false, VisibleOnProcessingResults = false, PopupVisible = false)]
        public virtual IEnumerable ComplianceViewRevenueTask(PXAdapter adapter)
        {
            PMTask entity = PXSelect<PMTask, Where<PMTask.taskID, Equal<Current<ComplianceDocument.revenueTaskID>>>>.Select(Base);
            if (entity != null)
            {
                var target = PXGraph.CreateInstance<ProjectTaskEntry>();
                target.Task.Current = entity;
                throw new PXRedirectRequiredException(target, true, "redirect") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
            }

            return adapter.Get();
        }

        public PXAction<PrimaryDac> complianceViewCostCode;
        [PXUIField(DisplayName = "View Cost Code", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(VisibleOnDataSource = false, VisibleOnProcessingResults = false, PopupVisible = false)]
        public virtual IEnumerable ComplianceViewCostCode(PXAdapter adapter)
        {
            PMCostCode entity = PXSelect<PMCostCode, Where<PMCostCode.costCodeID, Equal<Current<ComplianceDocument.costCodeID>>>>.Select(Base);
            if (entity != null)
            {
                var target = PXGraph.CreateInstance<CostCodeMaint>();
                target.Items.Current = entity;
                throw new PXRedirectRequiredException(target, true, "redirect") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
            }

            return adapter.Get();
        }

        public PXAction<PrimaryDac> complianceViewVendor;

        [PXUIField(DisplayName = "View Vendor", MapEnableRights = PXCacheRights.Select,
            MapViewRights = PXCacheRights.Select)]
        [PXButton(VisibleOnDataSource = false, VisibleOnProcessingResults = false, PopupVisible = false)]
        public virtual IEnumerable ComplianceViewVendor(PXAdapter adapter)
        {
            return ViewComplianceVendor<ComplianceDocument.vendorID>(adapter);
        }

        public PXAction<PrimaryDac> complianceViewSecondaryVendor;
        [PXUIField(DisplayName = "View Vendor", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(VisibleOnDataSource = false, VisibleOnProcessingResults = false, PopupVisible = false)]
        public virtual IEnumerable ComplianceViewSecondaryVendor(PXAdapter adapter)
        {
            return ViewComplianceVendor<ComplianceDocument.secondaryVendorID>(adapter);
        }

        public PXAction<PrimaryDac> complianceViewJointVendor;
        [PXUIField(DisplayName = "View Vendor", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(VisibleOnDataSource = false, VisibleOnProcessingResults = false, PopupVisible = false)]
        public virtual IEnumerable ComplianceViewJointVendor(PXAdapter adapter)
        {
            return ViewComplianceVendor<ComplianceDocument.jointVendorInternalId>(adapter);
        }
        
        public virtual IEnumerable ViewComplianceVendor<TVendorID>(PXAdapter adapter)
            where TVendorID : IBqlField
        {
            VendorMaint target = PXGraph.CreateInstance<VendorMaint>();
            var fieldName = Base.Caches<ComplianceDocument>().GetField(typeof(TVendorID));
            ComplianceDocument complianceDocument = (ComplianceDocument) Base.Caches<ComplianceDocument>().Current;
            
            if (complianceDocument == null)
                return adapter.Get();

            target.BAccount.Current = target.BAccount.Search<VendorR.bAccountID>(complianceDocument.GetPropertyValue<int>(fieldName));
            if (target.BAccount.Current != null)
            {
                throw new PXRedirectRequiredException(target, true, "redirect")
                    {Mode = PXBaseRedirectException.WindowMode.NewWindow};
            }
            else
            {
                VendorR vendor = PXSelect<VendorR, Where<VendorR.bAccountID, Equal<Current<TVendorID>>>>.Select(Base);
                throw new PXException(PXMessages.LocalizeFormat(ErrorMessages.ElementDoesntExistOrNoRights, vendor.AcctCD));
            }
        }
    }
}
