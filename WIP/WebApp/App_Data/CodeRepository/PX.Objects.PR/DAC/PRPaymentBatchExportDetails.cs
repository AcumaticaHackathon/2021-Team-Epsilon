using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CA;
using System;

namespace PX.Objects.PR
{
	[Serializable]
	[PXCacheName(Messages.PRPaymentBatchExportDetails)]
	[PXProjection(typeof(SelectFrom<PRPaymentBatchExportDetails>.Where<MatchWithBranch<paymentBranchID>.And<MatchWithPayGroup<payGroupID>>>), Persistent = true)]
	public class PRPaymentBatchExportDetails : IBqlTable
	{
		public class PK : PrimaryKeyOf<PRPaymentBatchExportDetails>.By<paymentBatchNbr, exportHistoryLineNbr, lineNbr>
		{
			public static PRPaymentBatchExportDetails Find(PXGraph graph, string paymentBatchNbr, int exportHistoryLineNbr, int lineNbr)
				=> FindBy(graph, paymentBatchNbr, exportHistoryLineNbr, lineNbr);
		}

		public static class FK
		{
			public class PRPaymentBatchExportHistory : PR.PRPaymentBatchExportHistory.PK.ForeignKeyOf<PRPaymentBatchExportDetails>.By<paymentBatchNbr, exportHistoryLineNbr> { }
			
			public class PRPayment : PR.PRPayment.PK.ForeignKeyOf<PRPaymentBatchExportDetails>.By<docType, refNbr> { }

			public class CABatch : CA.CABatch.PK.ForeignKeyOf<PRPaymentBatchExportDetails>.By<paymentBatchNbr> { }
		}

		#region PaymentBatchNbr
		[PXDBString(15, IsKey = true, IsUnicode = true)]
		[PXUIField(DisplayName = "Payment Batch Nbr.")]
		[PXParent(typeof(FK.PRPaymentBatchExportHistory))]
		[PXDBDefault(typeof(CABatch.batchNbr))]
		public virtual string PaymentBatchNbr { get; set; }
		public abstract class paymentBatchNbr : PX.Data.BQL.BqlString.Field<paymentBatchNbr> { }
		#endregion

		#region ExportHistoryLineNbr
		[PXDBInt(IsKey = true)]
		[PXUIField(DisplayName = "Export History Line Nbr.")]
		[PXDBDefault(typeof(PRPaymentBatchExportHistory.lineNbr))]
		public virtual int? ExportHistoryLineNbr { get; set; }
		public abstract class exportHistoryLineNbr : PX.Data.BQL.BqlInt.Field<exportHistoryLineNbr> { }
		#endregion

		#region LineNbr
		[PXDBInt(IsKey = true)]
		[PXUIField(DisplayName = "Line Nbr")]
		[PXLineNbr(typeof(PRPaymentBatchExportHistory))]
		public virtual int? LineNbr { get; set; }
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
		#endregion

		#region DocType
		[PXDBString(3, IsFixed = true)]
		[PXUIField(DisplayName = "Type")]
		public virtual string DocType { get; set; }
		public abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }
		#endregion

		#region RefNbr
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Reference Nbr")]
		public virtual string RefNbr { get; set; }
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
		#endregion

		#region DocDesc
		[PXDBString(60, IsUnicode = true)]
		[PXUIField(DisplayName = "Description")]
		public virtual string DocDesc { get; set; }
		public abstract class docDesc : PX.Data.BQL.BqlString.Field<docDesc> { }
		#endregion

		#region EmployeeID
		[Employee]
		[PXUIField(DisplayName = "Employee")]
		public virtual int? EmployeeID { get; set; }
		public abstract class employeeID : PX.Data.BQL.BqlInt.Field<employeeID> { }
		#endregion

		#region PayGroupID
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Pay Group")]
		public virtual string PayGroupID { get; set; }
		public abstract class payGroupID : PX.Data.BQL.BqlString.Field<payGroupID> { }
		#endregion

		#region PayPeriodID
		[PXDBString(6, IsFixed = true)]
		[PXUIField(DisplayName = "Pay Period")]
		public virtual string PayPeriodID { get; set; }
		public abstract class payPeriodID : PX.Data.BQL.BqlString.Field<payPeriodID> { }
		#endregion

		#region NetAmount
		[PXDBDecimal()]
		[PXUIField(DisplayName = "Net Amount")]
		public virtual Decimal? NetAmount { get; set; }
		public abstract class netAmount : PX.Data.BQL.BqlDecimal.Field<netAmount> { }
		#endregion

		#region ExtRefNbr
		[PXDBString(60, IsUnicode = true)]
		[PXUIField(DisplayName = "Check Nbr.")]
		public virtual string ExtRefNbr { get; set; }
		public abstract class extRefNbr : PX.Data.BQL.BqlString.Field<extRefNbr> { }
		#endregion

		#region PaymentBranchID
		public abstract class paymentBranchID : PX.Data.BQL.BqlInt.Field<paymentBranchID> { }
		[GL.Branch(Visible = false)]
		public int? PaymentBranchID { get; set; }
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