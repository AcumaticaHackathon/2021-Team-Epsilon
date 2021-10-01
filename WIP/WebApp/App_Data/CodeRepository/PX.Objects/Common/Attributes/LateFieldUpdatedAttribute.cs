using System;
using PX.Data;

namespace PX.Objects.Common
{
	/// <summary>
	/// Represents an event handler for the pseudo FieldUpdated event that subscribes as late as possible.
	/// Though this handler looks like a field event handler,
	/// in fact it uses the RowUpdated event in which it checks if the value of the target field is changed,
	/// and if yes - calls the <see cref="LateFieldUpdated(PXCache, PXFieldUpdatedEventArgs)"/> method.
	/// </summary>
	public abstract class LateFieldUpdatedAttribute : LateRowUpdatedAttribute
	{
		protected override void LateRowUpdated(PXCache cache, PXRowUpdatedEventArgs args)
		{
			if (args.Row != null && args.OldRow != null &&
				cache.GetValue(args.Row, FieldOrdinal) is var newValue &&
				cache.GetValue(args.OldRow, FieldOrdinal) is var oldValue &&
				!Equals(newValue, oldValue))
			{
				LateFieldUpdated(cache, new PXFieldUpdatedEventArgs(args.Row, oldValue, args.ExternalCall));
			}
		}

		protected abstract void LateFieldUpdated(PXCache cache, PXFieldUpdatedEventArgs args);
	}
}