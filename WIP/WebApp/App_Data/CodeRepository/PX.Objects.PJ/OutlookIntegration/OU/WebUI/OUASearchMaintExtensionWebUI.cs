using PX.Objects.PJ.ProjectsIssue.PJ.DAC;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.CT;
using PX.Objects.PM;
using PX.Data.UI;
using PX.Objects.CR.WebUI;

namespace PX.Objects.PJ.OutlookIntegration.OU.WebUI
{
	public sealed class OUASearchMaintExtensionWebUI : PXUIExtension<OUSearchMaintWebUI, OUSearchMaint>
	{
		public static bool IsActive()
		{
			//return true;
			return PXAccess.FeatureInstalled<FeaturesSet.constructionProjectManagement>();
		}

		[PXUIField(DisplayName = "New Request For Information")]
		[PXUIContainer(Id = "RequestForInformationOutlook", PlaceBefore = typeof(OUSearchMaintWebUI.Actions))]
		public class RequestForInformationOutlook : PXExposedView<DAC.RequestForInformationOutlook>
		{
			[PXUIControlField]
			public abstract class rFISummary : DAC.RequestForInformationOutlook.summary { }
			[PXUIControlField(AutoPostback = true)]
			public abstract class projectId : DAC.RequestForInformationOutlook.projectId { }
			[PXUIControlField]
			public abstract class contactId : DAC.RequestForInformationOutlook.contactId { }
			[PXUIControlField(AutoPostback = true)]
			public abstract class incoming : DAC.RequestForInformationOutlook.incoming { }
			[PXUIDropDownField(AutoPostback = true)]
			public abstract class rFIStatus : DAC.RequestForInformationOutlook.status { }
			[PXUIControlField(AutoPostback = true)]
			public abstract class rFIClassId : DAC.RequestForInformationOutlook.classId { }
			[PXUIControlField(AutoPostback = true)]
			public abstract class rFIPriorityId : DAC.RequestForInformationOutlook.priorityId { }
			[PXUIControlField]
			public abstract class rFIOwnerID : DAC.RequestForInformationOutlook.ownerID { }
			[PXUIDateField(AutoPostback = true)]
			public abstract class dueResponseDate : DAC.RequestForInformationOutlook.dueResponseDate { }
			[PXUIControlField(AutoPostback = true)]
			public abstract class isScheduleImpact : DAC.RequestForInformationOutlook.isScheduleImpact { }
			[PXUIControlField]
			public abstract class scheduleImpact : DAC.RequestForInformationOutlook.scheduleImpact { }
			[PXUIControlField(AutoPostback = true)]
			public abstract class isCostImpact : DAC.RequestForInformationOutlook.isCostImpact { }
			[PXUIControlField]
			public abstract class costImpact : DAC.RequestForInformationOutlook.costImpact { }
			[PXUIControlField]
			public abstract class designChange : DAC.RequestForInformationOutlook.designChange { }
		}

		[PXUIField(DisplayName = "New Project Issue")]
		[PXUIContainer(Id = "ProjectIssueOutlook", PlaceBefore = typeof(OUSearchMaintWebUI.Actions))]
		public class ProjectIssueOutlook : PXExposedView<DAC.ProjectIssueOutlook>
		{
			[PXUIControlField]
			public abstract class pISummary : DAC.ProjectIssueOutlook.summary { }
			[PXUIControlField(AutoPostback = true)]
			public abstract class pIProjectId : DAC.ProjectIssueOutlook.projectId { }
			[PXUIControlField(AutoPostback = true)]
			public abstract class pIClassId : DAC.ProjectIssueOutlook.classId { }
			[PXUIControlField(AutoPostback = true)]
			public abstract class pIPriority : DAC.ProjectIssueOutlook.priorityId { }
			[PXUIControlField]
			public abstract class pIOwner : DAC.ProjectIssueOutlook.ownerID { }
			[PXUIDateField(AutoPostback = true)]
			public abstract class pIDueDate : DAC.ProjectIssueOutlook.dueDate { }
		}


		[PXUIField(DisplayName = "Bottom Actions 2")]
		[PXUIActionContainer(PlaceAfter = typeof(OUSearchMaintWebUI.Actions))]		
		public class BottomActions2 : PXExposedView<OUSearchMaintWebUI.Filter>
		{

			[PXUIControlButton]
			public abstract class createRequestForInformation : PXExposedAction { }
			[PXUIControlButton]
			public abstract class createProjectIssue : PXExposedAction { }



			[PXUIControlButton]
			public abstract class redirectToCreateRequestForInformation : PXExposedAction { }
			[PXUIControlButton]
			public abstract class redirectToCreateProjectIssue : PXExposedAction { }
		}
		public class NewActivityExtension : PXCacheExtension<OUSearchMaintWebUI.NewActivity>
		{
			public static bool IsActive()
			{
				//return true;
				return PXAccess.FeatureInstalled<FeaturesSet.constructionProjectManagement>();
			}
			[PXUIControlField(PlaceAfter = typeof(OUSearchMaintWebUI.NewActivity.caseCD))]
			//[PXUIField(TabOrder = 25)]
			public abstract class activityContractID : CacheExtensions.OuActivityExtension.projectId { }
			[PXUIControlField(PlaceAfter = typeof(OUSearchMaintWebUI.NewActivity.caseCD))]
			//[PXUIField(TabOrder = 30)]
			public abstract class activityRequestForInformationId : CacheExtensions.OuActivityExtension.requestForInformationId { }
			[PXUIControlField(PlaceAfter = typeof(OUSearchMaintWebUI.NewActivity.caseCD))]
			//[PXUIField(TabOrder = 35)]
			public abstract class activityProjectIssueId : CacheExtensions.OuActivityExtension.projectIssueId { }
			[PXUIControlField(AutoPostback = true)]
			//[PXUIField(TabOrder = 55)]
			public abstract class isLinkProject : CacheExtensions.OuActivityExtension.isLinkProject { }
			[PXUIControlField(AutoPostback = true)]
			//[PXUIField(TabOrder = 60)]
			public abstract class isLinkRequestForInformation : CacheExtensions.OuActivityExtension.isLinkRequestForInformation { }
			[PXUIControlField(AutoPostback = true)]
			//[PXUIField(TabOrder = 65)]
			public abstract class isLinkProjectIssue : CacheExtensions.OuActivityExtension.isLinkProjectIssue { }
		}

		public override void Initialize()
		{
			base.Initialize();
			//AddFileFields();
		}
	}
}
