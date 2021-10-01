using PX.Data;
using PX.Data.BQL;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.IN;
using System;

namespace PX.Objects.PM
{
	[PXCacheName(Messages.PMWorkCodeProjectTaskSource)]
	[Serializable]
	public class PMWorkCodeLaborItemSource : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<PMWorkCodeLaborItemSource>.By<workCodeID, laborItemID>
		{
			public static PMWorkCodeLaborItemSource Find(PXGraph graph, string workCodeID, int? laborItemID) =>
				FindBy(graph, workCodeID, laborItemID);
		}

		public static class FK
		{
			public class WorkCode : PMWorkCode.PK.ForeignKeyOf<PMWorkCodeLaborItemSource>.By<workCodeID> { }
			public class LaborItem : InventoryItem.PK.ForeignKeyOf<PMWorkCodeLaborItemSource>.By<laborItemID> { }

		}
		#endregion

		#region WorkCodeID
		public abstract class workCodeID : BqlString.Field<workCodeID> { }
		[PXDBString(PMWorkCode.workCodeID.Length, IsKey = true)]
		[PXDBDefault(typeof(PMWorkCode.workCodeID))]
		[PXParent(typeof(FK.WorkCode))]
		public string WorkCodeID { get; set; }
		#endregion
		#region LaborItemID
		public abstract class laborItemID : BqlInt.Field<laborItemID> { }
		[PMLaborItem(null, null, null, IsKey = true)]
		[PXForeignReference(typeof(FK.LaborItem))]
		[PXDefault]
		[PXCheckUnique(ErrorMessage = Messages.DuplicateWorkCodeLaborItem)]
		public virtual int? LaborItemID { get; set; }
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