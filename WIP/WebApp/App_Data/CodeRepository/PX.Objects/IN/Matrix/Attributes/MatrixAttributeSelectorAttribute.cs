using PX.Common;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN.Matrix.GraphExtensions;
using PX.Objects.IN.Matrix.Graphs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN.Matrix.Attributes
{
	public class MatrixAttributeSelectorAttribute : PXSelectorAttribute, IPXFieldUpdatedSubscriber, IPXRowPersistingSubscriber
	{
		public const string DummyAttributeName = "~MX~DUMMY~";

		public class dummyAttributeName : PX.Data.BQL.BqlString.Constant<dummyAttributeName>
		{
			public dummyAttributeName() : base(DummyAttributeName) { }
		}

		public const string DummyAttributeValue = nameof(CSAnswers.Value);

		protected Type _secondField;
		protected bool _allowTheSameValue;

		public MatrixAttributeSelectorAttribute(Type type, Type secondField, bool allowTheSameValue, params Type[] fieldList)
			: base(type, fieldList)
		{
			_secondField = secondField;
			_allowTheSameValue = allowTheSameValue;
		}

		public override void FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if (e.NewValue as string == DummyAttributeName)
				return;

			if (e.Row != null && e.NewValue == null)
			{
				string secondFieldValue = sender.GetValue(e.Row, _secondField.Name) as string;

				if (secondFieldValue.IsNotIn(null, DummyAttributeName))
				{
					var values = SelectAll(sender, _FieldName, e.Row);
					bool isListEmpty = IsValueListEmpty(values);

					if (isListEmpty)
					{
						e.NewValue = DummyAttributeName;
						return;
					}
				}
			}

			base.FieldVerifying(sender, e);
		}

		public override void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			base.FieldSelecting(sender, e);

			if (e.ReturnValue as string == DummyAttributeName)
				e.ReturnValue = null;
		}

		public virtual void FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs args)
			=> SetDummyAttribute(sender, args.Row);

		protected virtual void SetDummyAttribute(PXCache cache, object row)
		{
			if (row == null)
				return;

			string fieldValue = cache.GetValue(row, _FieldName) as string;
			string secondFieldValue = cache.GetValue(row, _secondField.Name) as string;

			if (fieldValue.IsNotIn(null, DummyAttributeName) && secondFieldValue == null)
			{
				var secondFieldValues = SelectAll(cache, _secondField.Name, row);
				bool isSecondFieldListEmpty = IsValueListEmpty(secondFieldValues);

				if (isSecondFieldListEmpty)
					cache.SetValueExt(row, _secondField.Name, DummyAttributeName);
			}
			else if (fieldValue == null && secondFieldValue == DummyAttributeName)
			{
				cache.SetValueExt(row, _secondField.Name, null);
			}
		}

		protected virtual bool IsValueListEmpty(List<object> values)
			=> (values.Count <= 0 && !_allowTheSameValue) ||
				(values.Count <= 1 && _allowTheSameValue);

		public virtual void RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			bool insert = (e.Operation & PXDBOperation.Command) == PXDBOperation.Insert;
			bool update = (e.Operation & PXDBOperation.Command) == PXDBOperation.Update;

			if (!insert && !update)
				return;

			string fieldValue = sender.GetValue(e.Row, _FieldName) as string;
			if (fieldValue == DummyAttributeName)
			{
				var values = SelectAll(sender, _FieldName, e.Row);

				if (!IsValueListEmpty(values))
				{
					if (sender.RaiseExceptionHandling(_FieldName, e.Row, null, new PXSetPropertyKeepPreviousException(PXMessages.LocalizeFormat(ErrorMessages.FieldIsEmpty, _FieldName))))
					{
						throw new PXRowPersistingException(_FieldName, null, ErrorMessages.FieldIsEmpty, _FieldName);
					}
				}
			}
		}
	}
}
