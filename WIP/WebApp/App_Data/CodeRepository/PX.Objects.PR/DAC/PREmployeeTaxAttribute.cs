using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Payroll.Data;
using System;

namespace PX.Objects.PR
{
	[PXCacheName(Messages.PREmployeeTaxAttribute)]
	[Serializable]
	public class PREmployeeTaxAttribute : IBqlTable, IPRSetting, IStateSpecific
	{
		#region Keys
		public class PK : PrimaryKeyOf<PREmployeeTaxAttribute>.By<bAccountID, taxID, typeName, settingName>
		{
			public static PREmployeeTaxAttribute Find(PXGraph graph, int? bAccountID, int? taxID, string typeName, string settingName) =>
				FindBy(graph, bAccountID, taxID, typeName, settingName);
		}

		public static class FK
		{
			public class Employee : PREmployee.PK.ForeignKeyOf<PREmployeeTaxAttribute>.By<bAccountID> { }
			public class TaxCode : PRTaxCode.PK.ForeignKeyOf<PREmployeeTaxAttribute>.By<taxID> { }
			public class TaxCodeAttribute : PRTaxCodeAttribute.PK.ForeignKeyOf<PREmployeeTaxAttribute>.By<taxID, typeName, settingName> { }
		}
		#endregion

		#region BAccountID
		public abstract class bAccountID : PX.Data.BQL.BqlInt.Field<bAccountID> { }
		[PXDBInt(IsKey = true)]
		[PXDefault(typeof(PREmployee.bAccountID))]
		[PXParent(typeof(Select<PREmployee, Where<PREmployee.bAccountID, Equal<Current<PREmployeeTaxAttribute.bAccountID>>>>))]
		public virtual int? BAccountID { get; set; }
		#endregion
		#region TaxID
		public abstract class taxID : PX.Data.BQL.BqlInt.Field<taxID> { }
		[PXDBInt(IsKey = true)]
		[PXDBDefault(typeof(PREmployeeTax.taxID))]
		[PXParent(typeof(Select<PREmployeeTax, Where<PREmployeeTax.bAccountID, Equal<Current<PREmployeeTaxAttribute.bAccountID>>, And<PREmployeeTax.taxID, Equal<Current<PREmployeeTaxAttribute.taxID>>>>>))]
		[PXParent(typeof(FK.TaxCode))]
		public virtual int? TaxID { get; set; }
		#endregion
		#region TypeName
		public abstract class typeName : PX.Data.BQL.BqlString.Field<typeName> { }
		[PXDBString(50, IsKey = true, IsUnicode = true)]
		[PXUIField(DisplayName = "Type", Visible = false, Enabled = false)]
		[PXDefault]
		public virtual string TypeName { get; set; }
		#endregion
		#region SettingName
		public abstract class settingName : PX.Data.BQL.BqlString.Field<settingName> { }
		[PXDBString(255, IsKey = true, IsUnicode = true)]
		[PXDefault()]
		[PXUIField(DisplayName = "Setting", Visible = false, Enabled = false)]
		[PXSelector(typeof(Search<PRTaxCodeAttribute.settingName, Where<PRTaxCodeAttribute.taxID, Equal<Current<PREmployeeTaxAttribute.taxID>>, And<PRTaxCodeAttribute.typeName, Equal<Current<PREmployeeTaxAttribute.typeName>>>>>))]
		public virtual string SettingName { get; set; }
		#endregion
		#region Description
		public abstract class description : PX.Data.BQL.BqlString.Field<description> { }
		[PXString(60, IsUnicode = true)]
		[PXUIField(DisplayName = "Name", Enabled = false, Visibility = PXUIVisibility.SelectorVisible)]
		public virtual string Description { get; set; }
		#endregion
		#region Value
		public abstract class value : PX.Data.BQL.BqlString.Field<value> { }
		[PXDBString(255, IsUnicode = true)]
		[PXUIField(DisplayName = "Value")]
		[PXUIEnabled(typeof(Where<PREmployeeTaxAttribute.useDefault, Equal<False>>))]
		public virtual string Value { get; set; }
		#endregion
		#region UseDefault
		public abstract class useDefault : PX.Data.BQL.BqlBool.Field<useDefault> { }
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Use Default")]
		[PXUIEnabled(typeof(PRTaxCodeAttribute.allowOverride))]
		public virtual bool? UseDefault { get; set; }
		#endregion
		#region AllowOverride
		public abstract class allowOverride : PX.Data.BQL.BqlBool.Field<allowOverride> { }
		[PXBool]
		[PXUIField(DisplayName = "Allow Employee Override", Enabled = false, Visible = false)]
		public virtual bool? AllowOverride { get; set; }
		#endregion
		#region SortOrder
		public abstract class sortOrder : PX.Data.BQL.BqlInt.Field<sortOrder> { }
		[PXInt]
		[PXUIField(DisplayName = "Sort Order")]
		public virtual int? SortOrder { get; set; }
		#endregion
		#region Required
		public abstract class required : PX.Data.BQL.BqlBool.Field<required> { }
		[PXBool]
		[PXUIField(DisplayName = "Required", Enabled = false, Visible = false)]
		public virtual bool? Required { get; set; }
		#endregion
		#region AatrixMapping
		public abstract class aatrixMapping : PX.Data.BQL.BqlInt.Field<aatrixMapping> { }
		[PXDBInt]
		[PXUIField(Visible = false)]
		public virtual int? AatrixMapping { get; set; }
		#endregion
		#region AdditionalInformation
		[PXDBString(2048, IsUnicode = true)]
		[PXUIField(DisplayName = "Additional Information", Enabled = false)]
		public virtual string AdditionalInformation { get; set; }
		public abstract class additionalInformation : PX.Data.BQL.BqlString.Field<additionalInformation> { }
		#endregion
		#region FormBox
		[PXDBString(255, IsUnicode = true)]
		[PXUIField(DisplayName = "Form/Box", Enabled = false)]
		public virtual string FormBox { get; set; }
		public abstract class formBox : PX.Data.BQL.BqlString.Field<formBox> { }
		#endregion
		#region State
		[PXString(3, IsUnicode = true)]
		[TaxAttributeState]
		public virtual string State { get; set; }
		public abstract class state : PX.Data.BQL.BqlString.Field<state> { }
		#endregion
		#region CompanyNotes
		[PXString]
		[PXUIField(DisplayName = "Company Notes", Enabled = false)]
		[CompanyNoteText(typeof(settingName))]
		public virtual string CompanyNotes { get; set; }
		public abstract class companyNotes : PX.Data.BQL.BqlString.Field<companyNotes> { }
		#endregion
		#region NoteID
		[PXNote]
		public virtual Guid? NoteID { get; set; }
		public abstract class noteID : IBqlField { }
		#endregion
		#region ErrorLevel
		public abstract class errorLevel : PX.Data.BQL.BqlInt.Field<errorLevel> { }
		[PXInt]
		[PXFormula(null, typeof(MaxCalc<PREmployeeTax.errorLevel>))]
		public virtual int? ErrorLevel { get; set; }
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


