using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.EP;
using PX.Objects.PM;
using System;

namespace PX.Objects.PR.Standalone
{
	/// <summary>
	/// Standalone DAC related to PR.Objects.PR.PREmployee />
	/// </summary>
	[PXCacheName("Payroll Employee")]
	[Serializable]
	public class PREmployee : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<PREmployee>.By<bAccountID>
		{
			public static PREmployee Find(PXGraph graph, int? bAccountID) => FindBy(graph, bAccountID);
		}

		public new static class FK
		{
			public class EmployeeClass : PREmployeeClass.PK.ForeignKeyOf<PREmployee>.By<employeeClassID> { }
			public class WorkCode : PMWorkCode.PK.ForeignKeyOf<PREmployee>.By<workCodeID> { }
		}
		#endregion Keys
		
		#region BAccountID
		public abstract class bAccountID : BqlInt.Field<bAccountID> { }
		/// <summary>
		/// Key field used to retrieve an Employee
		/// </summary>
		[PXDBInt(IsKey = true)]
		[PXDefault(typeof(EPEmployee.bAccountID))]
		[PXParent(typeof(Select<EPEmployee, Where<EPEmployee.bAccountID, Equal<Current<PREmployee.bAccountID>>>>))]
		public int? BAccountID { get; set; }
		#endregion
		#region ActiveInPayroll
		public abstract class activeInPayroll : BqlBool.Field<activeInPayroll> { }
		/// <summary>
		/// Indicates whether the employee is active in the payroll module
		/// </summary>
		[PXDBBool]
		public bool? ActiveInPayroll { get; set; }
		#endregion
		#region EmployeeClassID
		public abstract class employeeClassID : BqlString.Field<employeeClassID> { }
		[PXDBString(10, IsUnicode = true)]
		[PXSelector(typeof(PREmployeeClass.employeeClassID))]
		public string EmployeeClassID { get; set; }
		#endregion
		#region WorkCodeUseDflt
		public abstract class workCodeUseDflt : BqlBool.Field<workCodeUseDflt> { }
		[PXDBBool]
		public bool? WorkCodeUseDflt { get; set; }
		#endregion
		#region WorkCodeID
		public abstract class workCodeID : BqlString.Field<workCodeID> { }
		[PXDBString(PMWorkCode.workCodeID.Length)]
		[PXUnboundDefault(typeof(Switch<Case<Where<workCodeUseDflt.IsEqual<True>>, Selector<employeeClassID, PREmployeeClass.workCodeID>>,
			workCodeID>))]
		public string WorkCodeID { get; set; }
		#endregion
	}
}