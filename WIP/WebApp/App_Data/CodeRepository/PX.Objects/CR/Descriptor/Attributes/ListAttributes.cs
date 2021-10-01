using PX.Data;

namespace PX.Objects.CR
{
	#region ContactStatus

	public class ContactStatus
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				(Active, Messages.Active),
				(Inactive, Messages.Inactive)
			) { }
		}

		public const string Active = "A";
		public const string Inactive = "I";

		public class active : PX.Data.BQL.BqlString.Constant<active>
		{
			public active() : base(Active) { }
		}
		public class inactive : PX.Data.BQL.BqlString.Constant<inactive>
		{
			public inactive() : base(Inactive) { }
		}
	}

	#endregion

	#region LocationStatus

	public class LocationStatus
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				(Active, Messages.Active),
				(Inactive, Messages.Inactive)
			)
			{ }
		}

		public const string Active = "A";
		public const string Inactive = "I";

		public class active : PX.Data.BQL.BqlString.Constant<active>
		{
			public active() : base(Active) { }
		}
		public class inactive : PX.Data.BQL.BqlString.Constant<inactive>
		{
			public inactive() : base(Inactive) { }
		}
	}

	#endregion
}
