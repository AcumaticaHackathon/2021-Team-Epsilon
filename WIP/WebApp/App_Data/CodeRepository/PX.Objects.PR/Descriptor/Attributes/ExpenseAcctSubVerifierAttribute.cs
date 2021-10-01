using PX.Data;
using PX.Objects.CS;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.PR
{
	public class ExpenseAcctSubVerifierAttribute : PXEventSubscriberAttribute, IPXFieldVerifyingSubscriber, IPXRowPersistingSubscriber
	{
		private Type _SubMaskField;
		private string _MaskEarningType;
		private string _MaskLaborItem;

		public ExpenseAcctSubVerifierAttribute(Type subMaskField, string maskEarningType, string maskLaborItem)
		{
			_SubMaskField = subMaskField;
			_MaskEarningType = maskEarningType;
			_MaskLaborItem = maskLaborItem;
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			sender.Graph.FieldVerifying.AddHandler(sender.GetItemType(), _SubMaskField.Name, (cache, e) =>
			{
				FieldVerifying(cache, e, true);
			});
		}

		public void FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			FieldVerifying(sender, e, false);
		}

		public void RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			string acctDefault = (string)sender.GetValue(e.Row, _FieldOrdinal);
			string subMask = (string)sender.GetValue(e.Row, _SubMaskField.Name);

			if (InvalidAcctSubCombo(sender, e.Row, acctDefault, subMask))
			{
				PXSetPropertyException exception = new PXSetPropertyException(Messages.EarningTypeAndLaborItemInAcctSub,
						sender.GetAttributesOfType<PXUIFieldAttribute>(e.Row, _FieldName).FirstOrDefault()?.DisplayName ?? _FieldName,
						sender.GetAttributesOfType<PXUIFieldAttribute>(e.Row, _SubMaskField.Name).FirstOrDefault()?.DisplayName ?? _SubMaskField.Name);
				sender.RaiseExceptionHandling(_FieldName, e.Row, acctDefault, exception);
				sender.RaiseExceptionHandling(_SubMaskField.Name, e.Row, subMask, exception);
				throw exception;
			}
		}

		private void FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e, bool isSub)
		{
			string acctDefault = (string)(isSub ? sender.GetValue(e.Row, _FieldOrdinal) : e.NewValue);
			string subMask = (string)(isSub ? e.NewValue : sender.GetValue(e.Row, _SubMaskField.Name));

			if (InvalidAcctSubCombo(sender, e.Row, acctDefault, subMask))
			{
				sender.RaiseExceptionHandling(
					isSub ? _SubMaskField.Name : _FieldName,
					e.Row,
					e.NewValue,
					new PXSetPropertyException(Messages.EarningTypeAndLaborItemInAcctSub,
						sender.GetAttributesOfType<PXUIFieldAttribute>(e.Row, _FieldName).FirstOrDefault()?.DisplayName ?? _FieldName,
						sender.GetAttributesOfType<PXUIFieldAttribute>(e.Row, _SubMaskField.Name).FirstOrDefault()?.DisplayName ?? _SubMaskField.Name));
			}
		}

		private bool InvalidAcctSubCombo(PXCache sender, object row, string acctDefault, string subMask)
		{
			if (!PXAccess.FeatureInstalled<FeaturesSet.subAccount>() || string.IsNullOrEmpty(subMask))
			{
				return false;
			}

			string prohibitedSubMaskValue = null;
			if (acctDefault == _MaskEarningType)
			{
				prohibitedSubMaskValue = _MaskLaborItem;
			}
			else if (acctDefault == _MaskLaborItem)
			{
				prohibitedSubMaskValue = _MaskEarningType;
			}

			PRSubAccountMaskAttribute subMaskAttribute = sender.GetAttributesOfType<PRSubAccountMaskAttribute>(row, _SubMaskField.Name).FirstOrDefault();
			if (subMaskAttribute != null)
			{
				PRDimensionMaskAttribute dimensionMaskAttribute = subMaskAttribute.GetAttribute<PRDimensionMaskAttribute>();
				if (dimensionMaskAttribute != null)
				{
					List<string> segmentMaskValues = dimensionMaskAttribute.GetSegmentMaskValues(subMask).ToList();
					if ((!string.IsNullOrEmpty(prohibitedSubMaskValue) && segmentMaskValues.Contains(prohibitedSubMaskValue)) ||
						(segmentMaskValues.Contains(_MaskEarningType) && segmentMaskValues.Contains(_MaskLaborItem)))
					{
						return true;
					}
				}
			}

			return false;
		}
	}
}
