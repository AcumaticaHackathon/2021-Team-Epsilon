using PX.Data;
using PX.Objects.GL;
using PX.Objects.GL.FinPeriods.TableDefinition;
using System;
using System.Linq;

namespace PX.Objects.PR
{
	/// <summary>
	/// Specialized for PR version of the <see cref="OpenPeriodAttribute"/><br/>
	/// Selector. Provides a list  of the active Fin. Periods, having PRClosed flag = false <br/>
	/// <example>
	/// [PROpenPeriod(typeof(PRPayment.paymentDate), typeof(PRPayment.organizationID))]
	/// </example>
	/// </summary>
	public class PROpenPeriodAttribute : OpenPeriodAttribute
	{
		#region Ctor

		/// <summary>
		/// Extended Ctor. 
		/// </summary>
		/// <param name="SourceType">Must be IBqlField. Refers a date, based on which "current" period will be defined</param>
		public PROpenPeriodAttribute(Type SourceType, Type organizationSourceType)
			: base(typeof(Search<FinPeriod.finPeriodID, Where<FinPeriod.pRClosed, Equal<False>, And<FinPeriod.status, Equal<FinPeriod.status.open>>>>), SourceType, organizationSourceType: organizationSourceType)
		{
		}
		#endregion

		#region Implementation

		public static void DefaultFirstOpenPeriod(PXCache sender, string FieldName)
		{
			foreach (PeriodIDAttribute attr in sender.GetAttributesReadonly(FieldName).OfType<PeriodIDAttribute>())
			{
				attr.SearchType = typeof(Search2<FinPeriod.finPeriodID, CrossJoin<GLSetup>, Where<FinPeriod.endDate, Greater<Required<FinPeriod.endDate>>, And<FinPeriod.active, Equal<True>, And<Where<GLSetup.restrictAccessToClosedPeriods, NotEqual<True>, Or<FinPeriod.pRClosed, Equal<False>>>>>>, OrderBy<Asc<FinPeriod.finPeriodID>>>);
			}
		}

		public static void DefaultFirstOpenPeriod<Field>(PXCache sender)
			where Field : IBqlField
		{
			DefaultFirstOpenPeriod(sender, typeof(Field).Name);
		}

		#endregion

		#region Avoid breaking changes for 2020R2
		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)]
		public PROpenPeriodAttribute(Type SourceType)
			: base(typeof(Search<FinPeriod.finPeriodID, Where<FinPeriod.pRClosed, Equal<False>, And<FinPeriod.status, Equal<FinPeriod.status.open>>>>), SourceType)
		{ }

		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)]
		public PROpenPeriodAttribute() : this(null) { }
		#endregion Avoid breaking changes for 2020R2
	}
}
