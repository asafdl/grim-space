using Godot;

namespace GrimSpace.Battle.Presentation.Camera;

public sealed class BattleCameraDirector
{
	public const float SafeMargin = 0.2f;
	public const float ComfortMargin = 0.15f;
	public const float PlaybackFollowResponse = 0.3f;
	public const float CombatInterestTween = 0.35f;
	public const float ReturnControlMin = 0.4f;
	public const float ReturnControlMax = 0.7f;
	public const float ManualFocusTween = 0.3f;
	public const float ManualInputGrace = 0.75f;
	public const float ReturnControlDistanceScale = 40f;

	private readonly ICameraRig _rig;

	private CameraMode _mode = CameraMode.Manual;
	private float _manualInputGraceRemaining;

	public BattleCameraDirector(ICameraRig rig) => _rig = rig;

	public bool NeedsTick =>
		_mode is CameraMode.Playback or CameraMode.Returning
		|| _rig.IsAutomationActive;

	public void EnterManual()
	{
		_mode = CameraMode.Manual;
		_rig.CancelAutomation();
	}

	public void BeginPlayback()
	{
		_mode = CameraMode.Playback;
		_manualInputGraceRemaining = 0f;
	}

	public void ReturnControl(Vector3 playerWorldPos)
	{
		_mode = CameraMode.Manual;

		if (_rig.AreVisible([playerWorldPos], SafeMargin))
			return;

		var duration = ReturnTweenDuration(_rig.Pivot, playerWorldPos);
		_rig.TweenPivotTo(playerWorldPos, duration);
		_mode = CameraMode.Returning;
	}

	public void ReportInterest(CameraInterest interest)
	{
		if (_mode != CameraMode.Playback)
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
		if (_mode == CameraMode.Returning && !_rig.IsAutomationActive)
			_mode = CameraMode.Manual;

		if (_mode != CameraMode.Playback)
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
		_rig.CancelAutomation();
		_manualInputGraceRemaining = ManualInputGrace;

		if (_mode == CameraMode.Returning)
			_mode = CameraMode.Manual;
	}

	public void FocusPlayer(Vector3 playerWorldPos)
	{
		_rig.CancelAutomation();
		_rig.TweenPivotTo(playerWorldPos, ManualFocusTween);
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
