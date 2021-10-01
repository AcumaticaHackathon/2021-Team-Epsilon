using System;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.IN;

namespace PX.Objects.PO
{
	[PXCacheName(Messages.POReceiptSplitToTransferSplitLink, PXDacType.Details)]
	public class POReceiptSplitToTransferSplitLink : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<POReceiptSplitToTransferSplitLink>.By<receiptNbr, receiptLineNbr, receiptSplitLineNbr, transferDocType, transferRefNbr, transferLineNbr, transferSplitLineNbr>
		{
			public static POReceiptSplitToTransferSplitLink Find(PXGraph graph, 
				string receiptNbr, int? receiptLineNbr, int? receiptSplitLineNbr, 
				string transferDocType, string transferRefNbr, int? transferLineNbr, int? transferSplitLineNbr)
				=> FindBy(graph, receiptNbr, receiptLineNbr, receiptSplitLineNbr, transferDocType, transferRefNbr, transferLineNbr, transferSplitLineNbr);
		}
		public static class FK
		{
			public class Receipt : POReceipt.PK.ForeignKeyOf<POReceiptSplitToTransferSplitLink>.By<receiptNbr> { }
			public class ReceiptLine : POReceiptLine.PK.ForeignKeyOf<POReceiptSplitToTransferSplitLink>.By<receiptNbr, receiptLineNbr> { }
			public class ReceiptLineSplit : Objects.PO.POReceiptLineSplit.PK.ForeignKeyOf<POReceiptSplitToTransferSplitLink>.By<receiptNbr, receiptLineNbr, receiptSplitLineNbr> { }
			public class Transfer : INRegister.PK.ForeignKeyOf<POReceiptSplitToTransferSplitLink>.By<transferDocType, transferRefNbr> { }
			public class TransferLine : IN.INTran.PK.ForeignKeyOf<POReceiptSplitToTransferSplitLink>.By<transferDocType, transferRefNbr, transferLineNbr> { }
			public class TransferLineSplit : IN.INTranSplit.PK.ForeignKeyOf<POReceiptSplitToTransferSplitLink>.By<transferDocType, transferRefNbr, transferLineNbr, transferSplitLineNbr> { }

			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use ReceiptLineSplit instead.")]
			public class POReceiptLineSplit : ReceiptLineSplit { }
			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use TransferLineSplit instead.")]
			public class INTranSplit : TransferLineSplit { }
			[Obsolete("This foreign key is obsolete and is going to be removed in 2021R1. Use TransferLine instead.")]
			public class INTran : TransferLine { }
		}
		#endregion

		#region ReceiptNbr
		[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "")]
		[PXDefault]
		public virtual String ReceiptNbr { get; set; }
		public abstract class receiptNbr : PX.Data.BQL.BqlString.Field<receiptNbr> { }
		#endregion
		#region ReceiptLineNbr
		[PXDBInt(IsKey = true)]
		[PXDefault]
		public virtual Int32? ReceiptLineNbr { get; set; }
		public abstract class receiptLineNbr : PX.Data.BQL.BqlInt.Field<receiptLineNbr> { }
		#endregion
		#region ReceiptSplitLineNbr
		[PXDBInt(IsKey = true)]
		[PXDefault]
		[PXParent(typeof(FK.ReceiptLineSplit))]
		public virtual Int32? ReceiptSplitLineNbr { get; set; }
		public abstract class receiptSplitLineNbr : PX.Data.BQL.BqlInt.Field<receiptSplitLineNbr> { }
		#endregion
		#region TransferDocType
		[PXDBString(1, IsFixed = true, IsKey = true)]
		public virtual String TransferDocType { get { return INDocType.Transfer; } set { } }
		public abstract class transferDocType : PX.Data.BQL.BqlString.Field<transferDocType> { }
		#endregion
		#region TransferRefNbr
		[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "")]
		[PXDefault]
		[PXSelector(typeof(
			Search<INRegister.refNbr,
			Where<INRegister.docType, Equal<INDocType.transfer>,
				And<INRegister.transferType, Equal<INTransferType.oneStep>>>>))]
		public virtual String TransferRefNbr { get; set; }
		public abstract class transferRefNbr : PX.Data.BQL.BqlString.Field<transferRefNbr> { }
		#endregion
		#region TransferLineNbr
		[PXDBInt(IsKey = true)]
		[PXDefault]
		public virtual Int32? TransferLineNbr { get; set; }
		public abstract class transferLineNbr : PX.Data.BQL.BqlInt.Field<transferLineNbr> { }
		#endregion
		#region TransferSplitLineNbr
		[PXDBInt(IsKey = true)]
		[PXDefault]
		[PXParent(typeof(FK.TransferLineSplit))]
		public virtual Int32? TransferSplitLineNbr { get; set; }
		public abstract class transferSplitLineNbr : PX.Data.BQL.BqlInt.Field<transferSplitLineNbr> { }
		#endregion
		#region Qty
		[PXDBQuantity]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Quantity", Enabled = false)]
		public virtual Decimal? Qty { get; set; }
		public abstract class qty : PX.Data.BQL.BqlDecimal.Field<qty> { }
		#endregion
	}
}