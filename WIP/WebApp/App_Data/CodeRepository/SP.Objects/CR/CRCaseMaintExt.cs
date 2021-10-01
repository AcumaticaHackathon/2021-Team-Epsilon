
using System;
using System.Collections;
using System.Collections.Generic;
using PX.Data;
using PX.Data.EP;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.CT;
using PX.Objects.EP;
using PX.Objects.SP.DAC;
using PX.SM;
using PX.Common;
using SP.Objects.SP;
using PX.Objects.CR.Workflows;

namespace SP.Objects.CR
{
	/// <exclude/>
	[DashboardType((int)DashboardTypeAttribute.Type.Default)]
	public class CRCaseMaintExt : PXGraphExtension<CRCaseMaint>
	{
		#region NewComment

		public class NewComment : IBqlTable
		{
			#region Subject
			public abstract class subject : IBqlField { }

			[PXString(255, IsUnicode = true)]
			[PXUIField(DisplayName = "Subject")]
			public virtual string Subject { get; set; }
			#endregion

			#region Body
			public abstract class body : IBqlField { }

			[PXString(1000, IsUnicode = true)]
			public virtual string Body { get; set; }
			#endregion
		}

		#endregion

		#region Selects

        [PXViewName("Case")]
		public PXSelectJoin<CRCase,
			LeftJoin<CRCaseClass, On<CRCase.caseClassID, Equal<CRCaseClass.caseClassID>>>,
			Where2<Where<CRCaseClass.isInternal, Equal<False>, Or<CRCaseClass.isInternal, IsNull>>,
				And<MatchWithBAccount<CRCase.customerID, Current<AccessInfo.userID>>>>>
			Case;

		[PXHidden]
		public PXSelect<CRCase,
			Where<CRCase.caseCD, Equal<Current<CRCase.caseCD>>>>
			CaseCurrent;

		[PXHidden]
		public PXSelect<CRCaseClass,
			Where<CRCaseClass.caseClassID, Equal<Required<CRCaseClass.caseClassID>>>>
			CaseClassCurrent;

        [PXHidden]
		public PXFilter<NewComment> Comment;

        [PXViewName(PX.Objects.CR.Messages.Activities)]
        [PXFilterable]
        public PXSelect<CRActivity
            ,Where<True,Equal<True>>,OrderBy<Desc<CRActivity.startDate>>
            > Activities;

		[PXViewName("Answers")]
        public CRAttributeList<CRCase> Answers;

        [PXHidden]
        public PXSelectJoin<Users,
            LeftJoin<EPEmployee,
                On<EPEmployee.userID, Equal<Users.pKID>>>,
            Where<EPEmployee.defContactID, Equal<Current<CRCase.ownerID>>>>
            Supporter;

        [PXHidden]
        public PXSelect<BAccount,
            Where<BAccount.bAccountID, Equal<Current<CRCase.customerID>>>>
            BAccountName;

        #endregion

        #region Ctor
		public override void Initialize()
		{
            if (PortalSetup.Current == null)
                throw new PXSetupNotEnteredException<PortalSetup>(ErrorMessages.SetupNotEntered);

            PXUIFieldAttribute.SetDisplayName<Users.fullName>(Base.Caches[typeof(Users)], "Assigned To");
            PXUIFieldAttribute.SetDisplayName<BAccount.acctName>(Base.Caches[typeof(BAccount)], "Business Account");
            PXUIFieldAttribute.SetEnabled<BAccount.acctName>(Base.Caches[typeof(BAccount)], null, false);

			BqlCommand previewcommand = Base.Views["Activities"].BqlSelect;
			PXView Preview = new PXView(Base, false, previewcommand);
            Base.Views.Add("Activities$Preview", Preview);

            var att = new CRPreviewAttribute(typeof(CRCase), typeof(CRActivity));
            att.Attach(Base, "Activities", null);

			Base.Actions["Action"].SetMenu(new ButtonMenu[0]);
			Base.Actions["Action"].SetVisible(false);

			if (Case.Current != null)
			{
				CRCaseClass row1 = CaseClassCurrent.SelectWindowed(0, 1, Case.Current.CaseClassID);
				if (row1 != null)
				{
                    bool IsNeedContract = PXAccess.FeatureInstalled<FeaturesSet.contractManagement>() && (bool)row1.RequireContract;
                    PXUIFieldAttribute.SetRequired<CRCase.contractID>(Case.Cache, IsNeedContract);
					PXDefaultAttribute.SetPersistingCheck<CRCase.contractID>(Case.Cache, Case.Current,
                                                                             IsNeedContract
																				 ? PXPersistingCheck.NullOrBlank
																				 : PXPersistingCheck.Nothing);
				}
				//PXDefaultAttribute.SetPersistingCheck<CRCase.contractID>(Case.Cache, Case.Current, Case.Current.CaseClassID == portalSetup.Current.DefaultCaseClassID ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			}
		}
        #endregion

        #region Select Delegate

        public virtual IEnumerable preview()
		{
			yield return Activities.Current; 
		}

        public virtual IEnumerable activities()
        {
            foreach (CRActivity ret in Base.Activities.Select())
            {
                if (ret.IsPrivate != true)
                    yield return ret;
            }
        }
        
		#endregion

        #region Cache Attach
        [PXDBInt]
        [PXUIField(DisplayName = "Contract")]
        [PXSelector(typeof(Search2<Contract.contractID,
                LeftJoin<ContractBillingSchedule, On<Contract.contractID, Equal<ContractBillingSchedule.contractID>>>,
            Where<Contract.isTemplate, NotEqual<True>,
                And<Contract.baseType, Equal<Contract.ContractBaseType>>>,
            OrderBy<Desc<Contract.contractCD>>>),
            new Type[] 
                    { 
                        typeof(Contract.contractCD),
			            typeof(Contract.description),
			            typeof(Contract.status),
			            typeof(Contract.expireDate),
                    },
            DescriptionField = typeof(Contract.description),
            SubstituteKey = typeof(Contract.contractCD), Filterable = true)]
        [PXRestrictor(typeof(Where<Contract.status, Equal<Contract.status.active>, Or<Contract.status, Equal<Contract.status.inUpgrade>>>), PX.Objects.CR.Messages.ContractIsNotActive)]
        [PXRestrictor(typeof(Where<Current<AccessInfo.businessDate>, LessEqual<Contract.graceDate>, Or<Contract.expireDate, IsNull>>), Messages.ContractExpired)]
        [PXRestrictor(typeof(Where<Current<AccessInfo.businessDate>, GreaterEqual<Contract.startDate>>), PX.Objects.CR.Messages.ContractActivationDateInFuture, typeof(Contract.startDate))]
        [PXRestrictor(typeof(Where2<
                            MatchWithBAccountNotNull<Contract.customerID>,
                        Or<
							MatchWithBAccountNotNull<ContractBillingSchedule.accountID>>>), "", typeof(AccessInfo.userID))]
        [PXFormula(typeof(Default<CRCase.customerID>))]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual void CRCase_ContractID_CacheAttached(PXCache sender) { }
        #endregion

		#region Actions

		public PXAction<CRCase> AddComment;
        [PXUIField(DisplayName = "Add Comment")]
		[PXButton(OnClosingPopup = PXSpecialButtonType.Refresh)]
		public virtual IEnumerable addComment(PXAdapter adapter)
		{
			var row = Case.Current;
            if (row == null) return adapter.Get();
			var graph = PXGraph.CreateInstance<SPComment>();

			var activitiesCache = Base.Caches[typeof(CRActivity)];
			CRActivity comment = (CRActivity)activitiesCache.CreateInstance();
			comment.ClassID = CRActivityClass.Activity;

			comment = graph.Comment.Insert(comment);
			graph.Case.Current = row;
			PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.Popup);

            return adapter.Get();
		} 

		public PXAction<CRCase> CloseCase;
		[PXUIField(DisplayName = "Close Case")]
		[PXButton]
		public virtual void closeCase()
		{
			var @case = Case.Current;
			if (@case == null) return;

			if (@case.IsActive == false)
				return;

			if(Comment.Ask(PXMessages.Localize(Messages.CloseCase), PXMessages.Localize(Messages.AreYouSureToCloseThisCase), MessageButtons.YesNo) == WebDialogResult.Yes)
			{
				Base.GetExtension<CaseWorkflow>().closeCaseFromPortal.Press();

				SPCaseClosedInquiry graph = PXGraph.CreateInstance<SPCaseClosedInquiry>();
				PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.Same);
			}
		}

		public PXAction<CRCase> ReopenCase;
		[PXUIField(DisplayName = "Reopen")]
		[PXButton]
		public virtual void reopenCase()
		{
			var @case = Case.Current;
			if (@case == null) return;

			if (@case.Released == true)
			{
				Comment.Ask(PXMessages.Localize(Messages.ReopenCase), PXMessages.Localize(Messages.UnfortunatelyItsNotPosibleToReopenThisCase), MessageButtons.OK, MessageIcon.Error);
			}
			else if (@case.IsActive != true)
			{
				if(Comment.Ask(PXMessages.Localize(Messages.ReopenCase), PXMessages.Localize(Messages.AreYouSureToReopenThisCase), MessageButtons.YesNo) == WebDialogResult.Yes)
				{
					Base.GetExtension<CaseWorkflow>().openCaseFromPortal.Press();
					
					SPCaseOpenInquiry graph = PXGraph.CreateInstance<SPCaseOpenInquiry>();
					PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.Same);
				}
			}
		}

		public PXAction<CRCase> NewCase;
		[PXUIField(DisplayName = "New Support Case")]
        [PXButton]
        public virtual void newCase()
        {
	        SPCaseNewEntry newCaseGraph = PXGraph.CreateInstance<SPCaseNewEntry>();
			var values = new Dictionary<string, object>();
			newCaseGraph.ExecuteInsert(nameof(Case), values, null);
			throw new PXRedirectRequiredException(newCaseGraph, Messages.NewSupportCase, PXRedirectHelper.WindowMode.Same);			
        }
		#endregion

        #region Data Handlers

        /*protected IEnumerable activities()
        {
			List<CRActivity> results = new List<CRActivity>();
			foreach (CRActivity activity in 
                    PXSelect<CRActivity, Where<CRActivity.refNoteID, Equal<Current<CRCase.noteID>>,
                    And2<Where<CRActivity.isPrivate, IsNull, Or<CRActivity.isPrivate, Equal<False>>>,
                        And<Where<CRActivity.isSystem, IsNull, Or<CRActivity.isSystem, Equal<False>>>>>>>.
                        Select(this.Base))
            {
                results.Add(activity);
            }
            return results;
        }*/

        #endregion

		#region Event Handlers
		protected virtual void CRCase_RowSelected(PXCache sender, PXRowSelectedEventArgs e, PXRowSelected sel)
		{
            if (sel != null)
                sel(sender, e);
            
            var row = e.Row as CRCase;
			if (row == null) return;

			Base.addNewContact.SetVisible(false);

			if (row.CaseCD == " <NEW>")
			{
				PXUIFieldAttribute.SetEnabled<CRCase.subject>(sender, null, false);
				PXUIFieldAttribute.SetEnabled<CRCase.caseClassID>(sender, null, false);
				PXUIFieldAttribute.SetEnabled<CRCase.priority>(sender, null, false);
				PXUIFieldAttribute.SetEnabled<CRCase.contractID>(sender, null, false);
			}
			else
			{
				var isJustInserted = sender.GetStatus(row) == PXEntryStatus.Inserted;
				var isActive = row.IsActive == true;
				var isReleased = row.Released == true;

				PXUIFieldAttribute.SetEnabled(sender, row, isActive && !isReleased);
				PXUIFieldAttribute.SetEnabled<CRCase.caseCD>(sender, row, true);
				PXUIFieldAttribute.SetEnabled<CRCase.caseClassID>(sender, row, false);
				AddComment.SetEnabled(!isJustInserted && !isReleased);
				CloseCase.SetEnabled(!isJustInserted && isActive && !isReleased);
				ReopenCase.SetEnabled(!isJustInserted && !isActive && !isReleased);
				//PXDefaultAttribute.SetPersistingCheck<CRCase.contractID>(Case.Cache, row, row.CaseClassID == portalSetup.Current.DefaultCaseClassID ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			}

			CRCaseClass caseClass = CaseClassCurrent.SelectWindowed(0, 1, row.CaseClassID);
			if (caseClass != null)
			{
                bool IsNeedContract = PXAccess.FeatureInstalled<FeaturesSet.contractManagement>() && (bool)caseClass.RequireContract;
                PXUIFieldAttribute.SetRequired<CRCase.contractID>(Case.Cache, IsNeedContract);
				PXDefaultAttribute.SetPersistingCheck<CRCase.contractID>(Case.Cache, row,
																		 IsNeedContract
																			 ? PXPersistingCheck.NullOrBlank
																			 : PXPersistingCheck.Nothing);
			}
			if (caseClass != null && caseClass.ReopenCaseTimeInDays != null && row.ResolutionDate != null)
			{
				if (caseClass.ReopenCaseTimeInDays != 0)
				{
					if (row.ResolutionDate.Value.AddDays((double)caseClass.ReopenCaseTimeInDays) < PXTimeZoneInfo.Now)
						Base.Actions["reopenCase"].SetVisible(false);
				}
			}

		    // allow edit only case own Company
            BAccount baccount = ReadBAccount.ReadCurrentAccount();
            bool AllowEdit = row.CustomerID == baccount.BAccountID;

            foreach (var cache in Base.Caches)
            {
                if (!cache.Key.IsAssignableFrom(Base.PrimaryItemType))
                    cache.Value.AllowInsert = AllowEdit;

                cache.Value.AllowDelete = AllowEdit;
                cache.Value.AllowUpdate = AllowEdit;
		    }

            foreach (PXAction action in Base.Actions.Values)
            {
                if (AllowEdit == false)
                {
                    action.SetEnabled(AllowEdit);
                }
            }
            if (Base.Actions.Contains("CancelClose"))
                Base.Actions["CancelClose"].SetEnabled(true);
            if (Base.Actions.Contains("CancelCloseToList"))
                Base.Actions["CancelCloseToList"].SetEnabled(true);
            if (Base.Actions.Contains("Cancel"))
                Base.Actions["Cancel"].SetEnabled(true);
            if (Base.Actions.Contains("Activities$RefreshPreview"))
                Base.Actions["Activities$RefreshPreview"].SetEnabled(true);
            if (Base.Actions.Contains("NewCase"))
                Base.Actions["NewCase"].SetEnabled(true);
        }

        protected virtual void CRCase_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e, PXRowUpdated upd)
        {
            if (upd != null)
                upd(sender, e);

            var row = e.Row as CRCase;
            if (row == null) return;

            // allow edit only case own Company
            BAccount baccount = ReadBAccount.ReadCurrentAccount();
            bool AllowEdit = row.CustomerID == baccount.BAccountID;

            foreach (var cache in Base.Caches)
            {
                if (!cache.Key.IsAssignableFrom(Base.PrimaryItemType))
                    cache.Value.AllowInsert = AllowEdit;

                cache.Value.AllowDelete = AllowEdit;
                cache.Value.AllowUpdate = AllowEdit;
            }

            Base.Caches[typeof(CRActivity)].AllowInsert = true;
            Base.Caches[typeof(CRActivity)].AllowDelete = true;
            Base.Caches[typeof(CRActivity)].AllowUpdate = true;

            foreach (PXAction action in Base.Actions.Values)
            {
                action.SetEnabled(AllowEdit);
            }
            Base.Actions["Cancel"].SetEnabled(true);
        }

        protected virtual void CRActivity_Body_FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
        {
            var row = e.Row as CRActivity;
            if (row == null) return;

            if (row.ClassID == CRActivityClass.Email)
            {
                var entity = (SMEmailBody)PXSelect<SMEmailBody, Where<SMEmailBody.refNoteID, Equal<Required<CRPMTimeActivity.noteID>>>>.Select(sender.Graph, row.NoteID);

                e.ReturnValue = entity.Body;
            }
        }
        #endregion
    }
}
