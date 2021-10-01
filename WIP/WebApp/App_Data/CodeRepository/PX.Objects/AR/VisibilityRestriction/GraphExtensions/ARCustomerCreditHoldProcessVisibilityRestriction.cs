using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.AR
{
	public class ARCustomerCreditHoldProcessVisibilityRestriction : PXGraphExtension<ARCustomerCreditHoldProcess>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.visibilityRestriction>();
		}

		public delegate PXResultset<Customer> GetCustomersToProcessDelegate(ARCustomerCreditHoldProcess.CreditHoldParameters header);

		[PXOverride]
		public PXResultset<Customer> GetCustomersToProcess(ARCustomerCreditHoldProcess.CreditHoldParameters header,
			GetCustomersToProcessDelegate baseMethod)
		{
			if (header.Action?.EndsWith(nameof(CustomerMaint.PutOnCreditHold)) == true)
			{
				return PXSelectJoin<Customer,
					InnerJoin<ARDunningLetter, On<Customer.bAccountID, Equal<ARDunningLetter.bAccountID>,
						And<ARDunningLetter.lastLevel, Equal<True>,
						And<ARDunningLetter.released, Equal<True>,
						And<ARDunningLetter.voided, NotEqual<True>>>>>>,
					Where<ARDunningLetter.dunningLetterDate,
						Between<Required<ARDunningLetter.dunningLetterDate>, Required<ARDunningLetter.dunningLetterDate>>,
						And<Customer.cOrgBAccountID, RestrictByUserBranches<Current<AccessInfo.userName>>>>,
					OrderBy<Asc<ARDunningLetter.bAccountID>>>.Select(Base, header.BeginDate, header.EndDate);
			}
			else if (header.Action?.EndsWith(nameof(CustomerMaint.RemoveCreditHold)) == true)
			{
				PXSelectBase<Customer> select = new PXSelectJoin<Customer,
					LeftJoin<ARDunningLetter, On<Customer.bAccountID, Equal<ARDunningLetter.bAccountID>,
						And<ARDunningLetter.lastLevel, Equal<True>,
						And<ARDunningLetter.released, Equal<True>,
						And<ARDunningLetter.voided, NotEqual<True>>>>>>,
					Where<Customer.cOrgBAccountID, RestrictByUserBranches<Current<AccessInfo.userName>>>>(Base);
				if (PXAccess.FeatureInstalled<FeaturesSet.parentChildAccount>())
				{
					select.WhereAnd<Where<Customer.bAccountID, Equal<Customer.sharedCreditCustomerID>>>();
				}
				return select.Select();
			}

			return new PXResultset<Customer>();
		}
	}
}