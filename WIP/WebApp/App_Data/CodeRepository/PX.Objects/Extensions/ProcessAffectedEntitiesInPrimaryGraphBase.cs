using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using PX.Data;

namespace PX.Objects.Extensions
{
	public abstract class ProcessAffectedEntitiesInPrimaryGraphBase<TSelf, TGraph, TEntity, TPrimaryGraphOfEntity> : PXGraphExtension<TGraph>
		where TSelf : ProcessAffectedEntitiesInPrimaryGraphBase<TSelf, TGraph, TEntity, TPrimaryGraphOfEntity>
		where TGraph : PXGraph
		where TEntity : class, IBqlTable, new()
		where TPrimaryGraphOfEntity : PXGraph, new()
	{
		[PXOverride]
		public virtual void Persist(Action basePersist)
		{
			IEnumerable<TEntity> affectedEntities = GetAffectedEntities();
			IEnumerable<TEntity> lateAffectedEntities = GetLatelyAffectedEntities();

			if (lateAffectedEntities != null || affectedEntities.Any())
			{
				if (PersistInSameTransaction)
				{
					using (var tran = new PXTransactionScope())
					{
						basePersist();
						var typesOfDirtyCaches = ProcessAffectedEntities(
							lateAffectedEntities == null
								? affectedEntities
								: lateAffectedEntities.Union(affectedEntities, Base.Caches<TEntity>().GetComparer()));

						tran.Complete();

						ClearCaches(Base, typesOfDirtyCaches);
					}
				}
				else
				{
					void OnAfterPersistHandler(PXGraph graph)
					{
						graph.OnAfterPersist -= OnAfterPersistHandler;

						var typesOfDirtyCaches = graph.FindImplementation<TSelf>().ProcessAffectedEntities(
							lateAffectedEntities == null
								? affectedEntities
								: lateAffectedEntities.Union(affectedEntities, Base.Caches<TEntity>().GetComparer()));

						ClearCaches(graph, typesOfDirtyCaches);
					}

					Base.OnAfterPersist += OnAfterPersistHandler;
					basePersist();
				}
			}
			else
				basePersist();
		}

		protected IEnumerable<TEntity> GetAffectedEntities()
		{
			return Base
				.Caches<TEntity>()
				.Updated
				.Cast<TEntity>()
				.Where(EntityIsAffected)
				.ToArray();
		}

		protected virtual IEnumerable<TEntity> GetLatelyAffectedEntities() => null;

		protected IEnumerable<Type> ProcessAffectedEntities(IEnumerable<TEntity> affectedEntities)
		{
			var typesOfDirtyCaches = new HashSet<Type>();
			List<TEntity> affectedEntitiesList = affectedEntities.ToList();
			if (affectedEntitiesList.Count != 0)
			{
				var foreignGraph = PXGraph.CreateInstance<TPrimaryGraphOfEntity>();
				foreach (var entity in affectedEntitiesList)
				{
					foreignGraph.Caches<TEntity>().Current = ActualizeEntity(foreignGraph, entity);

					ProcessAffectedEntity(foreignGraph, entity);

					if (foreignGraph.IsDirty)
					{
						foreach (var kvp in foreignGraph.Caches.Where(ch => ch.Value?.IsDirty == true))
							typesOfDirtyCaches.Add(kvp.Key);

						if (foreignGraph.Actions.Values.OfType<PXSave<TEntity>>().FirstOrDefault() is PXAction save)
							save.Press();
						else
							foreignGraph.Persist();
						foreignGraph.Clear();
					}
				}
				OnProcessed(foreignGraph);
			}
			return typesOfDirtyCaches;
		}

		protected virtual void OnProcessed(TPrimaryGraphOfEntity foreignGraph) { }

		private void ClearCaches(PXGraph graph, IEnumerable<Type> typesOfDirtyCaches)
		{
			if (ClearAffectedCaches)
			{
				foreach (Type cacheItemType in typesOfDirtyCaches)
					if (graph.Caches.Keys.Contains(cacheItemType))
						graph.Caches[cacheItemType].Clear();
			}
			ClearCaches(graph);
			graph.SelectTimeStamp();
		}

		protected bool WhenAnyFieldIsAffected(TEntity entity, params Expression<Func<TEntity, object>>[] fields)
		{
			var entityCache = Base.Caches<TEntity>();
			var origin = entityCache.GetOriginal(entity);
			return fields
				.Select(f => ExtractFieldName(f.Body))
				.Select(fn =>
				(
					OriginValue: entityCache.GetValue(origin, fn),
					CurrentValue: entityCache.GetValue(entity, fn)
				))
				.Any(p => !Equals(p.OriginValue, p.CurrentValue));

			string ExtractFieldName(Expression exp)
			{
				return exp is MemberExpression memberExp
					? memberExp.Member.Name
					: ExtractFieldName(((UnaryExpression)exp).Operand);
			}
		}

		protected virtual void ClearCaches(PXGraph graph)
		{
		}

		protected virtual bool ClearAffectedCaches => true;
		protected abstract bool PersistInSameTransaction { get; }
		protected abstract bool EntityIsAffected(TEntity entity);
		protected abstract void ProcessAffectedEntity(TPrimaryGraphOfEntity primaryGraph, TEntity entity);
		protected virtual TEntity ActualizeEntity(TPrimaryGraphOfEntity primaryGraph, TEntity entity) => entity;
	}
}