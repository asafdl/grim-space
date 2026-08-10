using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Domains.Flak;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Domains.Railgun;
using GrimSpace.Battle.Presentation.Domains.Torpedo;
using GrimSpace.Battle.Presentation.Domains.Turn;
using GrimSpace.Battle.Presentation.Domains.Weapons;
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

	public string FocusId => State.FocusId ?? Battle.PlayerId;

	public bool IsInspecting => FocusId != Battle.PlayerId;

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
		var canAct = activeUnit is not null && Battle.CanAct(activeUnit);
		var weaponQueued = Battle.Sim.Actions.Any(static action =>
			action is FlakAction or RailgunAction or TorpedoAction);

		var previewWorld = TurnUi.GetPreviewWorld(Battle);
		var focusId = state.FocusId ?? Battle.PlayerId;
		if (!UnitRegistry.For(previewWorld).TryGet(focusId, out var focusUnit) || !focusUnit.State.IsAlive)
		{
			state.ClearFocus();
			focusId = Battle.PlayerId;
		}

		var isInspecting = focusId != Battle.PlayerId;
		var canControl = acceptsCommands && canAct && !isInspecting;
		var effectiveMode = isInspecting ? EPlayerMode.Move : state.Mode;

		IReadOnlyList<Movement.MovePathSession> movePaths;
		IReadOnlyList<Coord> movePath;
		Coord? moveTarget;
		if (canControl || isInspecting)
		{
			movePaths = MoveUi.GetMovePaths(Battle.Sim, focusId, Battle.Sim.Actions);
			if (isInspecting)
			{
				movePath = [];
				moveTarget = null;
			}
			else
			{
				state.ClampMoveHover(movePaths.Count);
				(movePath, moveTarget) = MoveUi.GetPathHighlights(
					movePaths,
					state.MoveHoveredIndex,
					state.CommittedMovePath);
			}
		}
		else
		{
			movePaths = [];
			movePath = [];
			moveTarget = null;
		}

		var validFlakPortCells = effectiveMode == EPlayerMode.Flak
			? FlakUi.GetBurstCells(Battle, EFlakMount.Port)
			: [];
		var validFlakStarboardCells = effectiveMode == EPlayerMode.Flak
			? FlakUi.GetBurstCells(Battle, EFlakMount.Starboard)
			: [];
		var validFlakPickCells = new HashSet<Coord>(validFlakPortCells);
		validFlakPickCells.UnionWith(validFlakStarboardCells);
		var flakPreviewCells = effectiveMode == EPlayerMode.Flak
			? FlakUi.GetPreviewCells(Battle, state)
			: [];
		var railgunCells = effectiveMode == EPlayerMode.Railgun
			? RailgunUi.GetBurstCells(Battle)
			: [];
		var railgunPreviewCells = effectiveMode == EPlayerMode.Railgun
			? RailgunUi.GetPreviewCells(Battle, state)
			: [];
		var torpedoMountCells = effectiveMode == EPlayerMode.Torpedo
			? TorpedoUi.GetMountCells(Battle)
			: [];
		var torpedoEnvelopeLayers = effectiveMode == EPlayerMode.Torpedo
			? TorpedoUi.GetEnvelopeLayers(Battle, state)
			: [];

		EFlakMount? committedFlakMount = null;
		var committedRailgun = false;
		Coord? committedTorpedoMountCell = null;
		IReadOnlyList<IReadOnlySet<Coord>> committedTorpedoEnvelopeLayers = [];
		var showWeaponPreviews = acceptsCommands && !Battle.IsBattleOver && !isInspecting;
		if (showWeaponPreviews)
		{
			for (var i = Battle.Sim.Actions.Count - 1; i >= 0; i--)
			{
				switch (Battle.Sim.Actions[i])
				{
					case FlakAction flak when flak.ActorId == Battle.PlayerId:
						committedFlakMount = flak.Mount;
						break;
					case RailgunAction railgun when railgun.ActorId == Battle.PlayerId:
						committedRailgun = true;
						break;
					case TorpedoAction torpedo when torpedo.ActorId == Battle.PlayerId:
					{
						var ship = Battle.Sim.StateOf<ActorState>(Battle.PlayerId);
						var (position, _, _) = TorpedoMount.LaunchPose(ship, torpedo.Mount);
						committedTorpedoMountCell = position;
						committedTorpedoEnvelopeLayers = TorpedoUi.GetEnvelopeLayersForQueued(Battle, torpedo);
						break;
					}
					default:
						continue;
				}

				break;
			}
		}

		var threatenedUnitIds = !showWeaponPreviews
			? new HashSet<string>()
			: effectiveMode switch
			{
				EPlayerMode.Flak => FlakUi.GetThreatenedUnitIds(Battle),
				EPlayerMode.Railgun => RailgunUi.GetThreatenedUnitIds(Battle),
				EPlayerMode.Torpedo => TorpedoUi.GetThreatenedUnitIds(Battle, state),
				_ when committedFlakMount is { } mount =>
					WeaponThreatPreview.UnitIdsInCells(Battle, FlakUi.GetBurstCellsGeometry(Battle, mount)),
				_ when committedRailgun =>
					WeaponThreatPreview.UnitIdsInCells(Battle, RailgunUi.GetBurstCellsGeometry(Battle)),
				_ when committedTorpedoMountCell is not null =>
					TorpedoUi.GetThreatenedUnitIdsForLayers(Battle, committedTorpedoEnvelopeLayers),
				_ => new HashSet<string>(),
			};

		var actorId = Battle.PlayerId;
		var actorState = previewWorld.StateOf(actorId);
		var movePathApBaseline = Battle.Sim.RuntimeFor(focusId).ActivePath?.PathApSpent ?? 0;

		PresentationDiagnostics.LogMovePreview(
			Battle.TurnNumber,
			source: "build_frame",
			effectiveMode,
			acceptsCommands,
			hasPlanningActor: activeUnit is not null,
			canAct,
			weaponQueued,
			actorState.Position,
			movePathApBaseline,
			Battle.Sim.Actions.Count,
			movePaths);

		return new PresentationFrame
		{
			Mode = effectiveMode,
			ActiveUnit = activeUnit,
			FocusId = focusId,
			IsInspecting = isInspecting,
			ShowMovePreview = !Battle.IsBattleOver
				&& effectiveMode == EPlayerMode.Move
				&& (canControl || isInspecting),
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
			ThreatenedUnitIds = threatenedUnitIds,
			TorpedoMountCells = torpedoMountCells,
			TorpedoEnvelopeLayers = torpedoEnvelopeLayers,
			CommittedFlakMount = committedFlakMount,
			CommittedRailgun = committedRailgun,
			CommittedTorpedoMountCell = committedTorpedoMountCell,
			CommittedTorpedoEnvelopeLayers = committedTorpedoEnvelopeLayers,
			MovePath = movePath,
			MoveTarget = moveTarget,
			TurnNumber = Battle.TurnNumber,
			CanAct = canControl,
			CanFocusCamera = canControl && activeUnit is not null,
			CanUndo = canControl && Battle.Sim.Actions.Count > 0,
			WeaponLegality = canControl ? PlayerWeaponLegality.For(Battle) : default,
			ShowOutcomeOverlay = Battle.IsBattleOver,
			ShowWeaponPreviews = showWeaponPreviews,
			Outcome = Battle.Outcome.Result,
			ActionLogLines = ActionLogLines,
		};
	}
}
