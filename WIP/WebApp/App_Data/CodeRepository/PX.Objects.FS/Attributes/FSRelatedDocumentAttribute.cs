using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.Common;
using PX.Objects.IN;
using PX.Objects.PM;
using PX.Objects.SO;
using System;
using System.Collections;
using System.Text;

namespace PX.Objects.FS
{
    public class FSRelatedDocumentAttribute : PXStringAttribute
    {
        protected EntityHelper helper;

        protected Type _Dac;
        protected Type _EntityType;
        protected Type _DocType;
        protected Type _RefNbr;

        protected string _DocTypeS;
        public class DACReference
        {
            public Type _Type;
            public object[] _Keys;

            public DACReference(Type type, object[] keys)
            {
                _Type = type;
                _Keys = keys;
            }
        }

        public FSRelatedDocumentAttribute(Type dac, Type entityType, Type docType, Type refNbr)
            : base()
        {
            _Dac = dac;
            _EntityType = entityType;
            _DocType = docType;
            _RefNbr = refNbr;
        }

        public FSRelatedDocumentAttribute(Type dac)
            : base()
        {
            _Dac = dac;
        }

        public override void CacheAttached(PXCache sender)
        {
            base.CacheAttached(sender);

            helper = new EntityHelper(sender.Graph);
            PXButtonDelegate del = InitDelegate;
            string ActionName = sender.GetItemType().Name + "$" + _FieldName + "$Link";

            sender.Graph.Actions[ActionName] = (PXAction)Activator.CreateInstance(typeof(PXNamedAction<>).MakeGenericType(sender.BqlTable), new object[] { sender.Graph, ActionName, del, new PXEventSubscriberAttribute[] { new PXUIFieldAttribute { MapEnableRights = PXCacheRights.Select }, new PXButtonAttribute { OnClosingPopup = PXSpecialButtonType.Cancel } } });
        }

        protected virtual DACReference GetDACRef(PXCache cache, object row)
        {
            object[] keys = null;
            Type dacType = null;

            if (_EntityType != null)
            {
                keys = new object[] { cache.GetValue(row, _DocType.Name), cache.GetValue(row, _RefNbr.Name) };
                dacType = GetDACType(cache, row);
            }
            else
            {
                var refNbr = string.Empty;

                IFSRelatedDoc rRow = (IFSRelatedDoc)GetDACRow(cache, row);

                if (rRow != null) 
                { 
                    if (!string.IsNullOrEmpty(rRow.AppointmentRefNbr))
                    {
                        refNbr = rRow.AppointmentRefNbr;
                        dacType = typeof(FSAppointment);
                        keys = new object[] { rRow.SrvOrdType, refNbr };
                    }
                    else if (!string.IsNullOrEmpty(rRow.ServiceOrderRefNbr))
                    {
                        refNbr = rRow.ServiceOrderRefNbr;
                        dacType = typeof(FSServiceOrder);
                        keys = new object[] { rRow.SrvOrdType, refNbr };
                    }
                    else if (!string.IsNullOrEmpty(rRow.ServiceContractRefNbr))
                    {
                        refNbr = rRow.ServiceContractRefNbr;
                        dacType = typeof(FSServiceContract);
                        keys = new object[] { refNbr };
                    }
                }
            }

            return new DACReference(dacType, keys);
        }

        protected virtual Type GetDACType(PXCache cache, object row)
        {
            Type dacType = null;

            object value = cache.GetValue(row, _EntityType.Name);

            if (value == null) 
            {
                return null;
            }

            if (value.Equals(FSEntityType.SalesOrder))
            {
                dacType = typeof(SOOrder);
            }
            else if (value.Equals(FSEntityType.SOInvoice)
                    || value.Equals(FSEntityType.SOCreditMemo))
            {
                dacType = typeof(SOInvoice);
            }
            else if (value.Equals(FSEntityType.ARInvoice) 
                    || value.Equals(FSEntityType.ARCreditMemo))
            {
                dacType = typeof(ARInvoice);
            }
            else if (value.Equals(FSEntityType.APInvoice))
            {
                dacType = typeof(APInvoice);
            }
            else if (value.Equals(FSEntityType.PMRegister))
            {
                dacType = typeof(PMRegister);
            }
            else if (value.Equals(FSEntityType.INReceipt)
                    || value.Equals(FSEntityType.INIssue))
            {
                dacType = typeof(INRegister);
            }

            return dacType;
        }

        protected virtual object GetDACRow(PXCache cache, object currentRow)
        {
            object row = null;

            if (_Dac != null)
            {
                if (_Dac == typeof(SOLine))
                {
                    row = cache.GetExtension<FSxSOLine>(currentRow);
                }
                else if (_Dac == typeof(INTran))
                {
                    row = cache.GetExtension<FSxINTran>(currentRow);
                }
                else if (_Dac == typeof(APTran))
                {
                    row = cache.GetExtension<FSxAPTran>(currentRow);
                }
                else if (_Dac == typeof(ARTran))
                {
                    if (currentRow != null && currentRow.GetType() == typeof(FSARTran))
                    {
                        row = currentRow;
                    }
                    else 
                    {
                        ARTran rowARTran = (ARTran)currentRow;
                        row = FSARTran.PK.Find(cache.Graph, rowARTran?.TranType, rowARTran?.RefNbr, rowARTran?.LineNbr);
                    }
                }
                else if (_Dac == typeof(FSBillHistory))
                {
                    row = currentRow;
                }
            }

            return row;
        }


        protected virtual object GetReturnValue(PXCache sender, object row)
        {
            object returnValue = null;

            DACReference dacRef = GetDACRef(sender, row);

            if (dacRef._Keys != null && dacRef._Type != null)
            {
                returnValue = GetEntityRowID(sender.Graph.Caches[dacRef._Type], dacRef._Keys);
            }

            return returnValue;
        }

        public override void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            object returnValue = GetReturnValue(sender, e.Row);

            if (returnValue == null)
            {
                base.FieldSelecting(sender, e);
            }
            else
            {
                e.ReturnValue = returnValue;
            }
        }

        public virtual object GetEntityRowID(PXCache cache, object[] keys)
        {
            return GetEntityRowID(cache, keys, ", ");
        }

        public static object GetEntityRowID(PXCache cache, object[] keys, string separator)
        {
            StringBuilder result = new StringBuilder();
            int i = 0;

            foreach (string key in cache.Keys)
            {
                if (i >= keys.Length) break;

                object val = keys[i++];
                cache.RaiseFieldSelecting(key, null, ref val, true);

                if (val != null)
                {
                    if (result.Length != 0) result.Append(separator);
                    result.Append(val.ToString().TrimEnd());
                }
            }
            return result.ToString();
        }

        public IEnumerable InitDelegate(PXAdapter adapter)
        {
            PXCache cache = adapter.View.Graph.Caches[_Dac];

            if (cache.Current != null)
            {
                PXRefNoteBaseAttribute.PXLinkState state = null;
                DACReference dacRef = GetDACRef(cache, cache.Current);

                if (dacRef._Keys != null && dacRef._Type != null)
                {
                    state = (PXRefNoteBaseAttribute.PXLinkState)PXRefNoteBaseAttribute.PXLinkState.CreateInstance(null, dacRef._Type, dacRef._Keys);
                }

                if (state != null)
                {
                    helper.NavigateToRow(state.target.FullName, state.keys, PXRedirectHelper.WindowMode.NewWindow);
                }
                else
                {
                    helper.NavigateToRow((Guid?)cache.GetValue(cache.Current, _FieldName), PXRedirectHelper.WindowMode.NewWindow);
                }
            }

            return adapter.Get();
        }
    }
}
