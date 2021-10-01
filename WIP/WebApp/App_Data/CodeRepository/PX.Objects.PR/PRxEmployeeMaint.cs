using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;
using PX.Objects.EP;
using System.Collections;

namespace PX.Objects.PR
{
	public class PRxEmployeeMaint : PXGraphExtension<EmployeeMaint>
	{
		public SelectFrom<PREmployee>.Where<PREmployee.bAccountID.IsEqual<EPEmployee.bAccountID.FromCurrent>>.View PayrollEmployee;

		public SelectFrom<PREmployee>
			.InnerJoin<GL.Branch>.On<PREmployee.parentBAccountID.IsEqual<GL.Branch.bAccountID>>
			.Where<MatchWithBranch<GL.Branch.branchID>
				.And<MatchWithPayGroup<PREmployee.payGroupID>>
				.And<PREmployee.bAccountID.IsEqual<EPEmployee.bAccountID.FromCurrent>>>.View FilteredPayrollEmployee;

		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.payrollModule>();
		}

		public override void Initialize()
		{
			base.Initialize();
			Base.Action.AddMenuAction(CreateEditPREmployee);
		}

		public IEnumerable currentEmployee()
		{
			PREmployee payrollEmployee = PayrollEmployee.Select();
			PREmployee filteredPayrollEmployee = FilteredPayrollEmployee.Select();
			CreateEditPREmployee.SetEnabled(payrollEmployee == null || filteredPayrollEmployee != null);
			CreateEditPREmployee.SetCaption(payrollEmployee == null ? Messages.CreatePREmployeeLabel : Messages.EditPREmployeeLabel);
			return null;
		}

		public PXAction<EPEmployee> CreateEditPREmployee;
		[PXButton]
		[PXUIField(DisplayName = Messages.CreatePREmployeeLabel, MapEnableRights = PXCacheRights.Insert, MapViewRights = PXCacheRights.Insert)]
		public void createEditPREmployee()
		{
			var employeeSettingsGraph = PXGraph.CreateInstance<PREmployeePayrollSettingsMaint>();
			PREmployee payrollEmployee = PayrollEmployee.SelectSingle();
			if (payrollEmployee == null)
			{
				employeeSettingsGraph.Caches[typeof(EPEmployee)] = Base.Caches[typeof(EPEmployee)];
				employeeSettingsGraph.PayrollEmployee.Extend(Base.Employee.Current);
			}
			else if (FilteredPayrollEmployee.SelectSingle() == null)
			{
				return;
			}
			else
			{
				employeeSettingsGraph.PayrollEmployee.Current = payrollEmployee;
			}

			throw new PXRedirectRequiredException(employeeSettingsGraph, string.Empty);
		}

		#region Event Handlers

		protected virtual void _(Events.RowDeleting<EPEmployee> e)
		{
			if (e.Row == null)
			{
				return;
			}

			if (PayrollEmployee.SelectSingle() != null)
			{
				throw new PXException(Messages.DeleteEmployeePayrollSettings);
			}
		}

		protected virtual void _(Events.RowPersisted<PX.SM.UsersInRoles> e, PXRowPersisted baseHandler)
		{
			baseHandler?.Invoke(e.Cache, e.Args);

			if (e.TranStatus == PXTranStatus.Completed)
			{
				MatchWithPayGroupHelper.ClearUserPayGroupIDsSlot();
			}
		}

		#endregion Event Handlers
	}
}