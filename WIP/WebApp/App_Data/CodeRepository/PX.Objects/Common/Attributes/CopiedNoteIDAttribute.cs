using PX.Data;
using System;

namespace PX.Objects.Common.Attributes
{
	/// <exclude/>
	public class CopiedNoteIDAttribute : PXNoteAttribute
	{
		protected Type _entityType;

		public CopiedNoteIDAttribute(Type entityType, params Type[] searches)
			: base(searches)
		{
			_entityType = entityType;
			_ForceRetain = true;
		}

		protected override string GetEntityType(PXCache cache, Guid? noteId)
			=> _entityType.FullName;
	}
}
