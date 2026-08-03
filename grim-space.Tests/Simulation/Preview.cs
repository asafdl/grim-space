using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Simulation;

internal static class Preview
{
	public static IReadOnlyList<MovePathSession> GetLegalMoves(BattleOrchestrator battle) =>
		SimulationPreviewShim.GetLegalMoves(battle);

	public static ActorPreview Simulate(BattleOrchestrator battle)
	{
		var actor = battle.Sim.StateOf<ActorState>(battle.PlayerId);
		return new ActorPreview(actor.Position, actor.MomentumLevel, actor.ActionPoints);
	}

	internal readonly record struct ActorPreview(Coord Position, int MomentumLevel, int ActionPoints);
}
