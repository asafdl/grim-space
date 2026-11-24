using Godot;
using GrimSpace.Math.Camera;

namespace GrimSpace.Battle.Presentation.Camera;

public sealed class BattleCameraDirector
{
	public const float SafeMargin = 0.2f;
	public const float ComfortMargin = 0.15f;
	public const float PlaybackFollowResponse = 0.3f;
	public const float CombatInterestTween = 0.35f;
	public const float BarkFocusTween = 0.7f;
	public const float BarkSettleSeconds = 0.7f;
	public const float ReturnControlMin = 0.4f;
	public const float ReturnControlMax = 0.7f;
	public const float ManualInputGrace = 0.75f;
	public const float ReturnControlDistanceScale = 40f;

	private const float OverviewTransitionThreshold = 0.5f;
	private const float OverviewTweenMinDuration = 0.9f;
	private const float PlanningRecenterDurationScale = 1.2f;
	private readonly ICameraRig _rig;

	private CameraMode _mode = CameraMode.Manual;
	private float _manualInputGraceRemaining;
	private bool _barkFocus;
	private Vector3? _preBarkPivot;
	private bool _enemyHold;
	private OrbitPose? _preOverviewPose;
	private bool _manualAfterOverview;
	private bool _manualDuringPlayback;
	private bool _returnPending;
	private bool _focusNewPlayer;
	private bool _overviewTweenStarted;
	private int _overviewGeneration;

	public void BeginBark(Vector3 position)
	{
		if (!_barkFocus)
			_preBarkPivot = _rig.AutomationTarget ?? _rig.Pivot;
		_barkFocus = true;
		_rig.CancelAutomation();
		_rig.TweenPivotTo(position, BarkFocusTween);
	}

	public void EndBark()
	{
		if (!_barkFocus)
			return;

		_barkFocus = false;
		if (_preBarkPivot is not { } pivot)
			return;

		_preBarkPivot = null;
		_rig.CancelAutomation();
		_rig.TweenPivotTo(pivot, BarkFocusTween);
	}

	public BattleCameraDirector(ICameraRig rig) => _rig = rig;

	public bool NeedsTick =>
		_mode is CameraMode.Playback or CameraMode.Returning
		|| _rig.IsAutomationActive;

	public bool EnemyOverviewNeedsFullTransition { get; private set; }
	public bool IsEnemyOverviewMoving => _overviewTweenStarted && _rig.IsAutomationActive;

	public void EnterManual()
	{
		_overviewGeneration++;
		_mode = CameraMode.Manual;
		ClearEnemyFraming();
		ClearBarkFocus();
		_preOverviewPose = null;
		_manualAfterOverview = false;
		_manualDuringPlayback = false;
		_returnPending = false;
		_focusNewPlayer = false;
		_rig.CancelAutomation();
	}

	public void BeginPlayback()
	{
		_overviewGeneration++;
		_mode = CameraMode.Playback;
		_manualInputGraceRemaining = 0f;
		ClearBarkFocus();
		_preOverviewPose = null;
		_manualAfterOverview = false;
		_manualDuringPlayback = false;
		_returnPending = false;
		_focusNewPlayer = false;
		ClearEnemyFraming();
	}

	public Action? PrepareEnemyOverview(
		IReadOnlyList<Vector3> points,
		IReadOnlyList<Vector3>? priorityPoints = null,
		bool tween = false)
	{
		if (_mode != CameraMode.Playback)
			return null;

		_enemyHold = true;
		var target = _rig.CalculateOverview(points, priorityPoints ?? points);
		if (target is not { } pose)
			return null;

		var duration = CameraTransition.Duration(_rig.CurrentPose, pose);
		var shouldTween = tween && duration >= OverviewTransitionThreshold;
		EnemyOverviewNeedsFullTransition = shouldTween;
		_rig.CancelAutomation();
		var generation = ++_overviewGeneration;
		return () =>
		{
			if (generation != _overviewGeneration || _mode != CameraMode.Playback)
				return;
			if (!_manualAfterOverview)
				_preOverviewPose ??= _rig.CurrentPose;
			if (shouldTween)
			{
				_overviewTweenStarted = true;
				_rig.TweenFocusOn(pose.Pivot, pose.Distance, pose.Yaw, pose.Pitch,
					Mathf.Max(duration, OverviewTweenMinDuration));
			}
			else
				_rig.SetFocus(pose.Pivot, pose.Distance, pose.Yaw, pose.Pitch);
		};
	}

	public void PrepareReturnControl(bool focusNewPlayer = false)
	{
		_overviewGeneration++;
		_returnPending = true;
		_focusNewPlayer = focusNewPlayer;
		_mode = CameraMode.Manual;
		ClearEnemyFraming();
		_rig.CancelAutomation();
	}

	public void CompleteReturnControl(Vector3 playerWorldPos)
	{
		if (!_returnPending)
			return;
		_returnPending = false;
		if (_preOverviewPose is { } pose && !_manualAfterOverview)
			_rig.SetFocus(playerWorldPos, pose.Distance, pose.Yaw, pose.Pitch);
		else if (_focusNewPlayer && !_manualDuringPlayback && !_manualAfterOverview)
		{
			var current = _rig.CurrentPose;
			_rig.SetFocus(playerWorldPos, current.Distance, current.Yaw, current.Pitch);
		}
		else if (!_focusNewPlayer && !_manualAfterOverview)
			ReturnControl(playerWorldPos);
		_preOverviewPose = null;
		_manualAfterOverview = false;
		_manualDuringPlayback = false;
		_focusNewPlayer = false;
	}

	public void ReturnControl(Vector3 playerWorldPos)
	{
		ClearEnemyFraming();
		_mode = CameraMode.Manual;

		if (_rig.AreVisible([playerWorldPos], SafeMargin))
			return;

		var duration = ReturnTweenDuration(_rig.Pivot, playerWorldPos);
		_rig.TweenPivotTo(playerWorldPos, duration);
		_mode = CameraMode.Returning;
	}

	public void PlayImpactShake(ImpactShakeScale scale) =>
		_rig.PlayImpactShake(scale);

	public void OnPlanningChanged(
		Vector3 previousPosition,
		Vector3 currentPosition,
		int previousActionCount,
		int currentActionCount,
		bool enabled)
	{
		if (!enabled || _barkFocus || _mode == CameraMode.Playback
			|| currentActionCount == previousActionCount || previousPosition == currentPosition)
			return;

		var currentPose = _rig.CurrentPose;
		var targetPose = currentPose;
		targetPose.Pivot = currentPosition;
		_rig.TweenPivotTo(
			currentPosition,
			CameraTransition.Duration(currentPose, targetPose) * PlanningRecenterDurationScale);
	}

	public void ReportInterest(CameraInterest interest)
	{
		if (_mode != CameraMode.Playback || _barkFocus || _enemyHold)
			return;

		if (_rig.AreVisible(interest.Points, SafeMargin))
			return;

		if (_rig.IsAutomationActive && _rig.AutomationTarget is Vector3 automationTarget
			&& _rig.AreVisibleAtPivot(automationTarget, interest.Points, SafeMargin))
			return;

		if (!_rig.TryCalculateCorrection(interest.Points, ComfortMargin, out var target))
			return;

		_rig.TweenPivotTo(target, CombatInterestTween);
	}

	public void Tick(float delta, Vector3 playerRenderedPos)
	{
		if (_barkFocus)
			return;
		if (_mode == CameraMode.Returning && !_rig.IsAutomationActive)
			_mode = CameraMode.Manual;

		if (_mode != CameraMode.Playback || _enemyHold)
			return;

		if (_manualInputGraceRemaining > 0f)
		{
			_manualInputGraceRemaining -= delta;
			if (_manualInputGraceRemaining > 0f)
				return;
		}

		if (_rig.IsAutomationActive)
			return;

		if (_rig.AreVisible([playerRenderedPos], SafeMargin))
			return;

		if (!_rig.TryCalculateCorrection([playerRenderedPos], ComfortMargin, out var target))
			return;

		_rig.MovePivotToward(target, delta, PlaybackFollowResponse);
	}

	public void OnManualInputStarted()
	{
		_overviewGeneration++;
		_rig.CancelAutomation();
		_overviewTweenStarted = false;
		EnemyOverviewNeedsFullTransition = false;
		_preBarkPivot = null;
		if (_mode == CameraMode.Playback || _returnPending)
			_manualDuringPlayback = true;
		if (_preOverviewPose is not null || _enemyHold)
		{
			_preOverviewPose = null;
			_manualAfterOverview = true;
		}
		_manualInputGraceRemaining = ManualInputGrace;
		if (_mode == CameraMode.Returning)
			_mode = CameraMode.Manual;
	}

	public void FocusPlayer(Vector3 playerWorldPos)
	{
		_rig.CancelAutomation();
		_rig.TweenPivotTo(playerWorldPos);
	}

	private void ClearEnemyFraming()
	{
		_enemyHold = false;
		_overviewTweenStarted = false;
		EnemyOverviewNeedsFullTransition = false;
	}

	private void ClearBarkFocus()
	{
		_barkFocus = false;
		_preBarkPivot = null;
	}

	private static float ReturnTweenDuration(Vector3 from, Vector3 to) =>
		Mathf.Lerp(
			ReturnControlMin,
			ReturnControlMax,
			Mathf.Clamp((from - to).Length() / ReturnControlDistanceScale, 0f, 1f));

	private enum CameraMode
	{
		Manual,
		Playback,
		Returning,
	}
}
