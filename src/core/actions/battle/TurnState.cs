using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Core.Engine;

namespace GrimSpace.Core.Actions.Battle;

/// <summary>
/// Temporary per-turn planning state. Recreated on replay; never survives commit.
/// </summary>
public sealed class TurnState : IRuntimeContext
{
	public const int InitialMinPathApCost = 3;

	private int _rawYawQuarters;
	private int _momentumPaid;
	private int _momentumGainedFromMovement;
	private bool _spinBraked;
	private bool _spinDiscount;
	private int _minPathApCost = InitialMinPathApCost;
	private int _pathApSpent;
	private int _pathForwardSteps;
	private int _usedDirectionsMask;
	private int _moveStartMomentumLevel;
	private int _movementBuildupLevel;
	private int _movementBuildupForwardSteps;
	private bool _flakUsedThisTurn;

	public int RawYawQuarters => _rawYawQuarters;

	public int NetYaw => Orientation.NormalizeQuarters(_rawYawQuarters);

	public int MomentumPaidThisTurn => _momentumPaid;

	public int MomentumGainedFromMovementThisTurn => _momentumGainedFromMovement;

	public bool SpinBraked => _spinBraked;

	public bool HasSpinDiscount => _spinDiscount;

	public int MinPathApCost => _minPathApCost;

	public int PathApSpent => _pathApSpent;

	public int PathForwardSteps => _pathForwardSteps;

	public int UsedDirectionsMask => _usedDirectionsMask;

	public int MoveStartMomentumLevel => _moveStartMomentumLevel;

	public bool IsMovePathStarted => _pathForwardSteps > 0 || _usedDirectionsMask > 0;

	public bool FlakUsedThisTurn => _flakUsedThisTurn;

	public MomentumConfig.Buildup MovementBuildup =>
		new(_movementBuildupLevel, _movementBuildupForwardSteps);

	public void AddYawQuarters(int delta) => _rawYawQuarters += delta;

	public void AddMomentumPaid(int amount)
	{
		if (amount > 0)
			_momentumPaid += amount;
	}

	public int RefundMomentum(int requested)
	{
		var refund = System.Math.Min(requested, _momentumPaid);
		_momentumPaid -= refund;
		return refund;
	}

	public void MarkBrakedFromRetro()
	{
		_spinBraked = true;
		_spinDiscount = true;
	}

	public bool TryConsumeSpinDiscount()
	{
		if (!_spinDiscount)
			return false;

		_spinDiscount = false;
		return true;
	}

	public void MarkFlakUsed() => _flakUsedThisTurn = true;

	public void ResetMovePath(int startMomentum)
	{
		_minPathApCost = InitialMinPathApCost;
		_pathApSpent = 0;
		_pathForwardSteps = 0;
		_usedDirectionsMask = 0;
		_moveStartMomentumLevel = startMomentum;
		_movementBuildupLevel = startMomentum;
		_movementBuildupForwardSteps = 0;
	}

	public void ConsumeMinPathAp(int stepApCost)
	{
		_minPathApCost = System.Math.Max(0, _minPathApCost - stepApCost);
		if (stepApCost > 0)
			_pathApSpent += stepApCost;
	}

	public void RecordMoveStep(EStepDirection direction, int directionBit)
	{
		_usedDirectionsMask |= directionBit;
		if (direction == EStepDirection.Forward)
			_pathForwardSteps++;
	}

	public void SetMovementBuildup(MomentumConfig.Buildup buildup)
	{
		_movementBuildupLevel = buildup.Level;
		_movementBuildupForwardSteps = buildup.ForwardStepsTowardGain;
	}

	public TurnStateSnapshot Snapshot() =>
		new(
			_rawYawQuarters,
			_momentumPaid,
			_momentumGainedFromMovement,
			_spinBraked,
			_spinDiscount,
			_minPathApCost,
			_pathApSpent,
			_pathForwardSteps,
			_usedDirectionsMask,
			_moveStartMomentumLevel,
			_movementBuildupLevel,
			_movementBuildupForwardSteps,
			_flakUsedThisTurn);

	public void Restore(TurnStateSnapshot snapshot)
	{
		_rawYawQuarters = snapshot.RawYawQuarters;
		_momentumPaid = snapshot.MomentumPaid;
		_momentumGainedFromMovement = snapshot.MomentumGainedFromMovement;
		_spinBraked = snapshot.SpinBraked;
		_spinDiscount = snapshot.SpinDiscount;
		_minPathApCost = snapshot.MinPathApCost;
		_pathApSpent = snapshot.PathApSpent;
		_pathForwardSteps = snapshot.PathForwardSteps;
		_usedDirectionsMask = snapshot.UsedDirectionsMask;
		_moveStartMomentumLevel = snapshot.MoveStartMomentumLevel;
		_movementBuildupLevel = snapshot.MovementBuildupLevel;
		_movementBuildupForwardSteps = snapshot.MovementBuildupForwardSteps;
		_flakUsedThisTurn = snapshot.FlakUsedThisTurn;
	}

	public TurnState Clone()
	{
		var clone = new TurnState();
		clone.Restore(Snapshot());
		return clone;
	}

	public void Reset() => Clear();

	public void Clear()
	{
		_rawYawQuarters = 0;
		_momentumPaid = 0;
		_momentumGainedFromMovement = 0;
		_spinBraked = false;
		_spinDiscount = false;
		_minPathApCost = InitialMinPathApCost;
		_pathApSpent = 0;
		_pathForwardSteps = 0;
		_usedDirectionsMask = 0;
		_moveStartMomentumLevel = 0;
		_movementBuildupLevel = 0;
		_movementBuildupForwardSteps = 0;
		_flakUsedThisTurn = false;
	}
}

public readonly record struct TurnStateSnapshot(
	int RawYawQuarters,
	int MomentumPaid,
	int MomentumGainedFromMovement,
	bool SpinBraked,
	bool SpinDiscount,
	int MinPathApCost,
	int PathApSpent,
	int PathForwardSteps,
	int UsedDirectionsMask,
	int MoveStartMomentumLevel,
	int MovementBuildupLevel,
	int MovementBuildupForwardSteps,
	bool FlakUsedThisTurn);
