// This File is Distributed as Part of Acumatica Shared Source Code 
/* ---------------------------------------------------------------------*
*                               Acumatica Inc.                          *
*              Copyright (c) 1994-2011 All rights reserved.             *
*                                                                       *
*                                                                       *
* This file and its contents are protected by United States and         *
* International copyright laws.  Unauthorized reproduction and/or       *
* distribution of all or any portion of the code contained herein       *
* is strictly prohibited and will result in severe civil and criminal   *
* penalties.  Any violations of this copyright will be prosecuted       *
* to the fullest extent possible under law.                             *
*                                                                       *
* UNDER NO CIRCUMSTANCES MAY THE SOURCE CODE BE USED IN WHOLE OR IN     *
* PART, AS THE BASIS FOR CREATING A PRODUCT THAT PROVIDES THE SAME, OR  *
* SUBSTANTIALLY THE SAME, FUNCTIONALITY AS ANY ProjectX PRODUCT.        *
*                                                                       *
* THIS COPYRIGHT NOTICE MAY NOT BE REMOVED FROM THIS FILE.              *
* ---------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Data
{
	#region PXUIBaseStateAttribute
    /// <exclude/>
	public class PXBaseConditionAttribute : PXEventSubscriberAttribute
	{
		protected IBqlCreator _Condition;

		public virtual Type Condition
		{
			get
			{
				return _Condition?.GetType();
			}
			set
			{
				_Condition = PXFormulaAttribute.InitFormula(value);
			}
		}

		public PXBaseConditionAttribute() {}

		public PXBaseConditionAttribute(Type conditionType)
			: this()
		{
			_Condition = PXFormulaAttribute.InitFormula(conditionType);
		}

		public override PXEventSubscriberAttribute Clone(PXAttributeLevel attributeLevel)
		{
			if (attributeLevel == PXAttributeLevel.Item)
				return this;
			return base.Clone(attributeLevel);
		}

		protected internal static bool GetConditionResult(PXCache sender, object row, Type conditionType)
		{
			IBqlCreator condition = PXFormulaAttribute.InitFormula(conditionType);
			bool? result = null;
			object value = null;
			BqlFormula.Verify(sender, row, condition, ref result, ref value);
			return (value as bool?) == true;
		}

		protected static Type GetCondition<AttrType, Field>(PXCache sender, object row)
			where AttrType : PXBaseConditionAttribute
			where Field : IBqlField
		{
			return sender.GetAttributesReadonly<Field>().OfType<AttrType>().Select(attr => (attr).Condition).FirstOrDefault();
		}

		protected static Type GetCondition<AttrType>(PXCache sender, object row, string fieldName)
			where AttrType : PXBaseConditionAttribute
		{
			return sender.GetAttributes(row, fieldName).OfType<AttrType>().Select(attr => (attr).Condition).FirstOrDefault();
		}
	}
	#endregion

	#region PXUIRequiredAttribute
	/// <summary>This attribute conditionally puts a required field marker
	/// and checks for a null value when you save the object.</summary>
	/// <remarks>This field attribute combines the functionality of
	/// <tt>PXUIFieldAttribute.SetRequired</tt> and <tt>PXDefaultAttribute.SetPersistingCheck</tt>
	/// functions. Since <tt>PXUIRequiredAttribute</tt> implies a dynamic change
	/// of the mandatory, it should be used in conjunction with the ASPX property
	/// <tt>MarkRequired = "Dynamic"</tt>.
	/// <para>This attribute subscribes to the <tt>RowSelected</tt> event handler
	/// of the attribute level. It is applicable when the field mandatory
	/// depends on the state of the object only.</para>
	/// <para>You must use this attribute together with the <tt>PXDefaultAttribute</tt>
	/// attribute. You do not need to specify any value to the
	/// <tt>PXDefaultAttribute.PersistingCheck</tt> property, because its value
	/// is set automatically.</para>
	/// <para>Make sure that you do not use this attribute and the
	/// <tt>PXUIFieldAttribute.SetRequired</tt> method at the same time.</para>
	/// </remarks><seealso cref="PXUIEnabledAttribute"/>
	/// <example>
	/// <code lang="CS">
	/// [PXDBString(5, IsUnicode = false, IsFixed = true)]
	/// [PXDefault("W", PersistingCheck = PXPersistingCheck.Nothing)]
	/// [PXUIField(DisplayName = "Default Time Activity Type", Visibility = PXUIVisibility.SelectorVisible)]
	/// // ...
	/// [PXUIRequired(typeof(FeatureInstalled&lt;FeaturesSet.timeReportingModule&gt;))] // IBqlCreator used
	/// public virtual string DefaultActivityType { get; set; }
	/// [ContactDisplayName(typeof(Contact.lastName), typeof(Contact.firstName), typeof(Contact.midName),
	///     typeof(Contact.title), true)]
	/// [PXDependsOnFields(typeof(Contact.lastName), typeof(Contact.firstName), typeof(Contact.midName),
	///     typeof(Contact.title))]
	/// [PXUIField(DisplayName = "Display Name", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
	/// [PXDefault]
	/// // ...
	/// [PXUIRequired(typeof(Where&lt;Contact.contactType, Equal&lt;ContactTypesAttribute.lead&gt;,
	///     Or&lt;Contact.contactType, Equal&lt;ContactTypesAttribute.person&gt;&gt;&gt;))] // IBqlWhere used
	/// public virtual String DisplayName { get; set; }</code>
	/// </example>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Parameter | AttributeTargets.Method, AllowMultiple = false)]
	public class PXUIRequiredAttribute : PXBaseConditionAttribute, IPXFieldSelectingSubscriber, IPXRowPersistingSubscriber
	{
		public PXUIRequiredAttribute(Type conditionType) : base(conditionType) {}

		public override void CacheAttached(PXCache sender)
		{
			PXDefaultAttribute defAttr = GetDefaultAttribute(sender);
			if (defAttr != null)
			{
				defAttr.PersistingCheck = PXPersistingCheck.Nothing;
			}
			else
			{
				throw new PXException(ErrorMessages.UsageOfAttributeWithoutPrerequisites, GetType().Name, typeof(PXDefaultAttribute).Name, sender.GetItemType().FullName, FieldName);
			}
			base.CacheAttached(sender);
			sender.SetAltered(_FieldName, true);
		}

		public virtual void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			object row = e.Row;
			Type conditionType = Condition;
			if (row == null || conditionType == null)
				return;

			var persistingCheck = GetConditionResult(sender, row, conditionType) ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing;
			if (AttributeLevel == PXAttributeLevel.Item || e.IsAltered || persistingCheck != PXPersistingCheck.Nothing)
			{
				e.ReturnState = PXFieldState.CreateInstance(
					e.ReturnState, null, null, null,
					persistingCheck == PXPersistingCheck.Nothing ? (int?) null : 1,
					null, null, null,
					_FieldName, null, null, null,
					PXErrorLevel.Undefined, null, null, null,
					PXUIVisibility.Undefined, null, null, null);
			}
		}

		public virtual void RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			object row = e.Row;
			Type conditionType = Condition;
			if (row == null || conditionType == null)
				return;

			PXDefaultAttribute defaultAttribute = GetDefaultAttribute(sender);
			if (defaultAttribute != null)
			{
				PXPersistingCheck persistingCheck = GetConditionResult(sender, row, conditionType) ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing;
				defaultAttribute.PersistingCheck = persistingCheck;
				defaultAttribute.RowPersisting(sender, e);
				defaultAttribute.PersistingCheck = PXPersistingCheck.Nothing;
			}
		}

		private PXDefaultAttribute GetDefaultAttribute(PXCache sender) => sender.GetAttributesReadonly(_FieldName).OfType<PXDefaultAttribute>().FirstOrDefault();
	}
	#endregion

	#region PXUIEnabledAttribute
	/// <summary>This attribute manages the field editability at runtime.</summary>
	/// <remarks>This attribute subscribes to the <tt>RowSelected</tt> event handler
	/// of the attribute level. It is applicable when the field editability
	/// depends on the state of the object only. The constructor of the attribute
	/// takes a type that represents the condition for enabling or disabling the field
	/// as the first parameter. This type should be the same as for <tt>PXFormulaAttribute</tt>
	/// (<tt>IBqlCreator</tt>, <tt>IBqlField</tt> or <tt>Constant</tt>). As a syntactic sugar,
	/// types that implement <tt>IBqlWhere</tt> interface may be used as well.
	/// <para>You must use this attribute together with the <tt>PXUIFieldAttribute</tt>
	/// attribute.</para>
	/// <para>Make sure that you do not use this attribute and the
	/// <tt>PXUIFieldAttribute.SetEnabled</tt> method at the same time.</para></remarks>
	/// <example>
	/// In the example below, the <tt>AveragingConvention</tt> field is enabled
	/// if <tt>Depreciate</tt> is <tt>true</tt> for the same record. The <tt>Depreciable</tt> field
	/// is enabled if the record does not represent a particular asset (<tt>RecordType != AssetType</tt>),
	/// the asset is not acquired (<tt>IsAcquired != true</tt>), or the record was just inserted
	/// (<tt>EntryStatus = Inserted</tt>).
	/// <code lang="CS">
	/// [PXDBString(2, IsFixed = true)]
	/// [PXUIField(DisplayName = "Averaging Convention")]
	/// [FAAveragingConvention.List]
	/// // ...
	/// [PXUIEnabled(typeof(FABookSettings.depreciate))] // IBqlField used.
	/// public virtual string AveragingConvention { get; set; }
	///
	/// [PXDBBool]
	/// [PXUIField(DisplayName = "Depreciate", Visibility = PXUIVisibility.SelectorVisible)]
	/// // ...
	/// [PXUIEnabled(typeof(Where&lt;FixedAsset.recordType, NotEqual&lt;FARecordType.assetType&gt;, // IBqlWhere used
	///     Or&lt;FixedAsset.isAcquired, NotEqual&lt;True&gt;,
	///     Or&lt;EntryStatus, Equal&lt;EntryStatus.inserted&gt;&gt;&gt;&gt;))]
	/// public virtual bool? Depreciable { get; set; }</code>
	/// </example>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Parameter | AttributeTargets.Method)]
	public class PXUIEnabledAttribute : PXBaseConditionAttribute, IPXFieldSelectingSubscriber
	{
		public PXUIEnabledAttribute(Type conditionType)
			: base(conditionType)
		{
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			sender.SetAltered(_FieldName, true);
		}

		public virtual void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			if (e.Row == null || _Condition == null)
				return;

			e.ReturnState = PXFieldState.CreateInstance(e.ReturnState, null, null, null, null, null, null, null, null, null, null, null, PXErrorLevel.Undefined,
				GetConditionResult(sender, e.Row, Condition),
				null, null, PXUIVisibility.Undefined, null, null, null);
		}
	}
	#endregion

	#region PXUIVisibleAttribute
	/// <summary>This attribute manages the field visibility at runtime.</summary>
	/// <remarks>This attribute subscribes to the <tt>RowSelected</tt> event handler
	/// of the attribute level. It is applicable when the field visibility
	/// depends on the state of the object only.
	/// <para>Make sure that you do not use this attribute and the
	/// <tt>PXUIFieldAttribute.SetVisible</tt> method at the same time.</para></remarks>
	/// <seealso cref="PXUIEnabledAttribute"/>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Parameter | AttributeTargets.Method)]
	public class PXUIVisibleAttribute : PXBaseConditionAttribute, IPXFieldSelectingSubscriber
	{
		public PXUIVisibleAttribute(Type conditionType)
			: base(conditionType)
		{
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			sender.SetAltered(_FieldName, true);
		}

		public virtual void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			if (_Condition == null ||
				e.Row == null && BqlFormula.IsContextualFormula(sender, Condition))
				return;

			e.ReturnState = PXFieldState.CreateInstance(e.ReturnState, null, null, null, null, null, null, null, null, null, null, null, PXErrorLevel.Undefined, null, 
				GetConditionResult(sender, e.Row, Condition),
				null, PXUIVisibility.Undefined, null, null, null);
		}
	}
	#endregion

	/// <exclude/>
	[Flags]
	public enum TriggerPoints
	{
		RowInserted = 1,
		RowPersisting = 1 << 1,
		RowSelected = 1 << 2,
		FieldVerifying = 1 << 3,
	}

	#region PXUIVerifyAttribute
	/// <summary>This attribute enables verifying a field at runtime.</summary>
	/// <remarks>A field is verified when one of the following events occur:
	/// <tt>RowInserted</tt>, <tt>RowPersisting</tt>, <tt>FieldVerifying</tt>. You can also specify that the
	/// verification of a field is performed when the <tt>RowSelected</tt> event occurs;
	/// you can do it by assigning <tt>true</tt> to the <tt>OnSelectingVerify</tt>
	/// parameter of the constructor.</remarks>
	/// <seealso cref="PXUIEnabledAttribute"/>
	/// <example>
	/// <code lang="CS">
	/// [PXDBString(15, IsUnicode = true, IsKey = true, BqlField = typeof(POReceiptLine.receiptNbr))]
	/// [PXUIField(DisplayName = "Receipt Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
	/// // ...
	/// [PXUIVerify(typeof(Where&lt;receiptCuryID, Equal&lt;Current&lt;APInvoice.curyID>>>),  // IBqlWhere used
	///     PXErrorLevel.RowWarning,
	///     Messages.APDocumentCurrencyDiffersFromSourceDocument,
	///     true /* also validate on RowSelected */)]
	/// public virtual String ReceiptNbr { get; set; }</code>
	/// </example>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Parameter | AttributeTargets.Method, AllowMultiple = true)]
	public class PXUIVerifyAttribute : PXBaseConditionAttribute, IPXFieldVerifyingSubscriber, IPXRowInsertedSubscriber, IPXRowPersistingSubscriber
	{
		public TriggerPoints VerificationPoints { get; protected set; }
		protected PXErrorLevel _ErrorLevel;
		protected string _Message;
		protected Type[] _args;

		[Obsolete("This field has been deprecated and will be removed in Acumatica 7.0")]
		protected bool _OnSelectingVerify;
		[Obsolete("This field has been deprecated and will be removed in Acumatica 7.0")]
		protected bool _CheckOnVerifying;
		[Obsolete("This field has been deprecated and will be removed in Acumatica 7.0")]
		protected bool _CheckOnInserted;

		private void SetVerificationPoint(TriggerPoints triggerPoint, bool value)
		{
			if (value)
			{
				this.VerificationPoints |= triggerPoint;
			}
			else
			{
				this.VerificationPoints &= ~triggerPoint;
			}
		}

		public bool CheckOnVerify
		{
			get
			{
				return this.VerificationPoints.HasFlag(TriggerPoints.FieldVerifying);
			}
			set
			{
				this.SetVerificationPoint(TriggerPoints.FieldVerifying, value);
			}
		}

		public bool CheckOnInserted
		{
			get
			{
				return this.VerificationPoints.HasFlag(TriggerPoints.RowInserted);
			}
			set
			{
				this.SetVerificationPoint(TriggerPoints.RowInserted, value);
			}
		}

		public bool CheckOnRowSelected
		{
			get
			{
				return this.VerificationPoints.HasFlag(TriggerPoints.RowSelected);
			} 
			set
			{
				this.SetVerificationPoint(TriggerPoints.RowSelected, value);
			}
		}

		public bool CheckOnRowPersisting
		{
			get
			{
				return this.VerificationPoints.HasFlag(TriggerPoints.RowPersisting);
			}
			set
			{
				this.SetVerificationPoint(TriggerPoints.RowPersisting, value);
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether BQL fields specified by 
		/// <see cref="_args"/> should be converted to field names, not field values,
		/// when composing the error message.
		/// </summary>
		public bool MessageArgumentsAreFieldNames { get; set; } = false;

		public PXUIVerifyAttribute(Type conditionType, PXErrorLevel errorLevel, string message, bool OnSelectingVerify, params Type[] args)
			: this(conditionType, errorLevel, message, args)
		{
			this.CheckOnRowSelected = OnSelectingVerify;
		}

		public PXUIVerifyAttribute(Type conditionType, TriggerPoints verificationPoints, PXErrorLevel errorLevel, string message, params Type[] args)
			: this(conditionType, errorLevel, message, args)
		{
			this.VerificationPoints = verificationPoints;
		}

		public PXUIVerifyAttribute(Type conditionType, PXErrorLevel errorLevel, string message, params Type[] args)
			: base(conditionType)
		{
			_ErrorLevel = errorLevel;
			_Message = message;
			_args = args;
			this.VerificationPoints = TriggerPoints.RowInserted | TriggerPoints.RowPersisting | TriggerPoints.FieldVerifying;
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			if (_Condition != null)
			{
				List<Type> fields = new List<Type>();
				//_Condition.Parse(sender.Graph, null, null, fields, null, null, null);
				var exp = PX.Data.SQLTree.SQLExpression.None();
				_Condition.AppendExpression(ref exp, sender.Graph, new BqlCommandInfo(false) {Fields = fields, BuildExpression = false}, new BqlCommand.Selection());

				foreach (Type t in fields)
				{
					if (t.IsNested && (BqlCommand.GetItemType(t) == sender.GetItemType() || sender.GetItemType().IsSubclassOf(BqlCommand.GetItemType(t))))
					{
						if (!t.Name.Equals(_FieldName, StringComparison.OrdinalIgnoreCase))
						{
							sender.FieldUpdatedEvents[t.Name.ToLower()] += delegate(PXCache cache, PXFieldUpdatedEventArgs e)
							{
								dependentFieldUpdated(cache, e.Row);
							};
						}
					}
				}
				if (this.CheckOnRowSelected)
				{
					sender.RowSelected += delegate(PXCache cache, PXRowSelectedEventArgs e)
					{
						var ex = VerifyingAndGetError(this, sender, e.Row);

						object newValue = null;
						if (ex != null && ex.ErrorLevel == PXErrorLevel.Error)
						{
							newValue = sender.GetValueExt(e.Row, _FieldName);
							if (newValue is PXFieldState)
								newValue = ((PXFieldState)newValue).Value;
						}
						cache.RaiseExceptionHandling(FieldName, e.Row, newValue, ex);
					};
				}

			}
		}

		protected virtual void dependentFieldUpdated(PXCache sender, object row)
		{
			//if (PXUIFieldAttribute.GetError(sender, row, _FieldName) != null)
			//	return;
			var newValue = sender.GetValueExt(row, _FieldName);
			if (newValue is PXFieldState)
				newValue = ((PXFieldState)newValue).Value;

			try
			{
                if (this.CheckOnVerify)
				    Verifying(sender, row);
				sender.RaiseExceptionHandling(_FieldName, row, newValue, null);
			}
			catch (PXSetPropertyException ex)
			{
				sender.RaiseExceptionHandling(_FieldName, row, newValue, ex);
			}
		}

		private void Verifying(PXCache sender, object row)
		{            
			PXSetPropertyException error = VerifyingAndGetError(this, sender, row);
			if(error != null)
				throw error;
		}

		private static PXSetPropertyException VerifyingAndGetError(PXUIVerifyAttribute attr, PXCache sender, object row)
		{
			if (row == null)
				return null;

			if (attr == null)
				throw new PXException(ErrorMessages.AttributeNotDefined, typeof(PXUIVerifyAttribute).Name);

			if (attr.Condition != null && !GetConditionResult(sender, row, attr.Condition))
			{
				List<object> messageParams = new List<object>();

				foreach (Type arg in attr._args)
				{
					if (typeof(IConstant).IsAssignableFrom(arg))
					{
						IConstant constantInstance = Activator.CreateInstance(arg) as IConstant;
						messageParams.Add(constantInstance?.Value);
					}
					else if (attr.MessageArgumentsAreFieldNames)
					{
						messageParams.Add(PXUIFieldAttribute.GetDisplayName(sender, arg.Name));
					}
					else if (arg.IsGenericType && arg.GetGenericTypeDefinition() == typeof(Current<>))
					{
						Type field = arg.GetGenericArguments()[0];
						PXCache cache = sender.Graph.Caches[BqlCommand.GetItemType(field)];
						messageParams.Add(cache.GetValue(cache.Current, field.Name));
					}
					else
					{
						var value = sender.GetValueExt(row, arg.Name);

						if (value is PXFieldState)
							messageParams.Add(((PXFieldState)value).Value);
						else
							messageParams.Add(value);
					}
				}
				return new PXSetPropertyException(attr._Message, attr._ErrorLevel, messageParams.ToArray());
			}
			return null;
		}

		public static PXSetPropertyException VerifyingAndGetError<Field>(PXCache sender, object row) where Field:IBqlField
		{
			PXSetPropertyException err = null;
			foreach (PXSetPropertyException currentError in sender.GetAttributes<Field>(row).OfType<PXUIVerifyAttribute>()
				.Select(attr => VerifyingAndGetError(attr, sender, row))
				.Where(currentError => currentError != null))
			{
				switch (currentError.ErrorLevel)
				{
					case PXErrorLevel.RowError:
						return currentError;
					case PXErrorLevel.Error:
					case PXErrorLevel.RowWarning:
						if (err == null || err.ErrorLevel != PXErrorLevel.Error)
							err = currentError;
						break;
					case PXErrorLevel.Warning:
						if (err == null || err.ErrorLevel != PXErrorLevel.RowWarning)
							err = currentError;
						break;
					case PXErrorLevel.RowInfo:
						if (err == null || err.ErrorLevel != PXErrorLevel.Warning)
							err = currentError;
						break;
					default:
						err = currentError;
						break;
				}
			}
			return err;
		}

		void IPXFieldVerifyingSubscriber.FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if (this.CheckOnVerify)
			{
				var row = sender.CreateCopy(e.Row);
				sender.SetValue(row, _FieldName, e.NewValue);

				try
				{
					Verifying(sender, row);
                    sender.RaiseExceptionHandling(_FieldName, e.Row, null, null);
				}
				catch (PXSetPropertyException ex)
				{
					var newValue = sender.GetValueExt(row, _FieldName);
					if (newValue is PXFieldState)
						newValue = ((PXFieldState)newValue).Value;
					sender.RaiseExceptionHandling(_FieldName, e.Row, newValue, ex);
				}
			}
		}

		void IPXRowInsertedSubscriber.RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			if (this.CheckOnInserted)
			{
				try
				{
					Verifying(sender, e.Row);
				}
				catch (PXSetPropertyException ex)
				{
					var newValue = sender.GetValue(e.Row, _FieldName);
					sender.RaiseExceptionHandling(_FieldName, e.Row, newValue, ex);
				}
			}
		}

		void IPXRowPersistingSubscriber.RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if (!this.CheckOnRowPersisting)
			{
				return;
			}

			try
			{
				Verifying(sender, e.Row);
			}
			catch (PXSetPropertyException ex)
			{
				var newValue = sender.GetValueExt(e.Row, _FieldName);
				if (newValue is PXFieldState)
					newValue = ((PXFieldState)newValue).Value;
				sender.RaiseExceptionHandling(_FieldName, e.Row, newValue, ex);
			}
		}
	}
	#endregion
}