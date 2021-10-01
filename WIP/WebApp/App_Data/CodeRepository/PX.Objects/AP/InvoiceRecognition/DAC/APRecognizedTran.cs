using PX.Common;
using PX.Data;
using PX.Data.BQL;

namespace PX.Objects.AP.InvoiceRecognition.DAC
{
	[PXInternalUseOnly]
	[PXHidden]
	public class APRecognizedTran : APTran
	{
		[PXString(50, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Alternate ID")]
		public virtual string AlternateID { get; set; }
		public abstract class alternateID : BqlString.Field<alternateID> { }

		[APTranRecognizedInventoryItem(Filterable = true)]
		[PXUIField(Visible = false)]
		public virtual int? InternalAlternateID { get; set; }
		public abstract class internalAlternateID : BqlInt.Field<internalAlternateID> { }

		[PXInt]
		[PXUIField(Visible = false)]
		public virtual int? NumOfFoundIDByAlternate { get; set; }
		public abstract class numOfFoundIDByAlternate : BqlInt.Field<numOfFoundIDByAlternate> { }

		[PXBool]
		[PXUnboundDefault(false)]
		[PXUIField(Visible = false)]
		public virtual bool? InventoryIDManualInput { get; set; }
		public abstract class inventoryIDManualInput : BqlBool.Field<inventoryIDManualInput> { }

		[PXString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "PO Number", Enabled = false)]
		public virtual string RecognizedPONumber { get; set; }
		public abstract class recognizedPONumber : BqlString.Field<recognizedPONumber> { }

		[PXString]
		[PXUIField(Enabled = false, Visible = false)]
		public virtual string PONumberJson { get; set; }
		public abstract class pONumberJson : BqlString.Field<pONumberJson> { }
		
		#region Status
		public abstract class pOLinkStatus : PX.Data.BQL.BqlString.Field<pOLinkStatus> { }
		[PXString(1, IsFixed = true)]
		[PXDefault(APPOLinkStatus.NotLinked, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "PO Link Status", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[APPOLinkStatus.List]
		public virtual string POLinkStatus { get; set; }
		#endregion

		new public abstract class curyTranAmt : BqlDecimal.Field<curyTranAmt> { }

		new public abstract class refNbr : BqlString.Field<refNbr> { }

		new public abstract class tranType : BqlString.Field<tranType> { }
	}

	[PXInternalUseOnly]
	public class PageWord
	{
		public int? Page { get; set; }
		public int? Word { get; set; }
	}
	
	public class APPOLinkStatus
	{
		public static readonly string[] Values =
		{
			NotLinked,
			Linked,
			MultiplePOLinesFound,
			MultiplePRLinesFound
		};
		public static readonly string[] Labels =
		{
			Messages.NotLinked,
			Messages.Linked,
			Messages.MultiplePOLinesFound,
			Messages.MultiplePRLinesFound
		};

		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute()
				: base(Values, Labels) { }
		}

		public const string NotLinked = "N";
		public const string Linked = "L";
		public const string MultiplePOLinesFound = "M";
		public const string MultiplePRLinesFound = "P";
	}
}
