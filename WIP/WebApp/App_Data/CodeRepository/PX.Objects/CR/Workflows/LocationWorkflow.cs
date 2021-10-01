using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Common;

namespace PX.Objects.CR.Workflows
{
	public static class LocationWorkflow
	{
		[PXInternalUseOnly]
		public static void SetStatusTo(PXGraph graph, object entity, string targetStatus)
		{
			object realEntity = entity;
			var graphType = new EntityHelper(graph).GetPrimaryGraphType(ref realEntity, false);

			if (graphType == null)
				return;

			var locationMaint = PXGraph.CreateInstance(graphType) as LocationMaint;

			if (locationMaint == null)
				return;

			locationMaint.Location.Current = realEntity as Location;
			locationMaint.cancel.Press();

			string action = null;

			switch (targetStatus)
			{
				case LocationStatus.Active:
					action = nameof(LocationMaint.Activate);
					break;

				case LocationStatus.Inactive:
					action = nameof(LocationMaint.Deactivate);
					break;
			}

			if (action == null)
				return;

			locationMaint.Actions[action].Press();
		}

		public static void Configure(PXScreenConfiguration configuration)
		{
			var context = configuration.GetScreenConfigurationContext<LocationMaint, Location>();

			var isDefaultCondition = context
				.Conditions
				.FromBql<Location.isDefault.IsEqual<True>>()
				.WithSharedName("IsDefault");

			context.AddScreenConfigurationFor(screen => screen
				.StateIdentifierIs<Location.status>()
				.AddDefaultFlow(flow =>
				{
					return flow
						.WithFlowStates(states =>
						{
							states.Add(LocationStatus.Active, s => s
								.IsInitial()
								.WithActions(actions =>
								{
									actions.Add(g => g.Deactivate);
								}));

							states.Add(LocationStatus.Inactive, s => s
								.WithActions(actions =>
								{
									actions.Add(g => g.Activate, a => a.IsDuplicatedInToolbar());
								}));
						})
						.WithTransitions(transitions =>
						{
							transitions.Add(t => t
								.From(LocationStatus.Active)
								.To(LocationStatus.Inactive)
								.IsTriggeredOn(g => g.Deactivate));

							transitions.Add(t => t
								.From(LocationStatus.Inactive)
								.To(LocationStatus.Active)
								.IsTriggeredOn(g => g.Activate));
						});
				})
				.WithActions(actions =>
				{
					actions.Add(g => g.Activate, a => a
						.WithFieldAssignments(fields =>
						{
							fields.Add<Location.isActive>(f => f.SetFromValue(true));
						})
						.InFolder(FolderType.ActionsFolder));

					actions.Add(g => g.Deactivate, a => a
						.WithFieldAssignments(fields =>
						{
							fields.Add<Location.isActive>(f => f.SetFromValue(false));
						})
						.InFolder(FolderType.ActionsFolder)
						.IsDisabledWhen(isDefaultCondition));

				})
				.ForbidFurtherChanges());
		}
	}
}
