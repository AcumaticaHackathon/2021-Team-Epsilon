using CommonServiceLocator;
using PX.Data;
using PX.Objects.CA.Descriptor;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.PO;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PM
{
    public class CurrencySettings
	{
		public bool UseBaseCurrency { get; private set; }
		public bool ConverionRequired { get; private set; }

		public string FromCuryID { get; private set; }
		public string ToCuryID { get; private set; }

		public string RateTypeID { get; private set; }

		public CurrencySettings(bool useBaseCurrency, bool conversionRequired, string fromCuryID, string toCuryID, string rateTypeID)
		{
			if (conversionRequired)
			{
				if (string.IsNullOrEmpty(rateTypeID))
					throw new ArgumentException("Conversion is required but no rate type was passed.", nameof(rateTypeID));

				if (string.IsNullOrEmpty(fromCuryID))
					throw new ArgumentException("Conversion is required but no fromCuryID was passed.", nameof(fromCuryID));

				if (string.IsNullOrEmpty(toCuryID))
					throw new ArgumentException("Conversion is required but no toCuryID was passed.", nameof(toCuryID));
			}

			this.UseBaseCurrency = useBaseCurrency;
			this.ConverionRequired = conversionRequired;
			this.FromCuryID = fromCuryID;
			this.ToCuryID = toCuryID;
			this.RateTypeID = rateTypeID;
		}
	}
}