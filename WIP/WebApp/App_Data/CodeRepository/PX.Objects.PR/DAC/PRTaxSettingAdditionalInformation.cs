using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Payroll.Data;
using System;

namespace PX.Objects.PR
{
	[PXCacheName(Messages.PRTaxSettingAdditionalInformation)]
	[Serializable]
	public class PRTaxSettingAdditionalInformation : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<PRTaxSettingAdditionalInformation>.By<settingName, state, countryID>
		{
			public static PRTaxSettingAdditionalInformation Find(PXGraph graph, string settingName, string state, string countryID) => 
				FindBy(graph, settingName, state, countryID);
		}
		#endregion

		#region TypeName
		public abstract class typeName : PX.Data.BQL.BqlString.Field<typeName> { }
		[PXDBString(50, IsUnicode = true)]
		[PXDefault]
		public virtual string TypeName { get; set; }
		#endregion
		#region SettingName
		public abstract class settingName : PX.Data.BQL.BqlString.Field<settingName> { }
		[PXDBString(255, IsKey = true, IsUnicode = true)]
		[PXDefault]
		public virtual string SettingName { get; set; }
		#endregion
		#region AdditionalInformation
		[PXDBString(2048, IsUnicode = true)]
		public virtual string AdditionalInformation { get; set; }
		public abstract class additionalInformation : PX.Data.BQL.BqlString.Field<additionalInformation> { }
		#endregion
		#region UsedForSymmetry
		[PXDBBool]
		[PXDefault(false)]
		public virtual bool? UsedForSymmetry { get; set; }
		public abstract class usedForSymmetry : PX.Data.BQL.BqlBool.Field<usedForSymmetry> { }
		#endregion
		#region FormBox
		[PXDBString(255, IsUnicode = true)]
		public virtual string FormBox { get; set; }
		public abstract class formBox : PX.Data.BQL.BqlString.Field<formBox> { }
		#endregion
		#region State
		[PXDBString(3, IsUnicode = true, IsKey = true)]
		[PXDefault(TaxSettingAdditionalInformationKey.StateFallback)]
		public virtual string State { get; set; }
		public abstract class state : PX.Data.BQL.BqlString.Field<state> { }
		#endregion
		#region CountryID
		[PXDBString(2, IsFixed = true, IsKey = true)]
		[PXDefault]
		public virtual string CountryID { get; set; }
		public abstract class countryID : PX.Data.BQL.BqlString.Field<countryID> { }
		#endregion

		#region System Columns
		#region TStamp
		public class tStamp : IBqlField { }
		[PXDBTimestamp()]
		public byte[] TStamp { get; set; }
		#endregion
		#region CreatedByID
		public class createdByID : IBqlField { }
		[PXDBCreatedByID()]
		public Guid? CreatedByID { get; set; }
		#endregion
		#region CreatedByScreenID
		public class createdByScreenID : IBqlField { }
		[PXDBCreatedByScreenID()]
		public string CreatedByScreenID { get; set; }
		#endregion
		#region CreatedDateTime
		public class createdDateTime : IBqlField { }
		[PXDBCreatedDateTime()]
		public DateTime? CreatedDateTime { get; set; }
		#endregion
		#region LastModifiedByID
		public class lastModifiedByID : IBqlField { }
		[PXDBLastModifiedByID()]
		public Guid? LastModifiedByID { get; set; }
		#endregion
		#region LastModifiedByScreenID
		public class lastModifiedByScreenID : IBqlField { }
		[PXDBLastModifiedByScreenID()]
		public string LastModifiedByScreenID { get; set; }
		#endregion
		#region LastModifiedDateTime
		public class lastModifiedDateTime : IBqlField { }
		[PXDBLastModifiedDateTime()]
		public DateTime? LastModifiedDateTime { get; set; }
		#endregion
		#endregion
	}
}