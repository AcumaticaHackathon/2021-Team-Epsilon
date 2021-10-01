using PX.Data;
using System;

namespace PX.Objects.PR
{
	public class CompanyNoteTextAttribute : PXEventSubscriberAttribute, IPXFieldSelectingSubscriber
	{
		private Type _CompanySettingSelectorField;

		public CompanyNoteTextAttribute(Type companySettingSelectorField)
		{
			_CompanySettingSelectorField = companySettingSelectorField;
		}

		public void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			if (e.Row != null)
			{
				object parent = PXSelectorAttribute.Select(sender, e.Row, _CompanySettingSelectorField.Name);
				if (parent != null)
				{
					e.ReturnValue = PXNoteAttribute.GetNote(sender.Graph.Caches[parent.GetType()], parent);
				}
			}
		}
	}
}
