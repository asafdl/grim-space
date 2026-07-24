using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Planning;

/// <summary>
/// Move-path search state — matches the old MovePathFinder node key.
/// </summary>
internal readonly record struct MoveSearchState(
	Coord Position,
	int UsedDirectionsMask,
	int MomentumLevel,
	int MinPathApCost,
	int ActionPoints);

/// <summary>
/// Full-turn search state without consumable budgets (AP, missiles, etc.).
/// </summary>
internal readonly record struct BattleSearchState(
	Coord Position,
	Coord Fore,
	Coord Dorsal,
	int MomentumLevel,
	int UsedDirectionsMask,
	int PathForwardSteps,
	int MinPathApCost,
	int PathApSpent,
	int RawYawQuarters,
	int MoveStartMomentumLevel,
	int MovementBuildupLevel,
	int MovementBuildupForwardSteps,
	int MomentumPaid,
	int MomentumGainedFromMovement,
	bool SpinBraked,
	bool SpinDiscount,
	int HazardSignature);

internal static class BattleSearchVisit
{
	public static SearchVisitState ForMove(BattleBoard world, ActorSession runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		return new SearchVisitState(
			new MoveSearchState(
				actor.Position,
				runtime.UsedDirectionsMask,
				actor.MomentumLevel,
				runtime.MinPathApCost,
				actor.ActionPoints),
			[]);
	}

	public static SearchVisitState ForTurn(BattleBoard world, ActorSession runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		return new SearchVisitState(
			new BattleSearchState(
				actor.Position,
				actor.Fore,
				actor.Dorsal,
				actor.MomentumLevel,
				runtime.UsedDirectionsMask,
				runtime.PathForwardSteps,
				runtime.MinPathApCost,
				runtime.PathApSpent,
				runtime.RawYawQuarters,
				runtime.MoveStartMomentumLevel,
				runtime.MovementBuildupLevel,
				runtime.MovementBuildupForwardSteps,
				runtime.MomentumPaid,
				runtime.MomentumGainedFromMovement,
				runtime.SpinBraked,
				runtime.SpinDiscount,
				HashHazards(world)),
			[
				actor.ActionPoints,
				actor.MissilesRemaining,
				actor.FlakRemaining,
				actor.RailgunRemaining,
			]);
	}

	private static int HashHazards(BattleBoard world)
	{
		var hash = 0;
		foreach (var hazard in world.TurnHazards)
		{
			hash = HashCode.Combine(hash, hazard.Id);
			foreach (var cell in hazard.Cells)
				hash = HashCode.Combine(hash, cell);
		}

		return hash;
	}
}
