using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation;
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

/// <summary>
/// Player interaction and presentation assembly for one battle.
/// Lifecycle and frame emission live in <see cref="BattleDirector"/>.
/// </summary>
public sealed class BattleUi
{
	public BattleUi(BattleOrchestrator battle) => Battle = battle;

	public BattleOrchestrator Battle { get; }

	public InteractionState State { get; } = new();

	private MoveUi? _moveUi;

	public MoveUi MoveUi => _moveUi ??= MoveUi.Build(Battle.Sim, Battle.PlayerId);

	public void ResetMoveUi() => _moveUi = null;

	public void InstallMoveUi(MoveUi moveUi) => _moveUi = moveUi;

	/// <summary>Active unit when it is the human player's planning turn; null otherwise.</summary>
	public Unit? GetPlanningActor() =>
		Battle.GetActiveUnit() is { State.Id: var id } unit && id == Battle.PlayerId
			? unit
			: null;

	public bool Undo() => TurnUi.TryUndo(Battle, State);

	public bool TryQueueMove(Coord endPosition)
	{
		var options = MoveUi.GetMoveOptions(Battle.Sim.Actions);
		var optionIndex = -1;
		for (var i = 0; i < options.Count; i++)
		{
			if (options[i].EndPosition != endPosition)
				continue;

			optionIndex = i;
			break;
		}

		if (optionIndex < 0)
		{
			PresentationDiagnostics.LogMoveQueueDetail("endpoint_not_in_options", endPosition);
			return false;
		}

		return TryQueueMove(optionIndex, options);
	}

	public bool TryQueueMove(int optionIndex, IReadOnlyList<Movement.Option> options)
	{
		if (optionIndex < 0 || optionIndex >= options.Count)
		{
			PresentationDiagnostics.LogMoveQueueDetail(
				"index_out_of_range",
				optionIndex < options.Count && optionIndex >= 0 ? options[optionIndex].EndPosition : null);
			return false;
		}

		var actor = GetPlanningActor();
		if (actor is null)
		{
			PresentationDiagnostics.LogMoveQueueDetail("no_planning_actor");
			return false;
		}

		if (!Battle.CanAct(actor))
		{
			PresentationDiagnostics.LogMoveQueueDetail("cannot_act");
			return false;
		}

		var committed = Battle.Sim.Actions;
		if (!MoveUi.TryLocate(committed))
		{
			PresentationDiagnostics.LogMoveQueueDetail("prefix_not_in_cached_tree");
			return false;
		}

		var option = options[optionIndex];
		if (option.Steps.Count == 0)
		{
			PresentationDiagnostics.LogMoveQueueDetail("empty_steps", option.EndPosition);
			return false;
		}

		if (!Battle.Sim.TryEnqueue(actions: [..option.Steps]))
		{
			var actorState = Battle.Sim.StateOf<ActorState>(Battle.PlayerId);
			PresentationDiagnostics.LogMoveQueueDetail(
				$"sim_enqueue_failed steps={option.Steps.Count} pos={actorState.Position} "
				+ $"fore={actorState.Fore}",
				option.EndPosition);
			return false;
		}

		State.CommittedMovePath = option.Path;
		State.ClearHovers();
		return true;
	}

	public PresentationFrame BuildFrame(bool acceptsCommands = true)
	{
		var state = State;
		var activeUnit = GetPlanningActor();
		var moveOptions = activeUnit is not null && Battle.CanAct(activeUnit)
			? MoveUi.GetMoveOptions(Battle.Sim.Actions)
			: [];
		state.ClampMoveHover(moveOptions.Count);

		var previewWorld = TurnUi.GetPreviewWorld(Battle);
		var hazardCells = TurnUi.GetPreviewHazardCells(Battle);
		var validFlakPortCells = state.Mode == EPlayerMode.Flak
			? FlakUi.GetBurstCells(Battle, EFlakMount.Port)
			: [];
		var validFlakStarboardCells = state.Mode == EPlayerMode.Flak
			? FlakUi.GetBurstCells(Battle, EFlakMount.Starboard)
			: [];
		var validFlakPickCells = new HashSet<Coord>(validFlakPortCells);
		validFlakPickCells.UnionWith(validFlakStarboardCells);
		var flakPreviewCells = state.Mode == EPlayerMode.Flak
			? FlakUi.GetPreviewCells(Battle, state)
			: [];
		var railgunCells = state.Mode == EPlayerMode.Railgun
			? RailgunUi.GetBurstCells(Battle)
			: [];
		var railgunPreviewCells = state.Mode == EPlayerMode.Railgun
			? RailgunUi.GetPreviewCells(Battle, state)
			: [];
		var (path, target) = MoveUi.GetPathHighlights(
			moveOptions,
			state.MoveHoveredIndex,
			state.CommittedMovePath);

		var actorId = Battle.PlayerId;
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
				Battle,
				state,
				activeUnit,
				actorState,
				Battle.Sim.Actions.Count),
			CanAct = acceptsCommands && activeUnit is not null && Battle.CanAct(activeUnit),
			FlakAvailable = Capabilities.For(actorState.Type)
				.OfType<FlakDef>()
				.Any(def => Battle.Sim.Peek(new FlakAction(Battle.PlayerId, def.Mount)) is not null),
			RailgunAvailable = Battle.Sim.Peek(new RailgunAction(Battle.PlayerId)) is not null,
			ShowOutcomeOverlay = Battle.IsBattleOver,
			PlayerWon = Battle.IsBattleOver && Battle.WinnerId == Battle.PlayerId,
		};
	}
}
