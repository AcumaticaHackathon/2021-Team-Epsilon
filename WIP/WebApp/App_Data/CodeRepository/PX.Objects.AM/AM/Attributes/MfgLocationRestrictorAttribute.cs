using System;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN;

namespace PX.Objects.AM.Attributes
{
    /// <summary>
    /// Manufacturing warehouse location restrictor attribute.
    /// </summary>
    public class MfgLocationRestrictorAttribute : LocationRestrictorAttribute
    {
        protected Type _IsProductionType;
        protected Type _IgnoreLocationRestrictionField;
        protected Type _IgnoreLocationRestrictionParent;

        public MfgLocationRestrictorAttribute(Type IsReceiptType, Type IsSalesType, Type IsProductionType) 
            : base(IsReceiptType, IsSalesType, BqlCommand.Compose(typeof(Where<,>),typeof(boolFalse),typeof(Equal<boolTrue>)))
        {
            _IsProductionType = IsProductionType;
        }

        public MfgLocationRestrictorAttribute(Type IsReceiptType, Type IsSalesType, Type IsProductionType, Type IgnoreLocationRestrictionField)
            : this(IsReceiptType, IsSalesType, IsProductionType)
        {
            _IgnoreLocationRestrictionField = IgnoreLocationRestrictionField;
        }

        public MfgLocationRestrictorAttribute(Type IsReceiptType, Type IsSalesType, Type IsProductionType, Type IgnoreLocationRestrictionField, Type IgnoreLocationRestrictionParent)
            : this(IsReceiptType, IsSalesType, IsProductionType, IgnoreLocationRestrictionField)
        {
            _IgnoreLocationRestrictionParent = IgnoreLocationRestrictionParent;
        }

        //Mod to check IsProduction...
        public override void FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
        {
            INLocation location = null;
            try
            {
                location = (INLocation)PXSelectorAttribute.Select(sender, e.Row, _FieldName, e.NewValue);
            }
            catch (FormatException) { }

            if (IgnoreLocationRestriction(sender, e.Row))
            {
                return;
            }
            
            if (_AlteredCmd != null && location != null)
            {
                bool? IsReceipt = VerifyExpr(sender, e.Row, _IsReceiptType);
                bool? IsSales = VerifyExpr(sender, e.Row, _IsSalesType);
                bool? IsProduction = VerifyExpr(sender, e.Row, _IsProductionType);

                if (IsReceipt.GetValueOrDefault() && !location.ReceiptsValid.GetValueOrDefault())
                {
                    ThrowErrorItem(PX.Objects.IN.Messages.LocationReceiptsInvalid, e, location.LocationCD);
                }

                if (IsSales.GetValueOrDefault() && !location.SalesValid.GetValueOrDefault())
                {
                    ThrowErrorItem(PX.Objects.IN.Messages.LocationSalesInvalid, e, location.LocationCD);
                }

                if (IsProduction.GetValueOrDefault() && !location.ProductionValid.GetValueOrDefault())
                {
                    ThrowErrorItem(Messages.LocationProductionInvalid, e, location.LocationCD);
                }
            }
        }

        /// <summary>
        /// A condition where the manufacture location restrictions should be ignored (any location allowed)
        /// </summary>
        protected virtual bool IgnoreLocationRestriction(PXCache sender, object row)
        {
            if (_IgnoreLocationRestrictionField == null)
            {
                return false;
            }

            if (_IgnoreLocationRestrictionParent == null || _IgnoreLocationRestrictionParent.Name == _BqlTable.Name)
            {
                //get from self...
                return GetValueAsBool(sender, row, _IgnoreLocationRestrictionField.Name);
            }

            //else - get from parent...
            return GetValueAsBool(sender.Graph.Caches[_IgnoreLocationRestrictionParent],
                PXParentAttribute.SelectParent(sender, row, _IgnoreLocationRestrictionParent), 
                _IgnoreLocationRestrictionField.Name);
        }

        protected virtual bool GetValueAsBool(PXCache sender, object row, string fieldName)
        {
            if (sender == null || row == null)
            {
                return false;
            }

            var isRestrictionIgnored = sender.GetValue(row, fieldName);
            if (isRestrictionIgnored is bool)
            {
                return (bool)isRestrictionIgnored;
            }
            return false;
        }

        public override void ThrowErrorItem(string message, PXFieldVerifyingEventArgs e, object ErrorValue)
        {
            try
            {
                string locationCD = (string) (ErrorValue ?? string.Empty);
                //At least indicate the location CD value in the trace window...
                PXTrace.WriteWarning("MFG Location Error: {0} - {1}", locationCD.TrimIfNotNullEmpty(), message);
            }
            catch (Exception) { }
            base.ThrowErrorItem(message, e, ErrorValue);
        }
    }
}