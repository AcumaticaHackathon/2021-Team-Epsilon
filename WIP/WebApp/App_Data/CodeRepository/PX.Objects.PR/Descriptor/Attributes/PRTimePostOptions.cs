using PX.Data;
using PX.Objects.EP;
using System;

namespace PX.Objects.PR
{
	public class PRTimePostOptions : EPPostOptions
	{
		public new class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				new string[] { DoNotPost, PostToOffBalance },
				new string[] { Messages.PostFromPayroll, Messages.PostFromTime }) { }
		}
	}

	[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)]
	public class PRxEPPostOptions : EPPostOptions
	{
		public new class ListAttribute : EPPostOptions.ListAttribute, IPXRowSelectedSubscriber
		{
			public void RowSelected(PXCache sender, PXRowSelectedEventArgs e) { }
		}
	}
}
