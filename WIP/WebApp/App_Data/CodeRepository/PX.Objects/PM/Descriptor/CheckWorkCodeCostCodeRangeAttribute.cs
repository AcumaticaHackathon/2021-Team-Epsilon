using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using System;
using System.Linq;

namespace PX.Objects.PM
{
	public class CheckWorkCodeCostCodeRangeAttribute : PXEventSubscriberAttribute, IPXRowInsertingSubscriber, IPXRowUpdatingSubscriber, IPXRowPersistingSubscriber
	{
		public void RowInserting(PXCache sender, PXRowInsertingEventArgs e)
		{
			PMWorkCodeCostCodeRange row = e.Row as PMWorkCodeCostCodeRange;
			if (row == null)
			{
				return;
			}

			Verify(sender, row);
		}

		public void RowUpdating(PXCache sender, PXRowUpdatingEventArgs e)
		{
			PMWorkCodeCostCodeRange row = e.NewRow as PMWorkCodeCostCodeRange;
			if (row == null)
			{
				return;
			}

			Verify(sender, row);
		}

		public void RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			PMWorkCodeCostCodeRange row = e.Row as PMWorkCodeCostCodeRange;
			if (row == null || (e.Operation & PXDBOperation.Command) == PXDBOperation.Delete)
			{
				return;
			}

			Verify(sender, row);
		}

		public void Verify(PXCache sender, PMWorkCodeCostCodeRange row)
		{
			if (string.IsNullOrEmpty(row.CostCodeFrom) && string.IsNullOrEmpty(row.CostCodeTo))
			{
				sender.RaiseExceptionHandling(_FieldName, row, null, new PXSetPropertyException(PXMessages.LocalizeFormat(
					Messages.EmptyCostCodeRange,
					PXUIFieldAttribute.GetDisplayName(sender, nameof(PMWorkCodeCostCodeRange.costCodeFrom)),
					PXUIFieldAttribute.GetDisplayName(sender, nameof(PMWorkCodeCostCodeRange.costCodeTo))), PXErrorLevel.RowError));
			}
			else if (string.IsNullOrEmpty(row.CostCodeFrom))
			{
				if (SelectFrom<PMWorkCodeCostCodeRange>
					.Where<PMWorkCodeCostCodeRange.workCodeID.IsNotEqual<P.AsString>
						.And<PMWorkCodeCostCodeRange.costCodeFrom.IsLessEqual<P.AsString>
							.Or<PMWorkCodeCostCodeRange.costCodeFrom.IsNull>>>.View.Select(sender.Graph, row.WorkCodeID, row.CostCodeTo).TopFirst != null)
				{
					sender.RaiseExceptionHandling(nameof(PMWorkCodeCostCodeRange.costCodeFrom), row, null, new PXSetPropertyException(Messages.OverlappingCostCode));
				}
			}
			else if (string.IsNullOrEmpty(row.CostCodeTo))
			{
				if (SelectFrom<PMWorkCodeCostCodeRange>
					.Where<PMWorkCodeCostCodeRange.workCodeID.IsNotEqual<P.AsString>
						.And<PMWorkCodeCostCodeRange.costCodeTo.IsGreaterEqual<P.AsString>
							.Or<PMWorkCodeCostCodeRange.costCodeTo.IsNull>>>.View.Select(sender.Graph, row.WorkCodeID, row.CostCodeFrom).TopFirst != null)
				{
					sender.RaiseExceptionHandling(nameof(PMWorkCodeCostCodeRange.costCodeTo), row, null, new PXSetPropertyException(Messages.OverlappingCostCode));
				}
			}
			else if (row.CostCodeTo.CompareTo(row.CostCodeFrom) < 0)
			{
				sender.RaiseExceptionHandling(nameof(PMWorkCodeCostCodeRange.costCodeTo), row, row.CostCodeTo, new PXSetPropertyException(PXMessages.LocalizeFormat(
					Messages.CostCodeToGreaterThanFrom,
					PXUIFieldAttribute.GetDisplayName(sender, nameof(PMWorkCodeCostCodeRange.costCodeTo)),
					PXUIFieldAttribute.GetDisplayName(sender, nameof(PMWorkCodeCostCodeRange.costCodeFrom)))));
			}
			else if (SelectFrom<PMWorkCodeCostCodeRange>
				.Where<PMWorkCodeCostCodeRange.workCodeID.IsNotEqual<P.AsString>
					.And<Brackets<PMWorkCodeCostCodeRange.costCodeFrom.IsLessEqual<P.AsString>
							.And<PMWorkCodeCostCodeRange.costCodeTo.IsNull
								.Or<PMWorkCodeCostCodeRange.costCodeTo.IsGreaterEqual<P.AsString>>>>
						.Or<PMWorkCodeCostCodeRange.costCodeTo.IsGreaterEqual<P.AsString>
							.And<PMWorkCodeCostCodeRange.costCodeFrom.IsNull
								.Or<PMWorkCodeCostCodeRange.costCodeFrom.IsLessEqual<P.AsString>>>>>>.View
				.Select(sender.Graph, row.WorkCodeID, row.CostCodeTo, row.CostCodeFrom, row.CostCodeFrom, row.CostCodeTo).TopFirst != null)
			{
				sender.RaiseExceptionHandling(_FieldName, row, row.CostCodeFrom, new PXSetPropertyException(Messages.OverlappingCostCode, PXErrorLevel.RowError));
			}
		}
	}
}
