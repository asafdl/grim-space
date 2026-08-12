using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation;

/// <summary>
/// Presentation projection and battle meta.
/// Lifecycle and frame emission live in <see cref="BattleDirector"/>.
/// </summary>
public sealed class BattleUi
{
	private readonly PlanningPreview _preview = new();

	public BattleUi(BattleOrchestrator battle, UserExecutionAgent agent)
	{
		Battle = battle;
		Agent = agent;
	}

	public BattleOrchestrator Battle { get; }

	public UserExecutionAgent Agent { get; }

	public InteractionState State { get; } = new();

	public string FocusId => State.FocusId ?? Battle.PlayerId;

	public bool IsInspecting => FocusId != Battle.PlayerId;

	private readonly List<string> _actionLogLines = [];

	public IReadOnlyList<string> ActionLogLines => _actionLogLines;

	public IReadOnlyList<MovePathOption> PreviewMoveOptions() =>
		_preview.MoveOptions(Agent.Sim, Battle.PlayerId, FocusId, Agent.IsPlanning);

	public IReadOnlyDictionary<string, UnitDisplayState> PreviewUnits() =>
		_preview.PreviewUnits(Agent.Sim, Battle.PlayerId);

	public void AppendTurn(int turnNumber, IReadOnlyList<ITimelineEntry> history)
	{
		var units = UnitRegistry.For(Battle.Engine.World);
		_actionLogLines.Add($"--- Turn {turnNumber} ---");
		_actionLogLines.AddRange(ActionLog.Format(history, id => ActionLog.DisplayName(units, id)));
	}

	public PresentationFrame BuildFrame(bool acceptsCommands = true)
	{
		var state = State;
		var playerId = Battle.PlayerId;
		var sim = Agent.Sim;
		var isPlanning = Agent.IsPlanning;
		var previewUnits = _preview.PreviewUnits(sim, playerId);
		var focusId = state.FocusId ?? playerId;
		if (!previewUnits.TryGetValue(focusId, out var focusUnit) || !focusUnit.IsAlive)
		{
			state.ClearFocus();
			focusId = playerId;
			previewUnits = _preview.PreviewUnits(sim, playerId);
			focusUnit = previewUnits[playerId];
		}

		var isInspecting = focusId != playerId;
		var canControl = acceptsCommands && isPlanning && !isInspecting;
		var moveOptions = _preview.MoveOptions(sim, playerId, focusId, isPlanning);
		var movePathApBaseline = _preview.MovePathApBaseline(sim, playerId, focusId);
		var committedMovePath = _preview.CommittedMovePath(sim, playerId);
		var queuedWeapon = canControl ? _preview.QueuedWeapon(sim, playerId) : QueuedWeaponState.Empty;
		var weapons = canControl ? _preview.Weapons(sim, playerId) : WeaponPeek.Empty;
		var weaponQueued = queuedWeapon.FlakMount is not null
			|| queuedWeapon.Railgun
			|| queuedWeapon.TorpedoMount is not null;

		IReadOnlyList<Coord> movePath;
		Coord? moveTarget;
		if (canControl || isInspecting)
		{
			if (isInspecting)
			{
				movePath = [];
				moveTarget = null;
			}
			else
			{
				state.ClampMoveHover(moveOptions.Count);
				(movePath, moveTarget) = MoveUi.GetPathHighlights(
					moveOptions,
					state.MoveHoveredIndex,
					committedMovePath);
			}
		}
		else
		{
			movePath = [];
			moveTarget = null;
		}

		var showWeaponPreviews = acceptsCommands && !Battle.IsBattleOver && !isInspecting;
		var threatenedUnitIds = showWeaponPreviews
			? _preview.ThreatenedUnitIds(sim, playerId, state)
			: new HashSet<string>();
		var torpedoEnvelopeLayers = showWeaponPreviews
			? _preview.TorpedoEnvelopeLayers(sim, playerId, state)
			: [];

		PresentationDiagnostics.LogMovePreview(
			sim.AnchorTick,
			source: "build_frame",
			isInspecting ? EPlayerMode.Move : state.Mode,
			acceptsCommands,
			hasPlanningActor: isPlanning,
			isPlanning,
			weaponQueued,
			focusUnit.Position,
			movePathApBaseline,
			Agent.CanUndo ? 1 : 0,
			moveOptions);

		return new PresentationFrame
		{
			Mode = isInspecting ? EPlayerMode.Move : state.Mode,
			FocusId = focusId,
			FocusState = focusUnit,
			IsInspecting = isInspecting,
			ShowMovePreview = !Battle.IsBattleOver
				&& (isInspecting ? EPlayerMode.Move : state.Mode) == EPlayerMode.Move
				&& (canControl || isInspecting),
			MovePaths = moveOptions,
			MovePathApBaseline = movePathApBaseline,
			PreviewUnits = previewUnits,
			QueuedWeapon = queuedWeapon,
			Weapons = weapons,
			ThreatenedUnitIds = threatenedUnitIds,
			TorpedoEnvelopeLayers = torpedoEnvelopeLayers,
			FlakHoverMount = canControl ? state.FlakHoverMount : null,
			RailgunHovered = canControl && state.RailgunHovered,
			TorpedoHoverMount = canControl ? state.TorpedoHoverMount : null,
			MovePath = movePath,
			CommittedMovePath = committedMovePath,
			MoveTarget = moveTarget,
			TurnNumber = Battle.TurnNumber,
			CanAct = canControl,
			CanFocusCamera = canControl && isPlanning,
			CanUndo = canControl && Agent.CanUndo,
			ShowOutcomeOverlay = Battle.IsBattleOver,
			ShowWeaponPreviews = showWeaponPreviews,
			Outcome = Battle.Outcome.Result,
			ActionLogLines = ActionLogLines,
		};
	}
}
