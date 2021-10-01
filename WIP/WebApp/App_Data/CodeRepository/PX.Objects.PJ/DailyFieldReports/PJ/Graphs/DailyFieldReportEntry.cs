using System.Collections.Generic;
using PX.Objects.PJ.Common.Descriptor;
using PX.Objects.PJ.DailyFieldReports.Descriptor;
using PX.Objects.PJ.DailyFieldReports.PJ.DAC;
using PX.Objects.PJ.DailyFieldReports.PJ.Services;
using PX.Objects.PJ.DailyFieldReports.PM.CacheExtensions;
using PX.Objects.PJ.DailyFieldReports.PM.Services;
using PX.Objects.PJ.ProjectManagement.PJ.DAC;
using PX.Api.Models;
using PX.Common;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.WorkflowAPI;
using PX.Objects.CN.Common.Extensions;
using PX.Objects.CN.Common.Services.DataProviders;
using PX.Objects.PM;
using PX.Objects.EP;
using System.Collections;
using PX.Objects.CS;

namespace PX.Objects.PJ.DailyFieldReports.PJ.Graphs
{
    public class DailyFieldReportEntry : PXGraph<DailyFieldReportEntry, DailyFieldReport>
    {
        #region DAC Overrides

        #region EPApproval Cache Attached - Approvals Fields
        [PXDBDate()]
        [PXDefault(typeof(DailyFieldReport.date), PersistingCheck = PXPersistingCheck.Nothing)]
        protected virtual void _(Events.CacheAttached<EPApproval.docDate> e)
        {
        }
        #endregion

        #endregion

        public PXSetup<ProjectManagementSetup> ProjectManagementSetup;

        [PXViewName("Daily Field Report")]
        [PXCopyPasteHiddenFields(typeof(DailyFieldReport.icon))]
        public SelectFrom<DailyFieldReport>.View DailyFieldReport;
        
        public PXSetup<ProjectManagementSetup> PJSetup;

        [PXCopyPasteHiddenView]
        [PXViewName(PX.Objects.EP.Messages.Approval)]
        public EPApprovalAutomation<DailyFieldReport, DailyFieldReport.approved, DailyFieldReport.rejected, DailyFieldReport.hold, PJSetupDailyFieldReportApproval> Approval;

        [InjectDependency]
        public IProjectDataProvider ProjectDataProvider
        {
            get;
            set;
        }

        public DailyFieldReportEntry()
        {
            var pjsetup = PJSetup.Current;
        }

        #region Actions

        public PXAction<DailyFieldReport> Print;
        [PXButton(CommitChanges = true)]
        [PXUIField(DisplayName = "Print/Email", MapEnableRights = PXCacheRights.Select,
            MapViewRights = PXCacheRights.Select)]
        public virtual void print()
        {
            Persist();
            var parameters = new Dictionary<string, string>
            {
                [DailyFieldReportConstants.Print.DailyFieldReportId] = DailyFieldReport.Current.DailyFieldReportCd
            };
            throw new PXReportRequiredException(parameters, ScreenIds.DailyFieldReportForm, null);
        }

        public PXAction<DailyFieldReport> ViewAddressOnMap;
        [PXUIField(DisplayName = PX.Objects.CR.Messages.ViewOnMap)]
        [PXButton]
        public virtual void viewAddressOnMap()
        {
            var dailyFieldReport = DailyFieldReport.Current;
            new MapService(this).viewAddressOnMap(dailyFieldReport, dailyFieldReport.SiteAddress);
        }

        public PXAction<DailyFieldReport> complete;
        [PXButton(CommitChanges = true), PXUIField(DisplayName = "Complete")]
        protected virtual IEnumerable Complete(PXAdapter adapter) => adapter.Get();

        public PXAction<DailyFieldReport> hold;
        [PXButton(CommitChanges = true), PXUIField(DisplayName = "Hold")]
        protected virtual IEnumerable Hold(PXAdapter adapter) => adapter.Get();

        #endregion

        public override void CopyPasteGetScript(bool isImportSimple, List<Command> script, List<Container> containers)
        {
            var dailyFieldReportCopyConfigurationService = new DailyFieldReportCopyConfigurationService(this);
            dailyFieldReportCopyConfigurationService.ConfigureCopyPasteFields(script, containers);
        }

        public virtual void _(Events.RowPersisting<DailyFieldReport> args)
        {
            var dailyFieldReport = args.Row;
            var project = ProjectDataProvider.GetProject(this, dailyFieldReport.ProjectId);
            if (dailyFieldReport.Date < project?.StartDate)
            {
                args.Cache.RaiseException<DailyFieldReport.date>(dailyFieldReport,
                    DailyFieldReportMessages.DfrDateMustNotBeEarlierThenProjectStartDate);
            }
        }

        public virtual void _(Events.RowInserting<DailyFieldReport> args)
        {
            PXContext.SetScreenID(ScreenIds.DailyFieldReport);
        }

        public virtual void _(Events.FieldUpdated<DailyFieldReport, DailyFieldReport.projectId> args)
        {
            var dailyFieldReport = args.Row;
            if (dailyFieldReport.ProjectId != null)
            {
                var project = ProjectDataProvider.GetProject(this, dailyFieldReport.ProjectId);
                var projectExt = PXCache<PMProject>.GetExtension<PMProjectExt>(project);
                PMSiteAddress address = PXSelect<PMSiteAddress, Where<PMSiteAddress.addressID, Equal<Required<PMProjectExt.siteAddressID>>>>.Select(this, projectExt.SiteAddressID);
                dailyFieldReport.SiteAddress = address.AddressLine1;
                dailyFieldReport.City = address.City;
                dailyFieldReport.CountryID = address.CountryID;
                dailyFieldReport.State = address.State;
                dailyFieldReport.PostalCode = address.PostalCode;
                dailyFieldReport.Latitude = address.Latitude;
                dailyFieldReport.Longitude = address.Longitude;
            }
        }

        public virtual void _(Events.FieldUpdated<DailyFieldReport.countryId> args)
        {
            DailyFieldReport.Current.State = null;
        }

        protected virtual void _(Events.FieldDefaulting<EPApproval, EPApproval.descr> e)
        {
            if (DailyFieldReport.Current != null)
            {
                PMProject project = PMProject.PK.Find(this, DailyFieldReport.Current.ProjectId);
                if (project != null)
                {
                    e.NewValue = string.Format("Daily Field Report for {0} - {1}", project.ContractCD.Trim(), project.Description);
                }
            }
        }
    }
}