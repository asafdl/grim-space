using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation;

/// <summary>
/// Builds <see cref="PresentationFrame"/> from battle state, planning sim, and interaction state.
/// </summary>
public sealed class PresentationFrameBuilder
{
	private readonly PlanningPreview _preview = new();

	public InteractionState Interaction { get; } = new();

	private readonly List<ActionLog.Entry> _actionLogEntries = [];
	private int? _lastLoggedTurn;

	public IReadOnlyList<ActionLog.Entry> ActionLogEntries => _actionLogEntries;

	public string FocusId(BattleOrchestrator battle) =>
		Interaction.FocusId ?? battle.PlayerId;

	public bool IsInspecting(BattleOrchestrator battle) =>
		FocusId(battle) != battle.PlayerId;

	public IReadOnlyList<MovePathOption> PreviewMoveOptions(BattleOrchestrator battle, UserExecutionAgent agent) =>
		_preview.MoveOptions(agent.Sim, battle.PlayerId, FocusId(battle), agent.IsPlanning);

	public void AppendTurn(BattleOrchestrator battle, int turnNumber, IReadOnlyList<ITimelineEntry> history)
	{
		var units = UnitRegistry.For(battle.Engine.World);
		if (_lastLoggedTurn != turnNumber)
		{
			_actionLogEntries.Add(ActionLog.TurnHeader(turnNumber));
			_lastLoggedTurn = turnNumber;
		}
		_actionLogEntries.AddRange(ActionLog.Format(history, id => ActionLog.DisplayName(units, id)));
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
		var unitSnapshot = battle.Phase == EBattlePhase.BattleOver
			? CaptureLiveUnitSnapshot(battle)
			: _preview.PreviewUnitSnapshot(sim, playerId);
		var previewUnits = unitSnapshot.Units;
		var previewUnitTeams = unitSnapshot.Teams;
		var focusId = state.FocusId ?? playerId;
		if (!previewUnits.TryGetValue(focusId, out var focusUnit) || !focusUnit.IsAlive)
		{
			state.ClearFocus();
			focusId = playerId;
			focusUnit = previewUnits[playerId];
		}

		var isInspecting = focusId != playerId;
		if (state.RangeEnemyId is { } rangeEnemyId
			&& !IsLivingEnemy(previewUnits, previewUnitTeams, rangeEnemyId))
			state.ClearRangeEnemy();
		var canControl = acceptsCommands && isPlanning && !isInspecting;
		var moveOptions = _preview.MoveOptions(sim, playerId, focusId, isPlanning);
		var committedMoveCheckpoints = _preview.CommittedMoveCheckpoints(sim, playerId);
		var abilityActorId = canControl || isInspecting ? focusId : playerId;
		var legalCapabilities = canControl || isInspecting
			? Capabilities.LegalCapabilities(sim, abilityActorId)
			: [];
		var weapons = canControl || isInspecting
			? PlanningPreview.Weapons(legalCapabilities)
			: WeaponPeek.Empty;
		var abilities = canControl || isInspecting
			? PlanningPreview.Abilities(legalCapabilities)
			: AbilityLegality.Empty;
		var abilityChoices = canControl
			&& state.Mode != EPlayerMode.Move
			&& state.ActiveAbilitySpec is { } choiceSpec
				? AbilityActivation.ResolveChoices(
					choiceSpec,
					focusUnit.ToState(),
					legalCapabilities)
				: [];
		state.ClampAbilityHover(abilityChoices.Count);
		var hoveredAbilityChoice = state.AbilityHoveredIndex is int abilityHoverIndex
			? abilityChoices[abilityHoverIndex]
			: null;
		var weaponQueued = sim.Actions.Any(action =>
			action.ActorId == playerId && !MovePathIndex.IsMovementAction(action));
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
				state.ValidateMoveHover(moveOptions);
				hoveredMove = state.MoveHoveredCell is Coord hoveredCell
					? moveOptions.FirstOrDefault(option =>
						option.EndPosition == hoveredCell && option.Steps.Count > 0)
					: null;
				(moveCheckpoints, moveTarget) = MoveUi.GetPathHighlights(
					moveOptions,
					hoveredMove,
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
		var areaActions = showWeaponPreviews
			? _preview.AreaPreviews(sim, playerId, legalCapabilities)
			: AreaActionPreviews.Empty;
		var threatenedUnitIds = showWeaponPreviews
			? _preview.ThreatenedUnitIds(
				sim,
				playerId,
				hoveredAbilityChoice?.Action)
			: new HashSet<string>();
		var torpedoPreviews = showWeaponPreviews
			? _preview.VoidBombPreviews(
				sim,
				playerId,
				hoveredAbilityChoice?.Action)
			: TurnVolumePreviews.Empty;

		var instruction = default(ActionInstruction);
		if (canControl && state.ActionError is { } actionError)
		{
			instruction = new ActionInstruction(
				Visible: true,
				Label: actionError);
		}
		else if (canControl && state.Mode != EPlayerMode.Move)
		{
			instruction = new ActionInstruction(
				Visible: true,
				Label: BattleHudCopy.PickAbilitySource(state.Mode),
				Accent: state.ActiveAbilitySpec?.IconTint);
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
			agent.CanUndo ? 1 : 0,
			moveOptions);

		var activeMovePreview = canControl
			&& state.Mode == EPlayerMode.Move
			&& !battle.IsBattleOver
				? selectedMove ?? hoveredMove
				: null;
		if (canControl && state.Mode == EPlayerMode.Move && selectedMove is { } poseMove)
			state.RefreshPoseHitOpportunities(_preview.PoseHitOpportunities(sim, playerId, poseMove));

		var poseHitOpportunities =
			canControl && state.Mode == EPlayerMode.Move && !isInspecting
				? state.PoseHitOpportunities
				: [];

		Coord? reopenMoveCell = null;
		if (canControl
			&& state.Mode == EPlayerMode.Move
			&& !isInspecting
			&& state.MoveDestination is null
			&& MoveUi.CanReopenLastMove(sim.Actions, sim.UndoGroups, playerId))
		{
			reopenMoveCell = sim.StateOf<ActorState>(playerId).Position;
		}

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
			PreviewUnits = previewUnits,
			PreviewUnitTeams = previewUnitTeams,
			RangeEnemy = FindRangeEnemy(
				battle,
				previewUnits,
				previewUnitTeams,
				state.RangeEnemyId),
			Weapons = weapons,
			AreaActions = areaActions,
			Abilities = abilities,
			AbilityChoices = abilityChoices,
			AbilityHoveredIndex = canControl ? state.AbilityHoveredIndex : null,
			HoveredAbilityChoice = canControl ? hoveredAbilityChoice : null,
			ThreatenedUnitIds = threatenedUnitIds,
			VoidBombPreviews = torpedoPreviews,
			Instruction = instruction,
			MoveCheckpoints = moveCheckpoints,
			MoveTarget = moveTarget,
			HoveredMove = hoveredMove,
			SelectedMove = selectedMove,
			MoveDestination = state.MoveDestination,
			MoveGhostState = activeMovePreview?.ResultState,
			ReachableMoveHeadings = reachableMoveHeadings,
			IsMovePoseSelectionActive = state.IsMovePoseSelectionActive,
			SimulationTick = battle.TurnNumber,
			TurnNumber = battle.Phase == EBattlePhase.Replaying
				? battle.TurnNumber - 1
				: battle.TurnNumber,
			CanAct = canControl,
			CanFocusCamera = canControl && isPlanning,
			CanUndo = canControl
				&& (state.Mode != EPlayerMode.Move
					|| state.IsMovePoseSelectionActive
					|| agent.CanUndo),
			ShowOutcomeOverlay = battle.Phase == EBattlePhase.BattleOver,
			ShowWeaponPreviews = showWeaponPreviews,
			ActionLogEntries = ActionLogEntries,
			Outcome = battle.Engine.World.battleResult,
			PoseHitOpportunities = poseHitOpportunities,
			ReopenMoveCell = reopenMoveCell,
		};
	}

	private static UnitPreviewSnapshot CaptureLiveUnitSnapshot(BattleOrchestrator battle)
	{
		var units = UnitRegistry.For(battle.Engine.World).All.ToArray();
		return new UnitPreviewSnapshot(
			units.ToDictionary(
				unit => unit.State.Id,
				unit => UnitDisplayState.Capture(
					unit.State,
					battle.Engine.ActorRuntimes.For(unit.State.Id))),
			units.ToDictionary(unit => unit.State.Id, unit => unit.Team));
	}

	private static EnemyRangeTarget? FindRangeEnemy(
		BattleOrchestrator battle,
		IReadOnlyDictionary<string, UnitDisplayState> units,
		IReadOnlyDictionary<string, ETeam> teams,
		string? requestedEnemyId)
	{
		if (battle.Phase is not (EBattlePhase.PlayerTurn or EBattlePhase.Resolving))
			return null;
		if (!units.TryGetValue(battle.PlayerId, out var player))
			return null;

		if (requestedEnemyId is { } enemyId && IsLivingEnemy(units, teams, enemyId))
			return RangeTarget(enemyId, player.Position, units[enemyId].Position);

		EnemyRangeTarget? closest = null;
		foreach (var (id, team) in teams)
		{
			if (team != ETeam.Enemy
				|| !units.TryGetValue(id, out var enemy)
				|| !enemy.IsAlive)
				continue;

			var candidate = RangeTarget(id, player.Position, enemy.Position);
			var distance = candidate.SeparationMeters;
			if (closest is null
				|| distance < closest.Value.SeparationMeters
				|| (distance == closest.Value.SeparationMeters
					&& string.CompareOrdinal(id, closest.Value.EnemyId) < 0))
				closest = candidate;
		}

		return closest;
	}

	private static bool IsLivingEnemy(
		IReadOnlyDictionary<string, UnitDisplayState> units,
		IReadOnlyDictionary<string, ETeam> teams,
		string unitId) =>
		teams.TryGetValue(unitId, out var team)
		&& team == ETeam.Enemy
		&& units.TryGetValue(unitId, out var unit)
		&& unit.IsAlive;

	private static EnemyRangeTarget RangeTarget(string enemyId, Coord source, Coord target)
		=> new(enemyId, BattleDistance.SeparationMeters(source, target));
}
