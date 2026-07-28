using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record ClearTurnHazardsAction : IAction<BattleWorld, ActorRuntime>
{
	public string ActorId { get; } = EntityIds.System;
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		ClearTurnHazardsDef.Instance;
}

public sealed class ClearTurnHazardsDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public static ClearTurnHazardsDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId) => [];

	public ClearTurnHazardsAction Bind() => new();

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) => true;

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		[new ClearTurnHazardsEffect()];
}
