using PX.Data;

namespace PX.Objects.Common
{
    public static class PXOrgAccess
    {
        public static string GetBaseCuryID(string bAccountCD)
        {
            var cd = bAccountCD?.Trim();

            return string.IsNullOrEmpty(bAccountCD)
                ? null
                : PXAccess.GetBranch(PXAccess.GetBranchID(bAccountCD))?.BaseCuryID ??
                  PXAccess.GetOrganizationByID(PXAccess.GetOrganizationID(bAccountCD))?.BaseCuryID;
        }
        
        public static string GetBaseCuryID(int? bAccountID) =>
            bAccountID == null
                ? null
                : PXAccess.GetBranchByBAccountID(bAccountID)?.BaseCuryID ??
                  PXAccess.GetOrganizationByBAccountID(bAccountID)?.BaseCuryID;

        public static string GetCD(int? bAccountID) =>
            bAccountID == null
                ? null
                : PXAccess.GetBranchByBAccountID(bAccountID)?.BranchCD ??
                  PXAccess.GetOrganizationByBAccountID(bAccountID)?.OrganizationCD;
    }
}