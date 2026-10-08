using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Maneuvering;

namespace GrimSpace.Battle.Actions;

public sealed record HeadingTurnAction(
	string ActorId,
	EHeadingTurn Turn) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		HeadingDef.Instance;
}

public sealed class HeadingDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public static HeadingDef Instance { get; } = new();

	private static readonly EHeadingTurn[] AllTurns = Enum.GetValues<EHeadingTurn>();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		foreach (var turn in AllTurns)
		{
			if (!actor.Maneuverability.TryGetHeadingMpCost(turn, out _))
				continue;

			yield return Bind(actorId, turn);
		}
	}

	public HeadingTurnAction Bind(string actorId, EHeadingTurn turn) => new(actorId, turn);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(Cast(action), world, runtime);

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(HeadingTurnAction action, BattleWorld world, ActorRuntime runtime) =>
		world.StateOf(action.ActorId).Maneuverability.TryGetHeadingMpCost(action.Turn, out _);

	public bool IsLegal(HeadingTurnAction action, BattleWorld world, ActorRuntime runtime)
	{
		var actor = world.StateOf(action.ActorId);
		return actor.Maneuverability.TryGetHeadingMpCost(action.Turn, out var cost)
			&& actor.ManeuverPoints >= cost;
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		HeadingTurnAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var actor = world.StateOf(action.ActorId);
		if (!actor.Maneuverability.TryGetHeadingMpCost(action.Turn, out var cost))
		{
			throw new InvalidOperationException(
				$"Actor '{actor.Id}' does not support heading turn '{action.Turn}'.");
		}

		return
		[
			new HeadingTurnEffect(action.Turn),
			new MpChangeEffect(-cost),
		];
	}

	private static HeadingTurnAction Cast(IAction action) =>
		action as HeadingTurnAction ?? throw new ArgumentException($"Expected {nameof(HeadingTurnAction)}.", nameof(action));
}
