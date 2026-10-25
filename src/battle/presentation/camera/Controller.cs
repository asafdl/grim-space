using Godot;
using GrimSpace.Application;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Math.Camera;

namespace GrimSpace.Battle.Presentation.Camera;

public partial class Controller : Camera3D, ICameraRig
{
	public event Action? ManualInputStarted;

	private static readonly OrbitLimits Limits = new(
		MinDistance: 8f,
		MaxDistance: 280f,
		MinPitch: Mathf.DegToRad(-89f),
		MaxPitch: Mathf.DegToRad(89f));

	private const float DefaultDistance = 22f;
	private const float PivotEpsilon = 0.01f;
	private const float MaxCorrectionShift = 28f;

	private OrbitPose _pose;
	private Vector2 _lastMousePosition;
	private bool _orbiting;
	private bool _panning;
	private Tween? _pivotTween;
	private Tween? _impactShakeTween;
	private Vector3 _impactShakeLocalOffset;
	private Vector3? _automationTarget;

	public Vector3 Pivot => _pose.Pivot;
	public OrbitPose CurrentPose => _pose;
	public float Distance => _pose.Distance;
	public float Yaw => _pose.Yaw;
	public float Pitch => _pose.Pitch;
	public bool IsAutomationActive => _pivotTween is not null;
	public bool IsManualGestureActive => _orbiting || _panning;
	public Vector3? AutomationTarget => _automationTarget;

	public void SetPivot(Vector3 pivot)
	{
		CancelAutomation();
		_pose.Pivot = pivot;
		ApplyTransform();
	}

	public void SetFocus(Vector3 pivot, float distance, float yaw, float pitch)
	{
		CancelAutomation();
		_pose.SetFocus(pivot, distance, yaw, pitch, Limits);
		_automationTarget = null;
		ApplyTransform();
	}

	public bool AreVisible(IReadOnlyList<Vector3> points, float margin) =>
		AreVisibleWithCurrentTransform(points, margin);

	public bool AreVisibleAtPivot(Vector3 pivot, IReadOnlyList<Vector3> points, float margin)
	{
		var savedPivot = _pose.Pivot;
		_pose.Pivot = pivot;
		ApplyTransform();
		var visible = AreVisibleWithCurrentTransform(points, margin);
		_pose.Pivot = savedPivot;
		ApplyTransform();
		return visible;
	}

	public bool TryCalculateCorrection(
		IReadOnlyList<Vector3> points,
		float margin,
		out Vector3 targetPivot)
	{
		targetPivot = _pose.Pivot;

		if (points.Count == 0)
			return false;

		if (AreVisibleWithCurrentTransform(points, margin))
			return false;

		targetPivot = CapPivotShift(ComputeCentroid(points));
		return (_pose.Pivot - targetPivot).LengthSquared() > PivotEpsilon * PivotEpsilon;
	}

	public void TweenPivotTo(Vector3 target)
	{
		var targetPose = _pose;
		targetPose.Pivot = target;
		TweenPivotTo(target, CameraTransition.Duration(_pose, targetPose));
	}

	public void TweenPivotTo(Vector3 target, float duration)
	{
		CancelAutomation();

		if ((_pose.Pivot - target).LengthSquared() <= PivotEpsilon * PivotEpsilon)
			return;

		_automationTarget = target;
		_pivotTween = CreateTween();
		_pivotTween.TweenMethod(
			Callable.From<Vector3>(SetPivotInternal),
			_pose.Pivot,
			target,
			duration)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
		_pivotTween.Finished += OnPivotTweenFinished;
	}

	public void TweenFocusOn(Vector3 targetPivot, float targetDistance) =>
		TweenFocusOn(targetPivot, targetDistance, _pose.Yaw, _pose.Pitch);

	public void TweenFocusOn(
		Vector3 targetPivot,
		float targetDistance,
		float targetYaw,
		float targetPitch)
	{
		var targetPose = new OrbitPose
		{
			Pivot = targetPivot,
			Distance = targetDistance,
			Yaw = targetYaw,
			Pitch = targetPitch,
		};
		targetPose.Clamp(Limits);
		TweenFocusOn(
			targetPose.Pivot,
			targetPose.Distance,
			targetPose.Yaw,
			targetPose.Pitch,
			CameraTransition.Duration(_pose, targetPose));
	}

	public void TweenFocusOn(Vector3 targetPivot, float targetDistance, float duration) =>
		TweenFocusOn(targetPivot, targetDistance, _pose.Yaw, _pose.Pitch, duration);

	public void TweenFocusOn(
		Vector3 targetPivot,
		float targetDistance,
		float targetYaw,
		float targetPitch,
		float duration)
	{
		CancelAutomation();

		var startPivot = _pose.Pivot;
		var startDistance = _pose.Distance;
		var startYaw = _pose.Yaw;
		var startPitch = _pose.Pitch;
		targetDistance = Mathf.Clamp(targetDistance, Limits.MinDistance, Limits.MaxDistance);
		targetPitch = Mathf.Clamp(targetPitch, Limits.MinPitch, Limits.MaxPitch);
		_automationTarget = targetPivot;

		_pivotTween = CreateTween();
		_pivotTween.TweenMethod(
			Callable.From<float>(BlendFocus),
			0f,
			1f,
			duration)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
		_pivotTween.Finished += OnPivotTweenFinished;

		void BlendFocus(float t)
		{
			_pose.Pivot = startPivot.Lerp(targetPivot, t);
			_pose.Distance = Mathf.Lerp(startDistance, targetDistance, t);
			_pose.Yaw = Mathf.LerpAngle(startYaw, targetYaw, t);
			_pose.Pitch = Mathf.Lerp(startPitch, targetPitch, t);
			ApplyTransform();
		}
	}

	public static (float Yaw, float Pitch) OrbitAnglesForDirection(Vector3 direction) =>
		OrbitPose.AnglesForDirection(direction, Limits);

	public void MovePivotToward(Vector3 target, float delta, float responseTime)
	{
		if (responseTime <= 0f)
		{
			_pose.Pivot = target;
			ApplyTransform();
			return;
		}

		var t = Mathf.Clamp(delta / responseTime, 0f, 1f);
		_pose.Pivot += (target - _pose.Pivot) * t;
		ApplyTransform();
	}

	public void CancelAutomation()
	{
		if (_pivotTween is not null)
		{
			_pivotTween.Kill();
			_pivotTween = null;
		}

		_automationTarget = null;
	}

	private const float ShakeMagnitude = 18f;
	private const float HullHitStrengthMultiplier = 1.32f;
	private const int LethalShakeDamageBonus = 10;
	private const float ShakeDamageReference = 28f;
	private const float MinShakeStrength = 0.35f * ShakeMagnitude;
	private const float MaxShakeStrength = 1f * ShakeMagnitude;
	private const float MaxShakeStrengthCap = 2.2f * ShakeMagnitude;
	private const float ShakeOffsetPerStrength = 0.028f;
	private const float ShakeDurationMagnitude = 2.4f;
	private const float ShakeDurationAtMinStrength = 0.42f;
	private const float ShakeDurationAtMaxStrength = 1f;
	private const float ShakeBeat1Seconds = 0.045f;
	private const float ShakeBeat2Seconds = 0.055f;
	private const float ShakeBeat3Seconds = 0.07f;
	private const float ShakeRecoilPrimaryWeight = 1f;
	private const float ShakeRecoilReboundWeight = 0.78f;
	private const float ShakeRecoilSettleWeight = 0.48f;
	private const float ShakeRecoilCrossWeight = 0.18f;

	public void PlayImpactShake(ImpactShakeScale scale)
	{
		if (scale.TotalDamage <= 0)
			return;

		var effectiveDamage = scale.TotalDamage + (scale.TargetDied ? LethalShakeDamageBonus : 0);
		var strength = Mathf.Lerp(
			MinShakeStrength,
			MaxShakeStrength,
			Mathf.Clamp(effectiveDamage / ShakeDamageReference, 0f, 1f));
		if (scale.HullDamage > 0)
			strength *= HullHitStrengthMultiplier;
		strength = Mathf.Min(strength, MaxShakeStrengthCap);
		_impactShakeTween?.Kill();
		_impactShakeLocalOffset = Vector3.Zero;

		var maxOffset = ShakeOffsetPerStrength * strength;
		var strengthT = Mathf.InverseLerp(MinShakeStrength, MaxShakeStrengthCap, strength);
		var durationScale = ShakeDurationMagnitude * Mathf.Lerp(
			ShakeDurationAtMinStrength,
			ShakeDurationAtMaxStrength,
			strengthT);
		var recoilDirection = ResolveRecoilDirection(scale);
		var (beat1, beat2, beat3) = BuildShakeBeats(maxOffset, recoilDirection);
		_impactShakeTween = CreateTween();
		_impactShakeTween.TweenMethod(
			Callable.From<Vector3>(offset =>
			{
				_impactShakeLocalOffset = offset;
				ApplyTransform();
			}),
			beat1,
			beat2,
			ShakeBeat1Seconds * durationScale)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
		_impactShakeTween.TweenMethod(
			Callable.From<Vector3>(offset =>
			{
				_impactShakeLocalOffset = offset;
				ApplyTransform();
			}),
			beat2,
			beat3,
			ShakeBeat2Seconds * durationScale)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		_impactShakeTween.TweenMethod(
			Callable.From<Vector3>(offset =>
			{
				_impactShakeLocalOffset = offset;
				ApplyTransform();
			}),
			beat3,
			Vector3.Zero,
			ShakeBeat3Seconds * durationScale)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
		_impactShakeTween.Finished += () => _impactShakeTween = null;
	}

	private Vector3 ResolveRecoilDirection(ImpactShakeScale scale)
	{
		if (scale.RadialRecoilOriginWorld is { } origin)
		{
			ApplyTransform();
			var awayFromBlast = GlobalPosition - origin;
			if (awayFromBlast.LengthSquared() > 0.0001f)
				return awayFromBlast.Normalized();
		}

		return scale.WorldRecoilDirection;
	}

	private (Vector3 Beat1, Vector3 Beat2, Vector3 Beat3) BuildShakeBeats(
		float maxOffset,
		Vector3 worldRecoilDirection)
	{
		if (worldRecoilDirection.LengthSquared() < 0.0001f)
			return BuildOmnidirectionalShakeBeats(maxOffset);

		ApplyTransform();
		var basis = GlobalTransform.Basis;
		var planar = basis.X * worldRecoilDirection.Dot(basis.X)
			+ basis.Y * worldRecoilDirection.Dot(basis.Y);
		if (planar.LengthSquared() < 0.0001f)
			planar = basis.X;

		var primaryWorld = planar.Normalized();
		var crossWorld = primaryWorld.Cross(-basis.Z);
		if (crossWorld.LengthSquared() < 0.0001f)
			crossWorld = basis.Y;
		crossWorld = crossWorld.Normalized();

		var primary = WorldToCameraLocal(primaryWorld, basis);
		var cross = WorldToCameraLocal(crossWorld, basis);
		return (
			primary * (maxOffset * ShakeRecoilPrimaryWeight),
			primary * (-maxOffset * ShakeRecoilReboundWeight)
				+ cross * (maxOffset * ShakeRecoilCrossWeight),
			primary * (maxOffset * ShakeRecoilSettleWeight)
				+ cross * (-maxOffset * ShakeRecoilCrossWeight * 0.75f));
	}

	private static (Vector3 Beat1, Vector3 Beat2, Vector3 Beat3) BuildOmnidirectionalShakeBeats(
		float maxOffset) =>
		(
			new Vector3(maxOffset, 0f, 0f),
			new Vector3(-maxOffset * 0.72f, maxOffset * 0.38f, 0f),
			new Vector3(maxOffset * 0.42f, -maxOffset * 0.28f, 0f));

	private static Vector3 WorldToCameraLocal(Vector3 worldDirection, Basis cameraBasis) =>
		new(
			worldDirection.Dot(cameraBasis.X),
			worldDirection.Dot(cameraBasis.Y),
			worldDirection.Dot(cameraBasis.Z));

	public override void _Ready()
	{
		CullMask = PresentationLayers.World | PresentationLayers.Ux;

		_pose.Pivot = new Vector3(
			WorldMapping.CellSize * 4f,
			WorldMapping.CellSize * 4f,
			WorldMapping.CellSize * 4f);
		_pose = OrbitPose.FromOffset(_pose.Pivot, GlobalPosition, Limits);
		_pose.Distance = DefaultDistance;
		ApplyTransform();
	}

	public override void _Process(double delta)
	{
		if (GestureInputBlocked)
			return;

		var flatPan = Vector2.Zero;
		if (Input.IsActionPressed("battle_pan_up", true))
			flatPan.Y += 1f;
		if (Input.IsActionPressed("battle_pan_down", true))
			flatPan.Y -= 1f;
		if (Input.IsActionPressed("battle_pan_left", true))
			flatPan.X -= 1f;
		if (Input.IsActionPressed("battle_pan_right", true))
			flatPan.X += 1f;

		if (flatPan == Vector2.Zero
			&& GameSettings.MouseCameraPanEnabled
			&& !IsMouseOverUi())
			flatPan = MouseEdgePan();

		var verticalPan = 0f;
		if (Input.IsActionPressed("battle_pan_vertical_up", true))
			verticalPan += 1f;
		if (Input.IsActionPressed("battle_pan_vertical_down", true))
			verticalPan -= 1f;

		if (flatPan == Vector2.Zero && verticalPan == 0f)
			return;

		NotifyManualInput();
		var deltaF = (float)delta;
		var panSpeed = OrbitControls.KeyboardPanSpeed;
		if (flatPan != Vector2.Zero)
		{
			var (right, forward) = OrbitPose.ViewPanAxes(GlobalTransform.Basis);
			_pose.ViewPan(flatPan, right, forward, panSpeed, deltaF);
		}

		if (verticalPan != 0f)
			_pose.VerticalPan(Mathf.Sign(verticalPan), panSpeed, deltaF);

		ApplyTransform();
	}

	public override void _Input(InputEvent @event)
	{
		if (GestureInputBlocked)
			return;

		switch (@event)
		{
			case InputEventMouseButton { Pressed: true } orbitStart
				when orbitStart.IsActionPressed("battle_camera_orbit", false, true):
				_orbiting = true;
				_lastMousePosition = orbitStart.Position;
				NotifyManualInput();
				break;

			case InputEventMouseButton orbitEnd
				when !orbitEnd.Pressed && orbitEnd.IsActionReleased("battle_camera_orbit", true):
				_orbiting = false;
				break;

			case InputEventMouseButton { Pressed: true } panStart
				when GameSettings.MouseCameraPanEnabled
					&& panStart.IsActionPressed("battle_camera_pan", false, true):
				_panning = true;
				_lastMousePosition = panStart.Position;
				NotifyManualInput();
				GetViewport().SetInputAsHandled();
				break;

			case InputEventMouseButton panEnd
				when !panEnd.Pressed && panEnd.IsActionReleased("battle_camera_pan", true):
				_panning = false;
				break;

			case InputEvent zoomIn
				when zoomIn.IsPressed() && zoomIn.IsActionPressed("battle_zoom_in", false, true):
				NotifyManualInput();
				_pose.Zoom(-OrbitControls.ZoomStep, Limits);
				ApplyTransform();
				GetViewport().SetInputAsHandled();
				break;

			case InputEvent zoomOut
				when zoomOut.IsPressed() && zoomOut.IsActionPressed("battle_zoom_out", false, true):
				NotifyManualInput();
				_pose.Zoom(OrbitControls.ZoomStep, Limits);
				ApplyTransform();
				GetViewport().SetInputAsHandled();
				break;

			case InputEventMouseMotion motion when _orbiting || _panning:
			{
				var delta = motion.Position - _lastMousePosition;
				_lastMousePosition = motion.Position;
				NotifyManualInput();

				if (_orbiting)
					_pose.Orbit(delta, OrbitControls.OrbitSensitivity, Limits);
				else
					_pose.ScreenPan(
						delta,
						GlobalTransform.Basis.X,
						GlobalTransform.Basis.Y,
						OrbitControls.PanSensitivity);

				ApplyTransform();
				GetViewport().SetInputAsHandled();
				break;
			}

		}
	}

	public bool GestureInputBlocked { get; private set; }

	public void SetGestureInputBlocked(bool blocked)
	{
		GestureInputBlocked = blocked;
		if (!blocked)
			return;

		_orbiting = false;
		_panning = false;
		CancelAutomation();
	}

	private void SetPivotInternal(Vector3 pivot)
	{
		_pose.Pivot = pivot;
		ApplyTransform();
	}

	private void OnPivotTweenFinished()
	{
		_pivotTween = null;
		_automationTarget = null;
	}

	private void NotifyManualInput()
	{
		CancelAutomation();
		ManualInputStarted?.Invoke();
	}

	private bool IsMouseOverUi() => GetViewport().GuiGetHoveredControl() is not null;

	private Vector2 MouseEdgePan()
	{
		var viewportRect = GetViewport().GetVisibleRect();
		var mousePosition = GetViewport().GetMousePosition();
		var pan = Vector2.Zero;

		if (mousePosition.X <= viewportRect.Position.X + OrbitControls.MousePanEdgeMargin)
			pan.X -= 1f;
		else if (mousePosition.X >= viewportRect.End.X - OrbitControls.MousePanEdgeMargin)
			pan.X += 1f;

		if (mousePosition.Y <= viewportRect.Position.Y + OrbitControls.MousePanEdgeMargin)
			pan.Y += 1f;
		else if (mousePosition.Y >= viewportRect.End.Y - OrbitControls.MousePanEdgeMargin)
			pan.Y -= 1f;

		return pan;
	}

	private bool AreVisibleWithCurrentTransform(IReadOnlyList<Vector3> points, float margin)
	{
		if (points.Count == 0)
			return true;

		ArgumentOutOfRangeException.ThrowIfNegative(margin);

		if (margin >= 0.5f)
			throw new ArgumentOutOfRangeException(
				nameof(margin),
				"Viewport margin must be below 0.5.");

		var viewportRect = GetViewport().GetVisibleRect();
		var inset = viewportRect.Size * margin;

		var safeRect = new Rect2(
			viewportRect.Position + inset,
			viewportRect.Size - inset * 2f);

		foreach (var worldPoint in points)
		{
			if (IsPositionBehind(worldPoint))
				return false;

			var screenPoint = UnprojectPosition(worldPoint);

			if (!safeRect.HasPoint(screenPoint))
				return false;
		}

		return true;
	}

	private Vector3 ComputeCentroid(IReadOnlyList<Vector3> points)
	{
		if (points.Count == 1)
			return points[0];

		var sum = Vector3.Zero;
		foreach (var point in points)
			sum += point;

		return sum / points.Count;
	}

	private Vector3 CapPivotShift(Vector3 desiredPivot)
	{
		var offset = desiredPivot - _pose.Pivot;
		var length = offset.Length();
		if (length <= MaxCorrectionShift || length < 0.001f)
			return desiredPivot;

		return _pose.Pivot + offset / length * MaxCorrectionShift;
	}

	private void ApplyTransform()
	{
		GlobalPosition = _pose.CameraPosition();
		LookAt(_pose.Pivot, Vector3.Up);
		if (_impactShakeLocalOffset.LengthSquared() > 0.000001f)
			GlobalPosition += GlobalTransform.Basis * _impactShakeLocalOffset;
	}
}
