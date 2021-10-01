using PX.CloudServices.DAC;
using PX.CloudServices;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AP.InvoiceRecognition.DAC;
using PX.Objects.CR;
using PX.Objects.EP;
using PX.SM;
using PX.TM;
using System;
using System.Collections;

namespace PX.Objects.AP.InvoiceRecognition
{
	[PXInternalUseOnly]
	public class RecognizedRecordProcess : PXGraph<RecognizedRecordProcess>
	{
		[PXHidden]
		public class RecognizedRecordFilter : IBqlTable
		{
			[PXUIField(DisplayName = "Created Before")]
			[PXDefault(typeof(AccessInfo.businessDate))]
			[PXDBDate]
			public virtual DateTime? CreatedBefore { get; set; }
			public abstract class createdBefore : BqlDateTime.Field<createdBefore> { }

			[PXUIField(DisplayName = "Show Unprocessed Records")]
			[PXDBBool]
			public virtual bool? ShowUnprocessedRecords { get; set; }
			public abstract class showUnprocessedRecords : BqlBool.Field<showUnprocessedRecords> { }
		}

		[PXHidden]
		public class RecognizedRecordExt : RecognizedRecord
		{
			[PXUIField(DisplayName = "Selected")]
			[PXBool]
			public virtual bool? Selected { get; set; }
			public abstract class selected : BqlBool.Field<selected> { }
		}

		public PXFilter<RecognizedRecordFilter> Filter;

		public PXCancel<RecognizedRecordFilter> Cancel;
		public PXAction<RecognizedRecordFilter> ViewDocument;

		public PXFilteredProcessing<RecognizedRecordExt, RecognizedRecordFilter,
			   Where<RecognizedRecordExt.createdDateTime.IsLess<RecognizedRecordFilter.createdBefore.FromCurrent>>> Records;

		public RecognizedRecordProcess()
		{
			Records.SetSelected<RecognizedRecordExt.selected>();
			Records.SetProcessDelegate(DeleteRecognizedRecord);
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXUIField(DisplayName = "Created Date")]
		protected virtual void RecognizedRecordExt_CreatedDateTime_CacheAttached(PXCache sender)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[Owner]
		protected virtual void RecognizedRecordExt_Owner_CacheAttached(PXCache sender)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "Document Link", Visible = true)]
		protected virtual void RecognizedRecordExt_DocumentLink_CacheAttached(PXCache sender)
		{
		}

		public IEnumerable records()
		{
			if (Filter.Current.ShowUnprocessedRecords == true)
			{
				return Records.View.QuickSelect();
			}

			var command = Records.View.BqlSelect.WhereAnd<Where<RecognizedRecordExt.status.IsEqual<@P.AsString>>>();
			var parameters = new object[] { RecognizedRecordStatusListAttribute.Processed };
			var currents = new object[] { Filter.Current };
			var view = new PXView(this, true, command);

			return view.SelectMultiBound(currents, parameters);
		}

		private static void DeleteRecognizedRecord(RecognizedRecordExt record)
		{
			using (var tran = new PXTransactionScope())
			{
				PXCache cache;

				if (record.EntityType == PX.CloudServices.Models.APInvoiceDocumentType)
				{
					var graph = CreateInstance<APInvoiceRecognitionEntry>();
					var document = (APRecognizedInvoice)
						SelectFrom<APRecognizedInvoice>.
						Where<APRecognizedInvoice.recognizedRecordRefNbr.IsEqual<@P.AsGuid>>.
						View.ReadOnly.SelectSingleBound(graph, null, record.RefNbr);

					graph.Document.Current = document;
					graph.Delete.Press();

					cache = graph.RecognizedRecords.Cache;
				}
				else
				{
					var graph = CreateInstance<PXGraph>();
					cache = graph.Caches[typeof(RecognizedRecord)];

					record.RecognitionResult = null;
					cache.PersistUpdated(record);
					cache.ResetPersisted(record);
					graph.SelectTimeStamp();
					cache.PersistDeleted(record);
				}

				if (record.Status != RecognizedRecordStatusListAttribute.Processed)
				{
					DeleteFileNotes(cache, record);
				}

				tran.Complete();
			}
		}

		private static void DeleteFileNotes(PXCache cache, RecognizedRecordExt record)
		{
			var fileIds = PXNoteAttribute.GetFileNotes(cache, record);

			foreach (var fileId in fileIds)
			{
				var entitiesUseFile = 
					SelectFrom<NoteDoc>.
					Where<NoteDoc.fileID.IsEqual<@P.AsGuid>>.
					View.ReadOnly.Select(cache.Graph, fileId).Count;

				if (entitiesUseFile > 1)
				{
					continue;
				}

				UploadFileMaintenance.DeleteFile(fileId);
			}
		}

		protected virtual void _ (Events.FieldSelecting<RecognizedRecordExt.documentLink> e)
		{
			if (!(e.Row is RecognizedRecordExt row) || row.DocumentLink == null)
			{
				return;
			}

			string value = string.Empty;

			if (row.EntityType == Models.KnownModels[Models.ApInvoicesModel].DocumentType)
			{
				value = GetAPDocumentLinkStateValue(row.DocumentLink);
			}
			else if (row.EntityType == Models.KnownModels[Models.ReceiptModel].DocumentType)
			{
				value = GetReceiptDocumentLinkStateValue(row.DocumentLink);
			}
			else if (row.EntityType == Models.KnownModels[Models.BusinessCardsModel].DocumentType)
			{
				value = GetBusinessCardDocumentLinkStateValue(row.DocumentLink);
			}

			e.ReturnValue = value ?? string.Empty;
		}

		private string GetBusinessCardDocumentLinkStateValue(Guid? noteId)
		{
			var contact = GetContact(noteId);
			if (contact == null)
			{
				return null;
			}

			return $"{contact.ContactType} {contact.ContactID}";
		}

		private Contact GetContact(Guid? noteId)
		{
			var contact = (Contact)
				SelectFrom<Contact>.
				Where<Contact.noteID.IsEqual<@P.AsGuid>>.
				View.ReadOnly.SelectSingleBound(this, null, noteId);

			if (contact == null)
			{
				contact =
					SelectFrom<CRLead>.
					Where<CRLead.noteID.IsEqual<@P.AsGuid>>.
					View.ReadOnly.SelectSingleBound(this, null, noteId);
			}

			return contact;
		}

		private EPExpenseClaimDetails GetExpenseClaimDetail(Guid? noteId)
		{
			return SelectFrom<EPExpenseClaimDetails>.
				Where<EPExpenseClaimDetails.noteID.IsEqual<@P.AsGuid>>.
				View.ReadOnly.SelectSingleBound(this, null, noteId);
		}

		private string GetAPDocumentLinkStateValue(Guid? noteId)
		{
			var document = (APRegister)
				SelectFrom<APRegister>.
				Where<APRegister.noteID.IsEqual<@P.AsGuid>>.
				View.ReadOnly.SelectSingleBound(this, null, noteId);
			if (document == null)
			{
				return null;
			}

			return $"{document.DocType} {document.RefNbr}";
		}

		private string GetReceiptDocumentLinkStateValue(Guid? noteId)
		{
			var document = GetExpenseClaimDetail(noteId);
			if (document == null)
			{
				return null;
			}

			return $"{EPExpenseClaimDetails.DocType} {document.ClaimDetailCD}";
		}

		[PXButton]
		[PXUIField(Visible = false)]
		protected virtual void viewDocument()
		{
			var record = Records.Current;
			if (record == null || record.DocumentLink == null)
			{
				return;
			}

			if (record.EntityType == Models.KnownModels[Models.ApInvoicesModel].DocumentType)
			{
				ViewAPDocument(record.DocumentLink);
			}
			else if (record.EntityType == Models.KnownModels[Models.ReceiptModel].DocumentType)
			{
				ViewReceipt(record.DocumentLink);
			}
			else if (record.EntityType == Models.KnownModels[Models.BusinessCardsModel].DocumentType)
			{
				ViewBusinessCard(record.DocumentLink);
			}
		}

		private void ViewAPDocument(Guid? noteId)
		{
			var document = (APInvoice)
				SelectFrom<APInvoice>.
				Where<APInvoice.noteID.IsEqual<@P.AsGuid>>.
				View.ReadOnly.SelectSingleBound(this, null, noteId);

			if (document == null)
			{
				return;
			}

			var graph = CreateInstance<APInvoiceEntry>();
			graph.Document.Current = document;

			throw new PXRedirectRequiredException(graph, null);
		}

		private void ViewReceipt(Guid? noteId)
		{
			var document = GetExpenseClaimDetail(noteId);
			if (document == null)
			{
				return;
			}

			var graph = CreateInstance<ExpenseClaimDetailEntry>();
			graph.ClaimDetails.Current = document;

			throw new PXRedirectRequiredException(graph, null);
		}

		private void ViewBusinessCard(Guid? noteId)
		{
			var contact = GetContact(noteId);
			if (contact == null)
			{
				return;
			}

			if (contact is CRLead lead)
			{
				var graph = CreateInstance<LeadMaint>();
				graph.Lead.Current = lead;

				throw new PXRedirectRequiredException(graph, null);
			}
			else
			{
				var graph = CreateInstance<ContactMaint>();
				graph.Contact.Current = contact;

				throw new PXRedirectRequiredException(graph, null);
			}
		}
	}
}
