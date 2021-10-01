using System.Collections.Generic;
using System.Linq;

using PX.Data;
using PX.Objects.Common.Extensions;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.GL.DAC;
using CRLocation = PX.Objects.CR.Standalone.Location;

namespace PX.Objects.AP
{
	public class VendorMaintVisibilityRestriction: PXGraphExtension<VendorMaint>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		public override void Initialize()
		{
			base.Initialize();

			Base.BAccount.WhereAnd<Where<Vendor.vOrgBAccountID, RestrictByUserBranches<Current<AccessInfo.userName>>>>();
		}

		[PXHidden]
		public PXSelect<CRLocation> DummyLocations;

		public void _(Events.RowPersisting<Location> e)
		{
			if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Update)
			{
				//Persist only Location table because the projection Location may have
				//duplicate Address part when IsAddressSameAsMain == true
				CRLocation loc = Common.Utilities.Clone<Location, CRLocation>(Base, e.Row);
				DummyLocations.Cache.MarkUpdated(loc);
				e.Cancel = true;
			}
		}

		public void _(Events.CommandPreparing<CRLocation.noteID> e)
		{
			if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Update)
			{
				e.Args.ExcludeFromInsertUpdate();
			}
		}

		public void _(Events.RowUpdating<Vendor> e)
		{
			ResetLocationBranch = e.Row.VOrgBAccountID != e.NewRow.VOrgBAccountID;
			if (e.Row.VOrgBAccountID != 0 && e.NewRow.VOrgBAccountID == 0 &&
				Base.GetExtension<VendorMaint.LocationDetailsExt>().Locations.Select<CRLocation>().ToList().Any(l => l.VBranchID != null))
			{
				ResetLocationBranch =
					Base.GetExtension<VendorMaint.LocationDetailsExt>().Locations.Ask(Messages.Warning, Messages.KeepLocationBranchConfirmation, MessageButtons.YesNo) ==
					WebDialogResult.No;
			}
		}

		private bool? ResetLocationBranch = null;
		public void _(Events.RowUpdated<Vendor> e)
		{
			if (ResetLocationBranch != true) return;

			var branchList = PXAccess.GetOrganizationByBAccountID(e.Row.VOrgBAccountID)?.ChildBranches ??
							PXAccess.GetBranchByBAccountID(e.Row.VOrgBAccountID)?.SingleToList();

			var branches = branchList != null
				? new HashSet<int>(branchList.Select(_ => _.BranchID))
				: new HashSet<int>();

			var defaultBranch = branches.Count == 1
					? (int?)branches.First()
					: null;

			foreach (var location in Base.GetExtension<VendorMaint.LocationDetailsExt>().Locations.Select()
					.ToList()
					.RowCast<CRLocation>()
					.Where(l => defaultBranch != null || (l.VBranchID != null && !branches.Contains(l.VBranchID.Value))))
			{
				location.VBranchID = defaultBranch;
				if (Base.Caches<CRLocation>().GetStatus(location) == PXEntryStatus.Notchanged)
					Base.Caches<CRLocation>().MarkUpdated(location);
			}
		}
		
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		//have to join Vendor table for restrictor message parameter
		[Branch(searchType: typeof(Search2<Branch.branchID,
				InnerJoin<Organization,
					On<Branch.organizationID, Equal<Organization.organizationID>>,
					LeftJoin<Vendor,
						On<Vendor.bAccountID, Equal<Current<Vendor.bAccountID>>>>>,
				Where<MatchWithBranch<Branch.branchID>>>),
			useDefaulting: false,
			IsDetail = false,
			DisplayName = "Default Branch",
			BqlField = typeof(CRLocation.vBranchID),
			PersistingCheck = PXPersistingCheck.Nothing,
			IsEnabledWhenOneBranchIsAccessible = true)]
		[PXRestrictor(typeof(Where<Branch.branchID, Inside<Current<Vendor.vOrgBAccountID>>>),
			Messages.BranchRestrictedByVendor, new[] { typeof(Vendor.acctCD), typeof(Branch.branchCD) })]
		[PXDefault(typeof(Search<Branch.branchID,
				Where<Branch.bAccountID, Equal<Current<Vendor.vOrgBAccountID>>>>),
			PersistingCheck = PXPersistingCheck.Nothing)]
		public void _(Events.CacheAttached<CRLocation.vBranchID> e)
		{
		}
	}
}
