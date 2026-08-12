using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Weapons;
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

	public static bool FireFlak(BattleOrchestrator battle, EFlakMount mount) =>
		Enqueue(battle, [new FlakAction(battle.PlayerId, mount)]);

	public static bool FireRailgun(BattleOrchestrator battle) =>
		Enqueue(battle, [new RailgunAction(battle.PlayerId)]);

	public static bool FireTorpedo(BattleOrchestrator battle, ETorpedoMount mount) =>
		Enqueue(battle, [new TorpedoAction(battle.PlayerId, mount)]);

	public static bool Turn(BattleOrchestrator battle, EHeadingTurn turn) =>
		Enqueue(battle, [new HeadingTurnAction(battle.PlayerId, turn)]);

	public static IReadOnlyList<MovePathOption> MoveOptions(BattleOrchestrator battle) =>
		BattleTestFixture.Ui(battle).PreviewMoveOptions();

	public static PresentationFrame Frame(BattleOrchestrator battle) =>
		BattleTestFixture.Ui(battle).BuildFrame();

	public static void Focus(BattleOrchestrator battle, string? unitId)
	{
		if (unitId is null)
			BattleTestFixture.Ui(battle).State.ClearFocus();
		else
			BattleTestFixture.Ui(battle).State.FocusUnit(unitId);
	}

	private static bool Enqueue(BattleOrchestrator battle, IEnumerable<IAction> actions) =>
		battle.PlayerAgent.TryEnqueue(actions.ToList());
}
