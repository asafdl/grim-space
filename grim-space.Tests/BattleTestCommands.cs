using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Abilities;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests;

internal static class BattleTestCommands
{
	public static IReadOnlyList<MovePathSession> DiscoverPaths(
		BattleOrchestrator battle,
		string? actorId = null) =>
		MovePathEndpoints.DiscoverExtensions(battle.PlayerAgent.Sim, actorId ?? battle.PlayerId);

	public static bool Move(BattleOrchestrator battle, Coord endPosition)
	{
		var option = MoveOptions(battle)
			.FirstOrDefault(candidate => candidate.EndPosition == endPosition);
		if (option is null || option.Directions.Count == 0)
			return false;

		return Enqueue(
			battle,
			option.Directions.Select(direction => (IAction)new MoveStepAction(battle.PlayerId, direction)));
	}

	public static bool Undo(BattleOrchestrator battle) =>
		battle.PlayerAgent.Undo();

	public static bool FireFlak(BattleOrchestrator battle, ESpatialOrientation mountedOn) =>
		Enqueue(battle, [new FlakAction(battle.PlayerId, mountedOn)]);

	public static bool FireRailgun(BattleOrchestrator battle) =>
		Enqueue(battle, [new RailgunAction(battle.PlayerId)]);

	public static bool FireTorpedo(BattleOrchestrator battle, ESpatialOrientation mountedOn) =>
		Enqueue(battle, [new TorpedoAction(battle.PlayerId, mountedOn)]);

	public static bool DeployPatrol(BattleOrchestrator battle)
	{
		var carrierId = BattleTestFixture.FirstEnemyId(battle);
		var action = new SpawnPatrolAction(carrierId);
		if (!SpawnPatrolDef.Instance.IsLegal(action, battle.Engine.World, battle.Engine.ActorRuntimes.For(carrierId)))
			return false;

		battle.Engine.Commit([action]);
		return true;
	}

	public static bool Turn(BattleOrchestrator battle, EHeadingTurn turn) =>
		Enqueue(battle, [new HeadingTurnAction(battle.PlayerId, turn)]);

	public static IReadOnlyList<MovePathOption> MoveOptions(BattleOrchestrator battle) =>
		BattleTestFixture.FrameBuilder(battle).PreviewMoveOptions(battle, battle.PlayerAgent);

	public static PresentationFrame Frame(BattleOrchestrator battle) =>
		BattleTestFixture.FrameBuilder(battle).BuildFrame(
			battle,
			battle.PlayerAgent,
			acceptsCommands: battle.Phase == EBattlePhase.PlayerTurn);

	public static void Focus(BattleOrchestrator battle, string? unitId)
	{
		if (unitId is null)
			BattleTestFixture.FrameBuilder(battle).Interaction.ClearFocus();
		else
			BattleTestFixture.FrameBuilder(battle).Interaction.FocusUnit(unitId);
	}

	private static bool Enqueue(BattleOrchestrator battle, IEnumerable<IAction> actions) =>
		battle.PlayerAgent.TryEnqueue(actions.ToList());
}
