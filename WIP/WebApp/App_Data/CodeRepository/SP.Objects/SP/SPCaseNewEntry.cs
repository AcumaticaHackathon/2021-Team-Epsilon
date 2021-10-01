using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.CT;
using PX.Objects.GL;
using PX.Objects.SP.DAC;
using PX.SM;
using PX.Common;
using SP.Objects.CR;
using PX.Objects.CR.Workflows;

namespace SP.Objects.SP
{
	/// <exclude/>
	[DashboardType((int)DashboardTypeAttribute.Type.Default, TableAndChartDashboardTypeAttribute._AMCHARTS_DASHBOART_TYPE)]
	public class SPCaseNewEntry : PXGraph<SPCaseNewEntry>
	{
        public SPCaseNewEntry()
		{
            if (PortalSetup.Current == null)
				throw new PXSetupNotEnteredException<PortalSetup>(ErrorMessages.SetupNotEntered);

            var setting = PortalSetup.Current;
			CRSetup settingcase = crSetup.Current;

			PXUIFieldAttribute.SetDisplayName<Users.fullName>(Caches[typeof(Users)], "Assigned To");
			if (Case.Current != null)
			{
				CRCaseClass row1 = CaseClassesCurrent.SelectWindowed(0, 1, Case.Current.CaseClassID);
				if (row1 != null)
				{
					bool IsNeedContract = PXAccess.FeatureInstalled<FeaturesSet.contractManagement>() && (bool)row1.RequireContract;
					PXUIFieldAttribute.SetRequired<CRCase.contractID>(Case.Cache, IsNeedContract);
					PXDefaultAttribute.SetPersistingCheck<CRCase.contractID>(Case.Cache, Case.Current,
																									  IsNeedContract
																									? PXPersistingCheck.NullOrBlank
																									: PXPersistingCheck.Nothing);
				}
			}
		}

		#region Selects	

		// due to inheritance hell - Customer conflicts with BAccount. 
		[PXHidden]
		public PXSelect<BAccount>
			dummyBAccount;

		[PXHidden]
		public PXSelect<Contact>
			dummyContact;

		public PXSetup<CRSetup>
			 crSetup;

		[PXHidden]
		public PXSelect<WikiRevision>
			WikiRevision;

		public PXSelectOrderBy<CRCase,
			 OrderBy<Desc<CRCase.caseCD>>> Case;


		[PXHidden]
		public PXSelect<CRCase,
			Where<CRCase.caseCD, Equal<Current<CRCase.caseCD>>>> CaseCurrent;

		[PXHidden]
		public PXSelect<CRCaseClass,
			Where<CRCaseClass.caseClassID, Equal<Required<CRCaseClass.caseClassID>>>>
			CaseClassesCurrent;

		[PXHidden]
		public PXSelect<CRActivity> Activity;

		[PXViewName("Answers")]
		public CRAttributeList<CRCase> Answers;

		[PXHidden]
		public PXSelect<CRActivityStatistics> Stats;

		#endregion

		#region Actions	
		public PXCancel<CRCase> Cancel;
		public PXSave<CRCase> Save;

		public PXAction<CRCase> Submit;
		[PXUIField(DisplayName = "Submit")]
		[PXButton]
		public virtual IEnumerable submit(PXAdapter adapter)
		{
			if (ReadBAccount.ReadCurrentAccount() == null)
			{
				throw new Exception("You cannot submit a case because you are not associated with any business account.");
			}

			if (crSetup.Current.DefaultCaseAssignmentMapID != null)
			{
                CRCaseMaint assignGraph = PXGraph.CreateInstance<CRCaseMaint>();
                var processor = new PX.Objects.EP.EPAssignmentProcessor<CRCase>(assignGraph);
                processor.Assign(Case.Current, crSetup.Current.DefaultCaseAssignmentMapID);
                Case.Update(Case.Current);
			}

			// notification
			foreach (object r in Save.Press(adapter))
			{
			}

			/*EPActivity firstActivity = (EPActivity)Activity.Cache.CreateInstance();
			firstActivity.ClassID = CRActivityClass.Activity;
			firstActivity = (EPActivity)Activity.Cache.Insert(firstActivity);

			firstActivity.Subject = CaseCurrent.Current.Subject;
			firstActivity.Body = CaseCurrent.Current.Description;
			firstActivity.CreatedByID = CaseCurrent.Current.CreatedByID;
			firstActivity.StartDate = CaseCurrent.Current.CreatedDateTime;
			firstActivity.RefNoteID = CaseCurrent.Current.NoteID;
			Activity.Cache.SetDefaultExt<EPActivity.type>(firstActivity);
	        firstActivity.IsPrivate = false;
	        Activity.Cache.Update(firstActivity);*/

			this.Save.Press();

			CRCase _case = Case.Current;
			Case.Cache.Clear();
			CaseCurrent.Cache.Clear();
			Answers.Cache.Clear();

			if (_case != null)
			{
				var graph = PXGraph.CreateInstance<CRCaseMaint>();
				graph.Case.Current = _case;
				PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.Same);
			}
			return adapter.Get();

			/*WikiShowReader _graph = CreateInstance<WikiShowReader>();
		 _graph.Actions.Clear();
			_graph.Pages.Insert();
		 _graph.Pages.Current = wp;
			_graph.Pages.Cache.IsDirty = false;*/
			//throw new PXRedirectRequiredException(url, this, "");
		}
		#endregion

		#region Event Handlers
		protected virtual void CRCase_SLAETA_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
		    CRCase row = e.Row as CRCase;
			if (row == null || row.CreatedDateTime == null) return;

			if (row.ClassID != null && row.Severity != null)
			{
				var severity = (CRClassSeverityTime)PXSelect<CRClassSeverityTime,
														Where<CRClassSeverityTime.caseClassID, Equal<Required<CRClassSeverityTime.caseClassID>>,
														And<CRClassSeverityTime.severity, Equal<Required<CRClassSeverityTime.severity>>>>>.
														Select(this, row.ClassID, row.Severity);
				if (severity != null && severity.TimeReaction != null)
				{
					e.NewValue = ((DateTime)row.CreatedDateTime).AddMinutes((int)severity.TimeReaction);
					e.Cancel = true;
				}
			}

			if (row.Severity != null && row.ContractID != null)
			{
				var template = (Contract)PXSelect<Contract, Where<Contract.contractID, Equal<Required<CRCase.contractID>>>>.Select(this, row.ContractID);
				if (template == null) return;

				var sla = (ContractSLAMapping)PXSelect<ContractSLAMapping,
												  Where<ContractSLAMapping.severity, Equal<Required<CRCase.severity>>,
												  And<ContractSLAMapping.contractID, Equal<Required<CRCase.contractID>>>>>.
												  Select(this, row.Severity, template.TemplateID);
				if (sla != null && sla.Period != null)
				{
					e.NewValue = ((DateTime)row.CreatedDateTime).AddMinutes((int)sla.Period);
					e.Cancel = true;
				}
			}
        }

		protected virtual void CRCase_CustomerID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			e.NewValue = ReadBAccount.ReadCurrentAccount().With(_ => _.BAccountID);
			e.Cancel = true;
		}
		protected virtual void CRCase_ContactID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			e.NewValue = ReadCurrentContact().With(_ => _.ContactID);
			e.Cancel = true;
		}

		protected virtual void CRCase_RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			var row = e.Row as CRCase;
			if (row == null) return;
			if (row == null || row.CreatedDateTime == null) return;
			if (row.CaseClassID != null && row.Severity != null)
			{
				var severity = (CRClassSeverityTime)PXSelect<CRClassSeverityTime,
														Where<CRClassSeverityTime.caseClassID, Equal<Required<CRClassSeverityTime.caseClassID>>,
														And<CRClassSeverityTime.severity, Equal<Required<CRClassSeverityTime.severity>>>>>.
														Select(this, row.ClassID, row.Severity);
				if (severity != null && severity.TimeReaction != null)
				{
					row.SLAETA = ((DateTime)row.CreatedDateTime).AddMinutes((int)severity.TimeReaction);
				}
			}
			PXFieldState state = sender.GetValueExt<CRCase.caseClassID>(row) as PXFieldState;
			if(state!= null)
				row.CaseClassID = (string)state.Value;
		}

		protected virtual void CRCase_ContractID_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			CRCase row = e.Row as CRCase;
			if (row != null)
			{
				Contract contract = PXSelect<Contract,
												Where<Contract.contractID, Equal<Required<Contract.contractID>>,
												And<Contract.expireDate, LessEqual<Current<AccessInfo.businessDate>>>>>.Select(this, e.NewValue);
				if (contract != null)
				{
					sender.RaiseExceptionHandling<CRCase.contractID>(e.Row, contract.ContractCD, new PXSetPropertyException(Messages.ContractExpired, PXErrorLevel.Error, contract.ContractCD));
				}
			}
		}

		protected virtual void CRCase_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			CRCase row = e.Row as CRCase;
			if (row != null)
			{
				CRCaseClass row1 = CaseClassesCurrent.SelectWindowed(0, 1, row.CaseClassID);
				if (row1 != null)
				{
					bool IsNeedContract = PXAccess.FeatureInstalled<FeaturesSet.contractManagement>() && (bool)row1.RequireContract;
					PXUIFieldAttribute.SetRequired<CRCase.contractID>(Case.Cache, IsNeedContract);
					PXDefaultAttribute.SetPersistingCheck<CRCase.contractID>(Case.Cache, row,
																									  IsNeedContract
																									? PXPersistingCheck.NullOrBlank
																									: PXPersistingCheck.Nothing);
				}
			}
		}

		protected virtual void CRCase_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			CRCase row = e.Row as CRCase;
			if (row != null)
			{
				CRCaseClass row1 = CaseClassesCurrent.SelectWindowed(0, 1, row.CaseClassID);
				if (row1 != null)
				{
					bool IsNeedContract = PXAccess.FeatureInstalled<FeaturesSet.contractManagement>() && (bool)row1.RequireContract;
					PXUIFieldAttribute.SetRequired<CRCase.contractID>(Case.Cache, IsNeedContract);
					PXDefaultAttribute.SetPersistingCheck<CRCase.contractID>(Case.Cache, row,
																									  IsNeedContract
																									? PXPersistingCheck.NullOrBlank
																									: PXPersistingCheck.Nothing);
				}
			}
		}
		#endregion

		public override IEnumerable ExecuteSelect(string viewName, object[] parameters, object[] searches, string[] sortcolumns,
			bool[] descendings, PXFilterRow[] filters, ref int startRow, int maximumRows, ref int totalRows)
		{
			if (viewName == nameof(Case))
			{
				if (PX.Web.UI.PXPageCache.IsReloadPage)
				{
					var values = new Dictionary<string, object>();
					for (int i = 0; i < sortcolumns.Length; i++)
					{
						if (sortcolumns[i] != nameof(CRCase.CaseCD) && sortcolumns[i] != nameof(CRCase.OwnerID))
							values.Add(sortcolumns[i], searches[i]);
					}
					if (ExecuteInsert(viewName, values, null) > 0)
					{
						Views[viewName].Cache.IsDirty = (Views[viewName].Cache.Keys.Count == 0);
						searches = new object[] { this.Case.Current.CaseCD };
						sortcolumns = new[] { typeof(CRCase.caseCD).Name };
					}
				}
				else if (sortcolumns.Length == 0)
				{
					searches = new object[] { Case.Current != null ? Case.Current.CaseCD : "<NEW>" };
					sortcolumns = new[] { typeof(CRCase.caseCD).Name };
				}
				else if (sortcolumns.Length == 1 && searches.Length == 1 && searches[0] == null)
				{
					searches[0] = Case.Current != null ? Case.Current.CaseCD : "<NEW>";
					startRow = 0;
					maximumRows = 1;
				}
			}

			return base.ExecuteSelect(viewName, parameters, searches, sortcolumns, descendings, filters, ref startRow, maximumRows, ref totalRows);
		}

		public override int ExecuteInsert(string viewName, IDictionary values, params object[] parameters)
		{
			if (viewName == nameof(Case))
			{
				var graph = PXGraph.CreateInstance<CRCaseMaint>();
				var result = graph.ExecuteInsert(nameof(CRCaseMaint.Case), values, parameters);
				if (result > 0)
				{
					foreach (var inserted in graph.Case.Cache.Inserted)
					{
						Case.Cache.Insert(inserted);
					}
					return result;
				}
			}
			return base.ExecuteInsert(viewName, values, parameters);
		}

		private Contact ReadCurrentContact()
		{
			Guid userId = PXAccess.GetUserID();
			if (userId == Guid.Empty) return null;

			return (Contact)PXSelect<Contact,
				Where<Contact.userID, Equal<Required<Contact.userID>>>>.
				Select(this, userId);
		}
	}
}