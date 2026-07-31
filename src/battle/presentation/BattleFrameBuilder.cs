using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Domains.Flak;
using GrimSpace.Battle.Presentation.Domains.Move;
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

		var previewWorld = TurnUi.GetPreviewWorld(battle);
		var hazardCells = TurnUi.GetPreviewHazardCells(battle);
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
		var railgunCells = state.Mode == EPlayerMode.Railgun
			? RailgunUi.GetBurstCells(battle)
			: [];
		var railgunPreviewCells = state.Mode == EPlayerMode.Railgun
			? RailgunUi.GetPreviewCells(battle, state)
			: [];
		var (path, target) = MoveUi.GetPathHighlights(
			moveOptions,
			state.MoveHoveredIndex,
			state.CommittedMovePath);

		var actorId = battle.PlayerId;
		var actorState = previewWorld.StateOf(actorId);

		return new PresentationFrame
		{
			Mode = state.Mode,
			ActiveUnit = activeUnit,
			MoveOptions = moveOptions,
			PreviewWorld = previewWorld,
			ActorState = actorState,
			PreviewHazardCells = hazardCells,
			ValidFlakPortCells = validFlakPortCells,
			ValidFlakStarboardCells = validFlakStarboardCells,
			FlakPreviewCells = flakPreviewCells,
			ValidFlakPickCells = validFlakPickCells,
			RailgunCells = railgunCells,
			RailgunPreviewCells = railgunPreviewCells,
			MovePath = path,
			MoveTarget = target,
			HintText = TurnUi.BuildHint(
				battle,
				state,
				activeUnit,
				actorState,
				battle.Sim.Actions.Count),
			CanAct = !battle.IsBattleOver && activeUnit is not null && !battle.IsResolving,
			FlakAvailable = Capabilities.For(actorState.Type)
				.OfType<FlakDef>()
				.Any(def => battle.Sim.Peek(new FlakAction(battle.PlayerId, def.Mount)) is not null),
			RailgunAvailable = battle.Sim.Peek(new RailgunAction(battle.PlayerId)) is not null,
			ShowOutcomeOverlay = battle.IsBattleOver,
			PlayerWon = battle.IsBattleOver && battle.WinnerId == battle.PlayerId,
		};
	}
}
