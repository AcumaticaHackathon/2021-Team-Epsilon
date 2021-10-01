using PX.Data;
using PX.Objects.PM;
using PX.Objects.PR;
using System;

namespace PX.Objects.PR
{
	[PXHidden]
	public partial class PRCalculationEngine : PXGraph<PRCalculationEngine>
	{
		protected struct FringeSourceEarning
		{
			public FringeSourceEarning(PREarningDetail earning, PMProject project, decimal calculatedFringeRate, decimal setupFringeRate, decimal overtimeMultiplier)
			{
				this.Earning = earning;
				this.Project = project;
				this.CalculatedFringeRate = calculatedFringeRate;
				this.SetupFringeRate = setupFringeRate;
				this.OvertimeMultiplier = overtimeMultiplier;
			}

			public PREarningDetail Earning;
			public PMProject Project;
			public decimal CalculatedFringeRate;
			public decimal SetupFringeRate;
			public decimal OvertimeMultiplier;

			public decimal CalculatedFringeAmount => Math.Round(CalculatedFringeRate * Earning.Hours.GetValueOrDefault(), 2, MidpointRounding.AwayFromZero);

			#region Avoid breaking changes in 2021R1
			[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)]
			public FringeSourceEarning(PREarningDetail earning, PMProject project, decimal calculatedFringeRate, decimal setupFringeRate)
				: this(earning, project, calculatedFringeRate, setupFringeRate, 1)
			{ }
			#endregion
		}
	}
}
