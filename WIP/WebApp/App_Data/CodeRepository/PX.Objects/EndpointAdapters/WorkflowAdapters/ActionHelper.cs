using System;
using System.Linq;
using PX.Data;
using PX.Api.ContractBased.Models;

namespace PX.Objects.EndpointAdapters
{
	static class ActionHelper
	{
		public static void SubscribeToPersist<TTable>(this PXGraph graph, Action<PXCache<TTable>> action)
			where TTable : class, IBqlTable, new()
		{
			graph.OnBeforePersist += handler;

			void handler(PXGraph g)
			{
				g.OnBeforePersist -= handler;
				action(g.Caches<TTable>());
			}
		}

		public static void SubscribeToPersist<TTable>(this PXGraph graph, PXAction<TTable> action)
			where TTable : class, IBqlTable, new()
		{
			// need mark as dirty to call persist in any case, even if fields weren't updated,
			// otherwise action wouldn't be triggered
			graph.Caches<TTable>().IsDirty = true;

			graph.OnBeforePersist += handler;

			void handler(PXGraph g)
			{
				g.OnBeforePersist -= handler;
				action.Press();
			}
		}

		public static void SubscribeToPersistDependingOnBoolField<TTable>(this PXGraph graph, EntityValueField holdField, PXAction<TTable> actionIfTrue, PXAction<TTable> actionIfFalse, Action<PXCache<TTable>> afterAction = null)
			where TTable : class, IBqlTable, new()
		{
			if (string.IsNullOrEmpty(holdField?.Value)) return;
			if (!Boolean.TryParse(holdField.Value, out bool value))
				throw new InvalidOperationException($"'{holdField.Name}' value '{holdField.Value}' was not recognized as valid boolean");

			PXAction<TTable> action = value ? actionIfTrue : actionIfFalse;
			if (action == null) return;

			PXButtonState state = action.GetState(graph.Caches<TTable>().Current) as PXButtonState;
			if (state.Enabled)
			{
				graph.SubscribeToPersist(action);
				if (afterAction != null) graph.SubscribeToPersist(afterAction);
			}
		}

		public static void SetDropDownValue<Field, T>(this PXGraph graph, string value, object data)
			where Field : IBqlField
			where T : class, IBqlTable, new()
		{
			PXStringState state = (PXStringState)graph.Caches<T>().GetStateExt<Field>(data);
			int indexOfLabel = state.AllowedLabels.ToList().IndexOf(value);
			graph.Caches<T>().SetValueExt<Field>(data, indexOfLabel > -1 ? state.AllowedValues.ElementAt(indexOfLabel) : value);
		}
	}
}