using System;
using System.Collections.Generic;
using System.Text;
using PX.Data;
using PX.Objects.GL;
using PX.Objects.GL.DAC;

namespace PX.Objects.CM
{
	public class CMSetupMaint : PXGraph<CMSetupMaint>
	{
		public PXSelect<CMSetup> cmsetup;
		public PXSave<CMSetup> Save;
		public PXCancel<CMSetup> Cancel;
		public PXSelectJoin<Currency, 
			InnerJoin<Organization, On<Organization.baseCuryID, Equal<Currency.curyID>>,
				InnerJoin<Branch, On<Branch.organizationID, Equal<Organization.organizationID>>>>,
			Where<Branch.branchID, Equal<Current<AccessInfo.branchID>>>> basecurrency;
		public PXSetup<Company> company;
		public PXSelect<TranslDef, Where<TranslDef.translDefId, Equal<Current<CMSetup.translDefId>>>> baseTranslDef;

		public CMSetupMaint()
		{
			Company setup = company.Current;
			if (string.IsNullOrEmpty(setup.BaseCuryID))
			{
                throw new PXSetupNotEnteredException(ErrorMessages.SetupNotEntered, typeof(Company), PXMessages.LocalizeNoPrefix(CS.Messages.BranchMaint));
			}
		}
	}
}
