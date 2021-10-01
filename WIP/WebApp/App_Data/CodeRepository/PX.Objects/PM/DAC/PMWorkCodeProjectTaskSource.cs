using PX.Data;
using PX.Data.BQL;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CT;
using System;

namespace PX.Objects.PM
{
	[PXCacheName(Messages.PMWorkCodeProjectTaskSource)]
	[Serializable]
	public class PMWorkCodeProjectTaskSource : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<PMWorkCodeProjectTaskSource>.By<workCodeID, lineNbr>
		{
			public static PMWorkCodeProjectTaskSource Find(PXGraph graph, string workCodeID, int? lineNbr) =>
				FindBy(graph, workCodeID, lineNbr);
		}

		public static class FK
		{
			public class WorkCode : PMWorkCode.PK.ForeignKeyOf<PMWorkCodeProjectTaskSource>.By<workCodeID> { }
			public class Project : PMProject.PK.ForeignKeyOf<PMWorkCodeProjectTaskSource>.By<projectID> { }
			public class ProjectTask : PMTask.PK.ForeignKeyOf<PMWorkCodeProjectTaskSource>.By<projectTaskID> { }

		}
		#endregion

		#region WorkCodeID
		public abstract class workCodeID : BqlString.Field<workCodeID> { }
		[PXDBString(PMWorkCode.workCodeID.Length, IsKey = true)]
		[PXDBDefault(typeof(PMWorkCode.workCodeID))]
		[PXParent(typeof(FK.WorkCode))]
		public string WorkCodeID { get; set; }
		#endregion
		#region LineNbr
		public abstract class lineNbr : BqlInt.Field<lineNbr> { }
		[PXDBInt(IsKey = true)]
		[PXLineNbr(typeof(PMWorkCode))]
		public int? LineNbr { get; set; }
		#endregion
		#region ProjectID
		public abstract class projectID : BqlInt.Field<projectID> { }
		[Project(typeof(
			Where<PMProject.baseType.IsEqual<CTPRType.project>
				.And<PMProject.nonProject.IsNotEqual<True>>>),
			DisplayName = "Project")]
		[PXDefault]
		[PXForeignReference(typeof(FK.Project))]
		public int? ProjectID { get; set; }
		#endregion
		#region ProjectTaskID
		public abstract class projectTaskID : BqlInt.Field<projectTaskID> { }
		[ProjectTask(typeof(projectID), DisplayName = "Project Task", AllowNull = true)]
		[PXCheckUnique(typeof(projectID), IgnoreNulls = false, ErrorMessage = Messages.DuplicateWorkCodeProjectTask)]
		[PXForeignReference(typeof(FK.ProjectTask))]
		public int? ProjectTaskID { get; set; }
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