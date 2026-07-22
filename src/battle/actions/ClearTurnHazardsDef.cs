using GrimSpace.Battle.Board;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed class ClearTurnHazardsDef
	: IActionDef<IAction, BattleBoard, ActorSession, IEffect<BattleBoard, ActorSession>>
{
	public static ClearTurnHazardsDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleBoard world, ActorSession runtime, string ownerId) => [];

	public bool IsPossible(IAction action, BattleBoard world, ActorSession runtime) => true;

	public bool IsLegal(IAction action, BattleBoard world, ActorSession runtime) => true;

	public IReadOnlyList<IEffect<BattleBoard, ActorSession>> Resolve(
		IAction action,
		BattleBoard world,
		ActorSession runtime) =>
		[new ClearTurnHazardsEffect()];
}
