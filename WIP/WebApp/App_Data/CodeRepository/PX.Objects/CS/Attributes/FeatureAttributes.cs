using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using PX.Common;
using PX.Data;
using System.Linq;

namespace PX.Objects.CS
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class FeatureRestrictorAttribute : PXAggregateAttribute, IPXRowSelectedSubscriber
	{
		public FeatureRestrictorAttribute(Type checkUsage)
		{
			_Select = checkUsage != null ? BqlCommand.CreateInstance(checkUsage) : null;
			typeName = checkUsage == null ? null : checkUsage.FullName;
		}

		private readonly string typeName;
		protected BqlCommand _Select;


		#region IPXRowSelectedSubscriber Members
		void IPXRowSelectedSubscriber.RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			if (_Select == null ||
				  sender.Graph.GetType() == typeof(PXGraph) ||
				  sender.Graph.GetType() == typeof(PXGenericInqGrph) ||
				  sender.Graph.IsContractBasedAPI) return;

			PXFieldState state = sender.GetStateExt(e.Row, _FieldName) as PXFieldState;
			if (state != null && state.ErrorLevel != PXErrorLevel.Error)
			{
				if ((bool?)state.Value != true)
				{
					if (_Select.GetReferencedFields(false)
						.Where(_ => _.IsNested)
						.Select(_ => _.DeclaringType)
						.Union(_Select.GetTables())
						.Where(_ => typeof(PXCacheExtension).IsAssignableFrom(_))
						.Distinct()
						.All(_ => PXCache.IsActiveExtension(_)))
					{
						PXView view = sender.Graph.TypedViews.GetView(_Select, true);
						if (view.SelectSingle() != null)
							sender.RaiseExceptionHandling(_FieldName, e.Row, false, new PXSetPropertyException(Messages.FeaturesUsageWarning, PXErrorLevel.Warning));
					}
				}
				else
					sender.RaiseExceptionHandling(_FieldName, e.Row, false, null);
			}
		}

		public override string ToString()
		{
			return string.Concat("FeatureRestrictorAttribute<", typeName, ">");
		}

		#endregion
	}

	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class FeatureDependencyAttribute : PXEventSubscriberAttribute, IPXFieldVerifyingSubscriber
	{
		protected Type[] Depencency;
		protected bool AllDependenciesRequired;

		public FeatureDependencyAttribute(params Type[] dependency)
		{
			Depencency = dependency;
			AllDependenciesRequired = false;
		}

		public FeatureDependencyAttribute(bool allDependenciesRequired, params Type[] dependency)
		{
			Depencency = dependency;
			AllDependenciesRequired = allDependenciesRequired;
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);

			foreach (var type in Depencency)
			{
				sender.Graph.FieldUpdated.AddHandler(type.DeclaringType, type.Name, FieldUpdated);
			}
		}

		protected virtual void FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			bool allUnchecked = true;
			bool allChecked = true;
			foreach (Type field in Depencency)
			{
				var newValue = sender.GetValue(e.Row, field.Name);
				if ((bool?)newValue == true && allUnchecked == true)
					allUnchecked = false;
				else if ((bool?)newValue == false && allChecked == true)
					allChecked = false;
			}
			SetFieldValue(sender, e, allUnchecked, allChecked);
		}

		protected virtual void SetFieldValue(PXCache sender, PXFieldUpdatedEventArgs e, bool allUnchecked, bool allChecked)
		{
			if (allUnchecked || (AllDependenciesRequired && !allChecked))
			{
				sender.SetValueExt(e.Row, _FieldName, false);
			}
		}

		public void FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if ((bool?)e.NewValue == true && Depencency != null && e.Row != null)
			{
				bool allUnchecked = true;
				bool allChecked = true;
				string dependencyNames = null;
				foreach (Type field in Depencency)
				{
					PXFieldState state = GetFieldState(sender, e, field);
					if (state != null)
					{
						bool? stateValue = (bool?)state.Value;

						if (stateValue == null) return;

						if (stateValue == true)
							allUnchecked = false;
						else
							allChecked = false;

						dependencyNames = GetDependencyNames(dependencyNames, state, stateValue);
					}
				}
				ShowDependencyError(allUnchecked, allChecked, dependencyNames);
			}
		}

		protected virtual PXFieldState GetFieldState(PXCache sender, PXFieldVerifyingEventArgs e, Type field)
		{
			return sender.GetStateExt(e.Row, field.Name) as PXFieldState;
		}

		protected virtual string GetDependencyNames(string dependencyNames, PXFieldState state, bool? stateValue)
		{
			if (!AllDependenciesRequired || stateValue == false)
				dependencyNames = dependencyNames == null
					? state.DisplayName
					: dependencyNames + ", " + state.DisplayName;
			return dependencyNames;
		}

		protected virtual void ShowDependencyError(bool allUnchecked, bool allChecked, string dependencyNames)
		{
			if (!AllDependenciesRequired && allUnchecked)
				throw new PXSetPropertyException(Depencency.Length > 1 ? Messages.FeaturesDependencies : Messages.FeaturesDependency, dependencyNames);
			if (AllDependenciesRequired && !allChecked)
				throw new PXSetPropertyException(Messages.FeaturesDependenciesAllRequired, dependencyNames);
		}
	}

	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class FeatureMutuallyExclusiveDependencyAttribute : FeatureDependencyAttribute
	{
		public FeatureMutuallyExclusiveDependencyAttribute(params Type[] dependency)
		{
			Depencency = dependency;
			AllDependenciesRequired = false;
		}

		public FeatureMutuallyExclusiveDependencyAttribute(bool allDependenciesRequired, params Type[] dependency)
		{
			Depencency = dependency;
			AllDependenciesRequired = allDependenciesRequired;
		}

		protected override PXFieldState GetFieldState(PXCache sender, PXFieldVerifyingEventArgs e, Type field)
		{
			PXFieldState state = base.GetFieldState(sender, e, field);
			if (sender.GetValuePending(e.Row, field.Name) is bool value)
			{
				state.Value = value;
			}
			return state;
		}

		protected override void SetFieldValue(PXCache sender, PXFieldUpdatedEventArgs e, bool allUnchecked, bool allChecked)
		{
			if (allChecked || (AllDependenciesRequired && !allUnchecked))
			{
				sender.SetValueExt(e.Row, _FieldName, false);
			}
		}

		protected override string GetDependencyNames(string dependencyNames, PXFieldState state, bool? stateValue)
		{
			if (!AllDependenciesRequired || stateValue == true)
				dependencyNames = dependencyNames == null
					? state.DisplayName
					: dependencyNames + ", " + state.DisplayName;
			return dependencyNames;
		}

		protected override void ShowDependencyError(bool allUnchecked, bool allChecked, string dependencyNames)
		{
			if (!AllDependenciesRequired && allChecked)
				throw new PXSetPropertyException(Depencency.Length > 1 ? Messages.FeaturesDependenciesDisabled : Messages.FeaturesDependencyDisabled, dependencyNames);
			if (AllDependenciesRequired && !allUnchecked)
				throw new PXSetPropertyException(Messages.FeaturesDependenciesAllRequiredDisabled, dependencyNames);
		}

	}

	[PXDefault(false)]
	[PXDBBool]
	[PXUIField]
	public class FeatureAttribute : FeatureRestrictorAttribute, IPXFieldSelectingSubscriber, IPXFieldDefaultingSubscriber
	{
		public FeatureAttribute(bool defValue)
			: this(defValue, null, null)
		{

		}
		public FeatureAttribute(bool defValue, Type parent)
			: this(defValue, parent, null)
		{
		}
		public FeatureAttribute(Type parent)
			: this(parent, null)
		{

		}
		public FeatureAttribute(Type parent, Type checkUsage)
			: base(checkUsage)
		{
			Parent = parent;
		}
		public FeatureAttribute(bool defValue, Type parent, Type checkUsage)
			: this(parent, checkUsage)
		{
			this.GetAttribute<PXDefaultAttribute>().Constant = defValue;
		}

		protected bool _defValue;

		public Type Parent { get; set; }

		public string DisplayName
		{
			get { return this.GetAttribute<PXUIFieldAttribute>().DisplayName; }
			set { this.GetAttribute<PXUIFieldAttribute>().DisplayName = value; }
		}

		public bool Enabled
		{
			get { return this.GetAttribute<PXUIFieldAttribute>().Enabled; }
			set { this.GetAttribute<PXUIFieldAttribute>().Enabled = value; }
		}

		public bool Visible
		{
			get { return this.GetAttribute<PXUIFieldAttribute>().Visible; }
			set { this.GetAttribute<PXUIFieldAttribute>().Visible = value; }
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);

			if (this.Parent != null && Parent.DeclaringType != null)
			{
				var type = Parent.DeclaringType;
				if (typeof(PXCacheExtension).IsAssignableFrom(type) && type.BaseType.IsGenericType)
				{
					type = type.BaseType.GetGenericArguments()[type.BaseType.GetGenericArguments().Length - 1];
				}

				sender.Graph.RowUpdated.AddHandler(type, RowUpdated);
			}
		}

		public bool Top
		{
			get;
			set;
		}
		public bool SyncToParent
		{
			get;
			set;
		}

		protected virtual void RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			bool? currentValue = (bool?)sender.GetValue(e.Row, this.Parent.Name);
			bool? oldValue = (bool?)sender.GetValue(e.OldRow, this.Parent.Name);
			object status = sender.GetValue(e.Row, sender.Keys.Last());

			if (status != null && status is int? && (int)status != 3)
				return;

			if (currentValue != oldValue)
			{
				if (currentValue == true)
				{
					object value = sender.GetValue(e.Row, _FieldName);
					if (value == null || !(value is bool?) || (bool?)value != true)
						sender.SetDefaultExt(e.Row, _FieldName);
					if (SyncToParent)
						sender.SetValueExt(e.Row, _FieldName, true);
				}
				else
					sender.SetValueExt(e.Row, _FieldName, false);
			}
		}

		#region IPXFieldSelectingSubscriber Members

		public void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			bool enabled = !SyncToParent;
			if (sender.AllowUpdate != true || (this.Parent != null && (bool?)sender.GetValue(e.Row, this.Parent.Name) == false))
				enabled = false;
			e.ReturnState = PXFieldState.CreateInstance(e.ReturnState, typeof(Boolean), null, null, -1, null, null, null, _FieldName, null, DisplayName, null, PXErrorLevel.Undefined, enabled, null, null, PXUIVisibility.Undefined, null, null, null);
		}

		public void FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (this.Parent != null)
			{
				bool? currentValue = (bool?)sender.GetValue(e.Row, this.Parent.Name);
				if (currentValue != true)
				{
					e.Cancel = true;
					e.NewValue = false;
				}
			}
		}

		#endregion
	}
}
