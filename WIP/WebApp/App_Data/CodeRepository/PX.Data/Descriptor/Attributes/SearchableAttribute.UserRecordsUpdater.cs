// This File is Distributed as Part of Acumatica Shared Source Code 
/* ---------------------------------------------------------------------*
*                               Acumatica Inc.                          *
*              Copyright (c) 1994-2016 All rights reserved.             *
*                                                                       *
*                                                                       *
* This file and its contents are protected by United States and         *
* International copyright laws.  Unauthorized reproduction and/or       *
* distribution of all or any portion of the code contained herein       *
* is strictly prohibited and will result in severe civil and criminal   *
* penalties.  Any violations of this copyright will be prosecuted       *
* to the fullest extent possible under law.                             *
*                                                                       *
* UNDER NO CIRCUMSTANCES MAY THE SOURCE CODE BE USED IN WHOLE OR IN     *
* PART, AS THE BASIS FOR CREATING A PRODUCT THAT PROVIDES THE SAME, OR  *
* SUBSTANTIALLY THE SAME, FUNCTIONALITY AS ANY ProjectX PRODUCT.        *
*                                                                       *
* THIS COPYRIGHT NOTICE MAY NOT BE REMOVED FROM THIS FILE.              *
* ---------------------------------------------------------------------*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Serilog;
using Serilog.Events;
using SerilogTimings;
using SerilogTimings.Extensions;
using PX.Common;
using PX.Data.Update;
using PX.Data.UserRecords;
using PX.DbServices.Model.DataSet;
using PX.DbServices.Points.DbmsBase;
using PX.DbServices.Points.PXDataSet;

namespace PX.Data
{
	public partial class PXSearchableAttribute : PXEventSubscriberAttribute
	{
		/// <summary>
		/// A user records updater class. Adds entries to the transaction scope for changed DACs on row persisted event to update user records cached content when transaction completes.
		/// This functionality is integrated with <see cref="PXSearchableAttribute"/> because there will be too much overhead to introduce a new attribute. 
		/// The functionality of user records is related to the search since user records uses fields declared in <see cref="PXSearchableAttribute"/> constructor in DAC 
		/// to build information displayed to the user.
		/// </summary>
		[PXInternalUseOnly]
		protected class UserRecordsUpdater
		{
			private const string UserRecordsSlotName = "UserRecordsSlot";

			/// <summary>
			/// Updates row's user records cached content on row persisted event. This functionality is integrated with <see cref="PXSearchableAttribute"/> because it will be too much overhead to avoid
			/// introduction of new attribute. The functionality is a bit related since user records uses <see cref="PXSearchableAttribute"/> to build information displayed to the user.
			/// </summary>
			/// <param name="e">The row persisted event information.</param>
			/// <param name="noteID">Note ID.</param>
			/// <param name="contentUpdater">The content updater.</param>
			/// <param name="logger">The logger.</param>
			public void UpdateUserRecordsCachedContentOnRowPersisted(PXRowPersistedEventArgs e, Guid noteID, IRecordCachedContentUpdater contentUpdater,
																	 ILogger logger)
			{
				PXDBOperation operation = e.Operation.Command();

				if (e.TranStatus != PXTranStatus.Open || !(e.Row is IBqlTable entity) || operation == PXDBOperation.Insert)
					return;

				Type entityType = entity.GetType();

				// No need to rebuild record's content on the deletion of the record
				string content = e.Operation.Command() != PXDBOperation.Delete
					? RebuildCachedContentOfUserRecords(noteID, entity, entityType, contentUpdater, logger)
					: string.Empty;

				DacModificationType? modificationType = operation == PXDBOperation.Delete
					? DacModificationType.Delete
					: operation == PXDBOperation.Update
						? DacModificationType.Update
						: (DacModificationType?)null;

				if (modificationType != null)
				{
					// Acuminator disable once PX1073 ExceptionsInRowPersisted Diagnostic is not appliable in NetTools
					if (!PXTransactionScope.AddChangedDacEntryForUserRecordsModification(noteID, entityType, content, modificationType.Value))
					{
						logger.Error("Failed to add an entry for user records synchronization to the transaction scope for entity {EntityType} {NoteID}",
									 entityType.FullName, noteID);
					}
				}
			}

			private string RebuildCachedContentOfUserRecords(Guid noteID, IBqlTable entity, Type entityType,
															 IRecordCachedContentUpdater contentUpdater, ILogger logger)
			{
				var entityKey = (noteID, entityType);
				var cachedRecordContents = PXContext.GetSlot<Dictionary<(Guid NoteID, Type Type), string>>(UserRecordsSlotName);

				if (cachedRecordContents == null)
				{
					cachedRecordContents = new Dictionary<(Guid NoteID, Type Type), string>();
					PXContext.SetSlot(UserRecordsSlotName, cachedRecordContents);
				}

				if (!cachedRecordContents.TryGetValue(entityKey, out string content))
				{
					Guid? transactionID = PXTransactionScope.RootUID;

					using (logger.OperationAt(LogEventLevel.Verbose)
								 .Time("Rebuild of the visited record index in DB Transaction {TransactionID} for entity {EntityType} {NoteID}",
										transactionID, entityType.FullName, noteID))
					{
						logger.Verbose("Starting rebuild of the visited record index in DB Transaction {TransactionID} for entity {EntityType} {NoteID}",
									   transactionID, entityType.FullName, noteID);

						content = contentUpdater?.BuildCachedContent(entity);
						cachedRecordContents[entityKey] = content;
					}
				}

				return content;
			}
		}
	}
}
