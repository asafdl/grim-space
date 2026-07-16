using GrimSpace.Core.Actions.Battle;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Planning;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed class BattlePresenter
{
	private readonly Manager _manager;
	private readonly Selection _selection = new();

	public BattlePresenter(Manager manager) => _manager = manager;

	public Manager Manager => _manager;

	public EPlayerMode Mode { get; private set; } = EPlayerMode.Move;
	public EMissileMount? MissileMount { get; private set; }
	public int MissileRange { get; private set; } = CombatConfig.DorsalMissileMinRange;
	public Coord? MissileHover { get; private set; }
	public Unit? RailgunHover { get; private set; }

	public void SetMode(EPlayerMode mode)
	{
		Mode = mode;
		MissileMount = null;
		ClearInteraction();
	}

	public void SelectMissileMount(EMissileMount mount)
	{
		Mode = EPlayerMode.Missile;
		MissileMount = mount;
		MissileRange = CombatConfig.DorsalMissileMinRange;
		ClearInteraction();
	}

	public void CancelMissileMode()
	{
		Mode = EPlayerMode.Move;
		MissileMount = null;
		ClearInteraction();
	}

	public void ClearInteraction()
	{
		_selection.Clear();
		MissileHover = null;
		RailgunHover = null;
	}

	public void ResetAfterTurn() => CancelMissileMode();

	public bool EndTurn()
	{
		if (_manager.IsBattleOver)
			return false;

		_manager.ExecuteTurn(_manager.Player.FinalizePlan());
		ResetAfterTurn();
		return true;
	}

	public bool Undo()
	{
		if (!_manager.Player.TryUndoLast())
			return false;

		ClearInteraction();
		return true;
	}

	public void SetMoveHover(int? index, int optionCount) =>
		_selection.SetHover(index, optionCount);

	public void SetMissileHover(Coord? cell) => MissileHover = cell;

	public bool AdjustMissileRange(int delta)
	{
		if (Mode != EPlayerMode.Missile || MissileMount is not EMissileMount.Dorsal)
			return false;

		var next = System.Math.Clamp(
			MissileRange + delta,
			CombatConfig.DorsalMissileMinRange,
			CombatConfig.DorsalMissileMaxRange);
		if (next == MissileRange)
			return false;

		MissileRange = next;
		MissileHover = null;
		return true;
	}

	public void SetRailgunHover(Unit? target) =>
		RailgunHover = target is not null && IsRailgunLegal(target) ? target : null;

	public bool TryQueueMove(int optionIndex, IReadOnlyList<Option> options)
	{
		if (optionIndex < 0 || optionIndex >= options.Count)
			return false;

		if (!_manager.Player.TryEnqueue(new MoveAction(options[optionIndex])))
			return false;

		_selection.Clear();
		return true;
	}

	public bool TryQueueMissile(Coord center)
	{
		if (MissileMount is not EMissileMount mount)
			return false;

		if (!_manager.Player.TryEnqueue(new MissileAction(center, mount, MissileRange)))
			return false;

		MissileHover = null;
		return true;
	}

	public bool TryQueueRailgun(Unit target)
	{
		if (!_manager.Player.TryEnqueue(new RailgunAction(target.State.Id)))
			return false;

		RailgunHover = null;
		return true;
	}

	public bool TryQueueRoll(ERollDirection direction) =>
		_manager.Player.TryEnqueue(new RollAction(direction));

	public bool TryQueueHeadingTurn(EHeadingTurn turn) =>
		_manager.Player.TryEnqueue(new HeadingTurnAction(turn));

	public bool IsRailgunLegal(Unit target)
	{
		var enemy = _manager.GetEnemy();
		return enemy is not null
			&& target.State.Id == enemy.State.Id
			&& _manager.Player.IsLegal(new RailgunAction(target.State.Id));
	}

	public PresentationFrame BuildFrame()
	{
		var planning = _manager.Player;
		var activeUnit = planning.GetActiveActor();
		var pickOptions = View.GetMoveSelectionHighlights(planning, activeUnit);
		var previewOptions = View.GetMoveHighlights(planning, activeUnit);
		var hasCommittedMove = planning.Actions.Any(action => action is MoveAction);
		var exploring = _selection.HoveredIndex is not null;
		var displayOptions = hasCommittedMove && !exploring
			? previewOptions
			: pickOptions;
		_selection.ClampToCount(pickOptions.Count);

		var exitMissileMode = Mode == EPlayerMode.Missile && planning.MissilesRemainingThisTurn <= 0;
		if (exitMissileMode)
			CancelMissileMode();

		var simulation = View.GetTurnGhost(planning);
		var hazardCells = View.GetPlannedHazardHighlights(planning);
		var validMissileCells = GetValidMissileCells(activeUnit);
		var missilePreviewCells = GetMissilePreviewCells(activeUnit);
		var railgunTargets = View.GetRailgunTargetHighlights(planning, activeUnit);
		var (path, target) = MovementSelection.GetHighlights(pickOptions, _selection.HoveredIndex);
		if (!exploring)
			(path, target) = MovementSelection.WithCommittedMove(planning.Actions, path, target);

		var missileInRange = MissileHover is Coord hover
			&& MissileMount is EMissileMount mount
			&& planning.IsLegal(new MissileAction(hover, mount, MissileRange));

		return new PresentationFrame
		{
			Mode = Mode,
			MissileMount = MissileMount,
			MissileRange = MissileRange,
			ActiveUnit = activeUnit,
			MoveOptions = displayOptions,
			MovePickOptions = pickOptions,
			Simulation = simulation,
			PlannedHazardCells = hazardCells,
			ValidMissileCells = validMissileCells,
			MissilePreviewCells = missilePreviewCells,
			RailgunTargetCells = railgunTargets,
			RailgunHoveredCell = GetRailgunHoveredCell(simulation),
			MovePath = path,
			MoveTarget = target,
			MissileAimActive = Mode == EPlayerMode.Missile && MissileMount is not null && activeUnit is not null,
			MissileAimShip = Mode == EPlayerMode.Missile ? simulation.Player : null,
			HintText = BuildHint(activeUnit, simulation, missileInRange, planning),
			CanAct = !_manager.IsBattleOver && activeUnit is not null,
			MissilesRemaining = planning.MissilesRemainingThisTurn,
			ExitMissileMode = exitMissileMode,
		};
	}

	private HashSet<Coord> GetValidMissileCells(Unit? unit)
	{
		if (Mode != EPlayerMode.Missile || MissileMount is not EMissileMount mount || unit is null)
			return [];

		return View.GetMissileTargetHighlights(_manager.Player, mount, MissileRange);
	}

	private HashSet<Coord> GetMissilePreviewCells(Unit? unit)
	{
		if (Mode != EPlayerMode.Missile
			|| MissileMount is not EMissileMount mount
			|| unit is null
			|| MissileHover is not Coord hover
			|| !_manager.Player.IsLegal(new MissileAction(hover, mount, MissileRange)))
		{
			return [];
		}

		return View.GetMissileBlastHighlights(hover, _manager.Player.Grid);
	}

	private Coord? GetRailgunHoveredCell(SimulatedTurn simulation) =>
		RailgunHover is not null && IsRailgunLegal(RailgunHover)
			? simulation.Enemy.Position
			: null;

	private string BuildHint(
		Unit? unit,
		SimulatedTurn simulation,
		bool missileInRange,
		PlayerController planning)
	{
		if (unit is null)
			return "No active unit  |  WASD: pan  |  scroll/+/-: zoom  |  RMB: orbit  |  MMB: drag pan";

		var turnPrefix = _manager.IsBattleOver
			? $"Battle over — winner: {_manager.WinnerId}  |  "
			: $"Turn {_manager.Turn.TurnNumber}  |  ";

		return turnPrefix + CombatHints.BuildHint(
			Mode,
			simulation.Player,
			planning.MissilesRemainingThisTurn,
			planning.Actions.Count,
			RailgunHover,
			MissileMount,
			MissileRange,
			MissileHover,
			missileInRange);
	}
}
