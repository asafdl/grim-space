using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Events;
using GrimSpace.Battle.Presentation.Planning;
using GrimSpace.Battle.Spatial;
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
	public int MissileRange { get; private set; } = CombatConfig.ForeMissileMinRange;
	public Coord? MissileHover { get; private set; }
	public Coord? FlakHover { get; private set; }
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
		MissileRange = CombatConfig.ForeMissileMinRange;
		ClearInteraction();
	}

	public void SelectFlakMode()
	{
		Mode = EPlayerMode.Flak;
		MissileMount = null;
		ClearInteraction();
	}

	public void CancelFlakMode()
	{
		Mode = EPlayerMode.Move;
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
		FlakHover = null;
		RailgunHover = null;
	}

	public void ResetAfterTurn() => CancelMissileMode();

	public bool EndTurn(IPresentationEventSink? sink = null)
	{
		if (_manager.IsBattleOver)
			return false;

		_manager.ExecuteTurn(_manager.Player.FinalizePlan(), sink);
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

	public void SetFlakHover(Coord? cell) => FlakHover = cell;

	public bool AdjustMissileRange(int delta)
	{
		if (Mode != EPlayerMode.Missile || MissileMount is not EMissileMount.Fore)
			return false;

		var next = System.Math.Clamp(
			MissileRange + delta,
			CombatConfig.ForeMissileMinRange,
			CombatConfig.ForeMissileMaxRange);
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

		var ownerId = _manager.Player.OwnerId;
		if (!_manager.Player.TryEnqueueMovePath(options[optionIndex]))
			return false;

		_selection.Clear();
		return true;
	}

	public bool TryQueueMissile(Coord center)
	{
		if (MissileMount is not EMissileMount mount)
			return false;

		var ownerId = _manager.Player.OwnerId;
		if (!_manager.Player.TryEnqueue(new MissileAction(ownerId, center, mount, MissileRange)))
			return false;

		MissileHover = null;
		return true;
	}

	public bool TryQueueFlak(Coord cell)
	{
		var planning = _manager.Player;
		var frame = BodyFrame.From(planning.Board.StateOf(planning.OwnerId));
		var mount = FlakTargeting.MountForCell(frame, cell);
		if (mount is null)
			return false;

		var ownerId = planning.OwnerId;
		if (!planning.TryEnqueue(new FlakAction(ownerId, mount.Value)))
			return false;

		FlakHover = null;
		Mode = EPlayerMode.Move;
		return true;
	}

	public bool TryQueueRailgun(Unit target)
	{
		var ownerId = _manager.Player.OwnerId;
		if (!_manager.Player.TryEnqueue(new RailgunAction(ownerId, target.State.Id)))
			return false;

		RailgunHover = null;
		return true;
	}

	public bool TryQueueRoll(ERollDirection direction)
	{
		var ownerId = _manager.Player.OwnerId;
		return _manager.Player.TryEnqueue(new RollAction(ownerId, direction));
	}

	public bool TryQueueHeadingTurn(EHeadingTurn turn)
	{
		var ownerId = _manager.Player.OwnerId;
		return _manager.Player.TryEnqueue(new HeadingTurnAction(ownerId, turn));
	}

	public bool IsRailgunLegal(Unit target)
	{
		var enemy = _manager.GetEnemy();
		var ownerId = _manager.Player.OwnerId;
		return enemy is not null
			&& target.State.Id == enemy.State.Id
			&& _manager.Player.IsLegal(new RailgunAction(ownerId, target.State.Id));
	}

	public bool IsPlayerVictory() =>
		_manager.IsBattleOver
		&& _manager.WinnerId == _manager.Player.OwnerId;

	public PresentationFrame BuildFrame()
	{
		var planning = _manager.Player;
		var activeUnit = planning.GetActiveActor();
		var moveOptions = View.GetMoveHighlights(planning, activeUnit);
		_selection.ClampToCount(moveOptions.Count);

		var exitMissileMode = Mode == EPlayerMode.Missile && planning.MissilesRemainingThisTurn <= 0;
		if (exitMissileMode)
			CancelMissileMode();

		var simulation = View.GetTurnGhost(planning);
		var hazardCells = View.GetPlannedHazardHighlights(planning);
		var validMissileCells = GetValidMissileCells(activeUnit);
		var missilePreviewCells = GetMissilePreviewCells(activeUnit);
		var validFlakPortCells = GetFlakBurstCells(EFlakMount.Port);
		var validFlakStarboardCells = GetFlakBurstCells(EFlakMount.Starboard);
		var validFlakPickCells = new HashSet<Coord>(validFlakPortCells);
		validFlakPickCells.UnionWith(validFlakStarboardCells);
		var flakPreviewCells = GetFlakPreviewCells();
		var railgunTargets = View.GetRailgunTargetHighlights(planning, activeUnit);
		var enemyId = planning.Opponent.State.Id;
		var (path, target) = MovementSelection.GetHighlights(moveOptions, _selection.HoveredIndex);
		(path, target) = MovementSelection.WithCommittedMove(planning.Actions, path, target);

		var ownerId = planning.OwnerId;
		var missileInRange = MissileHover is Coord hover
			&& MissileMount is EMissileMount mount
			&& planning.IsLegal(new MissileAction(ownerId, hover, mount, MissileRange));

		return new PresentationFrame
		{
			Mode = Mode,
			MissileMount = MissileMount,
			MissileRange = MissileRange,
			ActiveUnit = activeUnit,
			MoveOptions = moveOptions,
			Simulation = simulation,
			PlannedHazardCells = hazardCells,
			ValidMissileCells = validMissileCells,
			MissilePreviewCells = missilePreviewCells,
			ValidFlakPortCells = validFlakPortCells,
			ValidFlakStarboardCells = validFlakStarboardCells,
			FlakPreviewCells = flakPreviewCells,
			ValidFlakPickCells = validFlakPickCells,
			RailgunTargetCells = railgunTargets,
			RailgunHoveredCell = GetRailgunHoveredCell(simulation, enemyId),
			MovePath = path,
			MoveTarget = target,
			MissileAimActive = Mode == EPlayerMode.Missile && MissileMount is not null && activeUnit is not null,
			MissileAimShip = Mode == EPlayerMode.Missile ? simulation.Actor : null,
			HintText = BuildHint(activeUnit, simulation, missileInRange, planning),
			CanAct = !_manager.IsBattleOver && activeUnit is not null && !_manager.IsResolving,
			MissilesRemaining = planning.MissilesRemainingThisTurn,
			FlakAvailable = LegalActions.IsFlakAvailable(planning.Board, planning.Context, planning.OwnerId),
			ExitMissileMode = exitMissileMode,
			ShowOutcomeOverlay = _manager.IsBattleOver,
			PlayerWon = IsPlayerVictory(),
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
			|| !_manager.Player.IsLegal(new MissileAction(_manager.Player.OwnerId, hover, mount, MissileRange)))
		{
			return [];
		}

		return View.GetMissileBlastHighlights(hover, _manager.Player.Grid);
	}

	private HashSet<Coord> GetFlakBurstCells(EFlakMount mount)
	{
		if (Mode != EPlayerMode.Flak)
			return [];

		return View.GetFlakBurstHighlights(_manager.Player, mount);
	}

	private HashSet<Coord> GetFlakPreviewCells()
	{
		if (Mode != EPlayerMode.Flak || FlakHover is not Coord hover)
			return [];

		var planning = _manager.Player;
		var frame = BodyFrame.From(planning.Board.StateOf(planning.OwnerId));
		var mount = FlakTargeting.MountForCell(frame, hover);
		if (mount is null)
			return [];

		if (!planning.IsLegal(new FlakAction(planning.OwnerId, mount.Value)))
			return [];

		return View.GetFlakBurstHighlights(planning, mount.Value);
	}

	private Coord? GetRailgunHoveredCell(SimulatedTurn simulation, string targetUnitId) =>
		RailgunHover is not null && IsRailgunLegal(RailgunHover)
			? simulation.Board.StateOf(targetUnitId).Position
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
			simulation.Actor,
			planning.MissilesRemainingThisTurn,
			planning.Actions.Count,
			RailgunHover,
			MissileMount,
			MissileRange,
			MissileHover,
			missileInRange);
	}
}
