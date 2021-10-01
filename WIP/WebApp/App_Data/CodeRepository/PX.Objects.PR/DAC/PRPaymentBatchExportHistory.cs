using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CA;
using PX.Objects.CM;
using PX.SM;
using System;

namespace PX.Objects.PR
{
	[PXCacheName(Messages.PRPaymentBatchExportHistory)]
	[Serializable]
	public class PRPaymentBatchExportHistory : IBqlTable
	{
		public class PK : PrimaryKeyOf<PRPaymentBatchExportHistory>.By<paymentBatchNbr, lineNbr>
		{
			public static PRPaymentBatchExportHistory Find(PXGraph graph, string paymentBatchNbr, int lineNbr)
				=> FindBy(graph, paymentBatchNbr, lineNbr);
		}

		public static class FK
		{
			public class CABatch : CA.CABatch.PK.ForeignKeyOf<PRPaymentBatchExportHistory>.By<paymentBatchNbr> { }
		}

		#region PaymentBatchNbr
		public abstract class paymentBatchNbr : PX.Data.BQL.BqlString.Field<paymentBatchNbr> { }
		[PXDBString(15, IsUnicode = true, IsKey = true)]
		[PXUIField(DisplayName = "Payment Batch")]
		[PXParent(typeof(FK.CABatch))]
		[PXDefault(typeof(CABatch.batchNbr))]
		public virtual string PaymentBatchNbr { get; set; }
		#endregion

		#region LineNbr
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
		[PXDBInt(IsKey = true)]
		[PXLineNbr(typeof(CABatch))]
		public virtual int? LineNbr { get; set; }
		#endregion

		#region UserID
		public abstract class userID : PX.Data.BQL.BqlGuid.Field<userID> { }
		[PXDBGuid]
		[PXUIField(DisplayName = "User", Enabled = false)]
		[PXDefault(typeof(AccessInfo.userID))]
		[PXSelector(typeof(Users.pKID), DescriptionField = typeof(Users.displayName))]
		public virtual Guid? UserID { get; set; }
		#endregion

		#region ExportDateTime
		public abstract class exportDateTime : PX.Data.BQL.BqlDateTime.Field<exportDateTime> { }
		[PXDBDateAndTime(PreserveTime = true)]
		[PXUIField(DisplayName = "Export Time", Enabled = false)]
		[PXDefault]
		public virtual DateTime? ExportDateTime { get; set; }
		#endregion

		#region Reason
		public abstract class reason : PX.Data.BQL.BqlString.Field<reason> { }
		[PXDBString(256, IsUnicode = true)]
		[PXUIField(DisplayName = "Reason", Enabled = false)]
		[PXDefault(PaymentBatchReason.Initial)]
		[PaymentBatchReason(ExclusiveValues = false)]
		public virtual string Reason { get; set; }
		#endregion

		#region BatchTotal
		public abstract class batchTotal : PX.Data.BQL.BqlDecimal.Field<batchTotal> { }
		[PXDBCury(typeof(CABatch.curyID))]
		[PXUIField(DisplayName = "Batch Total")]
		public virtual decimal? BatchTotal { get; set; }
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