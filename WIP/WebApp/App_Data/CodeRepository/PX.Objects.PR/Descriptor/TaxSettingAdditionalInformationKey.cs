using PX.Payroll.Data;
using System;
using System.Collections.Generic;

namespace PX.Objects.PR
{
	public class TaxSettingAdditionalInformationKey : IEquatable<TaxSettingAdditionalInformationKey>
	{
		public const string StateFallback = "DEF";

		public string SettingName { get; set; }
		public string State { get; set; }
		public string CountryID { get; set; }

		public TaxSettingAdditionalInformationKey(PRTaxSettingAdditionalInformation dbRecord)
		{
			SettingName = dbRecord.SettingName;
			State = dbRecord.State == StateFallback ? string.Empty : dbRecord.State;
			CountryID = dbRecord.CountryID;
		}

		public TaxSettingAdditionalInformationKey(TaxSettingDescription csvRecord)
		{
			SettingName = csvRecord.SettingName;
			State = csvRecord.State;
			CountryID = csvRecord.CountryID;
		}

		public TaxSettingAdditionalInformationKey(IPRSetting setting, bool checkState)
		{
			SettingName = setting.SettingName;
			CountryID = LocationConstants.USCountryCode;
			if (checkState && setting is IStateSpecific stateSpecific)
			{
				State = stateSpecific.State;
			}
		}

		public bool Equals(TaxSettingAdditionalInformationKey other)
		{
			return SettingName == other.SettingName
				&& (State == other.State || string.IsNullOrEmpty(State) && string.IsNullOrEmpty(other.State))
				&& CountryID == other.CountryID;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + SettingName.GetHashCode();
				hash = hash * 23 + (string.IsNullOrEmpty(State) ? 0 : State.GetHashCode());
				hash = hash * 23 + CountryID.GetHashCode();
				return hash;
			}
		}
	}
}
