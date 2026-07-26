using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Domains.Flak;
using GrimSpace.Battle.Presentation.Domains.Missile;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Domains.Orientation;
using GrimSpace.Battle.Presentation.Domains.Railgun;
using GrimSpace.Battle.Presentation.Domains.Turn;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation;

public static class BattleFrameBuilder
{
	public static PresentationFrame Build(BattleOrchestrator battle, InteractionState state)
	{
		var activeUnit = battle.GetActiveActor();
		var moveOptions = MoveUi.GetMoveOptions(battle, activeUnit);
		state.ClampMoveHover(moveOptions.Count);

		var exitMissileMode = state.Mode == EPlayerMode.Missile
			&& battle.Sim.StateOf<ActorState>(battle.PlayerId).MissilesRemaining <= 0;
		if (exitMissileMode)
			state.SetMoveMode();

		var previewBoard = TurnUi.GetTurnGhost(battle);
		var hazardCells = TurnUi.GetPlannedHazardCells(battle);
		var validMissileCells = state.Mode == EPlayerMode.Missile && state.MissileMount is EMissileMount mount && activeUnit is not null
			? MissileUi.GetValidTargetCells(battle, mount, state.MissileRange)
			: [];
		var missilePreviewCells = state.Mode == EPlayerMode.Missile && activeUnit is not null
			? MissileUi.GetPreviewCells(battle, state)
			: [];
		var validFlakPortCells = state.Mode == EPlayerMode.Flak
			? FlakUi.GetBurstCells(battle, EFlakMount.Port)
			: [];
		var validFlakStarboardCells = state.Mode == EPlayerMode.Flak
			? FlakUi.GetBurstCells(battle, EFlakMount.Starboard)
			: [];
		var validFlakPickCells = new HashSet<Coord>(validFlakPortCells);
		validFlakPickCells.UnionWith(validFlakStarboardCells);
		var flakPreviewCells = state.Mode == EPlayerMode.Flak
			? FlakUi.GetPreviewCells(battle, state)
			: [];
		var railgunTargets = RailgunUi.GetTargetCells(battle, activeUnit);
		var (path, target) = MoveUi.GetPathHighlights(
			moveOptions,
			state.MoveHoveredIndex,
			state.CommittedMovePath);

		var actorId = battle.PlayerId;
		var actorState = previewBoard.StateOf(actorId);
		var missileInRange = MissileUi.IsHoverLegal(battle, state);

		return new PresentationFrame
		{
			Mode = state.Mode,
			MissileMount = state.MissileMount,
			MissileRange = state.MissileRange,
			ActiveUnit = activeUnit,
			MoveOptions = moveOptions,
			PreviewBoard = previewBoard,
			ActorState = actorState,
			PlannedHazardCells = hazardCells,
			ValidMissileCells = validMissileCells,
			MissilePreviewCells = missilePreviewCells,
			ValidFlakPortCells = validFlakPortCells,
			ValidFlakStarboardCells = validFlakStarboardCells,
			FlakPreviewCells = flakPreviewCells,
			ValidFlakPickCells = validFlakPickCells,
			RailgunTargetCells = railgunTargets,
			RailgunHoveredCell = RailgunUi.GetHoveredCell(battle, state),
			MovePath = path,
			MoveTarget = target,
			MissileAimActive = state.Mode == EPlayerMode.Missile && state.MissileMount is not null && activeUnit is not null,
			MissileAimShip = state.Mode == EPlayerMode.Missile ? actorState : null,
			HintText = TurnUi.BuildHint(
				battle,
				state,
				activeUnit,
				actorState,
				battle.Sim.Actions.Count,
				missileInRange),
			CanAct = !battle.IsBattleOver && activeUnit is not null && !battle.IsResolving,
			MissilesRemaining = actorState.MissilesRemaining,
			FlakAvailable = Capabilities.For(actorState.Type)
				.OfType<FlakDef>()
				.Any(def => battle.Sim.Peek(new FlakAction(battle.PlayerId, def.Mount)) is not null),
			ExitMissileMode = exitMissileMode,
			ShowOutcomeOverlay = battle.IsBattleOver,
			PlayerWon = battle.IsBattleOver && battle.WinnerId == battle.PlayerId,
		};
	}
}
