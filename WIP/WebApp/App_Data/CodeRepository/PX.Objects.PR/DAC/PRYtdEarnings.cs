using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.EP;
using System;

namespace PX.Objects.PR
{
	[PXCacheName(Messages.PRYtdEarnings)]
	[Serializable]
	[PXAccumulator(new Type[] { typeof(PRYtdEarnings.amount) }, new Type[] { typeof(PRYtdEarnings.amount) }, SingleRecord = true)]
	public class PRYtdEarnings : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<PRYtdEarnings>.By<month, year, employeeID, typeCD, locationID>
		{
			public static PRYtdEarnings Find(PXGraph graph, int month, string year, int? employeeID, string typeCD, int? locationID) =>
				FindBy(graph, month, year, employeeID, typeCD, locationID);
		}

		public static class FK
		{
			public class Employee : PREmployee.PK.ForeignKeyOf<PRYtdEarnings>.By<employeeID> { }
			public class EarningType : EPEarningType.PK.ForeignKeyOf<PRYtdEarnings>.By<typeCD> { }
			public class Location : PRLocation.PK.ForeignKeyOf<PRYtdEarnings>.By<locationID> { }
		}
		#endregion

		#region Month
		[PXDBInt(IsKey = true)]
		[PXUIField(DisplayName = "Month")]
		public virtual int? Month { get; set; }
		public abstract class month : PX.Data.BQL.BqlInt.Field<month> { }
		#endregion

		#region Year
		[PXDBString(4, IsKey = true, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Year")]
		public virtual string Year { get; set; }
		public abstract class year : PX.Data.BQL.BqlString.Field<year> { }
		#endregion

		#region EmployeeID
		[PXDBInt(IsKey = true)]
		[PXUIField(DisplayName = "Employee")]
		public virtual int? EmployeeID { get; set; }
		public abstract class employeeID : PX.Data.BQL.BqlInt.Field<employeeID> { }
		#endregion

		#region TypeCD
		[PXDBString(EPEarningType.typeCD.Length, IsKey = true, IsUnicode = true)]
		[PXUIField(DisplayName = "Code")]
		public virtual string TypeCD { get; set; }
		public abstract class typeCD : PX.Data.BQL.BqlString.Field<typeCD> { }
		#endregion

		#region LocationID
		[PXDBInt(IsKey = true)]
		[PXUIField(DisplayName = "Location")]
		public int? LocationID { get; set; }
		public abstract class locationID : PX.Data.BQL.BqlInt.Field<locationID> { }
		#endregion

		#region Amount
		[PRCurrency]
		[PXUIField(DisplayName = "Amount")]
		public virtual Decimal? Amount { get; set; }
		public abstract class amount : PX.Data.BQL.BqlDecimal.Field<amount> { }
		#endregion

		#region System Columns
		#region CreatedByID
		[PXDBCreatedByID()]
		public virtual Guid? CreatedByID { get; set; }
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
		#endregion

		#region CreatedByScreenID
		[PXDBCreatedByScreenID()]
		public virtual string CreatedByScreenID { get; set; }
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
		#endregion

		#region CreatedDateTime
		[PXDBCreatedDateTime()]
		public virtual DateTime? CreatedDateTime { get; set; }
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
		#endregion

		#region LastModifiedByID
		[PXDBLastModifiedByID()]
		public virtual Guid? LastModifiedByID { get; set; }
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		#endregion

		#region LastModifiedByScreenID
		[PXDBLastModifiedByScreenID()]
		public virtual string LastModifiedByScreenID { get; set; }
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		#endregion

		#region LastModifiedDateTime
		[PXDBLastModifiedDateTime()]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		#endregion
		#endregion System Columns
	}
}