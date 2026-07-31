using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Presentation.Domains.Flak;
using GrimSpace.Battle.Presentation.Domains.Missile;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Domains.Orientation;
using GrimSpace.Battle.Presentation.Domains.Railgun;
using GrimSpace.Battle.Presentation.Domains.Turn;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation;

public sealed class BattleUi
{
	private readonly InteractionState _state = new();

	public BattleUi(BattleOrchestrator battle) => Battle = battle;

	public BattleOrchestrator Battle { get; }

	public EPlayerMode Mode => _state.Mode;
	public EMissileMount? MissileMount => _state.MissileMount;
	public int MissileRange => _state.MissileRange;

	public void SetMode(EPlayerMode mode) => _state.SetMode(mode);

	public void SelectMissileMount(EMissileMount mount) => _state.SelectMissileMount(mount);

	public void SelectFlakMode() => _state.SelectFlakMode();

	public void CancelFlakMode() => _state.SetMoveMode();

	public void CancelMissileMode() => _state.SetMoveMode();

	public void ClearInteraction() => _state.ClearInteraction();

	public void ResetAfterTurn()
	{
		_state.ResetAfterTurn();
	}

	public IReadOnlyList<IAction>? CommitAndResolve()
	{
		if (!TurnUi.TryCommit(Battle, out var playerActions))
			return null;

		var applied = Battle.ResolveTurn(playerActions);
		ResetAfterTurn();
		return applied;
	}

	public bool Undo() => TurnUi.TryUndo(Battle, _state);

	public int ActiveRingIndex => _state.ActiveRingIndex;

	public int? MoveHoveredIndex => _state.MoveHoveredIndex;

	public ERingBandPreset RingBandPreset => _state.RingBandPreset;

	public void SetRingBandPreset(ERingBandPreset preset)
	{
		var frame = BuildFrame();
		HashSet<Coord>? preserveEndpoints = null;
		if (frame.MovePreviewRingTable.RingCount > 0
			&& frame.ActiveRingIndex >= 0
			&& frame.ActiveRingIndex < frame.MovePreviewRingTable.RingCount)
		{
			preserveEndpoints = new HashSet<Coord>();
			foreach (var index in frame.MovePreviewRingTable.OptionIndicesOnRing(frame.ActiveRingIndex))
				preserveEndpoints.Add(frame.MoveOptions[index].EndPosition);
		}

		_state.SetRingBandPreset(preset, preserveEndpoints);
	}

	public void CycleMoveRing(int delta, int ringCount) => _state.CycleActiveRing(delta, ringCount);

	public void SetMoveHover(int? index, int optionCount) =>
		_state.SetMoveHover(index, optionCount);

	public void SetMissileHover(Coord? cell) => _state.MissileHover = cell;

	public void SetFlakHover(Coord? cell) => _state.FlakHover = cell;

	public bool AdjustMissileRange(int delta)
	{
		if (_state.Mode != EPlayerMode.Missile || _state.MissileMount is not EMissileMount.Fore)
			return false;

		var next = System.Math.Clamp(
			_state.MissileRange + delta,
			CombatConfig.ForeMissileMinRange,
			CombatConfig.ForeMissileMaxRange);
		if (next == _state.MissileRange)
			return false;

		_state.MissileRange = next;
		_state.MissileHover = null;
		return true;
	}

	public void SetRailgunHover(Unit? target) =>
		_state.RailgunHover = target is not null && RailgunUi.IsTargetLegal(Battle, target) ? target : null;

	public bool TryQueueMove(int optionIndex, IReadOnlyList<Movement.Option> options)
	{
		if (optionIndex < 0 || optionIndex >= options.Count)
			return false;

		return MoveUi.TryApply(Battle, _state, options[optionIndex]);
	}

	public bool TryQueueMissile(Coord center) => MissileUi.TryApply(Battle, _state, center);

	public bool TryQueueFlak(Coord cell) => FlakUi.TryApply(Battle, _state, cell);

	public bool TryQueueRailgun(Unit target) => RailgunUi.TryApply(Battle, _state, target);

	public bool TryQueueRoll(ERollDirection direction) => OrientationUi.TryApplyRoll(Battle, direction);

	public bool TryQueueHeadingTurn(EHeadingTurn turn) => OrientationUi.TryApplyHeadingTurn(Battle, turn);

	public PresentationFrame BuildFrame() => BattleFrameBuilder.Build(Battle, _state);
}
