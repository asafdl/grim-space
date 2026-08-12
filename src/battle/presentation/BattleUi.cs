using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation;

/// <summary>
/// Snapshot projection and battle meta for presentation.
/// Lifecycle and frame emission live in <see cref="BattleDirector"/>.
/// </summary>
public sealed class BattleUi
{
	public BattleUi(BattleOrchestrator battle, HumanExecutionAgent agent)
	{
		Battle = battle;
		Agent = agent;
	}

	public BattleOrchestrator Battle { get; }

	public HumanExecutionAgent Agent { get; }

	public InteractionState State { get; } = new();

	public string FocusId => State.FocusId ?? Battle.PlayerId;

	public bool IsInspecting => FocusId != Battle.PlayerId;

	private readonly List<string> _actionLogLines = [];

	public IReadOnlyList<string> ActionLogLines => _actionLogLines;

	public void AppendTurn(int turnNumber, IReadOnlyList<ITimelineEntry> history)
	{
		var units = UnitRegistry.For(Battle.Engine.World);
		_actionLogLines.Add($"--- Turn {turnNumber} ---");
		_actionLogLines.AddRange(ActionLog.Format(history, id => ActionLog.DisplayName(units, id)));
	}

	public PresentationFrame BuildFrame(HumanTurnSnapshot snapshot, bool acceptsCommands = true)
	{
		var state = State;
		var focusId = state.FocusId ?? snapshot.HumanActorId;
		if (!snapshot.PreviewUnits.TryGetValue(focusId, out var focusUnit) || !focusUnit.IsAlive)
		{
			state.ClearFocus();
			focusId = snapshot.HumanActorId;
			snapshot = Agent.BuildSnapshot(ViewInput(state));
			focusUnit = snapshot.PreviewUnits[snapshot.HumanActorId];
		}

		var isInspecting = focusId != snapshot.HumanActorId;
		var canControl = acceptsCommands && snapshot.CanAct && !isInspecting;
		var effectiveMode = isInspecting ? EPlayerMode.Move : state.Mode;
		var queuedWeapon = canControl ? snapshot.QueuedWeapon : QueuedWeaponState.Empty;
		var weapons = canControl ? snapshot.Weapons : WeaponPeek.Empty;
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
				state.ClampMoveHover(snapshot.MoveOptions.Count);
				(movePath, moveTarget) = MoveUi.GetPathHighlights(
					snapshot.MoveOptions,
					state.MoveHoveredIndex,
					snapshot.CommittedMovePath);
			}
		}
		else
		{
			movePath = [];
			moveTarget = null;
		}

		var showWeaponPreviews = acceptsCommands && !Battle.IsBattleOver && !isInspecting;
		var threatenedUnitIds = showWeaponPreviews
			? snapshot.ThreatenedUnitIds
			: new HashSet<string>();
		var torpedoEnvelopeLayers = showWeaponPreviews
			? snapshot.TorpedoEnvelopeLayers
			: [];

		PresentationDiagnostics.LogMovePreview(
			snapshot.TurnNumber,
			source: "build_frame",
			effectiveMode,
			acceptsCommands,
			hasPlanningActor: snapshot.CanAct,
			snapshot.CanAct,
			weaponQueued,
			focusUnit.Position,
			snapshot.MovePathApBaseline,
			snapshot.CanUndo ? 1 : 0,
			snapshot.MoveOptions);

		return new PresentationFrame
		{
			Mode = effectiveMode,
			FocusId = focusId,
			FocusState = focusUnit,
			IsInspecting = isInspecting,
			ShowMovePreview = !Battle.IsBattleOver
				&& effectiveMode == EPlayerMode.Move
				&& (canControl || isInspecting),
			MovePaths = snapshot.MoveOptions,
			MovePathApBaseline = snapshot.MovePathApBaseline,
			PreviewUnits = snapshot.PreviewUnits,
			QueuedWeapon = queuedWeapon,
			Weapons = weapons,
			ThreatenedUnitIds = threatenedUnitIds,
			TorpedoEnvelopeLayers = torpedoEnvelopeLayers,
			FlakHoverMount = canControl ? state.FlakHoverMount : null,
			RailgunHovered = canControl && state.RailgunHovered,
			TorpedoHoverMount = canControl ? state.TorpedoHoverMount : null,
			MovePath = movePath,
			CommittedMovePath = snapshot.CommittedMovePath,
			MoveTarget = moveTarget,
			TurnNumber = Battle.TurnNumber,
			CanAct = canControl,
			CanFocusCamera = canControl && snapshot.CanAct,
			CanUndo = canControl && snapshot.CanUndo,
			ShowOutcomeOverlay = Battle.IsBattleOver,
			ShowWeaponPreviews = showWeaponPreviews,
			Outcome = Battle.Outcome.Result,
			ActionLogLines = ActionLogLines,
		};
	}

	private static HumanTurnViewInput ViewInput(InteractionState state) =>
		new(state.FocusId, state.FlakHoverMount, state.RailgunHovered, state.TorpedoHoverMount);
}
