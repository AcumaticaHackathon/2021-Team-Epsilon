using System;
using System.Collections.Generic;
using PX.Data;
using PX.Data.SQLTree;

namespace PX.Objects.FA
{
	public class CanBeDepreciated<TFieldDepreciated, TFieldUnderConstruction> : IBqlWhere
			where TFieldDepreciated : IBqlField
			where TFieldUnderConstruction : IBqlField
	{
		private IBqlCreator whereEqualNotNull = new Where<TFieldDepreciated, Equal<True>, And<TFieldUnderConstruction, NotEqual<True>>>();

		private Type cacheType;

		public CanBeDepreciated()
		{
			cacheType = typeof(TFieldDepreciated).DeclaringType;
		}

		private IBqlCreator GetWhereClause(PXCache cache)
		{
			return whereEqualNotNull;
		}

		public bool AppendExpression(ref SQLExpression exp, PXGraph graph, BqlCommandInfo info, BqlCommand.Selection selection)
		{
			IBqlCreator clause = GetWhereClause(graph?.Caches[cacheType]);
			return clause.AppendExpression(ref exp, graph, info, selection);
		}

		public void Verify(PXCache cache, object item, List<object> pars, ref bool? result, ref object value)
		{
			if (typeof(IBqlField).IsAssignableFrom(typeof(TFieldDepreciated)) && typeof(TFieldDepreciated).IsNested && typeof(IBqlField).IsAssignableFrom(typeof(TFieldUnderConstruction)) && typeof(TFieldUnderConstruction).IsNested)
			{
				Type ParentType = BqlCommand.GetItemType(typeof(TFieldDepreciated));
				PXCache parentcache = cache.Graph.Caches[ParentType];
				IBqlCreator clause = GetWhereClause(parentcache);
				clause.Verify(parentcache, parentcache.Current, pars, ref result, ref value);
			}
			else
			{
				value = null;
			}
		}
	}
}
