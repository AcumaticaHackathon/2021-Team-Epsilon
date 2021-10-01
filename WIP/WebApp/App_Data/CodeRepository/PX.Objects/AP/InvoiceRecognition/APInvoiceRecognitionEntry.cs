using Newtonsoft.Json;
using PX.CloudServices.DAC;
using PX.CloudServices.DocumentRecognition;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.Search;
using PX.Data.Wiki.Parser;
using PX.Metadata;
using PX.Objects.AP.InvoiceRecognition.DAC;
using PX.Objects.AP.InvoiceRecognition.Feedback;
using PX.Objects.CM;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.PO;
using PX.SM;
using Serilog.Events;
using SerilogTimings.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ILogger = Serilog.ILogger;
using PX.CloudServices.Tenants;
using PX.Data.DependencyInjection;
using PX.Objects.SO;

namespace PX.Objects.AP.InvoiceRecognition
{
	[PXInternalUseOnly]
	public class APInvoiceRecognitionEntry : PXGraph<APInvoiceRecognitionEntry, APRecognizedInvoice>, IGraphWithInitialization
	{
		private const int _recognitionTimeoutMinutes = 10;
		private static readonly TimeSpan RecognitionPollingInterval = TimeSpan.FromSeconds(1);

		internal const string PdfExtension = ".pdf";

		public const string RefNbrNavigationParam = nameof(APRecognizedInvoice.RecognizedRecordRefNbr);
		public const string StatusNavigationParam = nameof(APRecognizedInvoice.RecognitionStatus);
		public const string NoteIdNavigationParam = nameof(APRecognizedInvoice.NoteID);

		private JsonSerializer _jsonSerializer = JsonSerializer.CreateDefault(VersionedFeedback._settings);

		private readonly HashSet<string> _alwaysDefaultPrimaryFields = new HashSet<string>
		{
			nameof(APRecognizedInvoice.VendorLocationID),
			nameof(APRecognizedInvoice.RecognizedRecordRefNbr),
			nameof(APRecognizedInvoice.RecognizedRecordStatus),
			nameof(APRecognizedInvoice.RecognitionStatus),
			nameof(APRecognizedInvoice.AllowFiles),
			nameof(APRecognizedInvoice.AllowFilesMsg),
			nameof(APRecognizedInvoice.AllowUploadFile)
		};

		[InjectDependency]
		internal IScreenInfoProvider ScreenInfoProvider { get; set; }

		[InjectDependency]
		internal Serilog.ILogger _logger { get; set; }

		[InjectDependency]
		internal IEntitySearchService EntitySearchService { get; set; }

		[InjectDependency]
		public IInvoiceRecognitionService InvoiceRecognitionClient { get; set; }

		[InjectDependency]
		internal ICloudTenantService _cloudTenantService { get; set; }

		public void Initialize()
		{
			SwitchDefaultsOffForUIFields();
		}

		private void SwitchDefaultsOffForUIFields()
		{
			var (primaryFields, detailFields) = GetUIFields();
			if (primaryFields == null || detailFields == null)
			{
				return;
			}

			PXFieldDefaulting defaultingSwitchOff = (sender, args) => args.Cancel = true;
			var primaryFieldsWithoutDefaulting = primaryFields.Where(fieldName => !_alwaysDefaultPrimaryFields.Contains(fieldName));
			foreach (var field in primaryFieldsWithoutDefaulting)
			{
				FieldDefaulting.AddHandler(Document.View.Name, field, defaultingSwitchOff);
			}

			foreach (var field in detailFields)
			{
				FieldDefaulting.AddHandler(Transactions.View.Name, field, defaultingSwitchOff);
			}
		}

		public PXSetup<APSetup> APSetup;

		public SelectFrom<APInvoice>.Where<APInvoice.docType.IsEqual<APRecognizedInvoice.docType.FromCurrent>.And<
			APInvoice.refNbr.IsEqual<APRecognizedInvoice.refNbr.FromCurrent>>> Invoices;

		public PXSelect<APRecognizedInvoice> Document;

		public PXSelect<Location,
			Where<Location.bAccountID, Equal<Current<APRecognizedInvoice.vendorID>>,
				And<Location.locationID, Equal<Optional<APRecognizedInvoice.vendorLocationID>>>>>
		VendorLocation;

		protected virtual IEnumerable document()
		{
			if (Document.Current != null && Caches[typeof(CurrencyInfo)].Current != null)
			{
				if (Caches[typeof(APRegister)].Current != Document.Current)
					Caches[typeof(APRegister)].Current = Document.Current;
				yield return Document.Current;
			}
			else
			{
				var records = this.QuickSelect(Document.View.BqlSelect);
				foreach (APRecognizedInvoice record in records)
				{
					if (record.RefNbr == null && record.DocType == null)
					{
						DefaultInvoiceValues(record);
						Document.Cache.SetStatus(record, PXEntryStatus.Held);
						Caches[typeof(APRegister)].Current = record;
					}

					if (Document.Current == null) record.IsRedirect = true;
					yield return record;
				}
			}
		}

		private void DefaultInvoiceValues(APRecognizedInvoice record)
		{
			record.DocType = record.EntityType;
			var inserted = Caches[typeof(APInvoice)].Insert(record);
			Caches[typeof(APInvoice)].Remove(inserted);
			Caches[typeof(APInvoice)].RestoreCopy(record, inserted);
			Caches[typeof(APInvoice)].IsDirty = false;
		}

		public PXSelect<APRecognizedTran> Transactions;

		public PXSelect<VendorR> Vendors;
		
		public SelectFrom<APRecognizedRecord>
			  .Where<APRecognizedRecord.refNbr.IsEqual<APRecognizedInvoice.recognizedRecordRefNbr.FromCurrent>>
			  .View RecognizedRecords;

		public PXFilter<BoundFeedback> BoundFeedback;

		public PXAction<APRecognizedInvoice> ContinueSave;

		public PXAction<APRecognizedInvoice> ProcessRecognition;
		public PXAction<APRecognizedInvoice> OpenDocument;
		public PXAction<APRecognizedInvoice> OpenDuplicate;

		public PXAction<APRecognizedInvoice> DumpTableFeedback;

		public PXAction<APRecognizedInvoice> AttachFromMobile;
		[PXButton]
		[PXUIField(Visible = false, Enabled = false)]
		public virtual void attachFromMobile()
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDBString(IsKey = false)]
		protected virtual void APRecognizedInvoice_RefNbr_CacheAttached(PXCache sender)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDBString(IsKey = false)]
		protected virtual void APRecognizedInvoice_DocType_CacheAttached(PXCache sender)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PopupMessage]
		[VendorActiveOrHoldPayments(
			Visibility = PXUIVisibility.SelectorVisible,
			DescriptionField = typeof(Vendor.acctName),
			CacheGlobal = true,
			Filterable = true)]
		[PXDefault]
		protected virtual void APRecognizedInvoice_VendorID_CacheAttached(PXCache sender)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDBDefault(typeof(APRecognizedInvoice.refNbr))]
		[PXParent(typeof(
			Select<APRecognizedInvoice,
			Where<APRecognizedInvoice.docType, Equal<Current<APRecognizedTran.tranType>>, And<APRecognizedInvoice.refNbr, Equal<Current<APRecognizedTran.refNbr>>>>>
		))]
		protected virtual void _(Events.CacheAttached<APRecognizedTran.refNbr> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXFormula(
			null,
			typeof(SumCalc<APRecognizedInvoice.curyLineTotal>)
		)]
		protected virtual void _(Events.CacheAttached<APRecognizedTran.curyTranAmt> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDBDefault(typeof(APRecognizedInvoice.docType))]
		protected virtual void _(Events.CacheAttached<APRecognizedTran.tranType> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXDefault(CloudServices.Models.APInvoiceDocumentType)]
		protected virtual void APRecognizedRecord_EntityType_CacheAttached(PXCache sender)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
        [PXDefault(CloudServices.Models.APInvoiceDocumentType)]
		protected virtual void APRecognizedInvoice_EntityType_CacheAttached(PXCache sender)
		{
		}

		public IEnumerable transactions()
		{
			if (Document.Current?.RecognitionStatus == RecognizedRecordStatusListAttribute.Processed)
			{
				return SelectFrom<APRecognizedTran>.
					Where<
						APRecognizedTran.tranType.IsEqual<APRecognizedInvoice.docType.FromCurrent>.
						And<APRecognizedTran.refNbr.IsEqual<APRecognizedInvoice.refNbr.FromCurrent>>.
						And<APRecognizedTran.lineType.IsNotEqual<SOLineType.discount>>>.
					OrderBy<APRecognizedTran.tranType.Asc, APRecognizedTran.refNbr.Asc, APRecognizedTran.lineNbr.Asc>.
					View.ReadOnly.Select(this);
			}

			return Transactions.Cache.Cached;
		}

		private void RemoveAttachedFile()
		{
			if (Document.Current?.FileID == null)
			{
				return;
			}

			var fileMaint = CreateInstance<UploadFileMaintenance>();

			var fileLink = (NoteDoc)fileMaint.FileNoteDoc.Select(Document.Current.FileID, Document.Current.NoteID);
			if (fileLink == null)
			{
				return;
			}

			fileMaint.FileNoteDoc.Delete(fileLink);
			fileMaint.Persist();
			PXNoteAttribute.ResetFileListCache(Document.Cache);

			Document.Current.FileID = null;
		}

		[PXUIField(DisplayName = "Save and Continue")]
		[PXButton]
		public void continueSave()
		{
			var invoiceEntryGraph = CreateInstance<APInvoiceEntry>();
			using (var tran = new PXTransactionScope())
			{
				SaveFeedback();

				EnsureTransactions();

				Document.Cache.IsDirty = false;
				Transactions.Cache.IsDirty = false;

				invoiceEntryGraph.SelectTimeStamp();
				InsertInvoiceData(invoiceEntryGraph);
				InsertCrossReferences(invoiceEntryGraph);
				tran.Complete();
			}
            
			throw new PXRedirectRequiredException(invoiceEntryGraph, false, null);
		}

		private void SaveFeedback()
		{
			var recognizedRecord = RecognizedRecords.Current ?? RecognizedRecords.SelectSingle();
			var sb = new StringBuilder(recognizedRecord.RecognitionFeedback);
            StringWriter sw = new StringWriter(sb, CultureInfo.InvariantCulture);
			if (Document.Current.FeedbackBuilder == null)
			{
				return;
			}
            var feedbackList = Document.Current.FeedbackBuilder.ToTableFeedbackList(Transactions.View.Name);
            using (JsonTextWriter jsonWriter = new JsonTextWriter(sw){Formatting = Formatting.None})
            {
                foreach (var feedbackItem in feedbackList)
                {
                    sb.AppendLine();
                    _jsonSerializer.Serialize(jsonWriter,feedbackItem);
                }
            }
            RecognizedRecords.Cache.SetValue<RecognizedRecord.recognitionFeedback>(recognizedRecord, sw.ToString());
            RecognizedRecords.Cache.PersistUpdated(recognizedRecord);
        }

		[PXButton]
		[PXUIField(DisplayName = "Recognize")]
		public virtual IEnumerable processRecognition(PXAdapter adapter)
		{
			var refNbr = Document.Current.RecognizedRecordRefNbr.Value;
			var fileId = Document.Current.FileID.Value;
			var noteId = Document.Current.NoteID.Value;

			var logger = _logger; //to avoid closing over graph
			PXLongOperation.StartOperation(this, method: () => RecognizeInvoiceData(refNbr, fileId, logger));

			return adapter.Get();
		}

		[PXButton]
		[PXUIField(DisplayName = "Open Document")]
		public virtual void openDocument()
		{
			Document.Cache.IsDirty = false;
			Transactions.Cache.IsDirty = false;

			var recognizedInvoice = (APInvoice)
				SelectFrom<APInvoice>
				.Where<APInvoice.noteID.IsEqual<@P.AsGuid>>
				.View.ReadOnly
				.SelectSingleBound(this, null, Document.Current.DocumentLink);

			var graph = CreateInstance<APInvoiceEntry>();
			graph.Document.Current = recognizedInvoice;

			throw new PXRedirectRequiredException(graph, null);
		}

		[PXButton]
		[PXUIField(DisplayName = "Open Duplicate Document")]
		public virtual void openDuplicate()
		{
			var duplicatedRecognizedInvoice = (APRecognizedInvoice)
				SelectFrom<APRecognizedInvoice>
				.Where<APRecognizedInvoice.recognizedRecordRefNbr.IsEqual<APRecognizedInvoice.duplicateLink.FromCurrent>>
				.View.ReadOnly
				.SelectSingleBound(this, null);

			var graph = CreateInstance<APInvoiceRecognitionEntry>();
			graph.Document.Current = duplicatedRecognizedInvoice;

			throw new PXRedirectRequiredException(graph, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
		}

		[PXButton]
		[PXUIField]
		public virtual void dumpTableFeedback()
		{
			Document.Current.FeedbackBuilder?.DumpTableFeedback();
		}

		protected virtual void _(Events.RowDeleting<APRecognizedInvoice> e)
		{
			if (e.Row == null)
			{
				return;
			}

			var recognizedRecord = RecognizedRecords.SelectSingle();
			if (recognizedRecord == null)
			{
				return;
			}

			// Keep attachments for invoice
			if (recognizedRecord.Status == RecognizedRecordStatusListAttribute.Processed)
			{
				var isSharedNote = Document.Current.DocumentLink == Document.Current.NoteID;
				if (isSharedNote)
				{
				PXNoteAttribute.ForceRetain<APRecognizedRecord.noteID>(RecognizedRecords.Cache);
				PXNoteAttribute.ForceRetain<APRecognizedInvoice.noteID>(e.Cache);
			}
			}

			recognizedRecord.RecognitionResult = null;

			// Update timestamp to delete row in case of invocation from PL
			SelectTimeStamp();
			RecognizedRecords.Cache.PersistUpdated(recognizedRecord);
			RecognizedRecords.Cache.ResetPersisted(recognizedRecord);

			// Update timestamp to get newer value then timestamp from record
			SelectTimeStamp();
			RecognizedRecords.Delete(recognizedRecord);
			UpdateDuplicates(recognizedRecord.RefNbr);

			Transactions.Cache.Clear();
		}

		protected virtual void _(Events.RowPersisting<APRecognizedInvoice> e)
		{
			e.Cancel = true;
		}

		protected virtual void _(Events.FieldDefaulting<APRecognizedInvoice.hold> e)
		{
			e.NewValue = true;
		}

		protected virtual void _(Events.FieldDefaulting<APRecognizedInvoice.allowFilesMsg> e)
		{
			e.NewValue = PXMessages.LocalizeFormatNoPrefixNLA(Web.UI.Msg.ErrFileTypesAllowed, PdfExtension);
		}

		protected virtual void _(Events.FieldDefaulting<APInvoice.docDate> e)
		{
			e.NewValue = null;
			e.Cancel = true;
		}

		protected virtual void _(Events.FieldDefaulting<BAccountR.type> e)
		{
			e.NewValue = BAccountType.VendorType;
		}

		protected virtual void _(Events.FieldUpdated<APRecognizedInvoice.vendorID> e)
		{
			var transactions = new List<APRecognizedTran>();

			foreach (APRecognizedTran tran in Transactions.Select())
			{
				if (tran.InventoryIDManualInput != true)
				{
					SetTranInventoryID(Transactions.Cache, tran);
				}

				transactions.Add(tran);
			}

			if (transactions.Count > 0)
			{
				SetTranRecognizedPONumbers(Transactions.Cache, transactions, false);
			}
		}

		private void SetTranRecognizedPONumbers(int? vendorId, HashSet<int?> inventoryIds)
		{
			SetTranRecognizedPONumbers(vendorId, inventoryIds, null);
		}

		private void SetTranRecognizedPONumbers(int? vendorId, HashSet<int?> inventoryIds, APRecognizedTran apTran)
		{
			var recognizedPONumbers = GetRecognizedPONumbers(vendorId, inventoryIds);

			if (apTran == null)
			{
				foreach (APRecognizedTran tran in Transactions.Select())
				{
					if (!inventoryIds.Contains(tran.InventoryID))
					{
						continue;
					}

					SetPOLink(tran, recognizedPONumbers);
				}
			}
			else
			{
				if (!inventoryIds.Contains(apTran.InventoryID))
				{
					return;
				}

				SetPOLink(apTran, recognizedPONumbers);
			}
		}

		public virtual void SetPOLink(APRecognizedTran apTran, IList<(int? InventoryId, string PONumber, PageWord PageWord)> recognizedPONumbers)
		{
			(int? _, string poNumber, PageWord pageWord) = recognizedPONumbers.FirstOrDefault(r => apTran.InventoryID == r.InventoryId);

			var poNumberJson = JsonConvert.SerializeObject(pageWord);
			var poNumberEncodedJson = HttpUtility.UrlEncode(poNumberJson);

			Transactions.Cache.SetValueExt<APRecognizedTran.pONumberJson>(apTran, poNumberEncodedJson);
			AutoLinkAPAndPO(apTran, poNumber);
		}

		//see LinkRecognizedLineExtension for implementation
		public virtual void AutoLinkAPAndPO(APRecognizedTran tran, string poNumber)
		{
		}

		private IList<(int? InventoryId, string PONumber, PageWord PageWord)> GetRecognizedPONumbers(int? vendorId, HashSet<int?> inventoryIds)
		{
			var poNumbers = new List<(int? inventoryId, string pONumber, PageWord pageWord)>();

			var recognizedRecord = RecognizedRecords.SelectSingle();
			if (recognizedRecord == null || string.IsNullOrEmpty(recognizedRecord.RecognitionResult))
			{
				return poNumbers;
			}

			var recognitionResultTyped = JsonConvert.DeserializeObject<DocumentRecognitionResult>(recognizedRecord.RecognitionResult);
			var pageCount = recognitionResultTyped?.Pages?.Count;

			if (pageCount == null || pageCount == 0)
			{
				return poNumbers;
			}

			var poInfos = GetPONumbers(vendorId, inventoryIds);
			if (poInfos.Count == 0)
			{
				return poNumbers;
			}

			foreach (var (inventoryId, pONumber) in poInfos)
			{
				PageWord pageWord = null;

				for (var pageIndex = 0; pageIndex < recognitionResultTyped.Pages?.Count && pageWord == null; pageIndex++)
				{
					var page = recognitionResultTyped.Pages[pageIndex];

					for (var wordIndex = 0; wordIndex < page?.Words?.Count && pageWord == null; wordIndex++)
					{
						var word = page.Words[wordIndex];

						if (!string.Equals(word?.Text, pONumber, StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}

						pageWord = new PageWord
						{
							Page = pageIndex,
							Word = wordIndex
						};
					}
				}

				if (pageWord == null)
				{
					continue;
				}

				poNumbers.Add((inventoryId, pONumber, pageWord));
			}

			return poNumbers;
		}

		protected virtual void _(Events.RowInserted<APRecognizedInvoice> e)
		{
            if (e.Row != null)
                Caches[typeof(APRegister)].Current = e.Row;
        }

		protected virtual void _(Events.RowSelected<APRecognizedInvoice> e)
		{
			Document.View.SetAnswer(null, WebDialogResult.OK);

            if (!(e.Row is APRecognizedInvoice document)) return;
            if(e.Row.DocType==null||e.Row.CuryInfoID==null)
                DefaultInvoiceValues(document);
            var recognizedRecord = RecognizedRecords.Current??RecognizedRecords.SelectSingle();
            if (recognizedRecord != null)
                e.Row.RecognitionStatus = recognizedRecord.Status;
            if (e.Row.IsRedirect == true)
            {
				if (recognizedRecord != null)
				{
					recognizedRecord.IsDataLoaded = false;
				}

				e.Row.IsRedirect = false;
            }

            if (e.Row.RecognizedRecordRefNbr != null && recognizedRecord?.IsDataLoaded != true)
            {
                RecognizedRecords.Cache.SetValue<APRecognizedRecord.recognitionFeedback>(recognizedRecord, null);
                LoadRecognizedData();
            }

            if (e.Row.NoteID != null)
            {
                ProcessFile(e.Cache, e.Row);
            }

            e.Row.AllowUploadFile = e.Row.RecognitionStatus ==
                                    APRecognizedInvoiceRecognitionStatusListAttribute.PendingFile ||
                                    e.Row.RecognitionStatus ==
                                    RecognizedRecordStatusListAttribute.PendingRecognition ||
                                    e.Row.RecognitionStatus == RecognizedRecordStatusListAttribute.Error;

			var showDelete = e.Row.RecognitionStatus != APRecognizedInvoiceRecognitionStatusListAttribute.PendingFile;
            Delete.SetVisible(showDelete);

            var showSaveContinue = e.Row.RecognitionStatus == RecognizedRecordStatusListAttribute.Recognized ||
                                   e.Row.RecognitionStatus == RecognizedRecordStatusListAttribute.Error;
            ContinueSave.SetVisible(showSaveContinue);

			var showProcessRecognition = e.Row.FileID != null &&
										 e.Row.RecognitionStatus == RecognizedRecordStatusListAttribute.PendingRecognition;

            ProcessRecognition.SetVisible(showProcessRecognition);

            var showOpenDocument = e.Row.RecognitionStatus == RecognizedRecordStatusListAttribute.Processed;
            OpenDocument.SetVisible(showOpenDocument);

            var showOpenDuplicate = e.Row.DuplicateLink != null;
            OpenDuplicate.SetVisible(showOpenDuplicate);
            if (showOpenDuplicate)
            {
                if (recognizedRecord != null)
                {
                    var duplicate = CheckForDuplicates(recognizedRecord.RefNbr, recognizedRecord.FileHash);
                    var warning = PXMessages.LocalizeFormatNoPrefixNLA(Messages.DuplicateFileForRecognitionTooltip,
                        duplicate.Subject);

                    PXUIFieldAttribute.SetWarning<APRecognizedInvoice.recognitionStatus>(e.Cache, e.Row, warning);
                }
            }

			if (e.Row.RecognitionStatus == RecognizedRecordStatusListAttribute.Error && !string.IsNullOrEmpty(e.Row.ErrorMessage))
			{
				PXUIFieldAttribute.SetError<APRecognizedInvoice.recognitionStatus>(e.Cache, e.Row, e.Row.ErrorMessage, e.Row.RecognitionStatus);
			}

            var allowEdit = e.Row.RecognitionStatus == RecognizedRecordStatusListAttribute.Recognized ||
                            e.Row.RecognitionStatus == RecognizedRecordStatusListAttribute.Error;
            Document.AllowInsert = allowEdit;
            Document.AllowUpdate = allowEdit;
            Document.AllowDelete = allowEdit;
            Transactions.AllowInsert = allowEdit;
            Transactions.AllowUpdate = allowEdit;
            Transactions.AllowDelete = allowEdit;

			HideNotSupportedActions();
		}

		private void HideNotSupportedActions()
		{
			var attachFromScanner = Actions[ActionsMessages.AttachFromScanner];
			if (attachFromScanner != null)
			{
				attachFromScanner.SetVisible(false);
			}

			// Replace the action as we cannot control its visibility
			// because it is based not on a primary view
			var attachFromMobile = Actions[nameof(AttachFromMobile)];
			if (attachFromMobile != null)
			{
				Actions[nameof(AttachFromMobile)] = AttachFromMobile;
			}
        }

		protected virtual void _(Events.FieldUpdated<APRecognizedInvoice.docType> e)
		{
			if (!(e.Args.Row is APRecognizedInvoice row))
			{
				return;
			}

			var docType = row.DocType;
			var drCr = row.DrCr;

			foreach (APRecognizedTran tran in Transactions.Select())
			{
				Transactions.Cache.SetValue<APRecognizedTran.tranType>(tran, docType);
				Transactions.Cache.SetValue<APRecognizedTran.drCr>(tran, drCr);

				if (tran.InventoryID != null)
				{
					object inventoryId = tran.InventoryID;

					try
					{
						Transactions.Cache.RaiseFieldVerifying<APRecognizedTran.inventoryID>(tran, ref inventoryId);
					}
					catch (PXSetPropertyException exception)
					{
						Transactions.Cache.RaiseExceptionHandling<APRecognizedTran.inventoryID>(tran, inventoryId, exception);
					}
				}
			}
		}

		protected virtual void _(Events.FieldUpdating<BoundFeedback.tableRelated> e)
		{
			var document = Document.Current;
			if (document == null)
			{
				return;
			}

			var unsupportedDocType = !APDocType.Invoice.Equals(document.DocType, StringComparison.Ordinal);
			if (unsupportedDocType)
			{
				return;
			}

			var feedbackBuilder = document.FeedbackBuilder;
			if (feedbackBuilder == null)
			{
				return;
			}

			var cellBoundJsonEncoded = e.NewValue as string;
			if (string.IsNullOrWhiteSpace(cellBoundJsonEncoded))
			{
				return;
			}

			var cellBoundJson = HttpUtility.UrlDecode(cellBoundJsonEncoded);
			feedbackBuilder.ProcessCellBound(cellBoundJson);

			e.NewValue = null;
		}

		protected virtual void _(Events.FieldUpdating<BoundFeedback.fieldBound> e)
		{
			var document = Document.Current;
            var recognizedRecord = RecognizedRecords.Current;
			if (document == null || recognizedRecord == null)
			{
				return;
			}

            var unsupportedDocType = !APDocType.Invoice.Equals(document.DocType, StringComparison.Ordinal);
			if (unsupportedDocType)
			{
				return;
			}

			var feedbackBuilder = document.FeedbackBuilder;
			if (feedbackBuilder == null)
			{
				return;
			}

			var documentJsonEncoded = e.NewValue as string;
			if (string.IsNullOrWhiteSpace(documentJsonEncoded))
			{
				return;
			}

			var documentJson = HttpUtility.UrlDecode(documentJsonEncoded);
			var fieldBoundFeedback = feedbackBuilder.ToFieldBoundFeedback(documentJson);
			if (fieldBoundFeedback == null)
			{
				return;
			}

            var sb = new StringBuilder(recognizedRecord.RecognitionFeedback);
            StringWriter sw = new StringWriter(sb, CultureInfo.InvariantCulture);
            using (JsonTextWriter jsonWriter = new JsonTextWriter(sw){Formatting = Formatting.None})
            {
                sb.AppendLine();
                _jsonSerializer.Serialize(jsonWriter,fieldBoundFeedback);
            }
            RecognizedRecords.Cache.SetValue<APRecognizedRecord.recognitionFeedback>(recognizedRecord, sw.ToString());
            e.NewValue = null;
		}

		protected virtual void _(Events.RowPersisting<APRecognizedTran> e)
		{
			e.Cancel = true;
		}

		protected virtual void _(Events.RowSelected<APRecognizedTran> e)
		{
			if (e.Row == null)
			{
				return;
			}

			var alternateIdWarning = e.Row.AlternateID != null && e.Row.NumOfFoundIDByAlternate > 1 ?
				PXMessages.LocalizeNoPrefix(CrossItemAttribute.CrossItemMessages.ManyItemsForCurrentAlternateID):
				null;

			PXUIFieldAttribute.SetWarning<APRecognizedTran.alternateID>(e.Cache, e.Row, alternateIdWarning);
		}

		protected virtual void _(Events.FieldUpdated<APRecognizedTran.alternateID> e)
		{
			if (!(e.Row is APRecognizedTran row))
			{
				return;
			}

			SetTranInventoryID(e.Cache, row);
			SetTranRecognizedPONumbers(e.Cache, new[] { row }, false);
		}

		private void SetTranInventoryID(PXCache cache, APRecognizedTran tran)
		{
			cache.SetValueExt<APRecognizedTran.internalAlternateID>(tran, tran.AlternateID);

			if (tran.InternalAlternateID == null && tran.InventoryIDManualInput == true)
			{
				return;
			}

			cache.SetValueExt<APRecognizedTran.inventoryID>(tran, tran.InternalAlternateID);
			Transactions.View.RequestRefresh();
		}

		protected virtual void _(Events.FieldUpdated<APRecognizedTran.inventoryID> e)
		{
			if (!(e.Row is APRecognizedTran row))
			{
				return;
			}

			var inventoryId = e.NewValue as int?;
			if (inventoryId != null)
			{
				e.Cache.SetDefaultExt<APRecognizedTran.uOM>(row);
			}

			var isManualInput = e.ExternalCall && e.NewValue != null;
			SetTranRecognizedPONumbers(e.Cache, new[] { row }, isManualInput);
		}

		private void SetTranRecognizedPONumbers(PXCache cache, IEnumerable<APRecognizedTran> transactions, bool isManualInput)
		{
			var vendorId = Document.Current?.VendorID;
			var inventoryIds = new HashSet<int?>();

			foreach (var tran in transactions)
			{
				if (tran.InventoryID != null)
				{
					if (tran.InventoryIDManualInput != true)
					{
						tran.InventoryIDManualInput = isManualInput;
					}

					if (vendorId != null)
					{
						inventoryIds.Add(tran.InventoryID);
					}
					else
					{
						cache.SetValueExt<APRecognizedTran.pONumberJson>(tran, null);
						AutoLinkAPAndPO(tran, null);
					}
				}
				else
				{
					cache.SetValueExt<APRecognizedTran.pONumberJson>(tran, null);
					AutoLinkAPAndPO(tran, null);
				}
			}

			if (inventoryIds.Count > 0)
			{
				if (transactions.Count() == 1)
				{
					SetTranRecognizedPONumbers(vendorId, inventoryIds, transactions.First());
				}
				else
				{
					SetTranRecognizedPONumbers(vendorId, inventoryIds);
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<APRecognizedTran.uOM> e)
		{
			if (!(e.Row is APRecognizedTran row))
			{
				return;
			}
			if (e.ExternalCall)
			{
				var uOM = e.NewValue as string;
				if (uOM == null)
				{
					AutoLinkAPAndPO(row, null);
				}
			}
		}

		protected virtual void _(Events.RowPersisting<CurrencyInfo> e)
		{
			e.Cancel = true;
		}

		internal static bool IsAllowedFile(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				return false;
			}

			var fileExtension = Path.GetExtension(name);

			return string.Equals(fileExtension, PdfExtension, StringComparison.OrdinalIgnoreCase);
		}

		private void LoadRecognizedData()
		{
			var recognizedRecord = RecognizedRecords.SelectSingle();
			if(recognizedRecord==null)
				return;

			if (string.IsNullOrEmpty(recognizedRecord.RecognitionResult))
			{
				Document.Current.RecognitionStatus = recognizedRecord.Status;
				Document.Current.DuplicateLink = recognizedRecord.DuplicateLink;

				return;
			}

			var recognitionResult = JsonConvert.DeserializeObject<DocumentRecognitionResult>(recognizedRecord.RecognitionResult);

			LoadRecognizedDataToGraph(this, recognizedRecord, recognitionResult);
		}

		private void ProcessFile(PXCache cache, APRecognizedInvoice invoice)
        {
			// File notes has random order as NoteDoc doesn't contain CreatedDateTime column
            var fileNotes = PXNoteAttribute.GetFileNotes(cache, invoice);

			if (fileNotes == null || fileNotes.Length == 0)
			{
				if (invoice.FileID != null)
				{
					RemoveAttachedFile();
					UpdateFileInfo(null);

					invoice.FileID = null;
				}

				return;
			}

			var fileId = fileNotes[0];
			var file = GetFile(this, fileId);

			if (invoice.RecognitionStatus == APRecognizedInvoiceRecognitionStatusListAttribute.PendingFile)
			{
				invoice.RecognitionStatus = RecognizedRecordStatusListAttribute.PendingRecognition;

				var recognizedRecord = CreateRecognizedRecord(file.Name, file.Data, invoice, fileId);

                invoice.EntityType = recognizedRecord.EntityType;
                invoice.FileHash = recognizedRecord.FileHash;
                invoice.RecognitionStatus = recognizedRecord.Status;
				invoice.DuplicateLink = recognizedRecord.DuplicateLink;
			}
			else if (invoice.FileID != null)
			{
				// File notes ordered by created time descending
				if (invoice.FileID != fileId)
				{
					RemoveAttachedFile();
					UpdateFileInfo(file);
				}
				// File notes ordered by created time ascending
				else if (fileNotes.Length == 2)
			{
					fileId = fileNotes[1];
					file = GetFile(this, fileId);

					RemoveAttachedFile();
				UpdateFileInfo(file);
			}
			}

			if (file == null)
			{
				return;
			}

			invoice.FileID = fileId;

			// To load restricted file by page via GetFile.ashx
			var fileInfoInMemory = new PX.SM.FileInfo(fileId, file.Name, null, file.Data);
			PXContext.SessionTyped<PXSessionStatePXData>().FileInfo[fileInfoInMemory.UID.ToString()] = fileInfoInMemory;
		}

        

        private void UpdateFileInfo(UploadFile file)
		{
			var recognizedRecord = RecognizedRecords.SelectSingle();
			if (recognizedRecord == null)
			{
				return;
			}

			if (file == null)
			{
				recognizedRecord.Subject = null;
				recognizedRecord.FileHash = null;
				recognizedRecord.DuplicateLink = null;
			}
			else
			{
				var originalFileName = PX.SM.FileInfo.GetShortName(file.Name);
				recognizedRecord.Subject = GetRecognizedSubject(null, originalFileName);
				recognizedRecord.FileHash = ComputeFileHash(file.Data);

				SetDuplicateInfo(recognizedRecord);
			}

			recognizedRecord.Owner = PXAccess.GetContactID();

			Caches[typeof(Note)].Clear();
			SelectTimeStamp();
			RecognizedRecords.Cache.PersistUpdated(recognizedRecord);
			RecognizedRecords.Cache.Clear();
			RecognizedRecords.Cache.IsDirty = false;
			SelectTimeStamp();
		}

		public APRecognizedRecord CreateRecognizedRecord(string fileName, byte[] fileData, Guid fileId, string description = null, string mailFrom = null, string messageId = null,
			int? owner = null, Guid? noteId = null)
		{
			fileName.ThrowOnNullOrWhiteSpace(nameof(fileName));
			fileData.ThrowOnNull(nameof(fileData));

			var recognizedRecord = RecognizedRecords.Insert();
			var originalFileName = PX.SM.FileInfo.GetShortName(fileName);

			recognizedRecord.Subject = description ?? GetRecognizedSubject(null, originalFileName);
			recognizedRecord.MailFrom = mailFrom;
			recognizedRecord.MessageID = string.IsNullOrWhiteSpace(messageId) ?
				messageId :
				NormalizeMessageId(messageId);
			recognizedRecord.FileHash = ComputeFileHash(fileData);
			recognizedRecord.Owner = owner ?? PXAccess.GetContactID();
            recognizedRecord.CloudTenantId = _cloudTenantService.TenantId;
            recognizedRecord.ModelName = InvoiceRecognitionClient.ModelName;
            recognizedRecord.CloudFileId = fileId;
			if (noteId != null)
			{
				recognizedRecord.NoteID = noteId;
			}

			SetDuplicateInfo(recognizedRecord);

			RecognizedRecords.Cache.PersistInserted(recognizedRecord);
			RecognizedRecords.Cache.Persisted(false);
			SelectTimeStamp();

			return recognizedRecord;
		}

        private RecognizedRecord CreateRecognizedRecord(string fileName, byte[] fileData, APRecognizedInvoice recognizedInvoice, Guid fileId)
        {
            fileName.ThrowOnNullOrWhiteSpace(nameof(fileName));
            fileData.ThrowOnNull(nameof(fileData));
            var recognizedRecord = (RecognizedRecord) RecognizedRecords.Cache.CreateInstance();
            recognizedRecord.NoteID = recognizedInvoice.NoteID;
            recognizedRecord.CustomInfo = recognizedInvoice.CustomInfo;
            recognizedRecord.DocumentLink = recognizedInvoice.DocumentLink;
            recognizedRecord.DuplicateLink = recognizedInvoice.DuplicateLink;
            recognizedRecord.EntityType = recognizedInvoice.EntityType;
            recognizedRecord.FileHash = ComputeFileHash(fileData);
            recognizedRecord.MailFrom = recognizedInvoice.MailFrom;
            recognizedRecord.MessageID = recognizedInvoice.MessageID;
            recognizedRecord.Owner = recognizedInvoice.Owner ?? PXAccess.GetContactID();
            recognizedRecord.RecognitionResult = recognizedInvoice.RecognitionResult;
            recognizedRecord.RecognitionStarted = recognizedInvoice.RecognitionStarted;
            recognizedRecord.RefNbr = recognizedInvoice.RecognizedRecordRefNbr;
            recognizedRecord.Status = recognizedInvoice.RecognitionStatus;
            var originalFileName = PX.SM.FileInfo.GetShortName(fileName);
            recognizedRecord.Subject = GetRecognizedSubject(null, originalFileName);
            SetDuplicateInfo(recognizedRecord);
            recognizedRecord.CloudTenantId = _cloudTenantService.TenantId;
            recognizedRecord.ModelName = InvoiceRecognitionClient.ModelName;
            recognizedRecord.CloudFileId = fileId;
            recognizedRecord = (RecognizedRecord)RecognizedRecords.Cache.Insert(recognizedRecord);
			RecognizedRecords.Cache.SetStatus(recognizedRecord, PXEntryStatus.Notchanged);
            RecognizedRecords.Cache.PersistInserted(recognizedRecord);
			RecognizedRecords.Cache.IsDirty = false;
			SelectTimeStamp();
            return recognizedRecord;
        }

		internal static byte[] ComputeFileHash(byte[] data)
		{
			using (var provider = new MD5CryptoServiceProvider())
			{
				return provider.ComputeHash(data);
			}
		}

		private void SetDuplicateInfo(RecognizedRecord recognizedRecord)
		{
			var duplicate = CheckForDuplicates(recognizedRecord.RefNbr, recognizedRecord.FileHash);

			if (duplicate.RefNbr == null)
			{
				return;
			}

			recognizedRecord.DuplicateLink = duplicate.RefNbr;
		}

		private void EnsureTransactions()
		{
			var detailsNotEmpty = Transactions.Cache.Cached
				.Cast<object>()
				.Any();
			if (detailsNotEmpty)
			{
                foreach (APRecognizedTran tran in Transactions.Cache.Cached)
                {
                    var documentCuryInfo = Document.Current?.CuryInfoID;
                    if(tran.CuryInfoID!=documentCuryInfo)
                        Transactions.Cache.SetValue<APRecognizedTran.curyInfoID>(tran, documentCuryInfo);
                }
				return;
			}

			var document = Document.Current;
			if (document == null)
			{
				return;
			}

			var summaryDetail = Transactions.Insert();
			if (summaryDetail == null)
			{
				return;
			}

			summaryDetail.TranDesc = document.DocDesc;
			summaryDetail.CuryLineAmt = document.CuryOrigDocAmt;

			Transactions.Update(summaryDetail);
		}

		private void InsertRowWithFieldValues(PXCache sourceCache, PXCache destCache, IEnumerable<string> fieldsToCopy,
			HashSet<string> forcedFields, object sourceRow)
		{
			var newRow = destCache.Insert();
			var cacheFields = fieldsToCopy.Where(field => destCache.Fields.Contains(field));

			foreach(var field in cacheFields)
			{
				var valueExt = sourceCache.GetValueExt(sourceRow, field);
				var value = PXFieldState.UnwrapValue(valueExt);
				if (value == null)
				{
					continue;
				}

				// To obtain correct state
				destCache.RaiseRowSelected(newRow);

				var state = destCache.GetStateExt(newRow, field) as PXFieldState;
				if (state?.Enabled == false && forcedFields?.Contains(field) != true)
				{
					continue;
				}

				destCache.SetValueExt(newRow, field, value);
				destCache.Update(newRow);
			}

			destCache.SetStatus(newRow, PXEntryStatus.Inserted);
		}

		private void InsertInvoiceData(APInvoiceEntry graph)
		{
			var (primaryFields, detailFields) = GetUIFields();
			var holdField = new[] { nameof(APInvoice.Hold) };
			primaryFields = holdField.Union(primaryFields, StringComparer.OrdinalIgnoreCase);
			InsertRowWithFieldValues(Document.Cache, graph.Document.Cache, primaryFields, holdField.ToHashSet(), Document.Current);

			graph.Document.Cache.SetValueExt<APInvoice.noteID>(graph.Document.Current, null);
			graph.Caches<APVendorRefNbr>().Clear(); // It contains old row with old noteID
			PXNoteAttribute.CopyNoteAndFiles(Document.Cache, Document.Current, graph.Document.Cache, graph.Document.Current, false, true);

			var manualFields = new[] { nameof(APTran.ManualPrice), nameof(APTran.ManualDisc) };
			detailFields = manualFields.Union(detailFields, StringComparer.OrdinalIgnoreCase);
			var detailFieldsWithPO = detailFields.Union(LinkRecognizedLineExtension.APTranPOFields, StringComparer.OrdinalIgnoreCase);
			var detailPOFields = LinkRecognizedLineExtension.APTranPOFields;
			foreach (APRecognizedTran tran in Transactions.Select())
			{
				IEnumerable<string> fieldsToCopy;
				HashSet<string> forcedFields;

				tran.ManualPrice = true;
				tran.ManualDisc = true;

				if (tran.POLinkStatus == APPOLinkStatus.Linked)
				{
					fieldsToCopy = detailFieldsWithPO;
					forcedFields = detailPOFields
						.Union(manualFields, StringComparer.OrdinalIgnoreCase)
						.ToHashSet();
				}
				else
				{
					fieldsToCopy = detailFields;
					forcedFields = manualFields.ToHashSet();
				}

				InsertRowWithFieldValues(Transactions.Cache, graph.Transactions.Cache, fieldsToCopy, forcedFields, tran);
			}

			var invoiceEntryExt = graph.GetExtension<APInvoiceEntryExt>();
			invoiceEntryExt.FeedbackParameters.Current.FeedbackBuilder = Document.Current.FeedbackBuilder;
			invoiceEntryExt.FeedbackParameters.Current.Links = Document.Current.Links;
			invoiceEntryExt.FeedbackParameters.Current.FeedbackFileID = Document.Current.FileID;

			var recognizedRecord = PXSelect<RecognizedRecord, Where<RecognizedRecord.refNbr, Equal<PX.Data.Required<RecognizedRecord.refNbr>>>>.Select(graph, Document.Current.RecognizedRecordRefNbr).FirstTableItems.FirstOrDefault();
			if(recognizedRecord==null)
				return;
            recognizedRecord.DocumentLink = graph.Document.Current.NoteID;
			recognizedRecord.Status = RecognizedRecordStatusListAttribute.Processed;

			var recognizedRecordCache = graph.Caches[typeof(RecognizedRecord)];
			recognizedRecordCache.Update(recognizedRecord);

			RecognizedRecords.View.Clear();
		}

		private void InsertCrossReferences(APInvoiceEntry graph)
		{
			if (Document.Current?.VendorID == null)
			{
				return;
			}

			var transactionsWitXref = Transactions
				.Select()
				.Select(r => r.GetItem<APRecognizedTran>())
				.Where(t => !string.IsNullOrEmpty(t.AlternateID) &&
							t.InventoryID != null)
				.ToList();
			if (transactionsWitXref.Count == 0)
			{
				return;
			}

			var xRefCache = graph.Caches[typeof(INItemXRef)];
			xRefCache.RaiseFieldDefaulting<INItemXRef.subItemID>(null, out var defSubItemId);

			var xRefView = new
				SelectFrom<INItemXRef>.
				Where<Match<AccessInfo.userName.FromCurrent>.And<
					  INItemXRef.inventoryID.IsEqual<@P.AsInt>.And<
					  INItemXRef.alternateType.IsEqual<INAlternateType.vPN>>.And<
					  INItemXRef.bAccountID.IsEqual<@P.AsInt>>>.And<
					  INItemXRef.alternateID.IsEqual<@P.AsString>>.And<
					  INItemXRef.subItemID.IsEqual<@P.AsInt>>>.
				View.ReadOnly(graph);

			foreach (var tran in transactionsWitXref)
			{
				var isRefExists = xRefView.SelectSingle(tran.InventoryID, Document.Current.VendorID, tran.AlternateID, defSubItemId) != null;
				if (isRefExists)
				{
					continue;
				}

				var newXref = new INItemXRef
				{
					InventoryID = tran.InventoryID,
					AlternateType = INAlternateType.VPN,
					BAccountID = Document.Current.VendorID,
					AlternateID = tran.AlternateID,
					SubItemID = defSubItemId as int?
				};

				xRefCache.Insert(newXref);
			}
		}

		internal static string NormalizeMessageId(string rawMessageId)
		{
			rawMessageId.ThrowOnNullOrWhiteSpace(nameof(rawMessageId));

			var braceIndex = rawMessageId.IndexOf('>');
			if (braceIndex == -1 || braceIndex == rawMessageId.Length - 1)
			{
				return rawMessageId;
			}

			return rawMessageId.Substring(0, braceIndex + 1);
		}

		internal static string GetRecognizedSubject(string emailSubject, string fileName)
		{
			if (string.IsNullOrWhiteSpace(emailSubject))
			{
				return fileName;
			}

			return $"{emailSubject}: {fileName}";
		}

		public (Guid? RefNbr, string Subject) CheckForDuplicates(Guid? recognizedRefNbr, byte[] fileHash)
		{
			var duplicateRecord = (RecognizedRecord)
				SelectFrom<RecognizedRecord>
				.Where<RecognizedRecord.refNbr.IsNotEqual<@P.AsGuid>.And<
					   RecognizedRecord.fileHash.IsEqual<@P.AsByteArray>>>
				.OrderBy<RecognizedRecord.createdDateTime.Asc>
				.View.ReadOnly.Select(this, recognizedRefNbr, fileHash);

			if (duplicateRecord == null)
			{
				return (null, null);
			}

			return (duplicateRecord.RefNbr, duplicateRecord.Subject);
		}

		public void UpdateDuplicates(Guid? refNbr)
		{
			var duplicatesView = new SelectFrom<APRecognizedRecord>
				.Where<RecognizedRecord.duplicateLink.IsEqual<@P.AsGuid>>
				.OrderBy<RecognizedRecord.createdDateTime.Asc>
				.View(this);
			var newDuplicateLink = default(Guid?);

			foreach (APRecognizedRecord record in duplicatesView.Select(refNbr))
			{
				if (newDuplicateLink == null)
				{
					newDuplicateLink = record.RefNbr;
					record.DuplicateLink = null;
				}
				else
				{
					record.DuplicateLink = newDuplicateLink;
				}

				RecognizedRecords.Update(record);
			}
		}

		public static void RecognizeInvoiceData(Guid recognizedRecordRefNbr, Guid fileId, ILogger logger)
		{
			var graph = CreateInstance<APInvoiceRecognitionEntry>();
           
            graph.RecognizeInvoiceDataInternal(recognizedRecordRefNbr, fileId, logger);
        }

		private void RecognizeInvoiceDataInternal(Guid recognizedRecordRefNbr, Guid fileId, ILogger logger)
		{
			var document = (APRecognizedInvoice)Document.Cache.CreateInstance();
			document.RecognizedRecordRefNbr = recognizedRecordRefNbr;
			int maxrows = 1;
			int startrow = 0;
			var result = Document.View.Select(null, null, new object[] {recognizedRecordRefNbr},
				new string[] {nameof(APRecognizedInvoice.recognizedRecordRefNbr)}, new[] {false}, null, ref startrow, 1, ref maxrows).FirstOrDefault();
			if (result!=null)
				Document.Current = result as APRecognizedInvoice;
			var recognizedRecord = RecognizedRecords.SelectSingle();

			document.RecognitionStatus = recognizedRecord.Status;

			try
			{
				var file = GetFile(this, fileId);

				if (!IsAllowedFile(file.Name))
				{
					var message = PXMessages.LocalizeFormatNoPrefixNLA(Messages.InvalidFileForRecognition, PdfExtension);

					throw new PXArgumentException(nameof(file), message);
				}

				if (recognizedRecord.RecognitionStarted != true)
				{
					MarkRecognitionStarted(this, recognizedRecord, null);
				}

				DocumentRecognitionResult recognitionResult = null;
				string errorMessage = null;
				try
				{
					recognitionResult = GetRecognitionInfo(InvoiceRecognitionClient, file, logger).Result;
				}
				catch (AggregateException e)
				{
					errorMessage = string.Join(Environment.NewLine, e.InnerExceptions.Select(i => i.Message));
					throw;
				}
				finally
				{
					UpdateRecognizedRecord(this, recognizedRecord, recognitionResult, errorMessage);
				}
            }
			catch
			{
				document.RecognitionStatus = RecognizedRecordStatusListAttribute.Error;

				throw;
			}
		}

		private static UploadFile GetFile(PXGraph graph, Guid fileId)
		{
			var result = (PXResult<UploadFile, UploadFileRevision>)
				PXSelectJoin<UploadFile,
				InnerJoin<UploadFileRevision,
				On<UploadFile.fileID, Equal<UploadFileRevision.fileID>, And<
				   UploadFile.lastRevisionID, Equal<UploadFileRevision.fileRevisionID>>>>,
				Where<UploadFile.fileID, Equal<Required<UploadFile.fileID>>>>.Select(graph, fileId);
			if (result == null)
			{
				return null;
			}

			var file = (UploadFile)result;
			var fileRevision = (UploadFileRevision)result;

			file.Data = fileRevision.Data;

			return file;
		}

		private static void MarkRecognitionStarted(APInvoiceRecognitionEntry graph, APRecognizedRecord record, string url)
		{
			record.RecognitionStarted = true;
			record.Status = RecognizedRecordStatusListAttribute.InProgress;
            record.ResultUrl = url;

			graph.RecognizedRecords.Update(record);
			graph.Persist();
		}

        private static void UpdateRecognizedRecordUrl(APInvoiceRecognitionEntry graph, APRecognizedRecord record, string url)
        {
            record.ResultUrl = url;
            record = graph.RecognizedRecords.Update(record);
            graph.RecognizedRecords.Cache.PersistUpdated(record);
            graph.RecognizedRecords.Cache.Persisted(false);
            graph.SelectTimeStamp();
        }

		private static void UpdateRecognizedRecord(APInvoiceRecognitionEntry graph, APRecognizedRecord record,
			DocumentRecognitionResult recognitionResult, string errorMessage)
		{
			var isError = recognitionResult == null;

			record.Status = isError ?
				RecognizedRecordStatusListAttribute.Error :
				RecognizedRecordStatusListAttribute.Recognized;
			record.RecognitionResult = JsonConvert.SerializeObject(recognitionResult);
            record.PageCount = recognitionResult?.Pages?.Count ?? 0;

			if (!string.IsNullOrEmpty(errorMessage))
			{
				record.ErrorMessage = errorMessage;
			}
			else if (recognitionResult == null)
			{
				record.ErrorMessage = Messages.RecognitionServiceNoResult;
			}
			else if (recognitionResult.Pages == null || recognitionResult.Pages.Count == 0)
			{
				record.ErrorMessage = Messages.RecognitionServiceEmptyResult;
			}

			record = graph.RecognizedRecords.Update(record);
			graph.RecognizedRecords.Cache.PersistUpdated(record);
			graph.RecognizedRecords.Cache.Persisted(false);
			graph.SelectTimeStamp();
		}

		private static async Task<DocumentRecognitionResult> GetRecognitionInfo(IInvoiceRecognitionService client,
			UploadFile file, Serilog.ILogger logger)
		{
			var extension = Path.GetExtension(file.Name);
			var mimeType = MimeTypes.GetMimeType(extension);

			using (var op = logger.OperationAt(LogEventLevel.Verbose, LogEventLevel.Error)
				.Begin("Recognizing document"))
			{
				using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(_recognitionTimeoutMinutes)))
				{
					try
					{
						var response = await client.SendFile(file.FileID.Value, file.Data, mimeType, cancellationTokenSource.Token);
						var result = await PollForResults(client, response, logger, cancellationTokenSource.Token);
						op.Complete();
						return result;
					}
					catch (Exception e)
					{
						op.SetException(e);
						throw;
					}
				}
			}
		}

		private static async Task<DocumentRecognitionResult> PollForResults(
			IInvoiceRecognitionService imageRecognitionWebClient, DocumentRecognitionResponse response, ILogger logger,
			CancellationToken cancellationToken)
		{
			var (result, state) = response;
			if (result != null)
			{
				LogSyncResponse(logger);
				return result;
			}

			using (var op = logger.BeginOperationVerbose("Polling for recognition results"))
			{
				var attempts = 0;
				while (!cancellationToken.IsCancellationRequested)
				{
					if (state == null)
						throw new InvalidOperationException("Unexpected empty state in document recognition response");

					attempts++;
					(result, state) = await imageRecognitionWebClient.GetResult(state, cancellationToken);
					if (result != null)
					{
						op.Complete("Attempts", attempts);
						return result;
					}

					await Task.Delay(RecognitionPollingInterval, cancellationToken);
				}

				op.EnrichWith("Attempts", attempts);

				//TODO: change to cancellationToken.ThrowIfCancellationRequested
				throw new PXException(Messages.WaitingTimeExceeded);
			}
		}

		private static void LogSyncResponse(ILogger logger) => logger.Verbose("Recognition returned result synchronously");

		private static void LoadRecognizedDataToGraph(APInvoiceRecognitionEntry graph, RecognizedRecord record,
			DocumentRecognitionResult recognitionResult)
		{
			var document = graph.Document.Current;
            var cache = graph.Document.Cache;
            cache.SetValue<APRecognizedInvoice.recognitionStatus>(document, record.Status);
            cache.SetValue<APRecognizedInvoice.duplicateLink>(document, record.DuplicateLink);
            cache.SetValue<APRecognizedInvoice.recognizedDataJson>(document, HttpUtility.UrlEncode(record.RecognitionResult));
            cache.SetValue<APRecognizedInvoice.noteID>(document, record.NoteID);
            graph.RecognizedRecords.Cache.SetValue<APRecognizedRecord.isDataLoaded>(record, true);

			if (recognitionResult == null)
			{
				return;
			}

			if (document.RecognitionStatus != RecognizedRecordStatusListAttribute.Processed)
			{
				// To avoid double calculation
				cache.SetValue<APRecognizedInvoice.curyLineTotal>(document, decimal.Zero);
			}

			var siteMapNode = PXSiteMap.Provider.FindSiteMapNodesByGraphType(typeof(APInvoiceRecognitionEntry).FullName).FirstOrDefault();
			if (siteMapNode == null)
			{
				return;
			}

			var (_, detailFields) = graph.GetUIFields();
			if (detailFields == null)
			{
				return;
			}

			if (document.RecognitionStatus != RecognizedRecordStatusListAttribute.Processed)
			{
				var invoiceDataLoader = new InvoiceDataLoader(recognitionResult, graph, detailFields.ToArray());
				invoiceDataLoader.Load(document);

				var vendorTermJson = JsonConvert.SerializeObject(invoiceDataLoader.VendorTerm);
				var vendorTermEncodedJson = HttpUtility.UrlEncode(vendorTermJson);

				cache.SetValue<APRecognizedInvoice.vendorTermJson>(document, vendorTermEncodedJson);
			}

			document.FeedbackBuilder = graph.GetFeedbackBuilder();
			document.Links = recognitionResult.Links;

			graph.Document.Cache.IsDirty = false;
			graph.Transactions.Cache.IsDirty = false;
		}

		private (IEnumerable<string> PrimaryFields, IEnumerable<string> DetailFields) GetUIFields()
		{
			var siteMapNode = PXSiteMap.Provider
				.FindSiteMapNodesByGraphType(typeof(APInvoiceRecognitionEntry).FullName)
				.FirstOrDefault();
			if (siteMapNode == null)
			{
				return (null, null);
			}

			var screenInfo = ScreenInfoProvider.TryGet(siteMapNode.ScreenID);
			if (screenInfo == null)
			{
				return (null, null);
			}

			if (!screenInfo.Containers.TryGetValue(nameof(Document), out var primaryContainer) ||
				!screenInfo.Containers.TryGetValue(nameof(Transactions), out var detailContainer))
			{
				return (null, null);
			}

			var primaryFields = primaryContainer.Fields.Select(f => f.FieldName);
			var detailFields = detailContainer.Fields.Select(f => f.FieldName);

			return (primaryFields, detailFields);
		}

		private FeedbackBuilder GetFeedbackBuilder()
		{
			var (primaryFields, detailFields) = GetUIFields();
			if (primaryFields == null || detailFields == null)
			{
				return null;
			}

			return new FeedbackBuilder(Document.Cache, primaryFields.ToHashSet(), detailFields.ToHashSet());
		}

		internal static async Task RecognizeFiles(IEnumerable<(string fileName, byte[] fileData, Guid fileId)> files, string subject = null,
			string mailFrom = null, string messageId = null, int? ownerId = null, bool newFiles = false)
		{
            var recognitionGraph = CreateInstance<APInvoiceRecognitionEntry>();
            var listToProcess = new List<APRecognizedRecord>();
            foreach (var file in files)
            {
                file.fileName.ThrowOnNullOrWhiteSpace(nameof(file.fileName));
                file.fileData.ThrowOnNull(nameof(file.fileData));

                if (newFiles)
                {
                    var fileInfoDb = new PX.SM.FileInfo(file.fileId, file.fileName, null, file.fileData);

                    var uploadFileGraph = CreateInstance<UploadFileMaintenance>();
                    if (!uploadFileGraph.SaveFile(fileInfoDb))
                    {
                        throw new PXException(Messages.FileCannotBeSaved, file.fileName);
                    }
                }

                var fileName = PX.SM.FileInfo.GetShortName(file.fileName);
                var recognizedSubject = GetRecognizedSubject(subject, fileName);

                if (!string.IsNullOrWhiteSpace(messageId))
                {
                    messageId = NormalizeMessageId(messageId);
                }

                var recognizedRecord = recognitionGraph.CreateRecognizedRecord(fileName, file.fileData, file.fileId, recognizedSubject, mailFrom,
                    messageId, ownerId);

                PXNoteAttribute.ForcePassThrow<RecognizedRecord.noteID>(recognitionGraph.RecognizedRecords.Cache);
                PXNoteAttribute.SetFileNotes(recognitionGraph.RecognizedRecords.Cache, recognizedRecord, file.fileId);
                if (!IsAllowedFile(fileName))
                {
                    UpdateRecognizedRecord(recognitionGraph, recognizedRecord, null, null);
                }
                var extension = Path.GetExtension(fileName);
                var mimeType = MimeTypes.GetMimeType(extension);

				DocumentRecognitionResult result = null;
				string state = null;
				try
				{
					//TODO: add more reasonable cancellation
					(result, state) = await recognitionGraph.InvoiceRecognitionClient.SendFile(file.fileId, file.fileData, mimeType, CancellationToken.None);
				}
				catch (Exception e)
				{
					UpdateRecognizedRecord(recognitionGraph, recognizedRecord, null, e.Message);
					throw e;
				}

				MarkRecognitionStarted(recognitionGraph, recognizedRecord, state);
				if (result != null)
					UpdateRecognizedRecord(recognitionGraph, recognizedRecord, result, null);
				else
					listToProcess.Add(recognizedRecord);
			}

            var logger = recognitionGraph._logger;

            using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(_recognitionTimeoutMinutes)))
            {
                var cancellationToken = cancellationTokenSource.Token;
                using (var op = logger.BeginOperationVerbose("Polling for recognition results"))
                {
                    var attempts = 0;

                    try
                    {
						var processedFilesCount = 0;

                        while (!cancellationToken.IsCancellationRequested && processedFilesCount < listToProcess.Count)
                        {
							attempts++;

                            foreach (var recognizedRecord in listToProcess.Where(r =>
                                r.Status == RecognizedRecordStatusListAttribute.InProgress))
                            {
								DocumentRecognitionResult result = null;
								string state = null;

								try
								{
									(result, state) = await recognitionGraph.InvoiceRecognitionClient.GetResult(recognizedRecord.ResultUrl, cancellationToken);
								}
								catch (Exception e)
								{
									UpdateRecognizedRecord(recognitionGraph, recognizedRecord, null, e.Message);
									throw;
								}

                                if (result != null)
                                {
                                    op.Complete("Attempts", attempts);
                                    UpdateRecognizedRecord(recognitionGraph, recognizedRecord, result, null);
									processedFilesCount++;
                                }
                                else if (state != recognizedRecord.ResultUrl)
                                {
                                    UpdateRecognizedRecordUrl(recognitionGraph, recognizedRecord, state);
                                }
                            }

							if (processedFilesCount < listToProcess.Count)
							{
								await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
							}
						}

						if (processedFilesCount < listToProcess.Count)
						{
							op.EnrichWith("Attempts", attempts);

							//TODO: change to cancellationToken.ThrowIfCancellationRequested
							throw new PXException(Messages.WaitingTimeExceeded);
						}
                    }
                    finally
                    {
                        foreach (var recognizedRecord in listToProcess.Where(r => r.Status == RecognizedRecordStatusListAttribute.InProgress))
                        {
                            UpdateRecognizedRecord(recognitionGraph, recognizedRecord, null, null);
                        }
                    }
                }
            }
        }

		internal static bool IsRecognitionInProgress(string messageId)
		{
			messageId.ThrowOnNullOrWhiteSpace(nameof(messageId));
			messageId = NormalizeMessageId(messageId);

			using (var record = PXDatabase.SelectSingle<RecognizedRecord>(
				new PXDataField<RecognizedRecord.refNbr>(),
				new PXDataFieldValue<RecognizedRecord.messageID>(messageId),
				new PXDataFieldValue<RecognizedRecord.status>(RecognizedRecordStatusListAttribute.InProgress)))
			{
				return record != null;
			}
		}

		internal static bool RecognizeInvoices(PXGraph graph, CRSMEmail message)
		{
			graph.ThrowOnNull(nameof(graph));
			message.ThrowOnNull(nameof(message));

			var cache = graph.Caches[typeof(CRSMEmail)];

			var allFiles = PXNoteAttribute.GetFileNotes(cache, message);
			if (allFiles == null || allFiles.Length == 0)
			{
				return false;
			}

			var filesToProcess = allFiles
				.Select(fileId => GetFile(graph, fileId))
				.Where(uploadFile => IsAllowedFile(uploadFile.Name))
				.ToArray();

			if (filesToProcess.Length == 0)
			{
				return false;
			}

            PXLongOperation.StartOperation(graph, () =>
            {
                RecognizeFiles(filesToProcess.Select(file => (file.Name, file.Data, file.FileID.Value)),
                    message.Subject, message.MailFrom, message.MessageId, message.OwnerID).Wait();
            });

            return true;
		}

		public virtual IList<(int? InventoryId, string PONumber)> GetPONumbers(int? vendorId, HashSet<int?> inventoryIds)
		{
			var poNumbers = new List<(int? InventoryId, string PONumber)>();

			var openPOLines = SelectFrom<POLine>.Where<POLine.vendorID.IsEqual<@P.AsInt>.
				And<POLine.cancelled.IsEqual<False>>.
				And<POLine.closed.IsEqual<False>>.
				And<Brackets<POLine.curyUnbilledAmt.IsNotEqual<decimal0>>.Or<POLine.unbilledQty.IsNotEqual<decimal0>>>>.View.Select(this, vendorId);

			foreach (POLine line in openPOLines)
			{
				if (inventoryIds.Contains(line.InventoryID))
				{
					poNumbers.Add((line.InventoryID, line.OrderNbr));
				}
			}

			return poNumbers;
		}
	}
}
