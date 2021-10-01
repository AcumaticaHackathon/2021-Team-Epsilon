using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Compilation;

namespace PX.Objects.FS
{
    public class CalendarComponentSetupMaint : PXGraph<CalendarComponentSetupMaint>
    {
        #region Actions
        public PXSave<FSSetup> Save;
        public PXCancel<FSSetup> Cancel;
        #endregion

        #region View Definitions
        [PXDynamicButton(new string[] { AppBoxFieldsPasteLineCmd, AppBoxFieldsResetLineCmd },
                         new string[] { ActionsMessages.PasteLine, ActionsMessages.ResetOrder },
                         TranslationKeyType = typeof(Common.Messages))]
        public class AppointmentBoxFields_View : PXOrderedSelect<FSSetup, AppointmentBoxComponentField,
                                                 Where<True, Equal<True>>,
                                                 OrderBy<
                                                     Asc<AppointmentBoxComponentField.sortOrder>>>
        {
            public AppointmentBoxFields_View(PXGraph graph) : base(graph) { }

            public AppointmentBoxFields_View(PXGraph graph, Delegate handler) : base(graph, handler) { }

            public const string AppBoxFieldsPasteLineCmd = "AppBoxFieldsPasteLineCmd";
            public const string AppBoxFieldsResetLineCmd = "AppBoxFieldsResetLineCmd";

            protected override void AddActions(PXGraph graph)
            {
                AddAction(graph, AppBoxFieldsPasteLineCmd, ActionsMessages.PasteLine, PasteLine);
                AddAction(graph, AppBoxFieldsResetLineCmd, ActionsMessages.ResetOrder, ResetOrder);
            }
        }

        [PXDynamicButton(new string[] { SOGridFieldsPasteLineCmd, SOGridFieldsResetLineCmd },
                         new string[] { ActionsMessages.PasteLine, ActionsMessages.ResetOrder },
                         TranslationKeyType = typeof(Common.Messages))]
        public class ServiceOrderComponentFields_View : PXOrderedSelect<FSSetup, ServiceOrderComponentField,
                                                 Where<True, Equal<True>>,
                                                 OrderBy<
                                                     Asc<ServiceOrderComponentField.sortOrder>>>
        {
            public ServiceOrderComponentFields_View(PXGraph graph) : base(graph) { }

            public ServiceOrderComponentFields_View(PXGraph graph, Delegate handler) : base(graph, handler) { }

            public const string SOGridFieldsPasteLineCmd = "SOGridFieldsPasteLineCmd";
            public const string SOGridFieldsResetLineCmd = "SOGridFieldsResetLineCmd";

            protected override void AddActions(PXGraph graph)
            {
                AddAction(graph, SOGridFieldsPasteLineCmd, ActionsMessages.PasteLine, PasteLine);
                AddAction(graph, SOGridFieldsResetLineCmd, ActionsMessages.ResetOrder, ResetOrder);
            }
        }

        [PXDynamicButton(new string[] { UAGridFieldsPasteLineCmd, UAGridFieldsResetLineCmd },
                         new string[] { ActionsMessages.PasteLine, ActionsMessages.ResetOrder },
                         TranslationKeyType = typeof(Common.Messages))]
        public class UnassignedAppComponentFields_View : PXOrderedSelect<FSSetup, UnassignedAppComponentField,
                                                         Where<True, Equal<True>>,
                                                         OrderBy<
                                                             Asc<ServiceOrderComponentField.sortOrder>>>
        {
            public UnassignedAppComponentFields_View(PXGraph graph) : base(graph) { }

            public UnassignedAppComponentFields_View(PXGraph graph, Delegate handler) : base(graph, handler) { }

            public const string UAGridFieldsPasteLineCmd = "UAGridFieldsPasteLineCmd";
            public const string UAGridFieldsResetLineCmd = "UAGridFieldsResetLineCmd";

            protected override void AddActions(PXGraph graph)
            {
                AddAction(graph, UAGridFieldsPasteLineCmd, ActionsMessages.PasteLine, PasteLine);
                AddAction(graph, UAGridFieldsResetLineCmd, ActionsMessages.ResetOrder, ResetOrder);
            }
        }
        #endregion

        #region Selects
        public PXSetup<FSSetup> SetupRecord;

        public PXSelect<FSAppointmentStatusColor> StatusColorRecords;

        public PXSelect<FSAppointmentStatusColor,
                    Where<FSAppointmentStatusColor.statusID, Equal<Current<FSAppointmentStatusColor.statusID>>>> StatusColorSelected;

        public AppointmentBoxFields_View AppointmentBoxFields;

        public ServiceOrderComponentFields_View ServiceOrderFields;

        public UnassignedAppComponentFields_View UnassignedAppointmentFields;
        #endregion

        #region Public Methods
        public virtual void ComponentFieldRowSelect(PXCache cache, FSCalendarComponentField row, Type objectName, Type field, bool isServiceOrder)
        {
            if (row != null)
            {
                if (!string.IsNullOrEmpty(row.ObjectName))
                {
                    List<string> values = new List<string>();
                    List<string> labels = new List<string>();

                    if (!string.IsNullOrEmpty(row.ObjectName))
                    {
                        this.AddTableFields(row.ObjectName, false, values, labels);

                        if (!String.IsNullOrEmpty(row.FieldName))
                        {
                            Type tableType = GetTableType(row.ObjectName, objectName.Name, cache, row);

                            if (tableType == null)
                            {
                                return;
                            }

                            PXCache fieldCache = Caches[tableType];

                            cache.RaiseExceptionHandling<FSCalendarComponentField.fieldName>(row, row.FieldName,
                                     fieldCache.Fields.Contains(row.FieldName) == false ? new PXSetPropertyException(ErrorMessages.GIFieldNotExists, PXErrorLevel.Warning, row.FieldName) : null);
                        }

                        PXStringListAttribute.SetList(cache, row, field.Name, values.ToArray(), labels.ToArray());
                    }
                }
            }

            List<string> aliases = new List<string>();
            List<string> dlabels = new List<string>();

            aliases.Add(typeof(FSServiceOrder).FullName);
            dlabels.Add(typeof(FSServiceOrder).Name);

            if (isServiceOrder == false) 
            {
                aliases.Add(typeof(FSAppointment).FullName);
                dlabels.Add(typeof(FSAppointment).Name);
            }  

            aliases.Add(typeof(FSContact).FullName);
            dlabels.Add(typeof(FSContact).Name);

            aliases.Add(typeof(FSAddress).FullName);
            dlabels.Add(typeof(FSAddress).Name);

            PXStringListAttribute.SetList(cache, row, objectName.Name, aliases.ToArray(), dlabels.ToArray());
        }
        #endregion

        #region Private Methods
        private Type GetTableType(string tableName, string fieldName, PXCache cache, object row, string warningMessage = null)
        {
            var type = PXBuildManager.GetType(tableName, false);

            if (type == null)
            {
                if (string.IsNullOrEmpty(warningMessage))
                {
                    warningMessage = ErrorMessages.GITableNotExists;
                }

                cache.RaiseExceptionHandling(fieldName, row, tableName, new PXSetPropertyException(warningMessage, PXErrorLevel.Warning, tableName));
            }

            return type;
        }

        private void AddTableFields(string tableName, bool needTableName, List<string> strlist, List<string> strDispNames, Func<PXCache, string, bool> predicate = null)
        {
            if (this.IsImport || string.IsNullOrEmpty(tableName))
            {
                return;
            }

            Type t = PXBuildManager.GetType(tableName, false);

            if (t == null)
            {
                return;
            }

            Dictionary<string, string> existingNames = new Dictionary<string, string>();
            List<KeyValuePair<string, string>> items = new List<KeyValuePair<string, string>>();
            PXCache cache = this.Caches[t];

            foreach (String field in cache.Fields)
            {
                String fname = field;
                Type bqlField = cache.GetBqlField(fname);

                if ((bqlField != null
                        && !BqlCommand.GetItemType(bqlField).IsAssignableFrom(t))
                    || (bqlField == null
                        && !fname.EndsWith("_Attributes")
                        && !fname.EndsWith(PXDBDecimalAttribute.SignSuffix)
                        && !cache.IsKvExtAttribute(fname)))
                {
                    continue;
                }

                String bqlFieldName = bqlField != null ? bqlField.Name : fname;

                if (cache.GetAttributes(fname).Any(a => a is PXDBTimestampAttribute) || existingNames.ContainsKey(fname))
                {
                    continue;
                }

                existingNames[fname] = fname;
                fname = needTableName ? tableName + "." + fname : fname;
                PXFieldState state = (PXFieldState)cache.GetStateExt(null, fname);
                string name, displayName;

                if (state != null && !string.IsNullOrEmpty(state.DescriptionName))
                {
                    if (predicate == null || predicate(cache, bqlFieldName + "_description"))
                    {
                        name = needTableName ? tableName + "." + bqlFieldName + "_description" : bqlFieldName + "_description";
                        displayName = fname + "_Description";
                        items.Add(new KeyValuePair<string, string>(name, displayName));
                    }
                }

                if (predicate == null || predicate(cache, bqlFieldName))
                {
                    name = needTableName ? tableName + "." + bqlFieldName : bqlFieldName;
                    displayName = fname;
                    items.Add(new KeyValuePair<string, string>(name, displayName));
                }
            }

            items.Sort(new Comparison<KeyValuePair<string, string>>(delegate (KeyValuePair<string, string> f1, KeyValuePair<string, string> f2)
            {
                return strDispNames == null ? string.Compare(f1.Key, f2.Key) : string.Compare(f1.Value, f2.Value);
            }));

            strlist.AddRange(items.Select(x => x.Key));

            if (strDispNames != null)
            {
                strDispNames.AddRange(items.Select(x => x.Value));
            }
        }
        #endregion

        #region Events
        protected virtual void _(Events.RowSelected<AppointmentBoxComponentField> e)
        {
            ComponentFieldRowSelect(e.Cache, e.Row, typeof(AppointmentBoxComponentField.objectName), typeof(AppointmentBoxComponentField.fieldName), false);
        }

        protected virtual void _(Events.RowSelected<ServiceOrderComponentField> e)
        {
            ComponentFieldRowSelect(e.Cache, e.Row, typeof(ServiceOrderComponentField.objectName), typeof(ServiceOrderComponentField.fieldName), true);
        }

        protected virtual void _(Events.RowSelected<UnassignedAppComponentField> e)
        {
            ComponentFieldRowSelect(e.Cache, e.Row, typeof(UnassignedAppComponentField.objectName), typeof(UnassignedAppComponentField.fieldName), false);
        }
        #endregion
    }
}
