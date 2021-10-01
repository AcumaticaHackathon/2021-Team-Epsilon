using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.CS.DAC;
using PX.Objects.GL.DAC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Objects.EP;
using PX.Objects.CR;
using PX.Objects.GL;
using System.Collections;
using PX.Data.BQL;
using PX.Objects.AR;
using PX.Objects.AP;
using PX.Objects.Common;

namespace PX.Objects.CS
{
	public class CompanyGroupsMaint : OrganizationMaint
	{
		protected override void OrganizationFilter()
		{
			BAccount.WhereAnd<Where<OrganizationBAccount.organizationType.IsEqual<OrganizationTypes.group>>>();
		}

		public PXAction<OrganizationBAccount> ViewCompany;

		public SelectFrom<GroupOrganizationLink>
			.InnerJoin<Organization>
				.On<GroupOrganizationLink.organizationID.IsEqual<Organization.organizationID>>
			.LeftJoin<Ledger>
				.On<Organization.actualLedgerID.IsEqual<Ledger.ledgerID>>
			.LeftJoin<PrimaryGroup>
				.On<GroupOrganizationLink.organizationID.IsEqual<PrimaryGroup.organizationID>>
			.Where<GroupOrganizationLink.groupID.IsEqual<Organization.organizationID.FromCurrent>>.View Organizations;


		public CompanyGroupsMaint()
			: base()
		{
			if (string.IsNullOrEmpty(Company.Current.BaseCuryID))
			{
				throw new PXSetupNotEnteredException(ErrorMessages.SetupNotEntered, typeof(Company), PXMessages.LocalizeNoPrefix(CS.Messages.OrganizationMaint));
			}

			Delete.SetConfirmationMessage(Messages.DeleteGroup);
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXUIField(DisplayName = "Group")]
		[PXDBDefault(typeof(Organization.organizationID), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXParent(typeof(Select<Organization, Where<Organization.organizationID, Equal<Current<GroupOrganizationLink.groupID>>>>))]
		protected override void GroupOrganizationLink_GroupID_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDBInt(IsKey = true)]
		[PXUIField(DisplayName = "Company ID")]
		[PXSelector(typeof(Search<Organization.organizationID, 
			Where<Match<Organization, Current<AccessInfo.userName>>
				.And<Organization.organizationType.IsNotEqual<OrganizationTypes.group>>
				.And<Organization.baseCuryID.IsEqual<Organization.baseCuryID.FromCurrent>>>>),
			typeof(Organization.organizationCD),
			typeof(Organization.organizationName),
			SubstituteKey = typeof(Organization.organizationCD))]
		protected override void GroupOrganizationLink_OrganizationID_CacheAttached(PXCache sender) { }

		[PXDimensionSelector("COMPANY",
			typeof(Search2<BAccount.acctCD,
				InnerJoin<Organization,
					On<Organization.bAccountID, Equal<BAccount.bAccountID>>>,
				Where<Match<Organization, Current<AccessInfo.userName>>
					.And<Organization.organizationType.IsEqual<OrganizationTypes.group>>>>),
			typeof(BAccount.acctCD),
			typeof(BAccount.acctCD), typeof(BAccount.acctName))]
		[PXDBString(30, IsUnicode = true, IsKey = true, InputMask = "")]
		[PXDefault()]
		[PXUIField(DisplayName = "Group ID", Visibility = PXUIVisibility.SelectorVisible)]
		protected void OrganizationBAccount_AcctCD_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "Group Name")]
		protected void OrganizationBAccount_AcctName_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Company Name")]
		protected override void Organization_OrganizationName_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault(typeof(IIf<Where<FeatureInstalled<FeaturesSet.multipleBaseCurrencies>>, Null, Current<Company.baseCuryID>>),
			PersistingCheck = PXPersistingCheck.NullOrBlank)]
		[PXUIField(DisplayName = "Currency ID")]
		[PXSelector(typeof(Search<CM.CurrencyList.curyID, Where<CM.CurrencyList.isActive, Equal<True>>>))]
		protected new void Organization_BaseCuryID_CacheAttached(PXCache sender) { }

		protected void _(Events.FieldUpdating<Organization.baseCuryID> e) 
		{
			if (e.Row == null) return;
			var org = (Organization)e.Row;
			var baccount = (CR.BAccount)PXSelect<CR.BAccount, Where<CR.BAccount.bAccountID, Equal<Required<CR.BAccount.bAccountID>>>>.SelectSingleBound(this, null, org.BAccountID);
			if (baccount == null) return;
			ResetVisibilityRestrictions(baccount.BAccountID, baccount.AcctCD, out bool cancelled);
			if (cancelled) e.Cancel = true;
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault(OrganizationTypes.Group)]
		protected void Organization_OrganizationType_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault(OrganizationTypes.Group)]
		protected void OrganizationBAccount_OrganizationType_CacheAttached(PXCache sender) { }

		[PXDBString(100)]
		[Country]
		protected void Address_CountryID_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "Actual Ledger", Visibility = PXUIVisibility.SelectorVisible)]
		protected void Ledger_LedgerCD_CacheAttached(PXCache sender) { }

		public override void Persist()
		{
			try
			{
				base.Persist();
			}
			finally
			{
				PXAccess.ResetOrganizationBranchSlot();
			}
		}

		public new static void RedirectTo(int? GroupID)
		{
			CompanyGroupsMaint companyGroupsMaint = CreateInstance<CompanyGroupsMaint>();

			Organization group = FindOrganizationByID(companyGroupsMaint, GroupID);

			if (group == null)
				return;

			companyGroupsMaint.BAccount.Current = companyGroupsMaint.BAccount.Search<OrganizationBAccount.bAccountID>(group.BAccountID);

			throw new PXRedirectRequiredException(companyGroupsMaint, true, string.Empty)
			{
				Mode = PXBaseRedirectException.WindowMode.NewWindow
			};
		}

		protected override void OrganizationBAccount_RowSelected(PXCache cache, PXRowSelectedEventArgs e)
		{
			base.OrganizationBAccount_RowSelected(cache, e);
		
			ActionsMenu.SetVisible(false);
		}

		protected override void Organization_RowSelected(PXCache cache, PXRowSelectedEventArgs e)
		{
			var installed = PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
			var hasCompanies = Organizations.SelectSingle() != null;
			PXUIFieldAttribute.SetEnabled<Organization.baseCuryID>(cache, e.Row, installed && !hasCompanies);
		}

		protected virtual void _(Events.RowDeleting<OrganizationBAccount> e)
		{
			if (e.Row == null) return;
			ResetVisibilityRestrictions(e.Row.BAccountID, e.Row.AcctCD, out bool cancelled);
			if (cancelled) e.Cancel = true;
		}

		[PXUIField(Visible = false, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable viewCompany(PXAdapter adapter)
		{
			GroupOrganizationLink link = Organizations.Current;

			if (link != null)
			{
				OrganizationMaint.RedirectTo(link.OrganizationID);
			}

			return adapter.Get();
		}

	}	
}
