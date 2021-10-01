using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.PM;
using System;

namespace PX.Objects.PR
{
	public class PRxRegisterEntry : PXGraphExtension<RegisterEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.payrollModule>();
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRemoveBaseAttribute(typeof(LocationIDAttribute))]
		[HybridLocationID(typeof(Where<Location.bAccountID, Equal<Current<PMTran.bAccountID>>>), typeof(PMTran.origModule), typeof(PRxPMTran.payrollWorkLocationID), DisplayName = "Location", DescriptionField = typeof(Location.descr))]
		protected virtual void _(Events.CacheAttached<PMTran.locationID> e) { }
	}

	public class HybridLocationIDAttribute : LocationIDAttribute, IPXFieldSelectingSubscriber
	{
		Type _ModuleField;
		Type _PayrollWorkLocationIDField;

		public HybridLocationIDAttribute(Type whereType, Type moduleField, Type payrollWorkLocationIDField)
			: base(whereType)
		{
			_ModuleField = moduleField;
			_PayrollWorkLocationIDField = payrollWorkLocationIDField;
		}

		public void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			object module = sender.GetValue(e.Row, _ModuleField.Name);

			if (module?.Equals(BatchModule.PR) == true && sender.GetValue(e.Row, _PayrollWorkLocationIDField.Name) is int payrollWorkLocationID)
			{
				PRLocation location = new SelectFrom<PRLocation>.Where<PRLocation.locationID.IsEqual<P.AsInt>>.View(sender.Graph).SelectSingle(payrollWorkLocationID);
				if (location != null)
				{
					e.ReturnValue = location.LocationCD;
				}
			}
		}
	}
}
