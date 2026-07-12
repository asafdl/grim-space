using GrimSpace.Battle.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
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

		_manager.RequestEndTurn();
		ResetAfterTurn();
		return true;
	}

	public bool Undo() => _manager.TryUndoLast();

	public void SetMoveHover(int? index, int optionCount) =>
		_selection.SetHover(index, optionCount);

	public void SetMissileHover(Coord? cell) => MissileHover = cell;

	public void SetRailgunHover(Unit? target) =>
		RailgunHover = target is not null && CanPlanRailgun(target) ? target : null;

	public bool TryQueueMove(int optionIndex, IReadOnlyList<Option> options)
	{
		if (optionIndex < 0 || optionIndex >= options.Count)
			return false;

		var unit = _manager.GetActivePlayer();
		if (unit is null)
			return false;

		if (!_manager.EnqueueMove(unit, options[optionIndex]))
			return false;

		_selection.Clear();
		return true;
	}

	public bool TryQueueMissile(Coord center)
	{
		if (MissileMount is not EMissileMount mount)
			return false;

		var unit = _manager.GetActivePlayer();
		if (unit is null || !_manager.EnqueueMissile(unit, center, mount))
			return false;

		MissileHover = null;
		return true;
	}

	public bool TryQueueRailgun(Unit target)
	{
		var unit = _manager.GetActivePlayer();
		if (unit is null || !_manager.EnqueueRailgun(unit, target))
			return false;

		RailgunHover = null;
		return true;
	}

	public bool TryQueueRoll(ERollDirection direction)
	{
		var unit = _manager.GetActivePlayer();
		return unit is not null && _manager.EnqueueRoll(unit, direction);
	}

	public bool TryQueueHeadingTurn(EHeadingTurn turn)
	{
		var unit = _manager.GetActivePlayer();
		return unit is not null && _manager.EnqueueHeadingTurn(unit, turn);
	}

	public bool CanPlanRailgun(Unit target)
	{
		var unit = _manager.GetActivePlayer();
		return unit is not null && _manager.CanPlanRailgun(unit, target);
	}

	public PresentationFrame BuildFrame()
	{
		var active = _manager.GetActivePlayerPreview();
		var options = active.Preview?.Options ?? [];
		_selection.ClampToCount(options.Count);

		var exitMissileMode = Mode == EPlayerMode.Missile && _manager.MissilesRemainingThisTurn <= 0;
		if (exitMissileMode)
			CancelMissileMode();

		var simulation = _manager.GetSimulation();
		var hazardCells = _manager.GetPlannedHazardCells();
		var validMissileCells = GetValidMissileCells(active.Unit);
		var missilePreviewCells = GetMissilePreviewCells(active.Unit);
		var railgunTargets = _manager.GetRailgunTargetCells(active.Unit);
		var (path, target) = MovementSelection.GetHighlights(
			options,
			_selection.SelectedIndex,
			_selection.HoveredIndex);

		var missileInRange = MissileHover is Coord hover
			&& MissileMount is EMissileMount mount
			&& active.Unit is not null
			&& _manager.CanPlanMissileAt(active.Unit, hover, mount);

		return new PresentationFrame
		{
			Mode = Mode,
			MissileMount = MissileMount,
			ActiveUnit = active.Unit,
			MoveOptions = options,
			Simulation = simulation,
			PlannedHazardCells = hazardCells,
			ValidMissileCells = validMissileCells,
			MissilePreviewCells = missilePreviewCells,
			RailgunTargetCells = railgunTargets,
			RailgunHoveredCell = GetRailgunHoveredCell(),
			MovePath = path,
			MoveTarget = target,
			MissileAimActive = Mode == EPlayerMode.Missile && MissileMount is not null && active.Unit is not null,
			MissileAimShip = Mode == EPlayerMode.Missile ? simulation.Player : null,
			HintText = BuildHint(active.Unit, simulation, missileInRange),
			CanAct = !_manager.IsBattleOver && active.Unit is not null,
			MissilesRemaining = _manager.MissilesRemainingThisTurn,
			ExitMissileMode = exitMissileMode,
		};
	}

	private HashSet<Coord> GetValidMissileCells(Unit? unit)
	{
		if (Mode != EPlayerMode.Missile || MissileMount is not EMissileMount mount || unit is null)
			return [];

		return _manager.GetValidMissileCells(unit, mount);
	}

	private HashSet<Coord> GetMissilePreviewCells(Unit? unit)
	{
		if (Mode != EPlayerMode.Missile
			|| MissileMount is not EMissileMount mount
			|| unit is null
			|| MissileHover is not Coord hover
			|| !_manager.CanPlanMissileAt(unit, hover, mount))
		{
			return [];
		}

		return _manager.GetMissilePreviewCells(hover);
	}

	private Coord? GetRailgunHoveredCell() =>
		RailgunHover is not null && CanPlanRailgun(RailgunHover)
			? _manager.GetSimulation().Enemy.Position
			: null;

	private string BuildHint(Unit? unit, SimulatedTurn simulation, bool missileInRange)
	{
		if (unit is null)
			return "No active unit  |  scroll/+/-: zoom  |  RMB: orbit";

		var turnPrefix = _manager.IsBattleOver
			? $"Battle over — winner: {_manager.WinnerId}  |  "
			: $"Turn {_manager.Turn.TurnNumber}  |  ";

		return turnPrefix + CombatHints.BuildHint(
			Mode,
			simulation.Player,
			_manager.MissilesRemainingThisTurn,
			_manager.PlannedActions.Count,
			RailgunHover,
			MissileMount,
			MissileHover,
			missileInRange);
	}
}
