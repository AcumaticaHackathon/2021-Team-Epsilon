using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Permissions;
using PX.Objects.SP.DAC;
using PX.SM;
using PX.Data;
using SP.Objects.SP;


namespace SP.Objects
{
	public class SMAccessPersonalMaintExt : PXGraphExtension<SMAccessPersonalMaint>
	{
		public delegate int? GetDefaultBranchIdDel1(string username, string companyId);

		[SecurityPermission(SecurityAction.Assert, Unrestricted = true)]
		[PXOverride]
		public virtual int? GetDefaultBranchId(string username, string companyId, GetDefaultBranchIdDel1 baseMethod)
		{
			var setup = PortalSetup.Current;
			if (setup != null)
			{
				if (setup.SellingBranchID != null)
				{
					return setup.SellingBranchID;
				}
				else if (setup.DisplayFinancialDocuments == FinancialDocumentsFilterAttribute.BY_BRANCH)
				{
					return setup.RestrictByBranchID;
				}
			}

			return baseMethod(username, companyId);
		}

		public delegate int? GetDefaultBranchIdDel2();

		[PXOverride]
		public virtual int? GetDefaultBranchId(GetDefaultBranchIdDel2 baseMethod)
		{
			var setup = PortalSetup.Current;
			if (setup != null)
			{
				if (setup.SellingBranchID != null)
				{
					return setup.SellingBranchID;
				}
				else if (setup.DisplayFinancialDocuments == FinancialDocumentsFilterAttribute.BY_BRANCH)
				{
					return setup.RestrictByBranchID;
				}
			}

			return baseMethod();
		}
	}
}