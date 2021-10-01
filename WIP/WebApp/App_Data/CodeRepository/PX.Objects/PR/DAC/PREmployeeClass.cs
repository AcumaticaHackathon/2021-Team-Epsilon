using PX.Data;
using PX.Data.BQL;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.EP;
using PX.Objects.PM;
using System;

namespace PX.Objects.PR.Standalone
{
	/// <summary>
	/// Standalone DAC related to PR.Objects.PR.PREmployeeClass />
	/// </summary>
	[PXCacheName("Payroll Employee Class")]
	[Serializable]
	public class PREmployeeClass : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<PREmployeeClass>.By<employeeClassID>
		{
			public static PREmployeeClass Find(PXGraph graph, string employeeClassID) => FindBy(graph, employeeClassID);
		}

		public new static class FK
		{
			public class WorkCode : PMWorkCode.PK.ForeignKeyOf<PREmployeeClass>.By<workCodeID> { }
		}
		#endregion Keys

		#region EmployeeClassID
		public abstract class employeeClassID : PX.Data.BQL.BqlString.Field<employeeClassID> { }
		[PXDBString(10, IsUnicode = true, IsKey = true, InputMask = ">aaaaaaaaaa")]
		public string EmployeeClassID { get; set; }
		#endregion
		#region WorkCodeID
		public abstract class workCodeID : BqlString.Field<workCodeID> { }
		[PXDBString(PMWorkCode.workCodeID.Length)]
		public string WorkCodeID { get; set; }
		#endregion
	}
}