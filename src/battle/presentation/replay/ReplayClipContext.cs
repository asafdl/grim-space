using Godot;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using BattleOrientation = GrimSpace.Battle.Movement.Orientation;

namespace GrimSpace.Battle.Presentation.Replay;

public sealed class ReplayClipContext(
	ReplayState replayState,
	IReadOnlyDictionary<string, UnitView> unitViews,
	HazardBurstView hazardBursts,
	Func<string, Color> colorFor,
	IReadOnlyDictionary<string, State> endStates,
	Action<State, Color> ensureView,
	Action<string> dismissUnitPresentation,
	Action<CameraInterest>? reportInterest = null,
	Action<GoopSpawnedFacts>? showGoop = null,
	Action<string>? removeGoop = null)
{
	public ReplayState ReplayState { get; } = replayState;
	public IReadOnlyDictionary<string, UnitView> UnitViews { get; } = unitViews;
	public HazardBurstView HazardBursts { get; } = hazardBursts;
	public Func<string, Color> ColorFor { get; } = colorFor;
	public IReadOnlyDictionary<string, State> EndStates { get; } = endStates;
	public Action<State, Color> EnsureView { get; } = ensureView;

	/// <summary>
	/// Replay-only: hide hull and movement trail before <see cref="ReplayState"/>
	/// reflects death (e.g. void bomb detonate VFX lead-in). Live world is unchanged until playback ends.
	/// </summary>
	public Action<string> DismissUnitPresentation { get; } = dismissUnitPresentation;

	public Action<CameraInterest>? ReportInterest { get; } = reportInterest;
	public Action<GoopSpawnedFacts>? ShowGoop { get; } = showGoop;
	public Action<string>? RemoveGoop { get; } = removeGoop;
	public ESpatialOrientation? PendingVoidBombMountedOn { get; set; }
	public IReadOnlyList<IAction> FollowingActions { private get; set; } = [];
	public IReadOnlyList<IRecord> FollowingRecords { private get; set; } = [];

	public AreaDamageFacts? FollowingAreaDamage(string sourceId, EImpactCause cause)
	{
		foreach (var record in FollowingRecords.OfType<Record<AreaDamageFacts>>())
		{
			if (record.Value.SourceId == sourceId && record.Value.Cause == cause)
				return record.Value;
		}

		return null;
	}

	public bool OrientationFlowsIntoMove(string actorId)
	{
		foreach (var action in FollowingActions)
		{
			if (action.ActorId != actorId)
				return false;
			if (action is HeadingTurnAction or RollAction)
				continue;
			return action is MoveStepAction;
		}

		return false;
	}

	public Coord? NextMovePosition(string actorId)
	{
		var projected = ReplayState.StateOf(actorId).Clone();
		foreach (var action in FollowingActions)
		{
			if (action.ActorId != actorId)
				return null;

			switch (action)
			{
				case HeadingTurnAction heading:
					BattleOrientation.ApplyHeadingTurn(projected, heading.Turn);
					break;
				case RollAction roll:
					BattleOrientation.ApplyRoll(projected, roll.Direction);
					break;
				case MoveStepAction move:
					return projected.Position + BodyFrame.From(projected).Step(move.Direction);
				case VoidBombMoveStepAction move:
					return projected.Position + BodyFrame.From(projected).Step(move.Direction);
				default:
					return null;
			}
		}

		return null;
	}
}
