using System;
using PX.Data;

namespace PX.Objects.Common.Attributes
{
	/// <summary>
	/// Allows a virtual object to borrow the note ID of another entity and use it to attach files.
	/// </summary>
	public class BorrowedNoteAttribute : PXNoteAttribute
	{
		public Type TargetEntityType { get; }
		public Type TargetGraphType { get; }

		public BorrowedNoteAttribute(Type targetEntityType, Type targetGraphType)
		{
			TargetEntityType = targetEntityType;
			TargetGraphType = targetGraphType;
		}

		protected override string GetEntityType(PXCache cache, Guid? noteId) => TargetEntityType?.FullName ?? base.GetEntityType(cache, noteId);
		protected override string GetGraphType(PXGraph graph) => TargetGraphType?.FullName ?? base.GetGraphType(graph);
	}
}