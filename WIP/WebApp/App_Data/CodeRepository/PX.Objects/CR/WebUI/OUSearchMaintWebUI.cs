using System;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Data.UI;
using PX.Common.Extensions;
using PX.Common;

namespace PX.Objects.CR.WebUI
{
	public class OUSearchMaintWebUI : PXUIExtension<OUSearchMaint>
	{
		[PXUIField(DisplayName = "Back Button")]
		[PXUIActionContainer]
		public class HeaderActions : PXExposedView<Filter>
		{
			[PXUIControlButton]
			public abstract class back : PXExposedAction { }
		}
		[PXUIField(DisplayName = "")]
		[PXUIContainer(Id = "Form")]
		public class Filter : PXExposedView<OUSearchEntity>
		{
			[PXUIDropDownField(AutoPostback = true)]
			public abstract class outgoingEmail : OUSearchEntity.outgoingEmail { }

			[PXUIField]
			[PXUILabel(Text = "Person")]
			public abstract class edpersonTitle : IBqlField { }
			[PXUIField(DisplayName = "Contact ID")]
			[PXUIControlField(SuppressLabel = true, AutoPostback = true)]
			public abstract class contactID : OUSearchEntity.contactID { }
			[PXUIControlField(Readonly = true)]
			public abstract class email : OUSearchEntity.eMail { }
			[PXUIControlField(SuppressLabel = true)]
			public abstract class ErrorMessage : OUSearchEntity.errorMessage { }
			[PXUIControlHiddenField(StoreValue = true)]
			public abstract class displayName : OUSearchEntity.displayName { }
			[PXUIControlField]
			public abstract class newContactFirstName : OUSearchEntity.newContactFirstName { }
			[PXUIControlField]
			public abstract class newContactLastName : OUSearchEntity.newContactLastName { }
			[PXUIControlField]
			public abstract class newContactEmail : OUSearchEntity.newContactEmail { }
			[PXUIControlField]
			public abstract class salutation : OUSearchEntity.salutation { }
			[PXUIControlField(AutoPostback = true)]
			public abstract class bAccountID : OUSearchEntity.bAccountID { }
			[PXUIControlField]
			public abstract class fullName : OUSearchEntity.fullName { }
			[PXUIDropDownField]
			public abstract class leadSource : OUSearchEntity.leadSource { }
			[PXUIDropDownField]
			public abstract class contactSource : OUSearchEntity.contactSource { }
			[PXUIControlField]
			public abstract class countryID : OUSearchEntity.countryID { }
			[PXUIControlFieldWithLabel(Label = typeof(entityName))]
			public abstract class entityID : OUSearchEntity.entityID { }
			[PXUIControlHiddenField]
			public abstract class entityName : OUSearchEntity.entityName { }

			[PXUIControlHiddenField]
			public abstract class attachmentNames : OUSearchEntity.attachmentNames { }
			[PXUIControlHiddenField]
			public abstract class attachmentsCount : OUSearchEntity.attachmentsCount { }
			[PXUIControlHiddenField]
			public abstract class isRecognitionInProgress : OUSearchEntity.isRecognitionInProgress { }

			

			[PXUIField]
			[PXUILongrunIndicator]
			public abstract class lr : IBqlField { }
			[PXUIField]
			[PXUILabel(Text = "AP Document recognition is in progress...")]
			public abstract class edRefreshText : IBqlField { }

			[PXUIField]
			[PXUILabel(Label = typeof(numOfRecognizedDocuments))]
			public abstract class recognizedDocuments : IBqlField { }

			[PXUIControlHiddenField]
			public abstract class numOfRecognizedDocuments : OUSearchEntity.numOfRecognizedDocuments { }
			[PXUIField]
			[PXUILabel(Text = "Select document")]
			public abstract class edPanelCaption : IBqlField { }

		}
		[PXUIField(DisplayName = "New Case")]
		[PXUIContainer(Id = "Case")]
		public class NewCase : PXExposedView<OUCase>
		{
			[PXUIControlField(AutoPostback = true)]
			public abstract class caseClassID : OUCase.caseClassID { }
			[PXUIControlField(AutoPostback =true)]
			public abstract class contractID : OUCase.contractID { }
			[PXUIDropDownField]
			public abstract class severity : OUCase.severity { }
			[PXUIControlField]
			public abstract class subject : OUCase.subject { }
		}
		[PXUIField(DisplayName = "New Opportunity")]
		[PXUIContainer(Id = "Opportunity")]
		public class NewOpportunity : PXExposedView<OUOpportunity>
		{
			[PXUIControlField]
			public abstract class classID : OUOpportunity.classID { }
			[PXUIControlField]
			public abstract class subject : OUOpportunity.subject { }
			[PXUIDropDownField]
			public abstract class stageID : OUOpportunity.stageID { }
			[PXUIDateField]
			public abstract class closeDate : OUOpportunity.closeDate { }
			[PXUIControlField(AutoPostback = true)]
			public abstract class manualAmount : OUOpportunity.manualAmount { }
			[PXUIControlField]
			public abstract class currencyID : OUOpportunity.currencyID { }
			[PXUIControlField]
			public abstract class branchID : OUOpportunity.branchID { }
		}
		[PXUIField(DisplayName = "Activity")]
		[PXUIContainer(Id = "Activity")]
		public class NewActivity : PXExposedView<OUActivity>
		{
			[PXUIControlField]
			//[PXUIField(TabOrder = 10)]
			public abstract class subject : OUActivity.subject { }
			[PXUIControlField]
			//[PXUIField(TabOrder = 15)]
			public abstract class caseCD : OUActivity.caseCD { }
			[PXUIControlField]
			//[PXUIField(TabOrder = 20)]
			public abstract class opportunityID : OUActivity.opportunityID { }
			[PXUIControlField(AutoPostback = true)]
			//[PXUIField(TabOrder = 40)]
			public abstract class isLinkContact : OUActivity.isLinkContact { }
			[PXUIControlField(AutoPostback = true)]
			//[PXUIField(TabOrder = 45)]
			public abstract class isLinkCase : OUActivity.isLinkCase { }
			[PXUIControlField(AutoPostback = true)]
			//[PXUIField(TabOrder = 50)]
			public abstract class isLinkOpportunity : OUActivity.isLinkOpportunity { }
		}
		[PXUIField(DisplayName = "Actions")]
		[PXUIActionContainer]
        public class Actions : PXExposedView<Filter>
        {
            [PXUIControlButton]
            public abstract class createAPDoc : PXExposedAction { }
			[PXUIControlButton]
            public abstract class viewAPDoc : PXExposedAction { }
			[PXUIControlButton]
            public abstract class createActivity : PXExposedAction { }
            [PXUIControlButton]
            public abstract class createOpportunity : PXExposedAction { }
            [PXUIControlButton]
            public abstract class createCase : PXExposedAction { }
            [PXUIControlButton]
            public abstract class createLead : PXExposedAction { }
            [PXUIControlButton]
            public abstract class createContact : PXExposedAction { }
            [PXUIControlButton]
            public abstract class goCreateLead : PXExposedAction { }
            [PXUIControlButton]
            public abstract class goCreateContact : PXExposedAction { }
            [PXUIControlButton]
            public abstract class viewContact : PXExposedAction { }
            [PXUIControlButton]
            public abstract class viewBAccount : PXExposedAction { }
            [PXUIControlButton]
            public abstract class viewEntity : PXExposedAction { }
            [PXUIControlButton]
            public abstract class goCreateActivity : PXExposedAction { }
            [PXUIControlButton]
            public abstract class goCreateCase : PXExposedAction { }
            [PXUIControlButton]
            public abstract class goCreateOpportunity : PXExposedAction { }

			// [PXUIControlButton]
			// public abstract class createRequestForInformation : PXExposedAction { }
			// [PXUIControlButton]
			// public abstract class createProjectIssue : PXExposedAction { }



			// [PXUIControlButton]
			// public abstract class redirectToCreateRequestForInformation : PXExposedAction { }
			// [PXUIControlButton]
			// public abstract class redirectToCreateProjectIssue : PXExposedAction { }


			[PXUIControlButton]
            public abstract class reply : PXExposedAction { }
        }
		[PXUIField(DisplayName = "Attachments")]
		[PXUIAttachmentsContainer(AttachmentsView = "APBillAttachments")]
		public class Attachments : PXExposedView<Filter>
		{

		}
		[PXUIField(DisplayName = "Bottom Actions")]
		[PXUIActionContainer]
		public class BottomActions : PXExposedView<Filter>
		{
			[PXUIControlButton]
			public abstract class createAPDocContinue : PXExposedAction { }
			[PXUIControlButton]
			public abstract class viewAPDocContinue : PXExposedAction { }
		}
		[PXUIField(DisplayName = "Outlook Data")]
		[PXUIOutlookData(Id = "Outlook-Data")]
		public class SourceMessage : PXExposedView<OUMessage, Filter>
		{
			[PXUIControlHiddenField(StoreValue = true)]
			public abstract class emailAddress : Filter.outgoingEmail { }
			[PXUIControlHiddenField(StoreValue = true)]
			public abstract class displayName : Filter.displayName { }
			[PXUIControlHiddenField(StoreValue = true)]
			public abstract class newFirstName : Filter.newContactFirstName { }
			[PXUIControlHiddenField(StoreValue = true)]
			public abstract class newLastName : Filter.newContactLastName { }
			[PXUIControlHiddenField(StoreValue = true)]
			public abstract class messageId : OUMessage.messageId { }
			[PXUIControlHiddenField(StoreValue = true)]
			public abstract class to : OUMessage.to { }
			[PXUIControlHiddenField(StoreValue = true)]
			public abstract class cC : OUMessage.cC { }
			[PXUIControlHiddenField(StoreValue = true)]
			public abstract class subject : OUMessage.subject { }
			[PXUIControlHiddenField(StoreValue = true)]
			public abstract class itemId : OUMessage.itemId { }
			[PXUIControlHiddenField(StoreValue = true)]
			public abstract class isIncome : OUMessage.isIncome { }
			[PXUIControlHiddenField(StoreValue = true)]
			public abstract class ewsUrl : OUMessage.ewsUrl { }
			[PXUIControlHiddenField(StoreValue = true)]
			public abstract class apiToken : OUMessage.token { }

			[PXUIControlHiddenField(StoreValue = true)]
			public abstract class attachmentNames : Filter.attachmentNames { }
			[PXUIControlHiddenField(StoreValue = true)]
			public abstract class attachmentsCount : Filter.attachmentsCount { }


		}
		[PXUIField(DisplayName = "LogoutActions")]
		[PXUIActionContainer]
		[PXHidden]
		public class LogoutActions : PXExposedView<Filter>
		{
			[PXUIControlButton]
			public abstract class logOut : PXExposedAction { }
		}

		public override void Initialize()
		{
			base.Initialize();
			AddFileFields();
		}
		private void AddFileFields()
		{
			var attachments = Base.APBillAttachments.Select()
				.AsEnumerable()
				.Select(a => (OUAPBillAttachment)a)
				.ToArray();
			bool filesVisible = false;

			switch (Base.Filter.Current.Operation)
			{
				case OUOperation.CreateAPDocument:
				case OUOperation.ViewAPDocument:
					filesVisible = true;
					//PXUIFieldAttribute.SetVisible<Filter.edPanelCaption>(Base.Caches<Filter>(), null, true);
					//break;
					//default:
					//PXUIFieldAttribute.SetVisible<Filter.edRefreshText>(Base.Caches<Filter>(), null, PXLongOperation.GetStatus(Base.UID) == PXLongRunStatus.InProcess);
					//PXUIFieldAttribute.SetVisible<Filter.edPanelCaption>(Base.Caches<Filter>(), null, false);
					break;
			}
			if (filesVisible)
			{
				Base.GoCreateContact.SetVisible(false);
				Base.GoCreateLead.SetVisible(false);
				Base.GoCreateActivity.SetVisible(false);
				Base.GoCreateCase.SetVisible(false);
				Base.GoCreateOpportunity.SetVisible(false);
			}
			for (var i = 0; i < attachments.Length; i++)
			{
				var itemIdCaptured = attachments[i].ItemId;
				var idCaptured = attachments[i].Id;
				var fieldName = string.Format("File{0}", i);
				if (!Base.Filter.Cache.Fields.Contains(fieldName))
				{
					Base.Filter.Cache.Fields.Add(fieldName);
					AddField<Attachments>(fieldName, nameof(Filter) + "." + fieldName);
					Base.FieldSelecting.AddHandler(Base.PrimaryView, fieldName, (s, e) =>
					{
                        bool fileVisible = false;

                        switch (Base.Filter.Current.Operation)
                        {
                            case OUOperation.CreateAPDocument:
                            case OUOperation.ViewAPDocument:
                                fileVisible = true;
                                break;
                        }
						Base.OUAPBillAttachmentSelectFileFieldSelecting(s, e, itemIdCaptured, idCaptured);
						if (e.ReturnState is PXFieldState state)
						{
							state.ControlConfig = new Dictionary<string, object> {{ "controlType", "qp-check-box" },
								{ "id", nameof(Attachments) + "." + fieldName },
								{ "fieldId", $"Exposed${typeof(Attachments).FullName}${nameof(Filter)}.{fieldName}" },
								{ "viewName", $"Exposed${typeof(Attachments).FullName}${nameof(Filter)}" },
								{"fieldName", fieldName },
								{ "config", new Dictionary<string, object> {
									{ "id", fieldName },
									{ "displayName", state.DisplayName },
									{ "label", state.DisplayName },
									{ "visible", fileVisible },
									{ "disabled", !state.Enabled },
									{ "enabled", state.Enabled },
									{"suppressLabel", true },
									{ "autopostback", true }

								}
							} };
						}
					});
					Base.FieldUpdating.AddHandler(Base.PrimaryView, fieldName, (s, e) =>
					{
						Base.OUAPBillAttachmentSelectFileFieldUpdating(s, e, itemIdCaptured, idCaptured);
					});
				}
			}
		}
		protected virtual void _(Events.RowSelected<Filter> e)
		{
			PXUIFieldAttribute.SetVisible<Filter.edRefreshText>(Base.Caches<Filter>(), e.Row, PXLongOperation.GetStatus(Base.UID) == PXLongRunStatus.InProcess);
			switch (Base.Filter.Current.Operation)
			{
				case OUOperation.CreateAPDocument:
				case OUOperation.ViewAPDocument:
					//filesVisible = true;
					PXUIFieldAttribute.SetVisible<Filter.edPanelCaption>(Base.Caches<Filter>(), e.Row, true);
					break;
				default:
					
					PXUIFieldAttribute.SetVisible<Filter.edPanelCaption>(Base.Caches<Filter>(), e.Row, false);
					break;
			}
		}
		
	}
}
