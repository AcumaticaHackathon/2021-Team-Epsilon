using System.Collections;
using PX.Objects.PJ.DailyFieldReports.Common.GenericGraphExtensions;
using PX.Objects.PJ.DailyFieldReports.Common.MappedCacheExtensions;
using PX.Objects.PJ.DailyFieldReports.Common.Mappings;
using PX.Objects.PJ.DailyFieldReports.Descriptor;
using PX.Objects.PJ.DailyFieldReports.PJ.DAC;
using PX.Objects.PJ.DailyFieldReports.PJ.Graphs;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CN.Common.Services.DataProviders;
using PX.Objects.CR;

namespace PX.Objects.PJ.DailyFieldReports.PJ.GraphExtensions
{
    public class DailyFieldReportEntryVisitorExtension : DailyFieldReportEntryExtension<DailyFieldReportEntry>
    {
        [PXCopyPasteHiddenView]
        public SelectFrom<DailyFieldReportVisitor>
            .Where<DailyFieldReportVisitor.dailyFieldReportId
                .IsEqual<DailyFieldReport.dailyFieldReportId.FromCurrent>>.View Visitors;

        [InjectDependency]
        public IBusinessAccountDataProvider BusinessAccountDataProvider
        {
            get;
            set;
        }

        protected override (string Entity, string View) Name =>
            (DailyFieldReportEntityNames.Visitor, ViewNames.Visitors);

        public virtual void _(Events.FieldUpdated<DailyFieldReportVisitor.businessAccountId> args)
        {
            if (args.NewValue is int businessAccountId && args.Row is DailyFieldReportVisitor visitor)
            {
                var businessAccount = BusinessAccountDataProvider.GetBusinessAccountReceivable(Base, businessAccountId);
                if (businessAccount.Type == BAccountType.EmployeeType)
                {
                    businessAccount = BusinessAccountDataProvider
                        .GetBusinessAccountReceivable(Base, businessAccount.ParentBAccountID);
                }
                visitor.Company = businessAccount.AcctName;
            }
        }

        public virtual void _(Events.RowSelected<DailyFieldReportVisitor> args)
        {
            var visitor = args.Row;
            if (Base.IsMobile && visitor != null)
            {
                visitor.LastModifiedDateTime = visitor.LastModifiedDateTime.GetValueOrDefault().Date;
            }
        }

        protected override DailyFieldReportRelationMapping GetDailyFieldReportRelationMapping()
        {
            return new DailyFieldReportRelationMapping(typeof(DailyFieldReportVisitor))
            {
                RelationId = typeof(DailyFieldReportVisitor.dailyFieldReportVisitorId)
            };
        }

        protected override PXSelectExtension<DailyFieldReportRelation> CreateRelationsExtension()
        {
           return new PXSelectExtension<DailyFieldReportRelation>(Visitors);
        }
        
        public PXAction<BAccountR> viewBAccount;
        [PXUIField(DisplayName = "View Business Account", MapEnableRights = PXCacheRights.Select,
            MapViewRights = PXCacheRights.Select)]
        [PXButton(VisibleOnDataSource = false, VisibleOnProcessingResults = false, PopupVisible = false)]
        public virtual IEnumerable ViewBAccount(PXAdapter adapter)
        {
            DailyFieldReportVisitor visitor = (DailyFieldReportVisitor) Visitors.Current;
            
            if (visitor == null)
                return adapter.Get();
            
            var businessAccount = BusinessAccountDataProvider.GetBusinessAccountReceivable(Base, visitor.BusinessAccountId);
            
            if (businessAccount == null || businessAccount.BAccountID == null) 
                return adapter.Get();

            if (businessAccount.Type == BAccountType.CustomerType
                || businessAccount.Type == BAccountType.CombinedType)
            {
                CustomerMaint target = PXGraph.CreateInstance<CustomerMaint>();
                
                target.BAccount.Current = target.BAccount.Search<BAccountR.bAccountID>(visitor.BusinessAccountId);;
                if (target.BAccount.Current != null)
                {
                    throw new PXRedirectRequiredException(target, true, "redirect")
                        {Mode = PXBaseRedirectException.WindowMode.NewWindow};
                }
                else
                {
                    Customer customer = PXSelect<Customer, Where<Customer.bAccountID, Equal<Current<DailyFieldReportVisitor.businessAccountId>>>>.Select(Base);
                    throw new PXException(PXMessages.LocalizeFormat(ErrorMessages.ElementDoesntExistOrNoRights, customer.AcctCD));
                }
            }
            else if (businessAccount.Type == BAccountType.VendorType)
            {
                VendorMaint target = PXGraph.CreateInstance<VendorMaint>();
                
                target.BAccount.Current = target.BAccount.Search<BAccountR.bAccountID>(visitor.BusinessAccountId);;;
                if (target.BAccount.Current != null)
                {
                    throw new PXRedirectRequiredException(target, true, "redirect")
                        {Mode = PXBaseRedirectException.WindowMode.NewWindow};
                }
                else
                {
                    VendorR vendor = PXSelect<VendorR, Where<VendorR.bAccountID, Equal<Current<DailyFieldReportVisitor.businessAccountId>>>>.Select(Base);
                    throw new PXException(PXMessages.LocalizeFormat(ErrorMessages.ElementDoesntExistOrNoRights, vendor.AcctCD));
                }
            }
            return adapter.Get();
        }
    }
}