using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class ResolveEngagementEffect : IEffect<StarMap, Runtime.ActorRuntime>
{
	private readonly BattleOutcome _outcome;

	public ResolveEngagementEffect(BattleOutcome outcome) => _outcome = outcome;

	public IReadOnlyList<IRecord> Apply(StarMap world, Runtime.ActorRuntime runtime, string actorId)
	{
		foreach (var handoff in _outcome.StateHandoffs)
		{
			if (handoff.HP > 0)
				continue;
			if (!world.FleetRegistry.TryFleetContainingMember(handoff.Id, out var fleet))
				continue;

			var surviving = fleet.Members.Where(member => member.Id != handoff.Id).ToArray();
			if (surviving.Length == 0)
				RemoveFleet(world, fleet.State.Id);
			else
				world.FleetRegistry.Replace(new Fleet(fleet.State, surviving));
		}

		foreach (var handoff in _outcome.StateHandoffs)
		{
			if (!world.FleetRegistry.TryFleetContainingMember(handoff.Id, out var fleet))
				continue;

			fleet.State.CurrentEngagement = null;
		}

		return [];
	}

	private static void RemoveFleet(StarMap world, string fleetId)
		=> world.FleetRegistry.Remove(fleetId);

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
