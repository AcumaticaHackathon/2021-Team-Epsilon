using PX.Data;
using PX.Objects.Common;
using PX.Objects.Common.Tools;
using PX.Objects.GL;
using PX.Objects.GL.FinPeriods.TableDefinition;
using PX.Objects.GL.FinPeriods;
using System;

namespace PX.Objects.CM
{
	public class RevalueAcountsBase<THistory> : PXGraph<RevalueAcountsBase<THistory>> 
		where THistory : class, IBqlTable, new()
	{
		[InjectDependency]
		public IFinPeriodRepository FinPeriodRepository { get; set; }

		[InjectDependency]
		public IFinPeriodUtils FinPeriodUtils { get; set; }

		public virtual ProcessingResult CheckFinPeriod(string finPeriodID, int? branchID)
		{
			ProcessingResult result = new ProcessingResult();
			int? organizationID = PXAccess.GetParentOrganizationID(branchID);
			FinPeriod period = FinPeriodRepository.FindByID(organizationID, finPeriodID);

			if (period == null)
			{
				result.AddErrorMessage(GL.Messages.FinPeriodDoesNotExistForCompany,
						FinPeriodIDFormattingAttribute.FormatForError(finPeriodID),
						PXAccess.GetOrganizationCD(PXAccess.GetParentOrganizationID(branchID)));
			}
			else
			{
				result = FinPeriodUtils.CanPostToPeriod(period);
			}

			if (!result.IsSuccess)
			{
				PXProcessing<THistory>.SetError(new PXException(result.GetGeneralMessage()));
			}

			return result;
		}

		public virtual void VerifyCurrencyEffectiveDate(PXCache cache, RevalueFilter filter)
		{
			cache.RaiseExceptionHandling<RevalueFilter.curyEffDate>(filter, filter.CuryEffDate, null);

			string finPeriodID = filter?.FinPeriodID;
			DateTime? curyEffDate = filter?.CuryEffDate;

			if (curyEffDate == null || String.IsNullOrEmpty(finPeriodID)) return;

			int? organizationID = PXAccess.GetParentOrganizationID(Accessinfo.BranchID);
			FinPeriod currentPeriod = FinPeriodRepository.FindByID(organizationID, finPeriodID);

			if (currentPeriod == null) return;

			bool dateBelongsToFinancialPeriod = currentPeriod.IsAdjustment == true && curyEffDate == currentPeriod.EndDate.Value.AddDays(-1)
												|| currentPeriod.IsAdjustment != true && curyEffDate >= currentPeriod.StartDate && curyEffDate < currentPeriod.EndDate;

			if (!dateBelongsToFinancialPeriod)
			{
				cache.RaiseExceptionHandling<RevalueFilter.curyEffDate>(filter, filter.CuryEffDate, new PXSetPropertyException(Messages.DateNotBelongFinancialPeriod, PXErrorLevel.Warning));
			}
		}

		protected virtual void _(Events.FieldDefaulting<RevalueFilter, RevalueFilter.curyEffDate> e)
		{
			if (e.Row == null) return;

			FinPeriod currentPeriod = FinPeriodRepository.FindByID(
				PXAccess.GetParentOrganizationID(Accessinfo.BranchID), 
				e.Row.FinPeriodID);

			if (currentPeriod?.EndDate != null)
			{
				e.NewValue = currentPeriod.EndDate;
				e.Cancel = true;
			}
		}

	}
}
