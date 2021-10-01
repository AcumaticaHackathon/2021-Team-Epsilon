using PX.Data;
using System;
using PX.Objects.AP;
using PX.Objects.EP;
using PX.Objects.CR;
using PX.Data.BQL;
using PX.SM;
using System.Linq;

namespace PX.CloudServices.DAC
{
	/// <exclude/>
	[Common.PXInternalUseOnly]
	public class RecognizedRecordEntityTypeListWithAnyAttribute : DAC.RecognizedRecordEntityTypeListAttribute
	{
		public RecognizedRecordEntityTypeListWithAnyAttribute()
		{
			_AllowedValues = new string[] { "ANY" }.Concat(_AllowedValues).ToArray();
			_AllowedLabels = new string[] { "All" }.Concat(_AllowedLabels).ToArray();
			_NeutralAllowedLabels = new string[] { "All" }.Concat(_NeutralAllowedLabels).ToArray();
		}
	}

	/// <exclude/>
	[Common.PXInternalUseOnly]
	public class PendingRecognitionStatus : PX.Data.BQL.BqlString.Constant<PendingRecognitionStatus>
	{
		public PendingRecognitionStatus()
			: base(RecognizedRecordStatusListAttribute.PendingRecognition)
		{
		}
	}

	/// <exclude/>
	[PXPrimaryGraph(
		new Type[]
		{
			typeof(APInvoiceEntry),
			typeof(ExpenseClaimDetailEntry),
			typeof(LeadMaint),
			typeof(ContactMaint),
		},
		new Type[]
		{
			typeof(Select<APInvoice,
				Where<APInvoice.noteID, Equal<Current<documentLink>>>>),
			typeof(Select<EPExpenseClaimDetails,
				Where<EPExpenseClaimDetails.noteID, Equal<Current<documentLink>>>>),
			typeof(Select<CRLead,
				Where<CRLead.noteID, Equal<Current<documentLink>>>>),
			typeof(Select<Contact,
				Where<Contact.noteID, Equal<Current<documentLink>>>>),
		}
		)]
	[PXProjection(typeof(Select2<RecognizedRecord,
		LeftJoin<APInvoice, On<APInvoice.noteID, Equal<RecognizedRecord.documentLink>>,
		LeftJoin<EPExpenseClaimDetails, On<EPExpenseClaimDetails.noteID, Equal<RecognizedRecord.documentLink>>,
		LeftJoin<EPExpenseClaim, On<EPExpenseClaim.refNbr, Equal<EPExpenseClaimDetails.refNbr>>,
		LeftJoin<Contact, On<Contact.noteID, Equal<RecognizedRecord.documentLink>>,
		LeftJoin<CRLead, On<CRLead.noteID, Equal<RecognizedRecord.documentLink>>>>>>>,
		Where<RecognizedRecord.status.IsNotEqual<PendingRecognitionStatus>>>),
		Persistent = false)]
	[Common.PXInternalUseOnly]
	[PXCacheName(Messages.RecognizedRecord)]
	[Serializable]
	public sealed class RecognizedRecordProjection : RecognizedRecord
	{
		public new abstract class entityType : Data.BQL.BqlString.Field<entityType> { }
		[PXUIField(DisplayName = "Entity Type")]
		[PXDefault()]
		[RecognizedRecordEntityTypeListWithAny]
		[PXDBString(3, IsKey = true, IsFixed = true)]
		public override string EntityType { get; set; }

		// This override is needed to specify DisplayMask="g" in order to hide the time part in the date/time drop-down
		// in the GI's selection area. CreatedDateTime field is used as a parameter (filter) in the GI.
		public new abstract class createdDateTime : Data.BQL.BqlDateTime.Field<createdDateTime> { }
		[PXDBCreatedDateTime(DisplayMask ="d")]
		public override DateTime? CreatedDateTime { get => base.CreatedDateTime; set => base.CreatedDateTime = value; }

		public new abstract class refNbr : Data.BQL.BqlString.Field<refNbr> { }
		[PXUIField(DisplayName = "Document Link")]
		[PXDefault]
		[PXDBGuid(withDefaulting: true, IsKey = true)]
		[PXSelector(typeof(Search<refNbr,
			Where<entityType, Equal<Current<entityType>>>>),
			DescriptionField = typeof(description),
			SelectorMode = PXSelectorMode.DisplayModeText)]
		public override Guid? RefNbr { get; set; }

		[PXNote]
		public override Guid? NoteID 
		{ 
			get => this.EPRefNbr != null || this.CNRefNbr != null || this.LDRefNbr != null ? this.DocumentLink : base.NoteID; 
			set => base.NoteID = value;
		}
		public new abstract class noteID : BqlGuid.Field<noteID> { }

		public new abstract class description : Data.BQL.BqlString.Field<description> { }
		public string Description { get; set; }

		public new abstract class aPRefNbr : Data.BQL.BqlString.Field<aPRefNbr> { }
		[PXDBString(15, IsUnicode = true, BqlField = typeof(APInvoice.refNbr))]
		public string APRefNbr { get; set; }

		public new abstract class ePRefNbr : Data.BQL.BqlString.Field<ePRefNbr> { }
		[PXDBString(15, IsUnicode = true, BqlField = typeof(EPExpenseClaimDetails.claimDetailCD))]
		public string EPRefNbr { get; set; }

		public new abstract class cNRefNbr : Data.BQL.BqlInt.Field<cNRefNbr> { }
		[PXDBInt(BqlField = typeof(Contact.contactID))]
		public int? CNRefNbr { get; set; }

		public new abstract class lDRefNbr : Data.BQL.BqlInt.Field<lDRefNbr> { }
		[PXDBInt(BqlField = typeof(CRLead.contactID))]
		public int? LDRefNbr { get; set; }

		public string RefNbr_description { get { return APRefNbr ?? EPRefNbr ?? CNRefNbr?.ToString() ?? LDRefNbr?.ToString(); } }

		public new abstract class documentLink : Data.BQL.BqlGuid.Field<documentLink> { }
	}
}
