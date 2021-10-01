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
using PX.Api.Soap.Screen;
using PX.BulkInsert.Provider;
using PX.Common;
using PX.Data.Search;
using PX.Data.Update;
using PX.Data.UserRecords;
using PX.Data.UserRecords.FavoriteRecords;
using PX.Data.UserRecords.RecentlyVisitedRecords;
using PX.DbServices.Model.DataSet;
using PX.DbServices.Points.DbmsBase;
using PX.DbServices.Points.PXDataSet;

namespace PX.Data
{
    /// <summary>
    /// Adds fields to the search index and configures the search result.
    /// </summary>
    /// <remarks>This attribute is assigned to the <tt>NoteID</tt> DAC field.
    /// You can make a search in the fields listed in this attribute and
    /// in the key fields. </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public partial class PXSearchableAttribute : PXEventSubscriberAttribute
    {
        protected static Regex ComposedFormatArgsRegex { get; } =
            new Regex(@"(?<!(?<!\{)\{)\{(?<index>\d+)(,(?<alignment>\d+))?(:(?<formatString>[^\}]+))?\}(?!\}(?!\}))",
                      RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        protected int category;
        protected Type[] fields;
        protected string titlePrefix;
        protected Type[] titleFields;
        protected PXView searchView;

        /// <summary>
        /// The format of the first line of the search result that is displayed.
        /// Numbers in curly braces reference to the fields listed in <tt>Line1Fields</tt>.
        /// </summary>
        public string Line1Format { get; set; }
        /// <summary>
        /// The format of the second line of the search result that is displayed.
        /// Numbers in curly braces reference to the fields listed in <tt>Line2Fields</tt>.
        /// </summary>
        public string Line2Format { get; set; }
        /// <summary>
        /// The fields that are referenced from <tt>Line1Format</tt>.
        /// </summary>
        public Type[] Line1Fields { get; set; }
        /// <summary>
        /// The fields that are referenced from <tt>Line2Format</tt>.
        /// </summary>
        public Type[] Line2Fields { get; set; }

        /// <summary>
        /// List of the fields to be indexed that contain numbers with prefixes (for example, <tt>"CT00000040"</tt>).
        /// </summary>
        /// <remarks>If a field contains a number with a prefix (like <tt>"CT00000040"</tt>), then searching for <tt>"0040"</tt>
        /// can pose a problem. All fields that are listed in <tt>NumberFields</tt> will get special treatment
        /// and will be indexed both with a prefix and without one (<tt>"CT00000040"</tt>, <tt>"00000040"</tt>).</remarks>
        public Type[] NumberFields { get; set; }

        /// <summary>
        /// The <tt>Select&lt;&gt;</tt> construction that selects the users of the documents
        /// which must be available in the search result.
        /// </summary>
        /// <remarks>If this <tt>Select&lt;&gt;</tt> construction is specified,
        /// then only those records or documents are shown that were
        /// created either by the current user or by a user from the returned set.
        /// The company tree hierarchy is not traversed.
        /// For example, you can use this setting to filter expense claims, timecards, etc.
        /// </remarks>
        public Type SelectDocumentUser { get; set; }

        /// <summary>
        /// Constraint that defines whether the given DAC instance is searchable or not.
        /// </summary>
        /// <remarks>For example, the <tt>Contact</tt> DAC is used to represent different types of records:
        /// the <tt>lead</tt> contacts are searchable, while the <tt>accountProperty</tt> contacts are not.</remarks>
        public Type WhereConstraint { get; set; }

        /// <summary>
        /// When a <tt>MatchWith</tt> type is used, the <tt>MatchWithJoin</tt> property
        /// must contain a join for the entity containing the <tt>GroupMask</tt> column.
        /// </summary>
        /// <remarks>For example, the graph that manages <tt>ARInvoice</tt> objects
        /// contains the following <tt>Match</tt> operator in the <tt>Document</tt> object:
        /// <para><tt>Match&lt;Customer, Current&lt;AccessInfo.userName&gt;&gt;</tt></para>
        /// <para>So the <tt>MatchWithJoin</tt> property must contain a join to the <tt>Customer</tt> table:</para>
        /// <para><tt>typeof(InnerJoin&lt;Customer, On&lt;Customer.bAccountID, Equal&lt;ARInvoice.customerID&gt;&gt;&gt;)</tt></para></remarks>
        public Type MatchWithJoin { get; set; }

        /// <summary>
        /// The <tt>SelectForFastIndexing</tt> request is used to define the relationship
        /// between the searchable fields and thus make it possible to rebuild the Full Text Search index
        /// and to use fields from other DACs in LineFields.
        /// </summary>
        /// <remarks>A search can involve additional joined tables, so that all searchable fields
        /// are retrieved by a single select request, and this prevents lazy loading of rows using the selector.</remarks>
        public Type SelectForFastIndexing { get; set; }

        /// <summary>
        /// The information for a complex search result creation regarding retrieval of values for fields from other DACs which don't have a selector attribute declared on them.
        /// </summary>
        /// <value>
        /// The information for a complex search result creation regarding retrieval of values for fields from other DACs which don't have a selector attribute declared on them.
        /// </value>
        private IForeignDacFieldRetrievalInfo[] ForeignDacFieldRetrievalInfos { get; }

        [InjectDependency]
        protected IRecordCachedContentUpdater UserRecordContentUpdater { get; set; }

        [InjectDependency]
        protected ILogger Logger { get; set; }

        private UserRecordsUpdater RecordsUpdater { get; set; } = new UserRecordsUpdater();

        internal PXSearchableAttribute(int category, string titlePrefix, Type[] titleFields, Type[] fields, Type foreignDacFieldRetrievalInfos) : 
                                  this(category, titlePrefix, titleFields, fields)
		{
            foreignDacFieldRetrievalInfos.ThrowOnNull(nameof(foreignDacFieldRetrievalInfos));

            switch (foreignDacFieldRetrievalInfos)
            {
                case Type fieldsFromSingleDAC
                when typeof(IForeignDacFieldRetrievalInfo).IsAssignableFrom(fieldsFromSingleDAC):
                    var dacRetrievalInfo = (IForeignDacFieldRetrievalInfo)Activator.CreateInstance(fieldsFromSingleDAC);
                    ForeignDacFieldRetrievalInfos = new[] { dacRetrievalInfo.CheckIfNull(nameof(dacRetrievalInfo)) };
                    break;

                case Type fieldsFromMultipleDACs
                when typeof(TypeArrayOf<IForeignDacFieldRetrievalInfo>).IsAssignableFrom(fieldsFromMultipleDACs):
                    ForeignDacFieldRetrievalInfos = TypeArrayOf<IForeignDacFieldRetrievalInfo>.CheckAndExtractInstances(fieldsFromMultipleDACs);
                    break;
            }		
        }

        /// <summary>
        /// Initializes the search parameters.
        /// </summary>
        /// <param name="category">The search category. This is one of the integer
        /// constants defined in the <tt>PX.Objects.SM.SearchCategory</tt> class.</param>
        /// <param name="titlePrefix">The format of the search result title.</param>
        /// <param name="titleFields">The fields whose values are used in the search result title.
        /// These fields are referenced from <tt>titlePrefix</tt>.</param>
        /// <param name="fields">The fields for which the index will be built.</param>
        public PXSearchableAttribute(int category, string titlePrefix, Type[] titleFields, Type[] fields)
        {
            this.category = category;
            this.fields = fields;
            this.titleFields = titleFields;
            this.titlePrefix = titlePrefix;
        }

        /// <summary>
        /// Returns all searchable fields including dependent fields and key fields.
        /// </summary>
        /// <remarks>For example, since <tt>Contact.DisplayName</tt> depends on <tt>FirstName</tt>,
        /// <tt>LastName</tt>, and other fields, all these fields will also be returned.</remarks>
        /// <returns>All searchable fields.</returns>
        public ICollection<Type> GetSearchableFields(PXCache cache)
        {
            cache.ThrowOnNull(nameof(cache));
            HashSet<Type> result = new HashSet<Type>();

            foreach (Type item in titleFields.Union(fields))
            {
                result.Add(item);

                foreach (Type dependable in PXDependsOnFieldsAttribute.GetDependsRecursive(cache, item.Name)
                                                                      .Select(cache.GetBqlField))
                {
                    result.Add(dependable);
                }

                //Note: Keys can be removed once 43383 is resolved.
                Type dacType = BqlCommand.GetItemType(item);
                foreach (Type key in cache.Graph.Caches[dacType].BqlKeys)
                {
                    result.Add(key);
                }
            }

            if (WhereConstraint != null)
            {
                foreach (Type type in BqlCommand.Decompose(WhereConstraint))
                {
                    if ((typeof(IBqlField)).IsAssignableFrom(type))
                    {
                        result.Add(type);
                    }
                }
            }

            return result;
        }

		/// <summary>
		/// Gets all searchable fields from <see cref="PXSearchableAttribute"/> including fields in <see cref="Line1Fields"/>, <see cref="Line2Fields"/> and <see cref="NumberFields"/>.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <returns/>
		public ISet<Type> GetAllSearchableFields(PXCache cache)
		{
			ICollection<Type> baseSearchableFields = GetSearchableFields(cache);
			HashSet<Type> allSearchableFields = (baseSearchableFields as HashSet<Type>) ?? baseSearchableFields.ToHashSet();
			IEnumerable<Type> fieldsToAdd = Line1Fields ?? Enumerable.Empty<Type>();

			if (Line2Fields != null)
			{
				fieldsToAdd = fieldsToAdd.Union(Line2Fields);
			}

			if (NumberFields != null)
			{
				fieldsToAdd = fieldsToAdd.Union(NumberFields);
			}

			fieldsToAdd.Where(field => !allSearchableFields.Contains(field))
					   .ForEach(field => allSearchableFields.Add(field));

			return allSearchableFields;
		}

        /// <summary>
		/// Gets searchable fields for user records features - recently visited records and favorite records.
		/// </summary>
		/// <returns/>
		internal IReadOnlyCollection<Type> GetSearchableFieldsForUserRecords()
        {
            int estimatedCapacity = (titleFields?.Length ?? 0) + (fields?.Length ?? 0) + (Line1Fields?.Length ?? 0) + (Line2Fields?.Length ?? 0);
            List<Type> searchableFields = new List<Type>(estimatedCapacity);

            if (titleFields?.Length > 0)
                searchableFields.AddRange(titleFields);

            AddFields(fields);
            AddFields(Line1Fields);
            AddFields(Line2Fields);

            return searchableFields;

            //---------------------------------------Local function-------------------------------
            void AddFields(Type[] fieldsArray)
			{
                if (fieldsArray == null || fieldsArray.Length == 0)
                    return;

				foreach (Type field in fieldsArray)
				{
                    if (!searchableFields.Contains(field))
                        searchableFields.Add(field);
				}
			}
        }

        private enum ListAttributeKind
		{
            None = 0,
            NonLocalizable = 1,
            Localizable = 2
		}

        private readonly Dictionary<Type, ListAttributeKind> _listAttributeKindByDacField = new Dictionary<Type, ListAttributeKind>();
        private static object _forLock = new object();

        private ListAttributeKind GetListAttributeKindForField(PXCache cache, Type field)
        {
            lock (((ICollection) _listAttributeKindByDacField).SyncRoot)
            {
                if (_listAttributeKindByDacField.TryGetValue(field, out ListAttributeKind listAttributeKind))
                    return listAttributeKind;
                
                listAttributeKind = ListAttributeKind.None;

                foreach (PXEventSubscriberAttribute attr in cache.GetAttributes(field.Name))
                {
					if (attr is PXStringListAttribute stringListAttribute)
                    {
                        listAttributeKind = stringListAttribute.IsLocalizable 
                                                ? ListAttributeKind.Localizable 
                                                : ListAttributeKind.NonLocalizable;
                        break;
                    }
                    else if (attr is PXIntListAttribute intListAttribute)
                    {
                        listAttributeKind = intListAttribute.IsLocalizable 
                                                ? ListAttributeKind.Localizable 
                                                : ListAttributeKind.NonLocalizable;
                        break;
                    }
                }

                _listAttributeKindByDacField.Add(field, listAttributeKind);
                return listAttributeKind;
            }
        }

        [PXInternalUseOnly]
        public override void CacheAttached(PXCache sender)
        {
            base.CacheAttached(sender);
            sender.RowPersisting += sender_RowPersisting;
            sender.RowPersisted += sender_RowPersisted;
        }

        [PXInternalUseOnly]
        public virtual bool IsSearchable(PXCache sender, object row)
        {
            if (WhereConstraint == null)
                return true;

            EnsureSearchView(sender);

            object[] par = searchView.PrepareParameters(new[] { row }, null);

            return searchView.BqlSelect.Meet(sender, row, par);
        }

        protected virtual void EnsureSearchView(PXCache sender)
        {
            if (searchView == null)
            {
                List<Type> list = new List<Type>();
                list.Add(typeof(Select<,>));
                list.Add(sender.GetItemType());
                list.AddRange(BqlCommand.Decompose(WhereConstraint));

                BqlCommand cmd = BqlCommand.CreateInstance(list.ToArray());
                searchView = new PXView(sender.Graph, true, cmd);
            }
        }

		private void sender_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			object val = sender.GetValue(e.Row, _FieldOrdinal);
			if (val == null)
			{
				Guid noteID = SequentialGuid.Generate();
				sender.SetValue(e.Row, _FieldOrdinal, noteID);
			}
		}

		private void sender_RowPersisted(PXCache sender, PXRowPersistedEventArgs e)
		{
			Guid? noteID = sender.GetValue(e.Row, _FieldOrdinal) as Guid?;

            if (noteID.HasValue)
            {
                // Acuminator disable once PX1043 SavingChangesInEventHandlers Diagnostic is not appliable in NetTools
                // Acuminator disable once PX1073 ExceptionsInRowPersisted Diagnostic is not appliable in NetTools
                RecordsUpdater.UpdateUserRecordsCachedContentOnRowPersisted(e, noteID.Value, UserRecordContentUpdater, Logger);
            }

			// Acuminator disable once PX1073 ExceptionsInRowPersisted Diagnostic is not appliable in NetTools
			if (!IsSearchable(sender, e.Row))
				return;

			Dictionary<Guid, SearchIndex> dict = PXContext.GetSlot<Dictionary<Guid, SearchIndex>>("SearchIndexSlot");

			if (dict == null)
			{
				dict = new Dictionary<Guid, SearchIndex>();
				PXContext.SetSlot("SearchIndexSlot", dict);
			}

			SearchIndex searchIndex = null;

			if (noteID.HasValue)
			{
				dict.TryGetValue(noteID.Value, out searchIndex);

				if (searchIndex == null || 
                    !string.Equals(searchIndex.EntityType, e.Row.GetType().FullName, StringComparison.OrdinalIgnoreCase))
				{
					Note note = PXSelect<Note, 
									Where<Note.noteID, Equal<Required<Note.noteID>>>>
								.SelectSingleBound(sender.Graph, null, noteID);

					searchIndex = BuildSearchIndex(sender, e.Row, null, note != null ? note.NoteText : null);
					dict[noteID.Value] = searchIndex;
				}
			}

			if (e.TranStatus == PXTranStatus.Completed)
			{
				if (noteID == null)
					// Acuminator disable once PX1073 ExceptionsInRowPersisted Diagnostic is not appliable in NetTools
					throw new PXException(MsgNotLocalizable.SearchIndexCannotBeSaved);

				if (e.Operation == PXDBOperation.Delete)
				{
					// Acuminator disable once PX1043 SavingChangesInEventHandlers Diagnostic is not appliable in NetTools
					// Acuminator disable once PX1073 ExceptionsInRowPersisted Diagnostic is not appliable in NetTools
					PXDatabase.Delete(typeof(SearchIndex),
									  new PXDataFieldRestrict(typeof(SearchIndex.noteID).Name, PXDbType.UniqueIdentifier, searchIndex.NoteID));
				}
				else
				{
					// Acuminator disable once PX1043 SavingChangesInEventHandlers Diagnostic is not appliable in NetTools
					// Acuminator disable once PX1073 ExceptionsInRowPersisted Diagnostic is not appliable in NetTools
					if (!Update(searchIndex))
					{
						// Acuminator disable once PX1043 SavingChangesInEventHandlers Diagnostic is not appliable in NetTools
						// Acuminator disable once PX1073 ExceptionsInRowPersisted Diagnostic is not appliable in NetTools
						Insert(searchIndex);
					}
				}
			}
		}

        [PXInternalUseOnly]
        public static bool Insert(SearchIndex record)
        {
            return PXDatabase.Insert(typeof(SearchIndex),
                                      new PXDataFieldAssign(typeof(SearchIndex.noteID).Name, PXDbType.UniqueIdentifier, record.NoteID),
                                      new PXDataFieldAssign(typeof(SearchIndex.indexID).Name, PXDbType.UniqueIdentifier, record.IndexID),
                                      new PXDataFieldAssign(typeof(SearchIndex.category).Name, PXDbType.Int, record.Category),
                                      new PXDataFieldAssign(typeof(SearchIndex.entityType).Name, PXDbType.NVarChar, record.EntityType),
                                      new PXDataFieldAssign(typeof(SearchIndex.content).Name, PXDbType.NText, record.Content));
        }

        [PXInternalUseOnly]
        public static bool Update(SearchIndex record)
        {
            return PXDatabase.Update(typeof(SearchIndex),
                                      new PXDataFieldRestrict(typeof(SearchIndex.noteID).Name, PXDbType.UniqueIdentifier, record.NoteID),
                                      new PXDataFieldAssign(typeof(SearchIndex.category).Name, PXDbType.Int, record.Category),
                                      new PXDataFieldAssign(typeof(SearchIndex.entityType).Name, PXDbType.NVarChar, record.EntityType),
                                      new PXDataFieldAssign(typeof(SearchIndex.content).Name, PXDbType.NText, record.Content));
        }

        [PXInternalUseOnly]
        public static bool Delete(SearchIndex record)
        {
            return PXDatabase.Delete(typeof(SearchIndex),
                                      new PXDataFieldRestrict(typeof(SearchIndex.noteID).Name, PXDbType.UniqueIdentifier, record.NoteID));
        }

        [PXInternalUseOnly]
        public static void BulkInsert(IEnumerable<SearchIndex> records)
        {
            PointDbmsBase point = PXDatabase.Provider.CreateDbServicesPoint();
            PxDataTable recordsToInsert = new PxDataTable(point.Schema.GetTable(typeof(SearchIndex).Name));

            TransferTableTask task = new TransferTableTask();
            task.Source = new PxDataTableAdapter(recordsToInsert);
            task.Destination = point.GetTable(typeof(SearchIndex).Name);
            task.AppendData = true;

            BatchTransferExecutorSync bex = new BatchTransferExecutorSync(new SimpleDataTransferObserver());
            bex.Tasks.Enqueue(task);

            var timestamp = new byte[] { };
            Stopwatch sw= new Stopwatch();
            sw.Start();
            int currentCompany = PXInstanceHelper.CurrentCompany;
            foreach (SearchIndex record in records)
            {
                // see database_schema.xml for correct columns order.
                recordsToInsert.AddRow(new object[]{currentCompany, record.NoteID.Value, record.IndexID.Value, record.EntityType, record.Category, record.Content, timestamp});
            }
            Debug.Print("DataTable filled in {0} sec.", sw.Elapsed.TotalSeconds);
            sw.Restart();
            bex.StartSync();
            Debug.Print("DataImport in {0} sec.", sw.Elapsed.TotalSeconds);
        }

        [PXInternalUseOnly]
        public virtual SearchIndex BuildSearchIndex(PXCache sender, object row, PXResult res, string noteText)
        {
            SearchIndex si = new SearchIndex();
            si.IndexID = Guid.NewGuid();
            si.NoteID = (Guid?)sender.GetValue(row, typeof(Note.noteID).Name);
            si.Category = category;
            si.Content = BuildContent(sender, row, res) + " " + noteText;

            Type entityType = GetEntityTypeDeclaringSearchAttribute(sender, row);
            si.EntityType = entityType.FullName ??
                            throw new InvalidOperationException($"Could not get the type declaring the {nameof(PXSearchableAttribute)} attribute for type {row.GetType().FullName}");
            return si;
        }

        private Type GetEntityTypeDeclaringSearchAttribute(PXCache sender, object row)
		{
            Type declaringSearchAttributeOrDerivedEntityType = row.GetType();

            while (typeof(IBqlTable).IsAssignableFrom(declaringSearchAttributeOrDerivedEntityType))
            {
                Type noteIDField = declaringSearchAttributeOrDerivedEntityType.GetNestedType(nameof(Note.noteID));

                if (noteIDField != null && sender.GetAttributesOfType<PXSearchableAttribute>(row, nameof(Note.noteID)).Any())
                    return declaringSearchAttributeOrDerivedEntityType;

                declaringSearchAttributeOrDerivedEntityType = declaringSearchAttributeOrDerivedEntityType.BaseType;
            }

            return null;
        }

        [PXInternalUseOnly]
        public virtual RecordInfo BuildRecordInfo(PXCache sender, object row)
        {
            List<Type> allFields = new List<Type>();
            allFields.AddRange(titleFields);
            if (Line1Fields != null)
            {
                foreach (Type field in Line1Fields)
                {
                    if (!allFields.Contains(field))
                    {
                        allFields.Add(field);
                    }
                }
            }

            if (Line2Fields != null)
            {
                foreach (Type field in Line2Fields)
                {
                    if (!allFields.Contains(field))
                    {
                        allFields.Add(field);
                    }
                }
            }

            Dictionary<Type, object> values = ExtractValues(sender, row, null, allFields);

            //Title:
            List<object> titleArgs = new List<object>();
            string title = string.Empty;
            if (titleFields != null && titleFields.Length > 0)
            {
                foreach (Type field in titleFields)
                {
                    if (values.ContainsKey(field))
                    {
                        titleArgs.Add(values[field]);
                    }
                    else
                    {
                        titleArgs.Add(string.Empty);
                    }
                }
            }
            if (titlePrefix != null)
            {
                title = string.Format(PXMessages.LocalizeNoPrefix(titlePrefix), titleArgs.ToArray());
                if (title.Trim().EndsWith("-"))
                {
                    title = title.Trim().TrimEnd('-');
                }
            }
            
            //Line 1:
            List<object> line1Args = new List<object>();
            List<string> line1DisplayNames = new List<string>();
            string line1 = string.Empty;
            if (Line1Fields != null && Line1Fields.Length > 0)
            {
                for (int i = 0; i < Line1Fields.Length; i++)
                {
                    Type field = Line1Fields[i];

                    if (values.ContainsKey(field) && values[field] != null && !string.IsNullOrWhiteSpace(values[field].ToString()))
                    {
                        string displayName = PXUIFieldAttribute.GetDisplayName(sender.Graph.Caches[BqlCommand.GetItemType(field)], field.Name);
                        if (string.IsNullOrWhiteSpace(displayName))
                            displayName = field.Name;
                        displayName = OverrideDisplayName(field, displayName);

                        line1Args.Add(values[field]);
                        line1DisplayNames.Add(displayName);
                    }
                    else
                    {
                        line1Args.Add(null);
                        line1DisplayNames.Add(string.Empty);
                    }

                }
            }
            line1 = BuildFormatedLine(Line1Format, line1Args, line1DisplayNames);


            //Line 2:
            List<object> line2Args = new List<object>();
            List<string> line2DisplayNames = new List<string>();
            string line2 = string.Empty;
            if (Line2Fields != null && Line2Fields.Length > 0)
            {
                for (int i = 0; i < Line2Fields.Length; i++)
                {
                    Type field = Line2Fields[i];

                    if (values.ContainsKey(field) && values[field] != null && !string.IsNullOrWhiteSpace(values[field].ToString()))
                    {
                        string displayName = PXUIFieldAttribute.GetDisplayName(sender.Graph.Caches[BqlCommand.GetItemType(field)], field.Name);
                        if (string.IsNullOrWhiteSpace(displayName))
                            displayName = field.Name;

                        displayName = OverrideDisplayName(field, displayName);

                        line2Args.Add(values[field]);
                        line2DisplayNames.Add(displayName);
                    }
                    else
                    {
                        line2Args.Add(null);
                        line2DisplayNames.Add(string.Empty);
                    }

                }
            }
            line2 = BuildFormatedLine(Line2Format, line2Args, line2DisplayNames);

            return new RecordInfo(title, line1, line2);
        }

        protected virtual string OverrideDisplayName(Type field, string displayName)
        {
            return displayName;
        }
       

		private string BuildFormatedLine(string compositeFormat, List<object> argValues, List<string> displayNames)
		{		
			MatchCollection matches = ComposedFormatArgsRegex.Matches(compositeFormat);

			int estimatedCapacity = matches.Count * 2 * 16 * displayNames.Count;  //16 chars - estimated name and value average length
			StringBuilder sb = new StringBuilder(estimatedCapacity);

			object[] argValuesArray = argValues.ToArray();
			object[] displayNamesArray = displayNames.ToArray();		

			for (int i = 0; i < matches.Count && i < argValues.Count; i++)
			{
				string formatedDisplayname = string.Format(matches[i].Value, displayNamesArray);
				string formatedValue = string.Format(matches[i].Value, argValuesArray);

				if (!string.IsNullOrWhiteSpace(formatedDisplayname) && !string.IsNullOrWhiteSpace(formatedValue))
				{
					sb.AppendFormat("{0}: {1} - ", formatedDisplayname, formatedValue);
				}
			}

            string result = sb.ToString();

            if (result.Length > 1)//remove trailing - 
            {
                result = result.Substring(0, result.Length - 3);
            }

            return result;
        }

        [PXInternalUseOnly]
        public virtual string BuildContent(PXCache sender, object row, PXResult res)
        {
            List<Type> allFields = new List<Type>(capacity: (titleFields?.Length ?? 0) + (fields?.Length ?? 0));
            allFields.AddRange(titleFields);

            if (fields?.Length > 0)
            {
                foreach (Type field in fields)
                {
                    if (!allFields.Contains(field))
                    {
                        allFields.Add(field);
                    }
                }
            }

            StringBuilder sb = new StringBuilder();
            Dictionary<Type, object> values = ExtractValues(sender, row, res, allFields, buildTranslations: true);
            
            //Title:
            List<string> titleNumbers = new List<string>();
            List<object> titleArgs = new List<object>();

            if (titleFields != null && titleFields.Length > 0)
            {
                foreach (Type field in titleFields)
                {
                    if (values.ContainsKey(field))
                    {
                        object fieldValue = values[field];
                        titleArgs.Add(fieldValue);
                        
                        if (fieldValue != null && NumberFields != null && NumberFields.Contains(field))
                        {
                            string strValue = fieldValue.ToString();
                            string numberWithoutPrefix = RemovePrefix(strValue);

                            if (numberWithoutPrefix.Length != strValue.Length)
                                titleNumbers.Add(numberWithoutPrefix);
                        }

                    }
                    else
                    {
                        titleArgs.Add(string.Empty);
                    }
                }
            }

            if (titlePrefix != null)
            {
                sb.Append(string.Format(titlePrefix, titleArgs.ToArray()));
            }

            sb.Append(" ");

            foreach (string num in titleNumbers )
            {
                sb.AppendFormat("{0} ", num);
            }
            
            if (fields?.Length > 0)
            {
                foreach (Type field in fields)
                {
                    if (values.ContainsKey(field))
                    {
                        if (values[field] != null)
                        {
                            sb.Append(values[field].ToString());
                            sb.Append(" ");
                        }
                    }
                }
            }
            
            return sb.ToString();
        }

        private string RemovePrefix(string strValue)
        {
            if (string.IsNullOrEmpty(strValue))
                return string.Empty;

            int firstDigitIndex = 0;
            for (int i = 0; i < strValue.Length; i++)
            {
                if (char.IsDigit(strValue[i]) && strValue[i] != '0')
                {
                    firstDigitIndex = i;
                    break;
                }
            }

            return strValue.Substring(firstDigitIndex);
        }

        protected virtual object GetFieldValue(PXCache sender, object row, Type field, bool disableLazyLoading)
        {
            return GetFieldValue(sender, row, field, disableLazyLoading, false);
        }

        private object GetFieldValue(PXCache sender, object row, Type field, bool disableLazyLoading, bool buildTranslations)
        {
            bool hasDBLocalizableStringAttr = sender.GetAttributes(field.Name)
                                                    .Any(attr => attr is PXDBLocalizableStringAttribute);
            ListAttributeKind listAttributeKind = GetListAttributeKindForField(sender, field);

            if (!buildTranslations)
            {
                if (listAttributeKind == ListAttributeKind.Localizable)
                    listAttributeKind = ListAttributeKind.NonLocalizable;

                hasDBLocalizableStringAttr = false;
            }

            if (disableLazyLoading && listAttributeKind == 0 && !hasDBLocalizableStringAttr)
                return sender.GetValue(row, field.Name);

            object value = sender.GetStateExt(row, field.Name);

            if (!(value is PXFieldState state))
                return value;

            if (hasDBLocalizableStringAttr && sender.GetStateExt(row, field.Name + "Translations") is string[] translations)
                return string.Join(" ", translations);

            value = state.Value;

            if (state is PXIntState intState && intState.AllowedValues != null && intState._NeutralLabels != null)
			{           
                int minLength = Math.Min(intState.AllowedValues.Length, Math.Min(intState.AllowedLabels.Length, intState._NeutralLabels.Length));

                for (int i = 0; i < minLength; i++)
                {
                    if (intState.AllowedValues[i] == (int)value)
                    {
                        value = listAttributeKind == ListAttributeKind.Localizable
                            ? GetAllTranslations(sender, intState.Name, i, intState._NeutralLabels, intState.AllowedLabels)
                            : intState.AllowedLabels[i];
                        break;
                    }
                }
            }
            else if (state is PXStringState stringState && stringState.AllowedValues != null && stringState._NeutralLabels != null)
			{
                int minLength = Math.Min(stringState.AllowedValues.Length, Math.Min(stringState.AllowedLabels.Length, stringState._NeutralLabels.Length));

                for (int i = 0; i < minLength; i++)
                {
                    if (stringState.AllowedValues[i] == (string)value)
                    {
                        value = listAttributeKind == ListAttributeKind.Localizable
                            ? GetAllTranslations(sender, stringState.Name, i, stringState._NeutralLabels, stringState.AllowedLabels)
                            : stringState.AllowedLabels[i];
                        break;
                    }
                }
            }

            //Following is a hack to get FinPeriod to format as it is visible to the user... couldn't find any other way to do it ((.
            if (state is PXStringState finPeriodStringState && finPeriodStringState.InputMask == "##-####")
            {
                string strFinPeriod = value.ToString();

                if (strFinPeriod.Length == 6)
                {
                    value = string.Format("{0}-{1}", strFinPeriod.Substring(0, 2), strFinPeriod.Substring(2, 4));
                }
            }

            return value;
        }

        private string GetAllTranslations(PXCache sender, string field, int i, string[] neutral, string[] theonly)
        {
            PXLocale[] locales = Common.PXContext.GetSlot<PXLocale[]>("SILocales");
            if (locales == null)
            {
                Common.PXContext.SetSlot("SILocales", locales = PXLocalesProvider.GetLocales());
            }
            if (locales.Length <= 1)
            {
                return theonly[i];
            }
            HashSet<string> list = new HashSet<string>();
            foreach (var locale in locales)
            {
                if (!String.Equals(locale.Name, System.Threading.Thread.CurrentThread.CurrentCulture.Name))
                {
                    using (new Common.PXCultureScope(new System.Globalization.CultureInfo(locale.Name)))
                    {
                        string[] labels = new string[neutral.Length];
                        PXLocalizerRepository.ListLocalizer.Localize(field, sender, neutral, labels);
                        if (!String.IsNullOrWhiteSpace(labels[i]))
                        {
                            list.Add(labels[i]);
                        }
                    }
                }
                else if (!String.IsNullOrWhiteSpace(theonly[i]))
                {
                    list.Add(theonly[i]);
                }
            }
            if (list.Count > 1)
            {
                return String.Join(" ", list);
            }
            return theonly[i];
        }

		public static PXSearchableAttribute GetSearchableAttribute(PXCache dacCache)
		{
			dacCache.ThrowOnNull(nameof(dacCache));

			if (!dacCache.Fields.Contains(nameof(INotable.NoteID)))
				return null;

			return dacCache.GetAttributesReadonly(nameof(INotable.NoteID))
						   .OfType<PXSearchableAttribute>()
						   .FirstOrDefault();
		}

        [PXInternalUseOnly]
        public static List<Type> GetAllSearchableEntities(PXGraph graph) =>
			ServiceManager.TableList.Where(table => table.Type.GetNestedType(nameof(Note.noteID)) != null &&
													GetSearchableAttribute(graph.Caches[table.Type]) != null)
									.Select(table => table.Type)
									.ToList(capacity: 30);

        [PXInternalUseOnly]
        public virtual Dictionary<Type, object> ExtractValues(PXCache sender, object row, PXResult res, IEnumerable<Type> fieldTypes)
        {
            return ExtractValues(sender, row, res, fieldTypes, false);
        }

        private Dictionary<Type, object> ExtractValues(PXCache rowCache, object row, PXResult res, IEnumerable<Type> fieldTypesCollection, bool buildTranslations)
        {
            var extractedValuesByField = new Dictionary<Type, object>();      
            var selectorFieldByTable = new Dictionary<Type, Type>();

            Type lastField = null;
            Type cacheDac = rowCache.GetItemType();
            List<Type> fieldTypes = fieldTypesCollection.ToList(capacity: (titleFields?.Length ?? 0) + (fields?.Length ?? 0));
            HashSet<Type> fieldTypesSet = fieldTypes.ToHashSet();

            foreach (Type field in fieldTypes)
            {
                Type dacTypeContainingField = BqlCommand.GetItemType(field);

                if (dacTypeContainingField == null)
                    continue;

                if (cacheDac.IsAssignableFrom(dacTypeContainingField) || dacTypeContainingField.IsAssignableFrom(cacheDac))  //field of the given table or a base dac/table
                {
                    if (!extractedValuesByField.ContainsKey(field))
                    {
                        object fieldValue = GetFieldValue(rowCache, row, field, disableLazyLoading: res != null, buildTranslations);
                        extractedValuesByField.Add(field, fieldValue);
                    }

                    lastField = field;
                }
                else if (lastField != null && typeof(IBqlTable).IsAssignableFrom(dacTypeContainingField))    //field of any other table
                {
                    object foreignDac = null;

                    if (res != null)
                    {
                        //mass processing - The values are searched in the joined resultset.
                        foreignDac = res[dacTypeContainingField];

                        if (foreignDac != null)
                        {
                            PXCache foreignCache = rowCache.Graph.Caches[foreignDac.GetType()];

                            if (!extractedValuesByField.ContainsKey(field))
                            {
                                object fieldValue = GetFieldValue(foreignCache, foreignDac, field, disableLazyLoading: false, buildTranslations);
                                extractedValuesByField.Add(field, fieldValue);
                            }
                        }
                    }

                    if (foreignDac != null)
                        continue;

                    //lazy loading - The values are selected through the selectors, with a call to DB                           
                    string selectorFieldName = selectorFieldByTable.TryGetValue(dacTypeContainingField, out Type selectorField)
                        ? selectorField.Name
                        : lastField.Name;
                    foreignDac = PXSelectorAttribute.Select(rowCache, row, selectorFieldName);

                    if (foreignDac == null)
                    {
                        var aggregateAttributes = rowCache.GetAttributesReadonly(selectorFieldName).OfType<PXAggregateAttribute>();

                        foreach (PXAggregateAttribute aggregateAttribute in aggregateAttributes)
                        {
                            var dimensionSelectorAttr = aggregateAttribute.GetAttribute<PXDimensionSelectorAttribute>();
                            var selectorAttr = dimensionSelectorAttr?.GetAttribute<PXSelectorAttribute>()
                                                                    ?? aggregateAttribute.GetAttribute<PXSelectorAttribute>();
                            if (selectorAttr == null)
                                continue;

                            PXView select = rowCache.Graph.TypedViews.GetView(selectorAttr.PrimarySelect, !selectorAttr.DirtyRead);
                            object[] pars = new object[selectorAttr.ParsCount + 1];
                            pars[pars.Length - 1] = rowCache.GetValue(row, selectorAttr.FieldOrdinal);
                            foreignDac = rowCache._InvokeSelectorGetter(row, selectorAttr.FieldName, select, pars, true) ??
                                         PXSelectorAttribute.SelectSingleBound(select, new object[] { row, rowCache.Graph.Accessinfo }, pars);
                        }
                    }

                    if (foreignDac is PXResult wrappedForeignDac)
                        foreignDac = wrappedForeignDac[0];

                    if (foreignDac != null)
                    {
                        if (!selectorFieldByTable.ContainsKey(dacTypeContainingField))
                        {
                            selectorFieldByTable.Add(dacTypeContainingField, lastField);
                            //result.Remove(lastField);
                        }

                        PXCache foreignCache = rowCache.Graph.Caches[foreignDac.GetType()];

                        if (!extractedValuesByField.ContainsKey(field))
                        {
                            object fieldValue = GetFieldValue(foreignCache, foreignDac, field, disableLazyLoading: false, buildTranslations);
                            extractedValuesByField.Add(field, fieldValue);
                        }
                    }
                    else if (ForeignDacFieldRetrievalInfos != null && ForeignDacFieldRetrievalInfos.Length > 0)
                    {
                        TryAddValueFromForeignDAC(rowCache.Graph, dacTypeContainingField, field, extractedValuesByField, fieldTypesSet,
                                                  rowCache, row, disableLazyLoading: res != null, buildTranslations);
                    }
                }
            }

            return extractedValuesByField;
        }

        private bool TryAddValueFromForeignDAC(PXGraph graph, Type foreignDacType, Type foreignDacField, Dictionary<Type, object> extractedValuesByField,
                                               HashSet<Type> fieldTypesSet, PXCache rowCache, object row, bool disableLazyLoading, bool buildTranslations)
		{
			if (extractedValuesByField.ContainsKey(foreignDacField))
				return true;

			IForeignDacFieldRetrievalInfo dacFieldsRetrievalInfo = 
                ForeignDacFieldRetrievalInfos.FirstOrDefault(info => info.ForeignDac.IsAssignableFrom(foreignDacType));

            if (dacFieldsRetrievalInfo?.ForeignDacFields.Contains(foreignDacField) != true)
                return false;

            BqlCommand selectQuery = BqlCommand.CreateInstance(dacFieldsRetrievalInfo.Query);

            if (selectQuery == null)
                return false;

            PXView view = new PXView(graph, isReadOnly: true, selectQuery);
            object[] parameters = PrepareParametersForForeignDacRetrieval(dacFieldsRetrievalInfo, fieldTypesSet, rowCache, row, 
                                                                          disableLazyLoading, buildTranslations, extractedValuesByField);
            var foreignDacResult = view.SelectSingle(parameters) as PXResult;

            if (foreignDacResult == null)
                return false;

            IBqlTable foreignDac = PXResult.Unwrap(foreignDacResult, foreignDacType);

            if (foreignDac == null)
                return false;

            var foreignDacCache = graph.Caches[foreignDacType];

            // Cache only foreign DAC fields that are requested by the user from the ExtractValues method 
            // to avoid the expansion of the returned result with fields that weren't requested.     
            var allForeignDacFieldsToCache = dacFieldsRetrievalInfo.ForeignDacFields.Where(field => fieldTypesSet.Contains(field));

            foreach (Type field in allForeignDacFieldsToCache)
			{
                object fieldValue = foreignDacCache.GetValue(foreignDac, field.Name);
                extractedValuesByField[field] = fieldValue;
            }

            // The foreignDacField value should be added because the field is always among fields requested by the user 
            // it is a part of the collection of fields passed by the user to ExtractValues
            return true;
        } 

        private object[] PrepareParametersForForeignDacRetrieval(IForeignDacFieldRetrievalInfo dacFieldsRetrievalInfo, HashSet<Type> fieldTypesSet,
                                                                 PXCache rowCache, object row, bool disableLazyLoading, bool buildTranslations,
                                                                 Dictionary<Type, object> extractedValuesByField)
        {
            if (dacFieldsRetrievalInfo.RequiredDacFields.Length == 0)
                return Array.Empty<object>();

            object[] parameters = new object[dacFieldsRetrievalInfo.RequiredDacFields.Length];

			for (int i = 0; i < dacFieldsRetrievalInfo.RequiredDacFields.Length; i++)
			{
                Type requiredDacField = dacFieldsRetrievalInfo.RequiredDacFields[i];

                if (requiredDacField.DeclaringType == null || 
                    (!BqlTable.IsAssignableFrom(requiredDacField.DeclaringType) && !requiredDacField.DeclaringType.IsAssignableFrom(BqlTable)))
				{
                    Logger.Error("{RequiredDacField} is not a DAC field from the DAC {DacType} which declares the PXSearchable attribute", 
                                 requiredDacField.FullName, BqlTable.FullName);
                    throw new InvalidOperationException($"{requiredDacField.FullName} is not a DAC field from the DAC {BqlTable.FullName} which declares the PXSearchable attribute");
				}

                if (!extractedValuesByField.TryGetValue(requiredDacField, out object requiredFieldValue))
                { 
                    requiredFieldValue = GetFieldValue(rowCache, row, requiredDacField, disableLazyLoading, buildTranslations);

                    // Check that the required DAC field was among the ones requested from the ExtractValues method 
                    // to avoid the expansion of the returned result with fields that weren't requested
                    if (fieldTypesSet.Contains(requiredDacField))
                        extractedValuesByField.Add(requiredDacField, requiredFieldValue);
                }

                parameters[i] = requiredFieldValue;
            }

            return parameters;
        }

        /// <exclude/>
        [DebuggerDisplay("{Title} / {Line1}; {Line2}")]
        public class RecordInfo
        {
            public string Title { get; private set; }
            public string Line1 { get; private set; }
            public string Line2 { get; private set; }

            public RecordInfo(string title, string line1, string line2)
            {
                this.Title = title;
                this.Line1 = line1;
                this.Line2 = line2;
            }
        }
    }
    
    public class SearchCategory
    {
        public const int All = ushort.MaxValue;

        public static int Parse(string module)
        {
            return All;
        }
    }
}
