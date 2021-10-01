using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.Objects.GL;
using PX.Objects.PR.Standalone;
using System;
using System.Collections.Generic;
using System.Text;

namespace PX.Objects.PM
{
	[PXDBString(PMWorkCode.workCodeID.Length)]
	[PXUIField(DisplayName = "WCC Code", FieldClass = nameof(FeaturesSet.Construction))]
	[PXRestrictor(typeof(Where<PMWorkCode.isActive.IsEqual<True>>), Messages.InactiveWorkCode, typeof(PMWorkCode.workCodeID))]
	public class PMWorkCodeAttribute : AcctSubAttribute, IPXFieldDefaultingSubscriber
	{
		protected Type _CostCodeField;
		protected Type _ProjectField;
		protected Type _ProjectTaskField;
		protected Type _LaborItemField;
		protected Type _EmployeeIDField;

		public PMWorkCodeAttribute()
		{
			PXSelectorAttribute select = new PXSelectorAttribute(typeof(PMWorkCode.workCodeID), DescriptionField = typeof(PMWorkCode.description));
			
			_Attributes.Add(select);
			_SelAttrIndex = _Attributes.Count - 1;
		}

		public PMWorkCodeAttribute(Type costCodeField, Type projectField, Type projectTaskField, Type laborItemField, Type employeeIDField) : this()
		{
			_CostCodeField = costCodeField;
			_ProjectField = projectField;
			_ProjectTaskField = projectTaskField;
			_LaborItemField = laborItemField;
			_EmployeeIDField = employeeIDField;
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);

			PXDBStringAttribute dbStringAttribute = DBAttribute as PXDBStringAttribute;
			if (dbStringAttribute != null && dbStringAttribute.IsKey)
			{
				// Working with selectors without a SubstituteKey as key fields presents some issues. When the selector is a key field, 
				// the input mask for the field gets cleared in CacheAttached. We need to manually set it back here
				// to ensure the selector field works correctly with free-form input.
				StringBuilder inputMask = new StringBuilder(">");
				for (int i = 0; i < dbStringAttribute.Length; i++)
				{
					inputMask.Append("a");
				}
				PXDBStringAttribute.SetInputMask(sender, _FieldName, inputMask.ToString());
			}

			List<Type> sourceFields = new List<Type>() { _CostCodeField, _ProjectField, _ProjectTaskField, _LaborItemField, _EmployeeIDField };
			foreach (Type sourceField in sourceFields)
			{
				SetFieldUpdatedHandler(sender, sourceField);
			}
		}

		public void FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (_ProjectField != null && _ProjectTaskField != null)
			{
				int? projectID = (int?)sender.GetValue(e.Row, _ProjectField.Name);
				int? projectTaskID = (int?)sender.GetValue(e.Row, _ProjectTaskField.Name);
				if (projectID != null && !ProjectDefaultAttribute.IsNonProject(projectID))
				{
					PMWorkCodeProjectTaskSource matchingProjectSource = new SelectFrom<PMWorkCodeProjectTaskSource>
						.InnerJoin<PMWorkCode>.On<PMWorkCodeProjectTaskSource.FK.WorkCode>
						.Where<PMWorkCode.isActive.IsEqual<True>
							.And<PMWorkCodeProjectTaskSource.projectID.IsEqual<P.AsInt>>
							.And<PMWorkCodeProjectTaskSource.projectTaskID.IsEqual<P.AsInt>
								.Or<PMWorkCodeProjectTaskSource.projectTaskID.IsNull>>>
						.OrderBy<PMWorkCodeProjectTaskSource.projectTaskID.Desc>.View(sender.Graph)
						.SelectSingle(projectID, projectTaskID);
					if (matchingProjectSource != null)
					{
						e.NewValue = matchingProjectSource.WorkCodeID;
						return;
					}
				} 
			}

			if (_LaborItemField != null)
			{
				int? laborItemID = (int?)sender.GetValue(e.Row, _LaborItemField.Name);
				if (laborItemID != null)
				{
					PMWorkCodeLaborItemSource matchingLaborItemSource = new SelectFrom<PMWorkCodeLaborItemSource>
						.InnerJoin<PMWorkCode>.On<PMWorkCodeLaborItemSource.FK.WorkCode>
						.Where<PMWorkCode.isActive.IsEqual<True>
							.And<PMWorkCodeLaborItemSource.laborItemID.IsEqual<P.AsInt>>>.View(sender.Graph)
						.SelectSingle(laborItemID);
					if (matchingLaborItemSource != null)
					{
						e.NewValue = matchingLaborItemSource.WorkCodeID;
						return;
					}
				}
			}

			if (_CostCodeField != null && PXAccess.FeatureInstalled<FeaturesSet.costCodes>())
			{
				int? costCodeID = (int?)sender.GetValue(e.Row, _CostCodeField.Name);
				if (costCodeID != null && costCodeID != CostCodeAttribute.GetDefaultCostCode())
				{
					PMCostCode costCode = PMCostCode.PK.Find(sender.Graph, costCodeID);
					if (costCode != null)
					{
						PMWorkCodeCostCodeRange matchingCodeRange = new SelectFrom<PMWorkCodeCostCodeRange>
							.InnerJoin<PMWorkCode>.On<PMWorkCodeCostCodeRange.FK.WorkCode>
							.Where<PMWorkCode.isActive.IsEqual<True>
								.And<PMWorkCodeCostCodeRange.costCodeFrom.IsLessEqual<P.AsString>>
								.And<PMWorkCodeCostCodeRange.costCodeTo.IsGreaterEqual<P.AsString>>>.View(sender.Graph)
							.SelectSingle(costCode.CostCodeCD, costCode.CostCodeCD);
						if (matchingCodeRange != null)
						{
							e.NewValue = matchingCodeRange.WorkCodeID;
							return;
						}
					}
				} 
			}

			if (PXAccess.FeatureInstalled<FeaturesSet.payrollModule>())
			{
				int? employeeID = GetEmployeeID(sender, e.Row);
				if (employeeID != null)
				{
					PREmployee payrollEmployee = PREmployee.PK.Find(sender.Graph, employeeID);
					if (payrollEmployee != null && !string.IsNullOrEmpty(payrollEmployee.WorkCodeID))
					{
						e.NewValue = payrollEmployee.WorkCodeID;
						return;
					}
				}
			}

			// No default found, leave as is
			e.NewValue = sender.GetValue(e.Row, _FieldName);
		}

		protected virtual int? GetEmployeeID(PXCache sender, object row)
		{
			return _EmployeeIDField == null ? null : (int?)sender.GetValue(row, _EmployeeIDField.Name);
		}

		protected void SetFieldUpdatedHandler(PXCache sender, Type field)
		{
			if (field == null)
			{
				return;
			}

			sender.Graph.FieldUpdated.AddHandler(sender.GetItemType(), field.Name, (cache, e) =>
			{
				cache.SetDefaultExt(e.Row, _FieldName);
			});
		}
	}

	public class PMWorkCodeInTimeActivityAttribute : PMWorkCodeAttribute
	{
		private Type _OwnerIDField;

		public PMWorkCodeInTimeActivityAttribute(Type costCodeField, Type projectField, Type projectTaskField, Type laborItemField, Type ownerIDField) :
			base(costCodeField, projectField, projectTaskField, laborItemField, null)
		{
			_OwnerIDField = ownerIDField;
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			SetFieldUpdatedHandler(sender, _OwnerIDField);
		}

		protected override int? GetEmployeeID(PXCache sender, object row)
		{
			if (_OwnerIDField == null)
			{
				return null;
			}

			int? ownerID = sender.GetValue(row, _OwnerIDField.Name) as int?;
			if (ownerID == null)
			{
				return null;
			}

			return new SelectFrom<EPEmployee>
				.Where<EPEmployee.defContactID.IsEqual<P.AsInt>>.View(sender.Graph).SelectSingle(ownerID)?.BAccountID;
		}
	}
}
