using PX.Data;
using PX.Objects.CR;
using PX.Objects.CR.Extensions.CRCreateActions;

namespace SP.Objects.SP.DAC
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public sealed class ContactFilterExt : PXCacheExtension<ContactFilter>
	{
		#region ContactClass

		public abstract class contactClass : PX.Data.BQL.BqlString.Field<contactClass> { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXSelector(typeof(Search<CRContactClass.classID, Where<CRContactClass.isInternal, Equal<False>>>))]
		public string ContactClass { get; set; }

		#endregion
	}
}
