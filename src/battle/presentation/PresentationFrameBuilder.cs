using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation;

/// <summary>
/// Builds <see cref="PresentationFrame"/> from battle state, planning sim, and interaction state.
/// </summary>
public sealed class PresentationFrameBuilder
{
	private readonly PlanningPreview _preview = new();

	public InteractionState Interaction { get; } = new();

	public bool IntroActive { get; set; }

	private readonly List<string> _actionLogLines = [];

	public IReadOnlyList<string> ActionLogLines => _actionLogLines;

	public string FocusId(BattleOrchestrator battle) =>
		Interaction.FocusId ?? battle.PlayerId;

	public bool IsInspecting(BattleOrchestrator battle) =>
		FocusId(battle) != battle.PlayerId;

	public IReadOnlyList<MovePathOption> PreviewMoveOptions(BattleOrchestrator battle, UserExecutionAgent agent) =>
		_preview.MoveOptions(agent.Sim, battle.PlayerId, FocusId(battle), agent.IsPlanning);

	public void AppendTurn(BattleOrchestrator battle, int turnNumber, IReadOnlyList<ITimelineEntry> history)
	{
		var units = UnitRegistry.For(battle.Engine.World);
		_actionLogLines.Add($"--- Turn {turnNumber} ---");
		_actionLogLines.AddRange(ActionLog.Format(history, id => ActionLog.DisplayName(units, id)));
	}

	public PresentationFrame BuildFrame(
		BattleOrchestrator battle,
		UserExecutionAgent agent,
		bool acceptsCommands)
	{
		var state = Interaction;
		var playerId = battle.PlayerId;
		var sim = agent.Sim;
		var isPlanning = agent.IsPlanning;
		var previewUnits = battle.Phase == EBattlePhase.BattleOver
			? CaptureLiveUnits(battle)
			: _preview.PreviewUnits(sim, playerId);
		var focusId = state.FocusId ?? playerId;
		if (!previewUnits.TryGetValue(focusId, out var focusUnit) || !focusUnit.IsAlive)
		{
			state.ClearFocus();
			focusId = playerId;
			previewUnits = battle.Phase == EBattlePhase.BattleOver
				? CaptureLiveUnits(battle)
				: _preview.PreviewUnits(sim, playerId);
			focusUnit = previewUnits[playerId];
		}

		var isInspecting = focusId != playerId;
		var canControl = acceptsCommands && isPlanning && !isInspecting;
		var moveOptions = _preview.MoveOptions(sim, playerId, focusId, isPlanning);
		var movePathApBaseline = _preview.MovePathApBaseline(sim, playerId, focusId);
		var committedMoveCheckpoints = _preview.CommittedMoveCheckpoints(sim, playerId);
		var queuedWeapon = canControl ? _preview.QueuedWeapon(sim, playerId) : QueuedWeaponState.Empty;
		var abilityActorId = canControl || isInspecting ? focusId : playerId;
		var weapons = canControl || isInspecting ? _preview.Weapons(sim, abilityActorId) : WeaponPeek.Empty;
		var abilities = canControl || isInspecting ? _preview.Abilities(sim, abilityActorId) : AbilityLegality.Empty;
		var weaponQueued = queuedWeapon.FlakMountedOn is not null
			|| queuedWeapon.Railgun
			|| queuedWeapon.TorpedoMountedOn is not null;
		var selectedMove = state.MoveDestination is { } destination
			&& state.RequestedMoveBasis is { } requestedBasis
			? moveOptions.FirstOrDefault(option =>
				option.EndPosition == destination && option.EndBasis == requestedBasis)
			: null;
		MovePathOption? hoveredMove = null;
		var reachableMoveHeadings = state.MoveDestination is { } moveDestination
			? moveOptions
				.Where(option => option.EndPosition == moveDestination)
				.Select(option => option.EndBasis.Forward)
				.ToHashSet()
			: [];

		IReadOnlyList<MoveCheckpoint> moveCheckpoints;
		Coord? moveTarget;
		if (canControl || isInspecting)
		{
			if (isInspecting)
			{
				moveCheckpoints = [];
				moveTarget = null;
			}
			else
			{
				state.ClampMoveHover(moveOptions.Count);
				hoveredMove = state.MoveHoveredIndex is int hoveredIndex
					? moveOptions[hoveredIndex]
					: null;
				(moveCheckpoints, moveTarget) = MoveUi.GetPathHighlights(
					moveOptions,
					state.MoveHoveredIndex,
					committedMoveCheckpoints,
					selectedMove);
			}
		}
		else
		{
			moveCheckpoints = [];
			moveTarget = null;
		}

		var showWeaponPreviews = acceptsCommands && !battle.IsBattleOver && !isInspecting;
		var threatenedUnitIds = showWeaponPreviews
			? _preview.ThreatenedUnitIds(sim, playerId, state)
			: new HashSet<string>();

		var instruction = default(ActionInstruction);
		if (canControl && state.Mode == EPlayerMode.Move && state.ConfirmationError is { } moveError)
		{
			instruction = new ActionInstruction(
				Visible: true,
				Label: moveError,
				CanConfirm: false);
		}
		else if (canControl && state.Mode != EPlayerMode.Move && state.ActiveAbilitySpec is { } activeSpec)
		{
			var legalCapabilities = Capabilities.LegalCapabilities(sim, playerId);
			var activation = AbilityActivation.For(activeSpec.Def);
			instruction = activation.ResolveInstruction(
				visible: true,
				stagedMountedOn: state.StagedMountedOn,
				capabilityIsLegal: Capabilities.IsLegalCapability(
					legalCapabilities,
					activeSpec.Def,
					state.StagedMountedOn),
				confirmationError: state.ConfirmationError);
		}

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
			agent.CanUndo ? 1 : 0,
			moveOptions);

		var activeMovePreview = canControl
			&& state.Mode == EPlayerMode.Move
			&& !IntroActive
			&& !battle.IsBattleOver
				? selectedMove ?? hoveredMove
				: null;

		return new PresentationFrame
		{
			Mode = isInspecting ? EPlayerMode.Move : state.Mode,
			FocusId = focusId,
			FocusState = focusUnit,
			IsInspecting = isInspecting,
			ShowMovePreview = !battle.IsBattleOver
				&& (isInspecting ? EPlayerMode.Move : state.Mode) == EPlayerMode.Move
				&& (canControl || isInspecting),
			MovePaths = moveOptions,
			MovePathApBaseline = movePathApBaseline,
			PreviewUnits = previewUnits,
			QueuedWeapon = queuedWeapon,
			Weapons = weapons,
			Abilities = abilities,
			ThreatenedUnitIds = threatenedUnitIds,
			FlakHoverMountedOn = canControl ? state.FlakHoverMountedOn : null,
			RailgunHovered = canControl && state.RailgunHovered,
			StagedMountedOn = canControl ? state.StagedMountedOn : null,
			Instruction = instruction,
			MoveCheckpoints = moveCheckpoints,
			MoveTarget = moveTarget,
			SelectedMove = selectedMove,
			MoveDestination = state.MoveDestination,
			MoveGhostState = activeMovePreview?.ResultState,
			ReachableMoveHeadings = reachableMoveHeadings,
			IsMoveDragging = state.IsMoveDragging,
			TurnNumber = battle.Phase == EBattlePhase.Replaying
				? battle.TurnNumber - 1
				: battle.TurnNumber,
			CanAct = canControl,
			CanFocusCamera = canControl && isPlanning,
			CanUndo = canControl && agent.CanUndo,
			ShowOutcomeOverlay = battle.Phase == EBattlePhase.BattleOver,
			ShowIntroOverlay = IntroActive,
			ShowWeaponPreviews = showWeaponPreviews,
			Outcome = battle.Outcome.Result,
			ActionLogLines = ActionLogLines,
		};
	}

	private static Dictionary<string, UnitDisplayState> CaptureLiveUnits(BattleOrchestrator battle) =>
		UnitRegistry.For(battle.Engine.World).All.ToDictionary(
			unit => unit.State.Id,
			unit => UnitDisplayState.Capture(unit.State));
}
