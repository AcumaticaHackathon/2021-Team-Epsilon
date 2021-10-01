using PX.Data;

namespace PX.Objects.PR
{
	public static class PRSetupHelper
	{
		public static PRSetup GetPayrollPreferences(PXGraph graph)
		{
			return graph.Caches[typeof(PRSetup)]?.Current as PRSetup ?? new PXSetupSelect<PRSetup>(graph).SelectSingle();
		}
	}
}
