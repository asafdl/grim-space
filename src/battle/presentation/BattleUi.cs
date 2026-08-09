using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Domains.Flak;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Domains.Railgun;
using GrimSpace.Battle.Presentation.Domains.Torpedo;
using GrimSpace.Battle.Presentation.Domains.Turn;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
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

	private readonly List<string> _actionLogLines = [];
	private MoveUi? _moveUi;

	public IReadOnlyList<string> ActionLogLines => _actionLogLines;

	public MoveUi MoveUi => _moveUi ??= MoveUi.Build(Battle.Sim, Battle.PlayerId);

	public void ResetMoveUi() => _moveUi = null;

	public void AppendTurn(int turnNumber, IReadOnlyList<ITimelineEntry> history)
	{
		var units = UnitRegistry.For(Battle.Engine.World);
		_actionLogLines.Add($"--- Turn {turnNumber} ---");
		_actionLogLines.AddRange(ActionLog.Format(history, id => ActionLog.DisplayName(units, id)));
	}

	/// <summary>Active unit when it is the human player's planning turn; null otherwise.</summary>
	public Unit? GetPlanningActor() =>
		Battle.GetActiveUnit() is { State.Id: var id } unit && id == Battle.PlayerId
			? unit
			: null;

	public bool Undo() => TurnUi.TryUndo(Battle, State);

	public bool TryQueueMove(Coord endPosition)
	{
		var paths = MoveUi.GetMovePaths(Battle.Sim, Battle.PlayerId, Battle.Sim.Actions);
		var pathIndex = -1;
		for (var i = 0; i < paths.Count; i++)
		{
			if (paths[i].EndPosition != endPosition)
				continue;

			pathIndex = i;
			break;
		}

		if (pathIndex < 0)
		{
			PresentationDiagnostics.LogMoveQueueDetail("endpoint_not_in_options", endPosition);
			return false;
		}

		return TryQueueMove(pathIndex, paths);
	}

	public bool TryQueueMove(int pathIndex, IReadOnlyList<Movement.MovePathSession> paths)
	{
		if (pathIndex < 0 || pathIndex >= paths.Count)
		{
			PresentationDiagnostics.LogMoveQueueDetail(
				"index_out_of_range",
				pathIndex < paths.Count && pathIndex >= 0 ? paths[pathIndex].EndPosition : null);
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

		var path = paths[pathIndex];
		if (path.Steps.Count == 0)
		{
			PresentationDiagnostics.LogMoveQueueDetail("empty_steps", path.EndPosition);
			return false;
		}

		if (!Battle.Sim.TryEnqueue(actions: [..path.Steps]))
		{
			var actorState = Battle.Sim.StateOf<ActorState>(Battle.PlayerId);
			PresentationDiagnostics.LogMoveQueueDetail(
				$"sim_enqueue_failed steps={path.Steps.Count} pos={actorState.Position} "
				+ $"fore={actorState.Fore}",
				path.EndPosition);
			return false;
		}

		State.CommittedMovePath = path.Cells;
		State.ClearHovers();
		return true;
	}

	public PresentationFrame BuildFrame(bool acceptsCommands = true)
	{
		var state = State;
		var activeUnit = GetPlanningActor();
		var movePaths = acceptsCommands && activeUnit is not null && Battle.CanAct(activeUnit)
			? MoveUi.GetMovePaths(Battle.Sim, Battle.PlayerId, Battle.Sim.Actions)
			: [];
		state.ClampMoveHover(movePaths.Count);

		var previewWorld = TurnUi.GetPreviewWorld(Battle);
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
		var torpedoMountCells = state.Mode == EPlayerMode.Torpedo
			? TorpedoUi.GetMountCells(Battle)
			: [];
		var torpedoEnvelopeLayers = state.Mode == EPlayerMode.Torpedo
			? TorpedoUi.GetEnvelopeLayers(Battle, state)
			: [];
		var (path, target) = MoveUi.GetPathHighlights(
			movePaths,
			state.MoveHoveredIndex,
			state.CommittedMovePath);

		var actorId = Battle.PlayerId;
		var actorState = previewWorld.StateOf(actorId);
		var movePathApBaseline = Battle.Sim.RuntimeFor(actorId).ActivePath?.PathApSpent ?? 0;

		return new PresentationFrame
		{
			Mode = state.Mode,
			ActiveUnit = activeUnit,
			MovePaths = movePaths,
			MovePathApBaseline = movePathApBaseline,
			PreviewWorld = previewWorld,
			ActorState = actorState,
			ValidFlakPortCells = validFlakPortCells,
			ValidFlakStarboardCells = validFlakStarboardCells,
			FlakPreviewCells = flakPreviewCells,
			ValidFlakPickCells = validFlakPickCells,
			RailgunCells = railgunCells,
			RailgunPreviewCells = railgunPreviewCells,
			TorpedoMountCells = torpedoMountCells,
			TorpedoEnvelopeLayers = torpedoEnvelopeLayers,
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
			TorpedoAvailable = TorpedoConfig.EnabledMounts
				.Any(mount => Battle.Sim.Peek(new TorpedoAction(Battle.PlayerId, mount)) is not null),
			ShowOutcomeOverlay = Battle.IsBattleOver,
			Outcome = Battle.Outcome.Result,
			ActionLogLines = ActionLogLines,
		};
	}
}
