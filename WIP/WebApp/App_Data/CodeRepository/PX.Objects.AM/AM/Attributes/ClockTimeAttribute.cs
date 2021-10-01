using PX.Data;
using System;

namespace PX.Objects.AM.Attributes
{
    public class ClockTimeAttribute : PXTimeSpanLongAttribute, IPXFieldSelectingSubscriber
    {
        public static DateTime PunchDateTime => Common.Dates.NowTimeOfDay;

        private Type _PunchInDateTime;
        private Type _PunchOutDateTime;

        public ClockTimeAttribute(Type punchInDateTime, Type punchOutDateTime) : this(punchInDateTime)
        {
            _PunchOutDateTime = punchOutDateTime;
        }

        public ClockTimeAttribute(Type punchInDateTime) : base()
        {
            _PunchInDateTime = punchInDateTime;
        }

        public override void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            var punchInDateTimeValue = sender.GetValue(e.Row, _PunchInDateTime.Name) as DateTime?;

            if (punchInDateTimeValue == null)
            {
                return;
            }

            var punchOutDateTimeValue = _PunchOutDateTime != null
                ? (sender.GetValue(e.Row, _PunchOutDateTime.Name) as DateTime? ?? Common.Current.BusinessTimeOfDay(sender.Graph))
                : Common.Current.BusinessTimeOfDay(sender.Graph);

            e.ReturnValue = GetTimeBetween(punchInDateTimeValue, punchOutDateTimeValue);

            base.FieldSelecting(sender, e);
        }

        public static int GetTimeBetween(DateTime? startDateTime, DateTime? endDateTime)
        {
            if (startDateTime == null || endDateTime == null)
            {
                return 0;
            }

            return Convert.ToInt32((endDateTime.Value - startDateTime.Value).TotalMinutes).NotLessZero();
        }

        public static bool StartBeforeEnd(DateTime startDateTime, DateTime endDateTime)
        {
            return TimeSpan.Compare(startDateTime.TimeOfDay, endDateTime.TimeOfDay) <= 0;
        }
    }
}