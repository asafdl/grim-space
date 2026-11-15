using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tutorials;

public sealed record BattleTutorialObjective(
	string TargetId,
	Coord Destination,
	GridBasis RequiredBasis);

public abstract record BattleTutorialObjectiveResult
{
	public sealed record Resolved(BattleTutorialObjective Objective) : BattleTutorialObjectiveResult;

	public sealed record Unreachable(string Reason) : BattleTutorialObjectiveResult;
}

public static class BattleTutorialObjectives
{
	public static BattleTutorialObjectiveResult ResolveShieldApproach(BattleOrchestrator battle)
	{
		var player = battle.Engine.World.StateOf(battle.PlayerId);
		var target = UnitRegistry.For(battle.Engine.World).All
			.Where(unit => unit.Team == ETeam.Enemy)
			.OrderBy(unit => unit.State.Position.ManhattanDistanceTo(player.Position))
			.FirstOrDefault();
		if (target is null)
			return new BattleTutorialObjectiveResult.Unreachable("No tutorial target.");

		var objective = new BattleTutorialObjective(
			target.State.Id,
			target.State.Position + target.State.Fore * 3,
			GridBasis.From(player.Fore, player.Dorsal, player.Starboard));
		if (!target.State.IsAlive)
			return new BattleTutorialObjectiveResult.Resolved(objective);

		var reachable = MovePathEndpoints.DiscoverExtensions(battle.PlayerAgent.Sim.ForkFromAnchor(), battle.PlayerId)
			.Any(session => session.EndPosition == objective.Destination
				&& session.EndBasis == objective.RequiredBasis
				&& session.Steps.Count > 0);
		return reachable
			? new BattleTutorialObjectiveResult.Resolved(objective)
			: new BattleTutorialObjectiveResult.Unreachable("The shield-shot pose is unreachable.");
	}

	public static bool HasQueuedShieldShot(BattleSimulation sim, string actorId, BattleTutorialObjective objective) =>
		MatchesQueuedMovement(sim, actorId, sim.Actions, objective)
		&& QueuedImpacts(sim, actorId, objective.TargetId, cannonOnly: true)
			.Any(impact => impact.ShieldDamage > 0 && impact.HullDamage == 0)
		&& !QueuedImpacts(sim, actorId, objective.TargetId).Any(impact => impact.HullDamage > 0);

	public static bool HasQueuedKill(BattleSimulation sim, string actorId, string targetId) =>
		QueuedImpacts(sim, actorId, targetId).Any(impact => impact.IsLethal);

	private static IEnumerable<ImpactFacts> QueuedImpacts(
		BattleSimulation sim, string actorId, string targetId, bool cannonOnly = false) =>
		Enumerable.Range(0, sim.Actions.Count)
			.Where(i => sim.Actions[i].ActorId == actorId
				&& (!cannonOnly || sim.Actions[i] is LightningCannonAction))
			.SelectMany(sim.RecordsFor)
			.OfType<Record<ImpactFacts>>()
			.Select(record => record.Value)
			.Where(impact => impact.TargetId == targetId);

	public static bool MatchesQueuedMovement(
		BattleSimulation sim,
		string actorId,
		IReadOnlyList<IAction> queuedActions,
		BattleTutorialObjective objective)
	{
		var movement = queuedActions.TakeWhile(action => action is MoveStepAction or HeadingTurnAction or RollAction)
			.ToArray();
		if (movement.Length == 0)
			return false;

		var projection = sim.ForkFromAnchor();
		foreach (var action in movement)
		{
			if (!projection.TryEnqueue(action))
				return false;
		}

		var actor = projection.StateOf<State>(actorId);
		var basis = GridBasis.From(actor.Fore, actor.Dorsal, actor.Starboard);
		return actor.Position == objective.Destination && basis == objective.RequiredBasis;
	}
}
