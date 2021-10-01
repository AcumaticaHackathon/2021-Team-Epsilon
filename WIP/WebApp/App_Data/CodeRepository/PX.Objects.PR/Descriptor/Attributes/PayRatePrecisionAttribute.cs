using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PR
{
	public class PayRatePrecisionAttribute : PXEventSubscriberAttribute
	{
		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);

			short? payRateDecimalPlaces = PRSetupHelper.GetPayrollPreferences(sender.Graph)?.PayRateDecimalPlaces;

			if (payRateDecimalPlaces != null)
				PXDBDecimalAttribute.SetPrecision(sender, FieldName, payRateDecimalPlaces);
		}
	}
}
