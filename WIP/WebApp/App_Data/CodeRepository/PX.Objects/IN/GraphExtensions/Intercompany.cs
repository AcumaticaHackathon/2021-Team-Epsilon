using System;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.SO;

namespace PX.Objects.IN.GraphExtensions
{
	public class Intercompany : PXGraphExtension<INReleaseProcess>
	{
		public static bool IsActive()
			=> PXAccess.FeatureInstalled<FeaturesSet.interBranch>()
			&& PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();

		[PXOverride]
		public virtual int? GetCogsAcctID(InventoryItem item, INSite site, INPostClass postclass, INTran tran, bool useTran,
			Func<InventoryItem, INSite, INPostClass, INTran, bool, int?> baseFunc)
		{
			if (tran.BAccountID != null && tran.SOOrderType != null)
			{
				Customer customer = Customer.PK.Find(Base, tran.BAccountID);
				if (customer != null && customer.IsBranch == true)
				{			
					SOOrderType ordertype = SOOrderType.PK.Find(Base, tran.SOOrderType);
					if (ordertype != null)
					{
						switch (ordertype.IntercompanyCOGSAcctDefault)
						{
							case SOIntercompanyAcctDefault.MaskItem:
								return item?.COGSAcctID;
							case SOIntercompanyAcctDefault.MaskLocation:
								if (customer.COGSAcctID == null)
								{
									throw new PXException(Messages.CustomerGOGSAccountIsEmpty);
								}
								return customer.COGSAcctID;
						}
					}
					return null;
				}
			}

			return baseFunc(item, site, postclass, tran, useTran);
		}
	}
}
