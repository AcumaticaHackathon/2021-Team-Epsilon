using PX.Data;
using PX.Data.BQL;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.IN;
using PX.Objects.PM;
using System;

namespace PX.Objects.PR
{
	[PXCacheName(Messages.PRProjectFringeBenefitRateReducingDeduct)]
	[Serializable]
	public class PRProjectFringeBenefitRateReducingDeduct : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<PRProjectFringeBenefitRateReducingDeduct>.By<projectID, deductCodeID>
		{
			public static PRProjectFringeBenefitRateReducingDeduct Find(PXGraph graph, int? projectID, int? deductCodeID) => 
				FindBy(graph, projectID, deductCodeID);
		}

		public static class FK
		{
			public class Project : PMProject.PK.ForeignKeyOf<PRProjectFringeBenefitRateReducingDeduct>.By<projectID> { }
			public class DeductionCode : PRDeductCode.PK.ForeignKeyOf<PRProjectFringeBenefitRateReducingDeduct>.By<deductCodeID> { }
		}
		#endregion

		#region ProjectID
		public abstract class projectID : BqlInt.Field<projectID> { }
		[PXDBInt(IsKey = true)]
		[PXDBDefault(typeof(PMProject.contractID))]
		[PXParent(typeof(FK.Project))]
		public virtual int? ProjectID { get; set; }
		#endregion
		#region DeductCodeID
		public abstract class deductCodeID : BqlInt.Field<deductCodeID> { }
		[PXDBInt(IsKey = true)]
		[PXDefault]
		[PXUIField(DisplayName = "Benefit Code")]
		[DeductionActiveSelector(typeof(Where<PRDeductCode.contribType.IsNotEqual<ContributionType.employeeDeduction>
			.And<PRDeductCode.certifiedReportType.IsNotNull>>))]
		[PXForeignReference(typeof(Field<deductCodeID>.IsRelatedTo<PRDeductCode.codeID>))]
		public virtual int? DeductCodeID { get; set; }
		#endregion
		#region IsActive
		public abstract class isActive : BqlBool.Field<isActive> { }
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Active")]
		public virtual bool? IsActive { get; set; }
		#endregion
		#region AnnualizationException
		public abstract class annualizationException : BqlBool.Field<annualizationException> { }
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Annualization Exception")]
		public virtual bool? AnnualizationException { get; set; }
		#endregion
		#region System Columns
		#region TStamp
		public abstract class tStamp : BqlByteArray.Field<tStamp> { }
		[PXDBTimestamp]
		public byte[] TStamp { get; set; }
		#endregion
		#region CreatedByID
		public abstract class createdByID : BqlGuid.Field<createdByID> { }
		[PXDBCreatedByID]
		public Guid? CreatedByID { get; set; }
		#endregion
		#region CreatedByScreenID
		public abstract class createdByScreenID : BqlString.Field<createdByScreenID> { }
		[PXDBCreatedByScreenID]
		public string CreatedByScreenID { get; set; }
		#endregion
		#region CreatedDateTime
		public abstract class createdDateTime : BqlDateTime.Field<createdDateTime> { }
		[PXDBCreatedDateTime]
		public DateTime? CreatedDateTime { get; set; }
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : BqlGuid.Field<lastModifiedByID> { }
		[PXDBLastModifiedByID]
		public Guid? LastModifiedByID { get; set; }
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : BqlString.Field<lastModifiedByScreenID> { }
		[PXDBLastModifiedByScreenID]
		public string LastModifiedByScreenID { get; set; }
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : BqlDateTime.Field<lastModifiedDateTime> { }
		[PXDBLastModifiedDateTime]
		public DateTime? LastModifiedDateTime { get; set; }
		#endregion
		#endregion
	}
}
