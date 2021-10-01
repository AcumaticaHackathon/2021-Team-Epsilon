using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CA;
using PX.Objects.CR;
using System;
using System.Linq;

namespace PX.Objects.PR
{
	[PXPrimaryGraph(
		new Type[] { typeof(PRDirectDepositBatchEntry) },
		new Type[] { typeof(Where<CABatch.origModule.IsEqual<GL.BatchModule.modulePR>>) })]
	[PXCacheName(Messages.PRCABatch)]
	[Serializable]
	public class PRCABatch : CABatch
	{
		#region BatchNbr
		public new abstract class batchNbr : PX.Data.BQL.BqlString.Field<batchNbr> { }
		[PXDBString(15, IsKey = true, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXDefault]
		[PXUIField(DisplayName = "Reference Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
		[CABatchType.Numbering]
		[PRCABatchRefNbr(typeof(Search<CABatch.batchNbr, Where<CABatch.origModule, Equal<GL.BatchModule.modulePR>>>))]
		public override string BatchNbr { get; set; }
		#endregion
		#region PaymentMethodID
		public new abstract class paymentMethodID : PX.Data.BQL.BqlString.Field<paymentMethodID> { }
		[PXDBString(10, IsUnicode = true)]
		[PXDefault]
		[PXUIField(DisplayName = "Payment Method")]
		[PXSelector(typeof(SearchFor<PaymentMethod.paymentMethodID>.Where<PRxPaymentMethod.useForPR.IsEqual<True>>))]
		public override string PaymentMethodID { get; set; }
		#endregion
		#region BatchTotal
		public abstract class batchTotal : PX.Data.BQL.BqlDecimal.Field<batchTotal> { }
		[PXUIField(DisplayName = "Batch Total", Enabled = false)]
		[BatchTotal]
		public virtual decimal? BatchTotal { get; set; }
		#endregion
		#region NoteID
		public new abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
		[PXSearchable(SM.SearchCategory.PR, Messages.SearchableTitlePRCABatch, new Type[] { typeof(batchNbr), typeof(BAccount.acctName) },
			new Type[] { typeof(paymentMethodID), typeof(batchSeqNbr), typeof(tranDesc), typeof(extRefNbr), typeof(BAccount.acctCD) },
			NumberFields = new Type[] { typeof(batchNbr) },
			Line1Format = "{0}{1:d}{2}", Line1Fields = new Type[] { typeof(extRefNbr), typeof(tranDate), typeof(batchSeqNbr) },
			Line2Format = "{0}", Line2Fields = new Type[] { typeof(tranDesc) }
		)]
		[PXNote]
		public override Guid? NoteID { get; set; }
		#endregion
		#region HeaderDescription
		public abstract class headerDescription : PX.Data.BQL.BqlString.Field<headerDescription> { }
		[BatchHeader(typeof(paymentMethodID), typeof(tranDate))]
		public string HeaderDescription { get; set; }
		#endregion
	}

	public class BatchTotalAttribute : PXDecimalAttribute
	{
		public override void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			base.FieldSelecting(sender, e);

			CABatch currentBatch = e.Row as CABatch;
			if (currentBatch == null)
				return;

			if (e.ReturnValue == null)
				e.ReturnValue = PRDirectDepositBatchEntry.GetBatchTotal(sender, currentBatch.BatchNbr);
		}
	}

	public class BatchHeaderAttribute : PXStringAttribute, IPXFieldDefaultingSubscriber
	{
		private readonly Type _PaymentMethodIDField;
		private readonly Type _TranDateField;

		public BatchHeaderAttribute(Type paymentMethodID, Type tranDate)
		{
			_PaymentMethodIDField = paymentMethodID;
			_TranDateField = tranDate;
		}

		public override void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			base.FieldSelecting(sender, e);

			if (e.Row == null)
				return;

			e.ReturnValue = GetHeader(sender, e.Row);
		}

		public void FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (e.Row == null)
				return;

			e.NewValue = GetHeader(sender, e.Row);
		}

		private string GetHeader(PXCache sender, object row)
		{
			string paymentMethodID = (PXSelectorAttribute.Select(sender, row, _PaymentMethodIDField.Name) as PaymentMethod)?.Descr;
			string tranDate = (sender.GetValue(row, _TranDateField.Name) as DateTime?)?.ToShortDateString();

			if (string.IsNullOrWhiteSpace(paymentMethodID) || string.IsNullOrWhiteSpace(tranDate))
				return string.Empty;

			return $"{paymentMethodID} - {tranDate}";
		}
	}
}
