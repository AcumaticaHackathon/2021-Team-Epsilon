using PX.Data;
using System;
using System.Linq;

namespace PX.Objects.PR
{
	public class ContributionType
	{
		public class ListAttribute : PXStringListAttribute, IPXRowSelectedSubscriber
		{
			private readonly Type _IsWorkersCompensationField;
			private readonly Type _IsPayableBenefitField;
			private readonly Type _IsGarnishmentField;

			public ListAttribute(Type isWorkersCompensationField = null, Type isPayableBenefitField = null) : this(isWorkersCompensationField, isPayableBenefitField, null)
			{ }

			public ListAttribute(Type isWorkersCompensationField, Type isPayableBenefitField, Type isGarnishmentField) : base(
				new string[] { EmployeeDeduction, EmployerContribution, BothDeductionAndContribution },
				new string[] { Messages.EmployeeDeduction, Messages.EmployerContribution, Messages.BothDeductionAndContribution })
			{
				_IsWorkersCompensationField = isWorkersCompensationField;
				_IsPayableBenefitField = isPayableBenefitField;
				_IsGarnishmentField = isGarnishmentField;
			}

			public override void CacheAttached(PXCache sender)
			{
				base.CacheAttached(sender);

				if (_IsWorkersCompensationField != null) 
					sender.Graph.FieldUpdated.AddHandler(_IsWorkersCompensationField.DeclaringType, _IsWorkersCompensationField.Name, CheckList);

				if (_IsPayableBenefitField != null)
					sender.Graph.FieldUpdated.AddHandler(_IsPayableBenefitField.DeclaringType, _IsPayableBenefitField.Name, CheckList);

				if (_IsGarnishmentField != null)
					sender.Graph.FieldUpdated.AddHandler(_IsGarnishmentField.DeclaringType, _IsGarnishmentField.Name, CheckList);
			}
			
			public void RowSelected(PXCache sender, PXRowSelectedEventArgs e)
			{
				if (e.Row != null && (_IsWorkersCompensationField != null || _IsPayableBenefitField != null || _IsGarnishmentField != null))
					SetList(sender, e.Row, _FieldName, GetAllowedValuesAndLabels(sender, e.Row));
			}

			private void CheckList(PXCache sender, PXFieldUpdatedEventArgs args)
			{
				if (args.Row == null)
					return;

				string currentValue = sender.GetValue(args.Row, FieldName) as string;
				Tuple<string, string>[] allowedValuesToLabels = GetAllowedValuesAndLabels(sender, args.Row);

				if (allowedValuesToLabels.Length == 1)
				{
					if (currentValue == null || allowedValuesToLabels.All(item => item.Item1 != currentValue))
						sender.SetValue(args.Row, FieldName, allowedValuesToLabels[0].Item1);
				}
				else if (currentValue != null && allowedValuesToLabels.All(item => item.Item1 != currentValue))
				{
					sender.SetValue(args.Row, FieldName, null);
				}
			}

			private Tuple<string, string>[] GetAllowedValuesAndLabels(PXCache sender, object row)
			{
				if (_IsGarnishmentField != null && true.Equals(sender.GetValue(row, _IsGarnishmentField.Name)))
				{
					return new[]
					{
						new Tuple<string, string>(EmployeeDeduction, Messages.EmployeeDeduction)
					};
				}
				
				if (_IsWorkersCompensationField != null && true.Equals(sender.GetValue(row, _IsWorkersCompensationField.Name)))
				{
					return new[]
					{
						new Tuple<string, string>(EmployerContribution, Messages.EmployerContribution), 
						new Tuple<string, string>(BothDeductionAndContribution, Messages.BothDeductionAndContribution)
					};
				}

				if (_IsPayableBenefitField != null && true.Equals(sender.GetValue(row, _IsPayableBenefitField.Name)))
				{
					return new[]
					{
						new Tuple<string, string>(EmployerContribution, Messages.EmployerContribution)
					};
				}

				return new[]
				{
					new Tuple<string, string>(EmployeeDeduction, Messages.EmployeeDeduction),
					new Tuple<string, string>(EmployerContribution, Messages.EmployerContribution),
					new Tuple<string, string>(BothDeductionAndContribution, Messages.BothDeductionAndContribution)
				};
			}
		}

		public class employeeDeduction : PX.Data.BQL.BqlString.Constant<employeeDeduction>
		{
			public employeeDeduction()
				: base(EmployeeDeduction)
			{
			}
		}

		public class employerContribution : PX.Data.BQL.BqlString.Constant<employerContribution>
		{
			public employerContribution()
				: base(EmployerContribution)
			{
			}
		}

		public class bothDeductionAndContribution : PX.Data.BQL.BqlString.Constant<bothDeductionAndContribution>
		{
			public bothDeductionAndContribution()
				: base(BothDeductionAndContribution)
			{
			}
		}

		public const string EmployeeDeduction = "DED";
		public const string EmployerContribution = "CNT";
		public const string BothDeductionAndContribution = "BTH";
	}
}
