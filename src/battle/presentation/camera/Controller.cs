using Godot;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Math.Camera;

namespace GrimSpace.Battle.Presentation.Camera;

public partial class Controller : Camera3D, ICameraRig
{
	public event Action? ManualInputStarted;

	private static readonly OrbitLimits Limits = new(
		MinDistance: 8f,
		MaxDistance: 280f,
		MinPitch: -0.3f,
		MaxPitch: 1.25f);

	private const float DefaultDistance = 22f;
	private const float PivotEpsilon = 0.01f;
	private const float MaxCorrectionShift = 28f;

	private OrbitPose _pose;
	private Vector2 _lastMousePosition;
	private bool _orbiting;
	private bool _panning;
	private Tween? _pivotTween;
	private Vector3? _automationTarget;

	public Vector3 Pivot => _pose.Pivot;
	public float Distance => _pose.Distance;
	public float Yaw => _pose.Yaw;
	public float Pitch => _pose.Pitch;
	public bool IsAutomationActive => _pivotTween is not null;
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
		var pan = Vector2.Zero;
		if (Input.IsKeyPressed(Key.W))
			pan.Y += 1f;
		if (Input.IsKeyPressed(Key.S))
			pan.Y -= 1f;
		if (Input.IsKeyPressed(Key.A))
			pan.X -= 1f;
		if (Input.IsKeyPressed(Key.D))
			pan.X += 1f;

		if (pan == Vector2.Zero)
			return;

		NotifyManualInput();
		pan = pan.Normalized();
		var (right, forward) = OrbitPose.FlatPanAxes(GlobalTransform.Basis);
		_pose.FlatPan(pan, right, forward, OrbitControls.KeyboardPanSpeed, (float)delta);
		ApplyTransform();
	}

	public override void _Input(InputEvent @event)
	{
		switch (@event)
		{
			case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right } mouseButton:
				_orbiting = true;
				_lastMousePosition = mouseButton.Position;
				NotifyManualInput();
				GetViewport().SetInputAsHandled();
				break;

			case InputEventMouseButton { Pressed: false, ButtonIndex: MouseButton.Right }:
				_orbiting = false;
				break;

			case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Middle } mouseButton:
				_panning = true;
				_lastMousePosition = mouseButton.Position;
				NotifyManualInput();
				GetViewport().SetInputAsHandled();
				break;

			case InputEventMouseButton { Pressed: false, ButtonIndex: MouseButton.Middle }:
				_panning = false;
				break;

			case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp }:
				NotifyManualInput();
				_pose.Zoom(-OrbitControls.ZoomStep, Limits);
				ApplyTransform();
				GetViewport().SetInputAsHandled();
				break;

			case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelDown }:
				NotifyManualInput();
				_pose.Zoom(OrbitControls.ZoomStep, Limits);
				ApplyTransform();
				GetViewport().SetInputAsHandled();
				break;

			case InputEventKey { Pressed: true, Echo: false, Keycode: Key.Equal or Key.KpAdd }:
				NotifyManualInput();
				_pose.Zoom(-OrbitControls.ZoomStep, Limits);
				ApplyTransform();
				GetViewport().SetInputAsHandled();
				break;

			case InputEventKey { Pressed: true, Echo: false, Keycode: Key.Minus or Key.KpSubtract }:
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
	}
}
