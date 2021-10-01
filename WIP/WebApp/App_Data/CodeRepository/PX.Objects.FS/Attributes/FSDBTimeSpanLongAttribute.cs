using PX.Data;
using PX.Objects.CS;
using System;
using System.Collections.Generic;
using System.Text;

namespace PX.Objects.FS
{
	public class FSDBTimeSpanLongAttribute : PXDBTimeSpanLongAttribute
	{
		#region Ctor
		public FSDBTimeSpanLongAttribute()
		{
			_Format = TimeSpanFormatType.LongHoursMinutes;
		}
		#endregion

		#region Implementation
		public override void CommandPreparing(PXCache sender, PXCommandPreparingEventArgs e)
		{
			if (e.Operation.Command() == PXDBOperation.Select
				&& e.Operation.Option() == PXDBOperation.External
				&& e.Value == null || e.Value is string)
			{
				return;
			}

			base.CommandPreparing(sender, e);
		}
		#endregion
	}


	public class FSDBTimeSpanLongAllowNegativeAttribute: FSDBTimeSpanLongAttribute
	{
		#region State
		protected string _TimeSpanLongHMNegativeMask = "C" + ActionsMessages.TimeSpanLongHM;
		protected string _OutputFormatNegative = "{0}{1,4}{2:00}";
		protected int _MaskLength = 7;
		#endregion

		#region Ctor
		public FSDBTimeSpanLongAllowNegativeAttribute()
		{
		}
		#endregion

		#region Implementation
		public override void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			if (_AttributeLevel == PXAttributeLevel.Item || e.IsAltered)
			{
				string inputMask = PXMessages.LocalizeNoPrefix(_TimeSpanLongHMNegativeMask);
				e.ReturnState = PXStringState.CreateInstance(e.ReturnState, _MaskLength, null, _FieldName, _IsKey, null, String.IsNullOrEmpty(inputMask) ? null : inputMask, null, null, null, null);
			}

			if (e.ReturnValue != null)
			{
				int returnValue = (int)e.ReturnValue;
				string negativeSymbol = " ";

				// It is needed to make ReturnValue positive to avoid errors when the output format is applied
				if (returnValue < 0)
				{
					returnValue *= -1;
					negativeSymbol = "-";
				}

				TimeSpan span = new TimeSpan(0, 0, returnValue, 0);
				int hours = span.Days * 24 + span.Hours;

				e.ReturnValue = string.Format(_OutputFormatNegative, negativeSymbol, hours, span.Minutes);
			}
		}

		public override void FieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e)
		{
			if (e.NewValue is string)
			{
				string str = (string)e.NewValue;
				int factor = 1;

				if (str[0] == '-')
				{
					factor = -1;
					e.NewValue = str.Replace("-", " ");
				}
				else if (str[0] != ' ' && (str[0] < '0' || str[0] > '9'))
				{
					e.NewValue = str.Replace(str[0], ' ');
				}

				int length = ((string)e.NewValue).Length;

				if (length < _MaskLength)
				{
					StringBuilder bld = new StringBuilder(_MaskLength);

					for (int i = length; i < _MaskLength; i++)
					{
						bld.Append('0');
					}

					bld.Append((string)e.NewValue);
					e.NewValue = bld.ToString();
				}

				int val = 0;

				if (!string.IsNullOrEmpty((string)e.NewValue) && int.TryParse(((string)e.NewValue).Replace(" ", "0"), out val))
				{
					int minutes = val % 100;
					int hours = (val - minutes) / 100;

					TimeSpan span = new TimeSpan(0, hours, minutes, 0);

					int totalMinutes = (int)span.TotalMinutes;

					// This is to avoid to insert time durations greater that the actual supported.
					// 600000 = 10000 h 00 m
					if (totalMinutes >= 600000)
					{
						totalMinutes = 599999;
					}

					e.NewValue = totalMinutes * factor;
				}
				else
				{
					e.NewValue = null;
				}
			}

			if (e.NewValue == null)
			{
				e.NewValue = (int)0;
			}
		}
		#endregion
	}
}
