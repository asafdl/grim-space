using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Presentation.Events;
using GrimSpace.Battle.Presentation.Planning;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed class BattlePresenter
{
	private readonly BattleOrchestrator _battle;
	private readonly Selection _selection = new();

	public BattlePresenter(BattleOrchestrator battle) => _battle = battle;

	public BattleOrchestrator Battle => _battle;

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
		if (_battle.IsBattleOver)
			return false;

		_battle.ResolveTurn(_battle.Actions.ToList(), sink);
		ResetAfterTurn();
		return true;
	}

	public bool Undo()
	{
		if (!_battle.TryUndoLast())
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

		if (!_battle.TryEnqueueMovePath(options[optionIndex]))
			return false;

		_selection.Clear();
		return true;
	}

	public bool TryQueueMissile(Coord center)
	{
		if (MissileMount is not EMissileMount mount)
			return false;

		if (!_battle.TryEnqueue(new MissileAction(_battle.OwnerId, center, mount, MissileRange)))
			return false;

		MissileHover = null;
		return true;
	}

	public bool TryQueueFlak(Coord cell)
	{
		var frame = BodyFrame.From(_battle.Board.StateOf(_battle.OwnerId));
		var mount = FlakTargeting.MountForCell(frame, cell);
		if (mount is null)
			return false;

		if (!_battle.TryEnqueue(new FlakAction(_battle.OwnerId, mount.Value)))
			return false;

		FlakHover = null;
		Mode = EPlayerMode.Move;
		return true;
	}

	public bool TryQueueRailgun(Unit target)
	{
		if (!_battle.TryEnqueue(new RailgunAction(_battle.OwnerId, target.State.Id)))
			return false;

		RailgunHover = null;
		return true;
	}

	public bool TryQueueRoll(ERollDirection direction) =>
		_battle.TryEnqueue(new RollAction(_battle.OwnerId, direction));

	public bool TryQueueHeadingTurn(EHeadingTurn turn) =>
		_battle.TryEnqueue(new HeadingTurnAction(_battle.OwnerId, turn));

	public bool IsRailgunLegal(Unit target)
	{
		var enemy = _battle.GetEnemy();
		return enemy is not null
			&& target.State.Id == enemy.State.Id
			&& _battle.IsLegal(new RailgunAction(_battle.OwnerId, target.State.Id));
	}

	public bool IsPlayerVictory() =>
		_battle.IsBattleOver
		&& _battle.WinnerId == _battle.OwnerId;

	public PresentationFrame BuildFrame()
	{
		var activeUnit = _battle.GetActiveActor();
		var moveOptions = View.GetMoveHighlights(_battle, activeUnit);
		_selection.ClampToCount(moveOptions.Count);

		var exitMissileMode = Mode == EPlayerMode.Missile && _battle.MissilesRemainingThisTurn <= 0;
		if (exitMissileMode)
			CancelMissileMode();

		var previewBoard = View.GetTurnGhost(_battle);
		var hazardCells = View.GetPlannedHazardHighlights(_battle);
		var validMissileCells = GetValidMissileCells(activeUnit);
		var missilePreviewCells = GetMissilePreviewCells(activeUnit);
		var validFlakPortCells = GetFlakBurstCells(EFlakMount.Port);
		var validFlakStarboardCells = GetFlakBurstCells(EFlakMount.Starboard);
		var validFlakPickCells = new HashSet<Coord>(validFlakPortCells);
		validFlakPickCells.UnionWith(validFlakStarboardCells);
		var flakPreviewCells = GetFlakPreviewCells();
		var railgunTargets = View.GetRailgunTargetHighlights(_battle, activeUnit);
		var enemyId = _battle.Opponent.State.Id;
		var (path, target) = MovementSelection.GetHighlights(moveOptions, _selection.HoveredIndex);
		(path, target) = MovementSelection.WithCommittedMove(
			_battle.Actions,
			path,
			target,
			_battle.Session.AnchorWorld,
			_battle.Session.AnchorRuntime,
			_battle.OwnerId);

		var ownerId = _battle.OwnerId;
		var missileInRange = MissileHover is Coord hover
			&& MissileMount is EMissileMount mount
			&& _battle.IsLegal(new MissileAction(ownerId, hover, mount, MissileRange));

		var actorState = previewBoard.StateOf(ownerId);

		return new PresentationFrame
		{
			Mode = Mode,
			MissileMount = MissileMount,
			MissileRange = MissileRange,
			ActiveUnit = activeUnit,
			MoveOptions = moveOptions,
			PreviewBoard = previewBoard,
			ActorState = actorState,
			PlannedHazardCells = hazardCells,
			ValidMissileCells = validMissileCells,
			MissilePreviewCells = missilePreviewCells,
			ValidFlakPortCells = validFlakPortCells,
			ValidFlakStarboardCells = validFlakStarboardCells,
			FlakPreviewCells = flakPreviewCells,
			ValidFlakPickCells = validFlakPickCells,
			RailgunTargetCells = railgunTargets,
			RailgunHoveredCell = GetRailgunHoveredCell(previewBoard, enemyId),
			MovePath = path,
			MoveTarget = target,
			MissileAimActive = Mode == EPlayerMode.Missile && MissileMount is not null && activeUnit is not null,
			MissileAimShip = Mode == EPlayerMode.Missile ? actorState : null,
			HintText = BuildHint(activeUnit, actorState),
			CanAct = !_battle.IsBattleOver && activeUnit is not null && !_battle.IsResolving,
			MissilesRemaining = _battle.MissilesRemainingThisTurn,
			FlakAvailable = Capabilities.For(actorState.Type)
				.OfType<FlakDef>()
				.Any(def => def.IsLegal(
					new FlakAction(_battle.OwnerId, def.Mount),
					_battle.Board,
					_battle.Runtime)),
			ExitMissileMode = exitMissileMode,
			ShowOutcomeOverlay = _battle.IsBattleOver,
			PlayerWon = IsPlayerVictory(),
		};
	}

	private HashSet<Coord> GetValidMissileCells(Unit? unit)
	{
		if (Mode != EPlayerMode.Missile || MissileMount is not EMissileMount mount || unit is null)
			return [];

		return View.GetMissileTargetHighlights(_battle, mount, MissileRange);
	}

	private HashSet<Coord> GetMissilePreviewCells(Unit? unit)
	{
		if (Mode != EPlayerMode.Missile
			|| MissileMount is not EMissileMount mount
			|| unit is null
			|| MissileHover is not Coord hover
			|| !_battle.IsLegal(new MissileAction(_battle.OwnerId, hover, mount, MissileRange)))
		{
			return [];
		}

		return View.GetMissileBlastHighlights(hover, _battle.Grid);
	}

	private HashSet<Coord> GetFlakBurstCells(EFlakMount mount)
	{
		if (Mode != EPlayerMode.Flak)
			return [];

		return View.GetFlakBurstHighlights(_battle, mount);
	}

	private HashSet<Coord> GetFlakPreviewCells()
	{
		if (Mode != EPlayerMode.Flak || FlakHover is not Coord hover)
			return [];

		var frame = BodyFrame.From(_battle.Board.StateOf(_battle.OwnerId));
		var mount = FlakTargeting.MountForCell(frame, hover);
		if (mount is null)
			return [];

		if (!_battle.IsLegal(new FlakAction(_battle.OwnerId, mount.Value)))
			return [];

		return View.GetFlakBurstHighlights(_battle, mount.Value);
	}

	private Coord? GetRailgunHoveredCell(BattleBoard previewBoard, string targetUnitId) =>
		RailgunHover is not null && IsRailgunLegal(RailgunHover)
			? previewBoard.StateOf(targetUnitId).Position
			: null;

	private string BuildHint(Unit? unit, State actorState)
	{
		if (unit is null)
			return "No active unit  |  WASD: pan  |  scroll/+/-: zoom  |  RMB: orbit  |  MMB: drag pan";

		var turnPrefix = _battle.IsBattleOver
			? $"Battle over — winner: {_battle.WinnerId}  |  "
			: $"Turn {_battle.TurnNumber}  |  ";

		return turnPrefix + CombatHints.BuildHint(
			Mode,
			actorState,
			_battle.MissilesRemainingThisTurn,
			_battle.Actions.Count,
			RailgunHover,
			MissileMount,
			MissileRange,
			MissileHover,
			MissileHover is Coord hover
				&& MissileMount is EMissileMount mount
				&& _battle.IsLegal(new MissileAction(_battle.OwnerId, hover, mount, MissileRange)));
	}
}
