using System;
using PX.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web.Caching;
using PX.Api;
using PX.Data.RichTextEdit;
using PX.Objects.CM;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.CS.DAC;
using PX.Objects.GL.DAC;
using PX.SM;
using PX.Objects.Common;
using PX.Objects.Common.Extensions;

namespace PX.Objects.GL
{
	[PXPrimaryGraph(
		new Type[] { typeof(GeneralLedgerMaint) },
		new Type[] { typeof(Select<Ledger,
			Where<Ledger.ledgerID, Equal<Current<Ledger.ledgerID>>>>)
		})]
	public class GeneralLedgerMaint : PXGraph<GeneralLedgerMaint, Ledger>
	{
		public static void RedirectTo(int? ledgerID)
		{
			var ledgerMaint = CreateInstance<GeneralLedgerMaint>();

			if (ledgerID != null)
			{
				ledgerMaint.LedgerRecords.Current = ledgerMaint.LedgerRecords.Search<Ledger.ledgerID>(ledgerID);
			}

			throw new PXRedirectRequiredException(ledgerMaint, true, string.Empty)
			{
				Mode = PXBaseRedirectException.WindowMode.NewWindow
			};
		}

		public static Ledger FindLedgerByID(PXGraph graph, int? ledgerID, bool isReadonly = true)
		{
			if (isReadonly)
			{
				return PXSelectReadonly<Ledger,
						Where<Ledger.ledgerID, Equal<Required<Ledger.ledgerID>>>>
					.Select(graph, ledgerID);
			}
			else
			{
				return PXSelect<Ledger,
						Where<Ledger.ledgerID, Equal<Required<Ledger.ledgerID>>>>
					.Select(graph, ledgerID);
			}
		}

		#region Graph Extensions

		public class OrganizationLedgerLinkMaint : OrganizationLedgerLinkMaintBase<GeneralLedgerMaint, Ledger>
		{
			protected Dictionary<int?, Ledger> LedgerIDMap = new Dictionary<int?, Ledger>();

			public PXAction<Ledger> ViewOrganization;

			public PXSelectJoin<OrganizationLedgerLink,
									LeftJoin<Organization,
										On<OrganizationLedgerLink.organizationID, Equal<Organization.organizationID>>>,
									Where<OrganizationLedgerLink.ledgerID, Equal<Current<Ledger.ledgerID>>>>
									OrganizationLedgerLinkWithOrganizationSelect;

			public override PXSelectBase<OrganizationLedgerLink> OrganizationLedgerLinkSelect => OrganizationLedgerLinkWithOrganizationSelect;

			public override PXSelectBase<Organization> OrganizationViewBase => Base.OrganizationView;

			public override PXSelectBase<Ledger> LedgerViewBase => Base.LedgerRecords;

			protected override Organization GetUpdatingOrganization(int? organizationID)
			{
				return Base.OrganizationView.Search<Organization.organizationID>(organizationID);
			}

			protected override Type VisibleField => typeof(OrganizationLedgerLink.organizationID);

			[PXUIField(Visible = false, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
			[PXButton]
			public virtual IEnumerable viewOrganization(PXAdapter adapter)
			{
				OrganizationLedgerLink link = OrganizationLedgerLinkSelect.Current;

				if (link != null)
				{
					OrganizationMaint.RedirectTo(link.OrganizationID);
				}

				return adapter.Get();
			}

			[PXMergeAttributes(Method = MergeMethod.Replace)]
			[PX.Objects.GL.Attributes.Organization(true, typeof(
				Search<Organization.organizationID,
					Where<Organization.organizationType.IsNotEqual<OrganizationTypes.group>
						.And<Where<Organization.baseCuryID.IsEqual<Ledger.baseCuryID.FromCurrent>.
							Or<Ledger.baseCuryID.FromCurrent.IsNull>.
							Or<Organization.baseCuryID.IsNull>.
							Or<Ledger.balanceType.FromCurrent.IsNotEqual<LedgerBalanceType.actual>>>>>>
				), null, IsKey = true, FieldClass = null)]
			protected virtual void OrganizationLedgerLink_OrganizationID_CacheAttached(PXCache sender) { }

			protected override void OrganizationLedgerLink_RowInserted(PXCache cache, PXRowInsertedEventArgs e)
			{
				base.OrganizationLedgerLink_RowInserted(cache, e);

				var link = e.Row as OrganizationLedgerLink;
				if (link != null && Base.LedgerRecords.Current != null && Base.LedgerRecords.Current.BaseCuryID == null)
				{
					Organization org = GetUpdatingOrganization(link.OrganizationID);
					Base.LedgerRecords.Cache.SetValueExt<Ledger.baseCuryID>(Base.LedgerRecords.Current, org.BaseCuryID);
				}
			}

			protected virtual void OrganizationLedgerLink_RowUpdating(PXCache cache, PXRowUpdatingEventArgs e)
			{
				var link = e.NewRow as OrganizationLedgerLink;
				if (link?.OrganizationID == null) return;
				CheckActualLedgerCanBeAssigned(Base.LedgerRecords.Current, link.OrganizationID.SingleToArray());
			}

			public virtual void Organization_RowPersisting(PXCache cache, PXRowPersistingEventArgs e)
			{
				e.Cancel = true;
			}

			protected virtual void Ledger_RowSelected(PXCache cache, PXRowSelectedEventArgs e)
			{
				Ledger ledger = e.Row as Ledger;
				PXEntryStatus status = cache.GetStatus(ledger);
				bool isExistingRecord = !(status == PXEntryStatus.Inserted && ledger.LedgerCD == null);
				Base.ChangeID.SetEnabled(isExistingRecord);

				if (ledger?.LedgerID != null)
				{
					bool hasHistory = GLUtility.IsLedgerHistoryExist(Base, ledger.LedgerID);

					PXUIFieldAttribute.SetEnabled<Ledger.balanceType>(Base.LedgerRecords.Cache, ledger, !hasHistory);

					bool canChangeCurrency = ledger.BalanceType != LedgerBalanceType.Actual && !hasHistory &&
											 PXAccess.FeatureInstalled<FeaturesSet.multicurrency>();

					if (!hasHistory && ledger.BalanceType == LedgerBalanceType.Actual && PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>())
					{
						canChangeCurrency = OrganizationLedgerLinkWithOrganizationSelect.SelectSingle() == null;
					}

					PXUIFieldAttribute.SetEnabled<Ledger.baseCuryID>(Base.LedgerRecords.Cache, ledger, canChangeCurrency);
				}
			}

			public virtual void Ledger_RowPersisting(PXCache cache, PXRowPersistingEventArgs e)
			{
				var ledger = e.Row as Ledger;

				if (ledger == null)
					return;

				if (ledger.LedgerID < 0)
				{
					LedgerIDMap[ledger.LedgerID] = ledger;
				}
			}

			public virtual void OnPersist(IEnumerable<Organization> organizations)
			{
				OrganizationMaint organizationMaint = CreateInstance<OrganizationMaint>();

				PXCache organizationCache = organizationMaint.OrganizationView.Cache;

				PXDBTimestampAttribute timestampAttribute = organizationCache
					.GetAttributesOfType<PXDBTimestampAttribute>(null, nameof(Organization.tstamp))
					.First();

				timestampAttribute.RecordComesFirst = true;

				foreach (Organization organization in organizations)
				{
					organizationMaint.Clear();

					organizationMaint.BAccount.Current = organizationMaint.BAccount.Search<OrganizationBAccount.bAccountID>(organization.BAccountID);

					organizationCache.Clear();
					organizationCache.ClearQueryCacheObsolete();

					if (organization.ActualLedgerID < 0)
					{
						organization.ActualLedgerID = LedgerIDMap[organization.ActualLedgerID].LedgerID;
					}

					organizationCache.Current = organization;
					organizationCache.SetStatus(organizationMaint.OrganizationView.Current, PXEntryStatus.Updated);

					organizationMaint.Actions.PressSave();
				}
			}
		}
		#endregion

		[PXImport(typeof(Ledger))]
		public PXSelect<Ledger> LedgerRecords;
		public PXSetup<Company> company;

		public PXSelectReadonly2<Branch,
									InnerJoin<Organization,
										On<Branch.organizationID, Equal<Organization.organizationID>>,
									InnerJoin<OrganizationLedgerLink,
										On<Organization.organizationID, Equal<OrganizationLedgerLink.organizationID>>>>,
									Where<OrganizationLedgerLink.ledgerID, Equal<Current<Ledger.ledgerID>>>>
									BranchesView;

		public PXSelect<Organization> OrganizationView;

		public GeneralLedgerMaint()
		{
			var mcFeatureInstalled = PXAccess.FeatureInstalled<FeaturesSet.multicurrency>();

			PXUIFieldAttribute.SetVisible<Ledger.baseCuryID>(LedgerRecords.Cache, null, mcFeatureInstalled);

			BranchesView.AllowSelect = PXAccess.FeatureInstalled<FeaturesSet.branch>();
			this.action.MenuAutoOpen = true;
			action.AddMenuAction(ChangeID);
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault(typeof(IIf<Where<FeatureInstalled<FeaturesSet.multipleBaseCurrencies>>, Null, Current<Company.baseCuryID>>),
			PersistingCheck = PXPersistingCheck.NullOrBlank)]
		[PXSelector(typeof(Search<CM.CurrencyList.curyID, Where<CM.CurrencyList.isActive, Equal<True>>>))]
		protected void Ledger_BaseCuryID_CacheAttached(PXCache sender) { }

		protected virtual void Ledger_BaseCuryID_FieldVerifying(PXCache cache, PXFieldVerifyingEventArgs e)
		{
			Ledger ledger = e.Row as Ledger;
			if (ledger != null && ledger.LedgerID != null && ledger.BaseCuryID != null)
			{
				if (GLUtility.IsLedgerHistoryExist(this, (int)ledger.LedgerID))
				{
					throw new PXSetPropertyException(Messages.CantChangeField, $"[{nameof(Ledger.baseCuryID)}]");
				}

				if (ledger.BalanceType == LedgerBalanceType.Actual
					&& company.Current.BaseCuryID != (string)e.NewValue
					&& !PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>())
				{
					throw new PXSetPropertyException(Messages.ActualLedgerInBaseCurrency, ledger.LedgerCD, company.Current.BaseCuryID);
				}
			}
		}

		protected virtual void Ledger_RowDeleting(PXCache cache, PXRowDeletingEventArgs e)
		{
			var ledger = e.Row as Ledger;

			if (ledger == null)
				return;

			CanBeLedgerDeleted(ledger);
		}

		protected virtual void Ledger_BalanceType_FieldVerifying(PXCache cache, PXFieldVerifyingEventArgs e)
		{
			Ledger ledger = e.Row as Ledger;

			string newBalanceType = (string) e.NewValue;

			if (ledger != null && ledger.LedgerID != null && ledger.CreatedByID != null)
			{
				if (GLUtility.IsLedgerHistoryExist(this, (int)ledger.LedgerID))
				{
					throw new PXSetPropertyException(Messages.CantChangeField, $"[{nameof(Ledger.balanceType)}]");
				}

				if (newBalanceType == LedgerBalanceType.Actual)
				{
					if (company.Current.BaseCuryID != ledger.BaseCuryID
						&& !PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>())
					{
						throw new PXSetPropertyException(Messages.ActualLedgerInBaseCurrency, 
							ledger.LedgerCD,
							company.Current.BaseCuryID);
					}

					CanBeLedgerSetAsActual(ledger, GetExtension<OrganizationLedgerLinkMaint>());

					IEnumerable<Organization> organizations = 
										PXSelectJoin<OrganizationLedgerLink,
													InnerJoin<Organization,
														On<Organization.organizationID, Equal<OrganizationLedgerLink.organizationID>>>,
													Where<OrganizationLedgerLink.ledgerID, Equal<Required<OrganizationLedgerLink.ledgerID>>>>
													.Select(this, ledger.LedgerID)
													.RowCast<Organization>();

					foreach (Organization organization in organizations)
					{
						organization.ActualLedgerID = ledger.LedgerID;

						OrganizationView.Cache.SmartSetStatus(organization, PXEntryStatus.Updated);
					}										
				}
			}
		}

		public override void Persist()
		{
			OrganizationLedgerLinkMaint linkMaint = GetExtension<OrganizationLedgerLinkMaint>();

			foreach (Ledger ledger in LedgerRecords.Cache.Deleted)
			{
				CanBeLedgerDeleted(ledger);
			}

			foreach (Ledger ledger in LedgerRecords.Cache.Updated)
			{
				string origBalanceType = LedgerRecords.Cache.GetValueOriginal<Ledger.balanceType>(ledger) as string;

				if (origBalanceType != ledger.BalanceType)
				{
					GLTran existingReleasedTran = PXSelectReadonly<GLTran,
															Where<GLTran.ledgerID, Equal<Required<GLTran.ledgerID>>,
																	And<GLTran.released, Equal<True>>>>
															.SelectSingleBound(this, null, ledger.LedgerID);

					if (existingReleasedTran != null)
					{
						throw new PXException(Messages.TheTypeOfTheLedgerCannotBeChangedBecauseAtLeastOneReleasedGLTransactionExists, ledger.LedgerCD);
					}

					if (origBalanceType == LedgerBalanceType.Actual)
					{
						SetActualLedgerIDNullInRelatedCompanies(ledger, linkMaint);
					}
				}

				if (ledger.BalanceType == LedgerBalanceType.Actual)
				{
					CanBeLedgerSetAsActual(ledger, linkMaint);
				}
			}

			Organization[] organizations = OrganizationView.Cache.Updated.Cast<Organization>()
																.Select(PXCache<Organization>.CreateCopy).ToArray();

			using (var tranScope = new PXTransactionScope())
			{
				base.Persist();

				OrganizationView.Cache.Clear();

				linkMaint.OnPersist(organizations);

				tranScope.Complete();
			}
		}

		protected virtual void CanBeLedgerSetAsActual(Ledger ledger, OrganizationLedgerLinkMaint linkMaint)
		{
			linkMaint.CheckActualLedgerCanBeAssigned(ledger, GetLinkedOrganizationIDs(ledger).ToArray());
		}

		private void SetActualLedgerIDNullInRelatedCompanies(Ledger ledger, OrganizationLedgerLinkMaint linkMaint)
		{
			linkMaint.SetActualLedgerIDNullInRelatedCompanies(ledger, GetOrganizationIDsWithActualLedger(ledger).ToArray());
		}

		private IEnumerable<int?> GetOrganizationIDsWithActualLedger(Ledger ledger)
		{
			return PXSelect<Organization, Where<Organization.actualLedgerID, Equal<Required<Organization.actualLedgerID>>>>
				.Select(this, ledger.LedgerID)
				.RowCast<Organization>()
				.Select(l => l.OrganizationID);
		}

		private IEnumerable<int?> GetLinkedOrganizationIDs(Ledger ledger)
		{
			return PXSelect<OrganizationLedgerLink,
																	Where<OrganizationLedgerLink.ledgerID, Equal<Required<OrganizationLedgerLink.ledgerID>>>>
																	.Select(this, ledger.LedgerID)
				.RowCast<OrganizationLedgerLink>()
				.Select(l => l.OrganizationID);
		}

		protected virtual void CanBeLedgerDeleted(Ledger ledger)
		{
			CheckLinksToOrganizationsOnDelete(ledger);

			Batch existingBatch = PXSelectReadonly<Batch,
					Where<Batch.ledgerID, Equal<Required<Batch.ledgerID>>>>
				.SelectSingleBound(this, null, ledger.LedgerID);

			if (existingBatch != null)
			{
				throw new PXException(Messages.TheLedgerCannotBeDeletedBecauseAtLeastOneGeneralLedgerBatchExists, ledger.LedgerCD);
			}

			GLTran existingTran = PXSelectReadonly<GLTran,
					Where<GLTran.ledgerID, Equal<Required<GLTran.ledgerID>>>>
				.SelectSingleBound(this, null, ledger.LedgerID);

			if (existingTran != null)
			{
				throw new PXException(Messages.TheLedgerCannotBeDeletedBecauseAtLeastOneGeneralLedgerTransactionHasBeenReleased, ledger.LedgerCD);
			}
		}

		// TODO: Rework to RIC on Delete engine after many-to-many messages fix
		protected virtual void CheckLinksToOrganizationsOnDelete(Ledger ledger)
		{
			Organization[] organizations = PXSelectJoin<OrganizationLedgerLink,
											InnerJoin<Organization,
												On<Organization.organizationID, Equal<OrganizationLedgerLink.organizationID>>>,
											Where<OrganizationLedgerLink.ledgerID, Equal<Required<OrganizationLedgerLink.ledgerID>>>>
											.Select(this, ledger.LedgerID).AsEnumerable()
											.Cast<PXResult<OrganizationLedgerLink, Organization>>()
											.Select(row => (Organization)row)
											.ToArray();

			if (organizations.Any())
			{
				throw new PXException(Messages.LedgerCannotBeDeletedBecauseCompanyOrCompaniesAreAssociated,
					ledger.LedgerCD.Trim(),
					organizations.Select(l => l.OrganizationCD.Trim()).ToArray().JoinIntoStringForMessage());
			}
		}
		#region Buttons
		public PXAction<Ledger> action;
		[PXUIField(DisplayName = "Actions", MapEnableRights = PXCacheRights.Select)]
		[PXButton(SpecialType = PXSpecialButtonType.ActionsFolder)]
		protected virtual IEnumerable Action(PXAdapter adapter)
		{
			return adapter.Get();
		}

		public PXChangeID<Ledger, Ledger.ledgerCD> ChangeID;
		#endregion
	}
}
