using System;

namespace PX.Objects.PR
{
	/// <summary>
	/// PR and wildcard, to use with Like BQL operator
	/// </summary>
	public class pr_ : PX.Data.BQL.BqlString.Constant<pr_>
	{
		public pr_() : base(PRWildCard) { }
		public const string PRWildCard = "PR%";
	}

	public class LocationConstants
	{
		public class Federal : PX.Data.BQL.BqlString.Constant<Federal>
		{
			public Federal() : base(FederalStateCode) { }
		}

		// ToDo: AC-138220, In the Payroll Phase 2 review all the places where the country is set to "US" by the default
		public class CountryUS : PX.Data.BQL.BqlString.Constant<CountryUS>
		{
			public CountryUS() : base(USCountryCode) { }
		}

		public const string FederalStateCode = "FED";

		public const string USCountryCode = "US";
		public const string CanadaCountryCode = "CA";

		public const string AlaskaStateAbbr = "AK";
		public const string PuertoRicoStateAbbr = "PR";
	}

	internal static class PRSubAccountMaskConstants
	{
		public const string EarningMaskName = "PREarnings";
		public const string DeductionMaskName = "PRDeductions";
		public const string BenefitExpenseMaskName = "PRBenefitExpense";
		public const string TaxMaskName = "PRTaxes";
		public const string TaxExpenseMaskName = "PRTaxeExpense";
	}

	public static class PRQueryParameters
	{
		public const string DownloadAuf = "DbgDownloadAuf";
	}

	internal static class PRFileNames
	{
		public const string Auf = "auf.txt";
	}

	public static class PayStubsDirectDepositReportParameters
	{
		public const string ReportID = "PR641015";
		public const string BatchNbr = "DDBatchID";
	}

	public class GLAccountSubSource
	{
		public const string Branch = "B";
		public const string Employee = "E";
		public const string DeductionCode = "D";
		public const string LaborItem = "L";
		public const string PayGroup = "P";
		public const string EarningType = "T";
		public const string TaxCode = "X";
	}

	public static class DateConstants
	{
		public const byte WeeksPerYear = 52;
	}
	
	public static class WebserviceContants
	{
		// This corresponds to the maxReceivedMessageSize of the binding used by the server, defined in the WS's web.config
		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)]
		public const int MaxRequestLength = 65536;
		// An estimated ratio of the payload portion of the total SOAP envelop used for WCF communication to the WS
		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)]
		public const double PayloadRatio = 0.9;

		// Has to match the first value defined in PX.Payroll.Data.AatrixPredefinedFields in the payrollws repo
		public const int FirstAatrixPredefinedField = 4000;

		// Has to be greater than the last value defined in PX.Payroll.Data.AatrixPredefinedFields in the payrollws repo
		public const int LastAatrixPredefinedField = 4999;
		
		public const string IncludeRailroadTaxesSetting = "IncludeRailroadTaxes";
		public const string CompanyWagesYtdSetting = "CompanyWagesYtd";
		public const string CompanyWagesQtdSetting = "CompanyWagesQtd";
	}
}