using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using PX.Common;
using PX.Objects;
using PX.Objects.CR;
using PX.Objects.SP;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.SP.DAC;
using PX.SM;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using SP.Objects.IN.DAC;
using SP.Objects.SP;
using PX.Objects.GL.DAC;
using PX.Objects.IN.DAC;
using SP.Objects.IN;
using GL = PX.Objects.GL;

namespace SP.Objects
{
	public class PortalSetupMaint : PXGraph<PortalSetupMaint>
	{
		[PXLocalizable]
		public static class Msg
		{
			public const string DefaultWarehouseCannotBeExcluded = "Default warehouse cannot be excluded";
		}

		#region WikiFoldersTree
		public class PXSelectWikiFoldersTreeWithVisibility : PXSelectBase<WikiPageSimple>
		{
			public PXSelectWikiFoldersTreeWithVisibility(PXGraph graph)
			{
				this.View = CreateView(graph, new PXSelectDelegate<Guid?>(folders));
			}
			public PXSelectWikiFoldersTreeWithVisibility(PXGraph graph, Delegate handler)
			{
				this.View = CreateView(graph, handler);
			}

			private PXView CreateView(PXGraph graph, Delegate handler)
			{
				return new PXView(graph, false,
					new Select<WikiPageSimple, Where<WikiPageSimple.parentUID, Equal<Argument<Guid?>>>,
							OrderBy<Asc<WikiPageSimple.number>>>(),
					handler);
			}

			internal IEnumerable folders(
				[PXGuid]
			Guid? PageID
			)
			{
				List<WikiPageSimple> ret = new List<WikiPageSimple>();
				bool needRoot = false;
				if (PageID == null)
				{
					PageID = Guid.Empty;
					needRoot = true;
				}

				HttpContext ctx = HttpContext.Current;
				HttpContext.Current = null; // remove context to avoid rights check.
				PXSiteMapNode parent = PXSiteMap.WikiProvider.FindSiteMapNodeFromKey(PageID.Value);
				if (needRoot)
				{
					ret.Add(CreateWikiPageSimple(parent));
				}

				else
				{
					foreach (PXSiteMapNode node in parent.ChildNodes)
					{
						if (PageID.Value == Guid.Empty)
						{
							PXSiteMapNode parent1 = PXSiteMap.Provider.FindSiteMapNodeFromKey(node.NodeID);
							if (parent1 != null)
							{
								ret.Add(CreateWikiPageSimple(node));
							}
						}
						else
						{
							if (node.Title != "Deleted Items")
								ret.Add(CreateWikiPageSimple(node));
						}
					}
				}
				HttpContext.Current = ctx;
				return ret;
			}

			private WikiPageSimple CreateWikiPageSimple(PXSiteMapNode node)
			{
				WikiPageSimple result = new WikiPageSimple();
				result.PageID = node.NodeID;
				result.ParentUID = node.ParentID;
				result.Title = string.IsNullOrEmpty(node.Title) ? ((PXWikiMapNode)node).Name : node.Title;
				return result;
			}
		}
		#endregion
		public SelectFrom<PortalSetup>.Where<PortalSetup.IsCurrentPortal>.View CRSetupRecord;
		public PXSelectWikiFoldersTreeWithVisibility WikiMap;
		public PXSelectSiteMapTree<False, False, False, True, True> SiteMap;

		public PXSelect<PreferencesGeneral> CRPreferencesGeneralSetupRecord;

		public PXSelect<INSite,
				Where<INSite.siteID, NotEqual<SiteAttribute.transitSiteID>,
					   And<Match<Current<AccessInfo.userName>>>>> CRSetupINSite;

		[PXHidden]
		public PXSelect<Organization, Where<Organization.organizationID, Equal<Current<PortalSetup.restrictByOrganizationID>>>> RestrictCompany;
		[PXHidden]
		public PXSelect<PX.Objects.GL.Branch, Where<PX.Objects.GL.Branch.branchID, Equal<Current<PortalSetup.restrictByBranchID>>>> RestrictBranch;
		[PXHidden]
		public PXSelect<PX.Objects.GL.Branch, Where<PX.Objects.GL.Branch.branchID, Equal<Current<PortalSetup.sellingBranchID>>>> SellingBranch;
		[PXHidden]
		public PXSelect<WarehouseReference, Where<WarehouseReference.portalSetupID.IsEqual<PortalSetup.portalSiteID>>> WarehouseReference;

		#region ctor
		private bool IsCalledFromSetupNotEnteredExceptionPage()
		{
			try
			{
				return HttpContext.Current?.Request?.Params["exceptionID"] is string exceptionID
					&& PXContext.Session?.Exception[exceptionID] is PXSetupNotEnteredException;
			}
			catch
			{
				return false;
			}
		}
		public PortalSetupMaint()
		{
			if (WebConfig.PortalSiteID == null && !IsCalledFromSetupNotEnteredExceptionPage())
			{
				throw new PXException(Messages.PortalSiteIdIsNotSpecified);
			}

			PXUIFieldAttribute.SetEnabled<INSite.siteCD>(CRSetupINSite.Cache, null, false);
			PXUIFieldAttribute.SetEnabled<INSite.descr>(CRSetupINSite.Cache, null, false);
			PXUIFieldAttribute.SetEnabled<INSite.branchID>(CRSetupINSite.Cache, null, false);

			PXUIFieldAttribute.SetVisible<PortalSetup.sellingBranchID>(CRSetupRecord.Cache, null, PXAccess.FeatureInstalled<FeaturesSet.b2BOrdering>());
			PXUIFieldAttribute.SetVisible<PortalSetup.defaultStockItemWareHouse>(CRSetupRecord.Cache, null, PXAccess.FeatureInstalled<FeaturesSet.warehouse>());
			PXUIFieldAttribute.SetVisible<PortalSetup.defaultnonStockItemWareHouse>(CRSetupRecord.Cache, null, PXAccess.FeatureInstalled<FeaturesSet.warehouse>());

			PXUIFieldAttribute.SetVisible<INSite.siteCD>(CRSetupINSite.Cache, null, PXAccess.FeatureInstalled<FeaturesSet.warehouse>());
			PXUIFieldAttribute.SetVisible<INSite.descr>(CRSetupINSite.Cache, null, PXAccess.FeatureInstalled<FeaturesSet.warehouse>());
			PXUIFieldAttribute.SetVisible<INSite.included>(CRSetupRecord.Cache, null, PXAccess.FeatureInstalled<FeaturesSet.warehouse>());

			PXUIFieldAttribute.SetVisible<PortalSetup.availableQty>(CRSetupRecord.Cache, null, PXAccess.FeatureInstalled<FeaturesSet.inventory>());
		}
		#endregion

		#region Select Delegate
		public virtual IEnumerable cRSetupINSite()
		{
			PXResultset<INSite> rusultSet;

			using (new PXReadBranchRestrictedScope())
			{
				rusultSet = PXSelectJoin<INSite,
				   InnerJoin<PX.Objects.GL.Branch, On<PX.Objects.GL.Branch.branchID, Equal<INSite.branchID>>,
						LeftJoin<Organization, On<Organization.organizationID, Equal<PX.Objects.GL.Branch.organizationID>>>>,
				   Where<INSite.siteID, NotEqual<SiteAttribute.transitSiteID>,
						And<Match<Current<AccessInfo.userName>>>>>.Select(this);
			}

			foreach (var result in rusultSet)
			{
				WarehouseReference warehouseReference = PXSelect<WarehouseReference,
						Where<WarehouseReference.siteID, Equal<Required<INSite.siteID>>,
							And<WarehouseReference.portalSetupID, Equal<PortalSetup.portalSiteID>>>>.
						SelectSingleBound(this, null, ((INSite)result).SiteID);
				if (warehouseReference != null)
				{
					((INSite)result).Included = true;
				}
				yield return result;
			}
		}
		#endregion

		#region Event Handler
		protected virtual void PortalSetup_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			PXUIFieldAttribute.SetEnabled<INSite.siteCD>(CRSetupINSite.Cache, null, false);
			PXUIFieldAttribute.SetEnabled<INSite.descr>(CRSetupINSite.Cache, null, false);
			PXUIFieldAttribute.SetEnabled<INSite.branchID>(CRSetupINSite.Cache, null, false);

			var row = e.Row as PortalSetup;
			if (row == null) return;

			if (sender.GetStatus(row) == PXEntryStatus.Inserted)
			{
				sender.IsDirty = true;
			}

			if (!PXAccess.FeatureInstalled<FeaturesSet.multipleUnitMeasure>())
			{
				row.BaseUOM = true;
				PXUIFieldAttribute.SetVisible<PortalSetup.baseUOM>(CRSetupRecord.Cache, null, false);
			}

			var documentRestrictionEnabled = PXAccess.FeatureInstalled<FeaturesSet.branch>();
			PXUIFieldAttribute.SetEnabled<PortalSetup.displayFinancialDocuments>(CRSetupRecord.Cache, null, documentRestrictionEnabled);
			PXUIFieldAttribute.SetEnabled<PortalSetup.restrictByOrganizationID>(CRSetupRecord.Cache, null, documentRestrictionEnabled);
			PXUIFieldAttribute.SetEnabled<PortalSetup.restrictByBranchID>(CRSetupRecord.Cache, null, documentRestrictionEnabled);

			PXUIFieldAttribute.SetVisible<PortalSetup.restrictByOrganizationID>(CRSetupRecord.Cache, null, row.DisplayFinancialDocuments == FinancialDocumentsFilterAttribute.BY_COMPANY);
			PXUIFieldAttribute.SetVisible<PortalSetup.restrictByBranchID>(CRSetupRecord.Cache, null, row.DisplayFinancialDocuments == FinancialDocumentsFilterAttribute.BY_BRANCH);

			if (row.DisplayFinancialDocuments == FinancialDocumentsFilterAttribute.BY_COMPANY && row.RestrictByOrganizationID != null)
			{
				var company = (Organization)RestrictCompany.Search<Organization.organizationID>(row.RestrictByOrganizationID);

				if (company == null)
					CRSetupRecord.Cache.RaiseExceptionHandling<PortalSetup.restrictByOrganizationID>(row, row.RestrictByOrganizationID,
					new PXSetPropertyException(Messages.CompanyIsDeleted, PXErrorLevel.Warning));
				else if (company.Active != true)
					CRSetupRecord.Cache.RaiseExceptionHandling<PortalSetup.restrictByOrganizationID>(row, row.RestrictByOrganizationID,
					new PXSetPropertyException(Messages.CompanyIsNotActive, PXErrorLevel.Warning));
				else
					CRSetupRecord.Cache.RaiseExceptionHandling<PortalSetup.restrictByOrganizationID>(row, null, null);
			}

			if (row.DisplayFinancialDocuments == FinancialDocumentsFilterAttribute.BY_BRANCH && row.RestrictByBranchID != null)
			{
				var branch = (PX.Objects.GL.Branch)RestrictBranch.Search<PX.Objects.GL.Branch.branchID>(row.RestrictByBranchID);

				if (branch == null)
					CRSetupRecord.Cache.RaiseExceptionHandling<PortalSetup.restrictByBranchID>(row, row.RestrictByBranchID,
					new PXSetPropertyException(Messages.BranchIsDeleted, PXErrorLevel.Warning));
				else if (branch.Active != true)
					CRSetupRecord.Cache.RaiseExceptionHandling<PortalSetup.restrictByBranchID>(row, row.RestrictByBranchID,
					new PXSetPropertyException(Messages.BranchIsNotActive, PXErrorLevel.Warning));
				else
					CRSetupRecord.Cache.RaiseExceptionHandling<PortalSetup.restrictByBranchID>(row, null, null);

				row.SellingBranchID = row.RestrictByBranchID;
			}
			if (row.SellingBranchID != null)
			{
				var branch =
					(PX.Objects.GL.Branch)SellingBranch.Search<PX.Objects.GL.Branch.branchID>(row.SellingBranchID);

				if (branch == null)
					CRSetupRecord.Cache.RaiseExceptionHandling<PortalSetup.sellingBranchID>(row, row.SellingBranchID,
						new PXSetPropertyException(Messages.BranchIsDeleted, PXErrorLevel.Warning));
				else if (branch.Active != true)
					CRSetupRecord.Cache.RaiseExceptionHandling<PortalSetup.sellingBranchID>(row, row.SellingBranchID,
						new PXSetPropertyException(Messages.BranchIsNotActive, PXErrorLevel.Warning));
				else
					CRSetupRecord.Cache.RaiseExceptionHandling<PortalSetup.sellingBranchID>(row, null, null);
			}

			PXUIFieldAttribute.SetEnabled<PortalSetup.sellingBranchID>(CRSetupRecord.Cache, null, row.DisplayFinancialDocuments != FinancialDocumentsFilterAttribute.BY_BRANCH);
		}

		protected virtual void INSite_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			INSite row = e.Row as INSite;
			if (row == null) return;

			if (row.Included == true)
			{
				WarehouseReference warehouseReference = new WarehouseReference();
				warehouseReference.SiteID = row.SiteID;
				warehouseReference.PortalSetupID = CRSetupRecord.Current.PortalSetupID;
				this.Caches[typeof(WarehouseReference)].Insert(warehouseReference);
			}
			else
			{
				if ((row.SiteID == CRSetupRecord.Current.DefaultNonStockItemWareHouse ||
					  row.SiteID == CRSetupRecord.Current.DefaultStockItemWareHouse))
				{
					CRSetupRecord.Ask(Messages.ErrorType, Msg.DefaultWarehouseCannotBeExcluded, MessageButtons.OK);
					row.Included = true;
					return;
				}

				WarehouseReference warehouseReference = PXSelect<WarehouseReference,
						Where<WarehouseReference.siteID, Equal<Required<INSite.siteID>>,
							 And<WarehouseReference.portalSetupID, Equal<PortalSetup.portalSiteID>>>>.
						SelectSingleBound(this, null, row.SiteID);
				if (warehouseReference != null)
				{
					this.Caches[typeof(WarehouseReference)].Delete(warehouseReference);
				}
			}
		}

		protected virtual void PortalSetup_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			var row = e.Row as PortalSetup;
			if (row == null) return;
			var oldRow = e.OldRow as PortalSetup;
			if (oldRow == null) return;

			WarehouseReference defaultNonStockItemWareHouse = PXSelect<WarehouseReference,
					Where<WarehouseReference.siteID, Equal<Required<INSite.siteID>>,
						  And<WarehouseReference.portalSetupID, Equal<PortalSetup.portalSiteID>>>>.
					SelectSingleBound(this, null, row.DefaultNonStockItemWareHouse);

			if (row.DefaultNonStockItemWareHouse != null &&
				row.DefaultNonStockItemWareHouse != oldRow.DefaultNonStockItemWareHouse &&
				defaultNonStockItemWareHouse == null)
			{
				defaultNonStockItemWareHouse = new WarehouseReference();
				defaultNonStockItemWareHouse.SiteID = row.DefaultNonStockItemWareHouse;
				defaultNonStockItemWareHouse.PortalSetupID = row.PortalSetupID;
				this.Caches[typeof(WarehouseReference)].Insert(defaultNonStockItemWareHouse);
			}

			WarehouseReference defaultStockItemWareHouse = PXSelect<WarehouseReference,
					Where<WarehouseReference.siteID, Equal<Required<INSite.siteID>>,
						  And<WarehouseReference.portalSetupID, Equal<PortalSetup.portalSiteID>>>>.
					SelectSingleBound(this, null, row.DefaultStockItemWareHouse);

			if (row.DefaultStockItemWareHouse != null &&
				row.DefaultStockItemWareHouse != oldRow.DefaultStockItemWareHouse &&
				defaultStockItemWareHouse == null)
			{
				defaultStockItemWareHouse = new WarehouseReference();
				defaultStockItemWareHouse.SiteID = row.DefaultStockItemWareHouse;
				defaultStockItemWareHouse.PortalSetupID = row.PortalSetupID;
				this.Caches[typeof(WarehouseReference)].Insert(defaultStockItemWareHouse);
			}

			if (!sender.ObjectsEqual<PortalSetup.displayFinancialDocuments,
														 PortalSetup.restrictByOrganizationID>(oldRow, row))
			{
				if (!sender.ObjectsEqual<PortalSetup.displayFinancialDocuments>(oldRow, row))
				{
					switch (row.DisplayFinancialDocuments)
					{
						case FinancialDocumentsFilterAttribute.ALL:
							sender.RaiseExceptionHandling<PortalSetup.restrictByOrganizationID>(row, null, null);
							sender.RaiseExceptionHandling<PortalSetup.restrictByBranchID>(row, null, null);
							row.RestrictByOrganizationID = null;
							row.RestrictByBranchID = null;
							break;

						case FinancialDocumentsFilterAttribute.BY_COMPANY:
							sender.RaiseExceptionHandling<PortalSetup.restrictByBranchID>(row, null, null);
							row.RestrictByBranchID = null;
							break;

						case FinancialDocumentsFilterAttribute.BY_BRANCH:
							sender.RaiseExceptionHandling<PortalSetup.restrictByOrganizationID>(row, null, null);
							row.RestrictByOrganizationID = null;
							break;
					}
					row.SellingBranchID = null;
				}
				else
				{
					row.SellingBranchID = GetDefaultSellingBranchId(row.RestrictByOrganizationID);
				}
			}
		}

		protected virtual void PortalSetup_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			var row = e.Row as PortalSetup;

			if (row.DisplayFinancialDocuments == FinancialDocumentsFilterAttribute.BY_COMPANY && row.RestrictByOrganizationID == null)
			{
				if (sender.RaiseExceptionHandling<PortalSetup.restrictByOrganizationID>(e.Row, null, new PXSetPropertyException(ErrorMessages.FieldIsEmpty, typeof(PortalSetup.restrictByOrganizationID).Name)))
				{
					throw new PXRowPersistingException(typeof(PortalSetup.restrictByOrganizationID).Name, null, ErrorMessages.FieldIsEmpty, typeof(PortalSetup.restrictByOrganizationID).Name);
				}
			}

			if (row.DisplayFinancialDocuments == FinancialDocumentsFilterAttribute.BY_BRANCH && row.RestrictByBranchID == null)
			{
				if (sender.RaiseExceptionHandling<PortalSetup.restrictByBranchID>(e.Row, null, new PXSetPropertyException(ErrorMessages.FieldIsEmpty, typeof(PortalSetup.restrictByBranchID).Name)))
				{
					throw new PXRowPersistingException(typeof(PortalSetup.restrictByBranchID).Name, null, ErrorMessages.FieldIsEmpty, typeof(PortalSetup.restrictByBranchID).Name);
				}
			}

			if (PXAccess.FeatureInstalled<FeaturesSet.b2BOrdering>())
			{
				if (row.SellingBranchID == null)
				{
					if (sender.RaiseExceptionHandling<PortalSetup.sellingBranchID>(e.Row, null,
						new PXSetPropertyException(ErrorMessages.FieldIsEmpty, typeof(PortalSetup.sellingBranchID).Name)))
					{
						throw new PXRowPersistingException(typeof(PortalSetup.sellingBranchID).Name, null,
							ErrorMessages.FieldIsEmpty, typeof(PortalSetup.sellingBranchID).Name);
					}
				}
				else
				{
					PortalSetup portalSetupSource =
						PXSelectReadonly<PortalSetup, Where<PortalSetup.IsCurrentPortal>>
						.SelectSingleBound(this, null);
					if (portalSetupSource != null && row.SellingBranchID != portalSetupSource.SellingBranchID)
					{
						PXDatabase.Delete<PortalCardLines>(new PXDataFieldRestrict(nameof(PortalCardLines.PortalNoteID),
						   PXDbType.UniqueIdentifier, null, row.NoteID, PXComp.EQ));
					}
				}
			}
		}

		protected virtual void PreferencesGeneral_PortalExternalAccessLink_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			PreferencesGeneral row = (PreferencesGeneral)e.Row;
			if (row != null)
			{
				String validexpr = "[a-zA-Z]://[a-zA-Z0-9]";
				String validexpr1 = "^\\~\\/[a-zA-Z0-9]";
				string url = (string)e.NewValue;
				Regex urlregex = new Regex(validexpr);
				Regex urlregex1 = new Regex(validexpr1);

				if (!String.IsNullOrEmpty(url))
				{
					Match m = urlregex.Match(url);
					Match m1 = urlregex1.Match(url);
					if (!m.Success && !m1.Success)
					{
						throw new PXException(ErrorMessages.UrlValidation, url);
					}
				}
			}
		}

		protected virtual void PreferencesGeneral_PortalHomePage_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			PreferencesGeneral row = (PreferencesGeneral)e.Row;
			if (row != null)
			{
				if (e.NewValue != null)
				{
					PXSiteMapNode selectednode = PXSiteMap.Provider.FindSiteMapNodeFromKey((Guid)e.NewValue);
					if (selectednode != null)
					{
						WikiDescriptor wd = PXSelect<WikiDescriptor,
						Where<WikiDescriptor.pageID, Equal<Required<WikiDescriptor.pageID>>>>.Select(this, selectednode.NodeID);
						if (wd != null)
						{
							sender.RaiseExceptionHandling<PreferencesGeneral.portalHomePage>(e.Row, row.PortalHomePage, new PXSetPropertyException(Messages.NotValidStartPage, PXErrorLevel.Error, row.PortalHomePage));
						}
					}
				}
			}
		}
		protected virtual void PortalSetup_DefaultOrderType_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			var row = (PortalSetup)e.Row;
			if (row != null && !PXAccess.FeatureInstalled<FeaturesSet.inventory>() && PXAccess.FeatureInstalled<FeaturesSet.b2BOrdering>())
			{
				e.NewValue = null;
				e.Cancel = true;
			}
		}
		protected virtual void PortalSetup_PortalSetupID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			var row = (PortalSetup)e.Row;
			if (row != null)
			{
				e.NewValue = WebConfig.PortalSiteID;
				e.Cancel = true;
			}
		}

		protected virtual void PortalSetup_AvailableQty_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			var row = (PortalSetup)e.Row;
			if (row != null && !PXAccess.FeatureInstalled<FeaturesSet.inventory>() && PXAccess.FeatureInstalled<FeaturesSet.b2BOrdering>())
			{
				e.NewValue = false;
				e.Cancel = true;
			}
		}

		#endregion

		#region Action
		public PXSave<PortalSetup> Save;
		public PXCancel<PortalSetup> Cancel;
		#endregion

		#region CacheAttached
		[PXMergeAttributes]
		[PXSelector(typeof(Search5<GL.DAC.Organization.organizationID,
			LeftJoin<PX.Objects.GL.Branch, On<PX.Objects.GL.Branch.organizationID, Equal<GL.DAC.Organization.organizationID>, And<GL.Branch.active, Equal<boolTrue>>>>,
			Where<GL.DAC.Organization.active, Equal<boolTrue>, And<Where<Organization.organizationType, Equal<OrganizationTypes.withoutBranches>, Or<Branch.branchID, IsNotNull>>>>,
			Aggregate<GroupBy<Organization.organizationID>>>),
			typeof(GL.DAC.Organization.organizationCD),
			typeof(GL.DAC.Organization.organizationName),
			SubstituteKey = typeof(GL.DAC.Organization.organizationCD),
			DescriptionField = typeof(GL.DAC.Organization.organizationName))]
		[PXRestrictor(typeof(Where<GL.DAC.Organization.active, Equal<boolTrue>>), Messages.CompanyIsNotActive, typeof(GL.DAC.Organization.organizationName))]
		protected virtual void PortalSetup_RestrictByOrganizationID_CacheAttached(PXCache sender)
		{
		}
		#endregion

		#region CacheAttached
		[PXMergeAttributes]
		[PXUIField(DisplayName = "Include in Warehouses List")]
		protected virtual void INSite_Included_CacheAttached(PXCache e)
		{
		}

		[PXMergeAttributes]
		[PXUIField(DisplayName = "Company")]
		protected virtual void Organization_OrganizationCD_CacheAttached(PXCache e)
		{
		}
		#endregion

		#region Methods
		public static int? GetDefaultSellingBranchId(int? restrictByOrganizationId)
		{
			var resultSet = PXSelectJoin<Organization,
				   InnerJoin<GL.Branch, On<GL.Branch.organizationID, Equal<Organization.organizationID>>>,
				   Where<Organization.organizationID, Equal<Required<PortalSetup.restrictByOrganizationID>>>>
				.SelectSingleBound(new PXGraph(), null, restrictByOrganizationId);
			if (resultSet.Count > 0)
			{
				var organization = PXResult.Unwrap<Organization>(resultSet[0]);

				if (organization != null && organization.OrganizationType.Equals(OrganizationTypes.WithoutBranches))
				{
					var branch = PXResult.Unwrap<GL.Branch>(resultSet[0]);
					return branch?.BranchID;
				}
			}

			return null;
		}
		#endregion
	}

	[Obsolete("Use WebConfig.PortalSiteID for string and PortalSetup.portalSiteID for BqlConst.")]
	public class WebSiteConfig
	{
		public static string WebSiteID => WebConfig.PortalSiteID;

		public class webSiteID : BqlString.Constant<webSiteID>
		{
			public webSiteID() : base(WebSiteID) { }
		}
	}
}
