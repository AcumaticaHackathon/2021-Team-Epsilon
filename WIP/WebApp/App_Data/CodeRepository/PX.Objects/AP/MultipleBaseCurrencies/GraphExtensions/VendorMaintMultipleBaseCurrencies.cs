using System.Collections;
using System.Linq;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.Common;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.CA;
using CRLocation = PX.Objects.CR.Standalone.Location;

namespace PX.Objects.AP
{
	public class VendorMaintMultipleBaseCurrencies : PXGraphExtension<VendorMaint>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		protected virtual void _(Events.RowSelected<Vendor> e)
		{
			if (e.Row == null)
				return;

			PXUIFieldAttribute.SetRequired<Vendor.vOrgBAccountID>(e.Cache, PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>());
		}

		protected virtual void _(Events.FieldVerifying<Vendor, Vendor.vOrgBAccountID> e)
		{

			if (e.NewValue != null && (int)e.NewValue != 0
				&& e.Row.BaseCuryID != null
				&& e.Row.BaseCuryID != PXOrgAccess.GetBaseCuryID((int)e.NewValue)
				&& SelectFrom<APHistory>
					.Where<APHistory.vendorID.IsEqual<Vendor.bAccountID.FromCurrent>>
					.View.SelectSingleBound(Base, new object[]{e.Row}, null).Any()
			)
			{

				e.NewValue = PXOrgAccess.GetCD((int)e.NewValue);

				throw new PXSetPropertyException(Messages.EntityCannotBeAssociated, PXErrorLevel.Error,
						e.Row.BaseCuryID,
						e.Row.AcctCD);
			}
		}

		protected virtual void _(Events.FieldUpdated<Vendor, Vendor.vOrgBAccountID> e)
		{
			e.Row.BaseCuryID = PXOrgAccess.GetBaseCuryID(e.Row.VOrgBAccountID) ?? Base.Accessinfo.BaseCuryID;
		}

		public void _(Events.RowUpdated<Vendor> e)
		{
			if (e.OldRow.BaseCuryID != e.Row.BaseCuryID)
			{
				foreach (CRLocation location in Base.GetExtension<VendorMaint.LocationDetailsExt>().Locations.Select())
				{
					location.VCashAccountID = null;
					if (Base.Caches<CRLocation>().GetStatus(location) == PXEntryStatus.Notchanged)
						Base.Caches<CRLocation>().MarkUpdated(location);
				}
			}
		}
	}
}
