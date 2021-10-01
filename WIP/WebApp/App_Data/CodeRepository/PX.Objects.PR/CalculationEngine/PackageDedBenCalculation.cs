using PX.Data;
using PX.Objects.EP;
using PX.Objects.PR;
using System;

namespace PX.Objects.PR
{
	[PXHidden]
	public partial class PRCalculationEngine : PXGraph<PRCalculationEngine>
	{
		protected class PackageDedBenCalculation
		{
			private decimal? _DeductionAmount = 0;
			private decimal? _BenefitAmount = 0;

			public decimal RegularHoursForDed { get; private set; }
			public decimal OvertimeHoursForDed { get; private set; }
			public decimal RegularHoursAmountForDed { get; private set; }
			public decimal OvertimeHoursAmountForDed { get; private set; }
			public decimal RegularHoursForBen { get; private set; }
			public decimal OvertimeHoursForBen { get; private set; }
			public decimal RegularHoursAmountForBen { get; private set; }
			public decimal OvertimeHoursAmountForBen { get; private set; }
			public decimal? DeductionAmount 
			{
				get 
				{
					return _DeductionAmount;
				}
				set
				{
					_DeductionAmount = Math.Round(value.GetValueOrDefault(), 2, MidpointRounding.AwayFromZero);
				}
			}
			public decimal? BenefitAmount 
			{
				get
				{
					return _BenefitAmount;
				}
				set
				{
					_BenefitAmount = Math.Round(value.GetValueOrDefault(), 2, MidpointRounding.AwayFromZero);
				}
			}

			public decimal TotalHoursForDed => RegularHoursForDed + OvertimeHoursForDed;
			public decimal TotalAmountForDed => RegularHoursAmountForDed + OvertimeHoursAmountForDed;
			public decimal TotalHoursForBen => RegularHoursForBen + OvertimeHoursForBen;
			public decimal TotalAmountForBen => RegularHoursAmountForBen + OvertimeHoursAmountForBen;

			public bool SameApplicableForDedAndBen => RegularHoursForDed == RegularHoursForBen
				&& OvertimeHoursForDed == OvertimeHoursForBen
				&& RegularHoursAmountForDed == RegularHoursAmountForBen
				&& OvertimeHoursAmountForDed == OvertimeHoursAmountForBen;
			public bool HasAnyApplicableForDed => RegularHoursForDed != 0
				|| OvertimeHoursForDed != 0
				|| RegularHoursAmountForDed != 0
				|| OvertimeHoursAmountForDed != 0
				|| DeductionAmount != 0;
			public bool HasAnyApplicableForBen => RegularHoursForBen != 0
				|| OvertimeHoursForBen != 0
				|| RegularHoursAmountForBen != 0
				|| OvertimeHoursAmountForBen != 0
				|| BenefitAmount != 0;

			public PackageDedBenCalculation(PREarningDetail earning, EPEarningType earningType, PRDeductCode deductCode, PRCalculationEngine graph)
			{
				if (earningType.IsOvertime == true)
				{
					OvertimeHoursForDed = graph.GetDedBenApplicableHours(deductCode, ContributionType.EmployeeDeduction, earning);
					OvertimeHoursAmountForDed = graph.GetDedBenApplicableAmount(deductCode, ContributionType.EmployeeDeduction, earning);
					RegularHoursForDed = 0m;
					RegularHoursAmountForDed = 0m;
					OvertimeHoursForBen = graph.GetDedBenApplicableHours(deductCode, ContributionType.EmployerContribution, earning);
					OvertimeHoursAmountForBen = graph.GetDedBenApplicableAmount(deductCode, ContributionType.EmployerContribution, earning);
					RegularHoursForBen = 0m;
					RegularHoursAmountForBen = 0m;
				}
				else
				{
					RegularHoursForDed = graph.GetDedBenApplicableHours(deductCode, ContributionType.EmployeeDeduction, earning);
					RegularHoursAmountForDed = graph.GetDedBenApplicableAmount(deductCode, ContributionType.EmployeeDeduction, earning);
					OvertimeHoursForDed = 0m;
					OvertimeHoursAmountForDed = 0m;
					RegularHoursForBen = graph.GetDedBenApplicableHours(deductCode, ContributionType.EmployerContribution, earning);
					RegularHoursAmountForBen = graph.GetDedBenApplicableAmount(deductCode, ContributionType.EmployerContribution, earning);
					OvertimeHoursForBen = 0m;
					OvertimeHoursAmountForBen = 0m;
				}
			}

			public void Add(PackageDedBenCalculation other)
			{
				RegularHoursForDed += other.RegularHoursForDed;
				OvertimeHoursForDed += other.OvertimeHoursForDed;
				RegularHoursAmountForDed += other.RegularHoursAmountForDed;
				OvertimeHoursAmountForDed += other.OvertimeHoursAmountForDed;
				RegularHoursForBen += other.RegularHoursForBen;
				OvertimeHoursForBen += other.OvertimeHoursForBen;
				RegularHoursAmountForBen += other.RegularHoursAmountForBen;
				OvertimeHoursAmountForBen += other.OvertimeHoursAmountForBen;
				DeductionAmount += other.DeductionAmount;
				BenefitAmount += other.BenefitAmount;
			}

			#region Obsolete 2020R2
			[Obsolete]
			public decimal RegularHours { get; }
			[Obsolete]
			public decimal RegularHoursAmount { get; }
			[Obsolete]
			public decimal OvertimeHours { get; }
			[Obsolete]
			public decimal OvertimeHoursAmount { get; }
			[Obsolete]
			public PackageDedBenCalculation(PREarningDetail earning, EPEarningType earningType)
			{
				if (earningType.IsOvertime == true)
				{
					OvertimeHoursForDed = earning.Hours.GetValueOrDefault();
					OvertimeHoursAmountForDed = earning.Amount.GetValueOrDefault();
					RegularHoursForDed = 0m;
					RegularHoursAmountForDed = 0m;
					OvertimeHoursForBen = earning.Hours.GetValueOrDefault();
					OvertimeHoursAmountForBen = earning.Amount.GetValueOrDefault();
					RegularHoursForBen = 0m;
					RegularHoursAmountForBen = 0m;
				}
				else
				{
					RegularHoursForDed = earning.Hours.GetValueOrDefault();
					RegularHoursAmountForDed = earning.Amount.GetValueOrDefault();
					OvertimeHoursForDed = 0m;
					OvertimeHoursAmountForDed = 0m;
					RegularHoursForBen = earning.Hours.GetValueOrDefault();
					RegularHoursAmountForBen = earning.Amount.GetValueOrDefault();
					OvertimeHoursForBen = 0m;
					OvertimeHoursAmountForBen = 0m;
				}
			}
			#endregion
		}
	}
}
