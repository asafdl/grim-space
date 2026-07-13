using GrimSpace.Core.Actions.Battle;
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

		_manager.ExecuteTurn(_manager.Player.FinalizePlan());
		ResetAfterTurn();
		return true;
	}

	public bool Undo() => _manager.Player.TryUndoLast();

	public void SetMoveHover(int? index, int optionCount) =>
		_selection.SetHover(index, optionCount);

	public void SetMissileHover(Coord? cell) => MissileHover = cell;

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

		if (!_manager.Player.TryEnqueue(new MissileAction(center, mount)))
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
		var player = _manager.Player;
		var active = player.GetActivePlayerPreview();
		var options = active.Preview?.Options ?? [];
		_selection.ClampToCount(options.Count);

		var exitMissileMode = Mode == EPlayerMode.Missile && player.MissilesRemainingThisTurn <= 0;
		if (exitMissileMode)
			CancelMissileMode();

		var simulation = player.GetSimulation();
		var hazardCells = player.GetPlannedHazardCells();
		var validMissileCells = GetValidMissileCells(active.Unit);
		var missilePreviewCells = GetMissilePreviewCells(active.Unit);
		var railgunTargets = player.GetRailgunTargetCells(active.Unit);
		var (path, target) = MovementSelection.GetHighlights(
			options,
			_selection.SelectedIndex,
			_selection.HoveredIndex);

		var missileInRange = MissileHover is Coord hover
			&& MissileMount is EMissileMount mount
			&& player.IsLegal(new MissileAction(hover, mount));

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
			MissilesRemaining = player.MissilesRemainingThisTurn,
			ExitMissileMode = exitMissileMode,
		};
	}

	private HashSet<Coord> GetValidMissileCells(Unit? unit)
	{
		if (Mode != EPlayerMode.Missile || MissileMount is not EMissileMount mount || unit is null)
			return [];

		return _manager.Player.GetValidMissileCells(mount);
	}

	private HashSet<Coord> GetMissilePreviewCells(Unit? unit)
	{
		if (Mode != EPlayerMode.Missile
			|| MissileMount is not EMissileMount mount
			|| unit is null
			|| MissileHover is not Coord hover
			|| !_manager.Player.IsLegal(new MissileAction(hover, mount)))
		{
			return [];
		}

		return _manager.Player.GetMissilePreviewCells(hover);
	}

	private Coord? GetRailgunHoveredCell() =>
		RailgunHover is not null && IsRailgunLegal(RailgunHover)
			? _manager.Player.GetSimulation().Enemy.Position
			: null;

	private string BuildHint(Unit? unit, SimulatedTurn simulation, bool missileInRange)
	{
		if (unit is null)
			return "No active unit  |  WASD: pan  |  scroll/+/-: zoom  |  RMB: orbit  |  MMB: drag pan";

		var turnPrefix = _manager.IsBattleOver
			? $"Battle over — winner: {_manager.WinnerId}  |  "
			: $"Turn {_manager.Turn.TurnNumber}  |  ";

		return turnPrefix + CombatHints.BuildHint(
			Mode,
			simulation.Player,
			_manager.Player.MissilesRemainingThisTurn,
			_manager.Player.PlannedActions.Count,
			RailgunHover,
			MissileMount,
			MissileHover,
			missileInRange);
	}
}
