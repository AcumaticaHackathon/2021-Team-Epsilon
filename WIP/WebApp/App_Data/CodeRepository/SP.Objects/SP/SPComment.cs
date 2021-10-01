using System;
using System.Collections;
using PX.Common;
using PX.Data;
using PX.Data.EP;
using PX.Objects.CR;
using PX.Objects.EP;
using PX.Objects.SP.DAC;
using PX.SM;
using SP.Objects.CR;

namespace SP.Objects.SP
{
    [Serializable]
    public class SPComment : PXGraph<SPComment>
    {
        #region ctor
        public SPComment()
        {
            PXDefaultAttribute.SetPersistingCheck<CRActivityExt.commentSubject>(Comment.Cache, null, PXPersistingCheck.NullOrBlank);
            if (Comment.Current != null)
            {
                if (PXSiteMap.Provider.FindSiteMapNodeByScreenID(Comment.Current.LastModifiedByScreenID).GraphType.IndexOf("Opportunity") > -1)
                {
                    PXUIFieldAttribute.SetVisible<CRActivityExt.commenttype>(Comment.Cache, null, true);
                    PXDefaultAttribute.SetPersistingCheck<CRActivityExt.commenttype>(Comment.Cache, null, PXPersistingCheck.NullOrBlank);

                    PXUIFieldAttribute.SetVisible<CRActivityExt.commentStartDate>(Comment.Cache, null, true);
                    PXDefaultAttribute.SetPersistingCheck<CRActivityExt.commentStartDate>(Comment.Cache, null, PXPersistingCheck.NullOrBlank);
                }
            }
        }
        #endregion

        #region Select
        [PXHidden]
        public PXSelect<CROpportunity> Opportunity;

        [PXHidden]
        public PXSelect<UploadFile> UploadFiles;

        [PXHidden]
        public PXSelect<CRCase> Case;
		
		[PXHidden]
		public PXSelect<CRCase, Where<CRCase.caseCD, Equal<Current<CRCase.caseCD>>>> CaseCurrent;

        public PXSelect<CRActivity> Comment;

        [PXHidden]
        public PXSelect<CRActivityStatistics> Stats;

        [PXHidden]
        public PXSelect<Note> Note;
        #endregion

        #region Actions
        public PXAction<CRActivity> SaveClose;
        [PXUIField(DisplayName = "Save & Close")]
        [PXButton]
        public virtual IEnumerable saveClose(PXAdapter pxAdapter)
        {
            CRActivityExt row1;
            row1 = PXCache<CRActivity>.GetExtension<CRActivityExt>(Comment.Current);
            BAccount baccount = ReadBAccount.ReadCurrentAccount();
            // need review
            Note.Cache.Clear();

            // ToDo (переделать на LastModified)
            if (PXSiteMap.Provider.FindSiteMapNodeByScreenID(Comment.Current.CreatedByScreenID)?.GraphType?.IndexOf("Opportunity") > -1)
            {
                var CurrentOpportunity = Opportunity.Current;
                if (CurrentOpportunity == null) return pxAdapter.Get();
                var CurrentComment = (CRActivity)Comment.Cache.CreateCopy(Comment.Cache.Current);
                CurrentComment.UIStatus = ActivityStatusListAttribute.Open;//Open (Cust)
                CurrentComment.Subject = string.IsNullOrWhiteSpace(row1.CommentSubject) ? "<Empty Subject>" : row1.CommentSubject;
                CurrentComment.Body = Tools.ConvertSimpleTextToHtml(row1.CommentBody);
                CurrentComment.RefNoteID = PXNoteAttribute.GetNoteID<CROpportunity.noteID>(this.Caches[typeof(CROpportunity)], CurrentOpportunity);

                CurrentComment.Type = row1.CommentType;
                CurrentComment.StartDate = row1.CommentStartDate;
                CurrentComment.IsExternal = true;
                CurrentComment.ContactID = CurrentOpportunity.ContactID;
                CurrentComment.BAccountID = baccount.BAccountID;

                this.EnsureCachePersistence(CurrentComment.GetType());
                Comment.Cache.Update(CurrentComment);
                this.Actions.PressSave();
            }

            if (PXSiteMap.Provider.FindSiteMapNodeByScreenID(Comment.Current.CreatedByScreenID)?.GraphType?.IndexOf("Case") > -1)
            {                
                var CurrentCase = Case.Current;
                if (CurrentCase == null) return pxAdapter.Get();

                var CurrentComment = (CRActivity)Comment.Cache.CreateCopy(Comment.Cache.Current);
                CurrentComment.UIStatus = ActivityStatusListAttribute.Open; //Open (Cust)
                CurrentComment.Subject = string.IsNullOrWhiteSpace(row1.CommentSubject)
                    ? "<Empty Subject>"
                    : row1.CommentSubject;
                CurrentComment.Body = Tools.ConvertSimpleTextToHtml(row1.CommentBody);
                CurrentComment.OwnerID = CurrentCase.OwnerID;

                CurrentComment.RefNoteID = PXNoteAttribute.GetNoteID<CRCase.noteID>(this.Caches[typeof(CRCase)], CurrentCase);
                CurrentComment.StartDate = PXTimeZoneInfo.Now;
                CurrentComment.IsExternal = true;
                CurrentComment.Type = row1.CommentType;
                CurrentComment.ContactID = CurrentCase.ContactID;
                CurrentComment.BAccountID = baccount.BAccountID;

                this.EnsureCachePersistence(CurrentComment.GetType());
                Comment.Cache.Update(CurrentComment);

                Actions.PressSave();
                Note.Cache.Persist(PXDBOperation.Insert);

				var curCase = CaseCurrent.SelectSingle();
				CRCase caseCopy = (CRCase)Case.Cache.CreateCopy(curCase);
				if (caseCopy.Released != true)
				{
					var caseMaint = PXGraph.CreateInstance<CRCaseMaint>();
					caseMaint.Case.Current = caseMaint.Case.Cache.CreateCopy(caseCopy) as CRCase;
					caseMaint.GetExtension<PX.Objects.CR.Workflows.CaseWorkflow>().openCaseFromProcessing.Press();
				}

				try
				{
                    SendEmail(Case.Current, Comment.Current);
                }
                catch
                {
                }
            }
            return pxAdapter.Get();
        }

        public PXAction<CRActivity> Close;
        [PXUIField(DisplayName = "Close")]
        [PXButton]
        public virtual void close()
        {
            Comment.Cache.Clear();
        }
        #endregion

        public void SendEmail(CRCase _case, CRActivity _comment)
        {
            TemplateNotificationGenerator templateSender = null;
            Notification notification = null;

            var setup = PortalSetup.Current;
            if (setup.CaseActivityNotificationTemplateID != null)
            {
                notification = PXSelect<Notification,
                    Where<Notification.notificationID,
                        Equal<Required<Notification.notificationID>>>>.SelectWindowed(this, 0, 1,
                            setup.CaseActivityNotificationTemplateID);
            }

            if (notification != null)
            {
                try
                {
                    CRCaseMaint graph = PXGraph.CreateInstance<CRCaseMaint>();

                    graph.Case.Current = _case;
                    graph.ActivitiesSelect.Current = _comment;

                    templateSender = TemplateNotificationGenerator.Create(graph, _case, notification);
                    templateSender.LinkToEntity = false;
                }
                catch (StackOverflowException) { throw; }
                catch (OutOfMemoryException) { throw; }
                catch { }
            }

            if (templateSender != null)
            {
                var activitiesCache = this.Caches[typeof(SMEmail)];
                SMEmail Activity = (SMEmail)activitiesCache.CreateInstance();

                string MailTo = templateSender.ParseExpression(templateSender.To);
                string MailCc = templateSender.ParseExpression(templateSender.Cc);
                if (String.IsNullOrEmpty(MailTo) && String.IsNullOrEmpty(MailCc))
                {
                    return;
                }
                                
                CREmailActivityMaint graph = (CREmailActivityMaint)PXGraph.CreateInstance(typeof(CREmailActivityMaint));
                Activity = (SMEmail)graph.Caches[typeof(SMEmail)].Insert(Activity);

                Activity.Subject = templateSender.ParseExpression(templateSender.Subject);
                Activity.Body = templateSender.ParseExpression(templateSender.Body);
                Activity.IsIncome = false;
                Activity.MailFrom = templateSender.ParseExpression(templateSender.From);

                EMailAccount _account = null;
                if (!String.IsNullOrEmpty(templateSender.ParseExpression(templateSender.From)))
                {
                    _account = PXSelect<EMailAccount,
                        Where<EMailAccount.address, Equal<Required<EMailAccount.address>>>>.SelectWindowed(this, 0, 1, templateSender.From);
                }
                if (_account == null)
                {
                    _account = MailAccountManager.GetDefaultEmailAccount();
                    Activity.MailFrom = _account.Address;
                }

                Activity.MailReply = templateSender.ParseExpression(templateSender.Reply);
                Activity.MailTo = MailTo;
                Activity.MailCc = MailCc;
                Activity.MPStatus = MailStatusListAttribute.Draft;
                Activity.MailAccountID = _account.EmailAccountID;
                Activity.MPStatus = MailStatusListAttribute.PreProcess;
                Activity.RetryCount = 0;
                Activity = (SMEmail)graph.Caches[typeof(SMEmail)].Update(Activity);
                graph.Persist();
            }            
        }
    }
}
