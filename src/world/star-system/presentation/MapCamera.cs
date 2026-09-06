using Godot;
using GrimSpace.Math.Camera;

namespace GrimSpace.World.StarSystem.Presentation;

/// <summary>
/// Map orbit camera. Shared orbit/pan/zoom math; pivot clamped to the navigational XZ plane.
/// </summary>
public partial class MapCamera : Camera3D
{
	private static readonly OrbitLimits Limits = new(
		MinDistance: 4f,
		MaxDistance: 72f,
		MinPitch: 0.12f,
		MaxPitch: 1.15f);

	private const float DefaultDistance = 42f;
	private const float FovDegrees = 34f;

	private OrbitPose _pose = new()
	{
		Yaw = Mathf.DegToRad(-38f),
		Pitch = Mathf.DegToRad(25f),
		Distance = DefaultDistance,
	};

	private OrbitPose _capturedPose;
	private Vector3 _center;
	private float _boundsHalfX;
	private float _boundsHalfZ;
	private Vector2 _lastMousePosition;
	private bool _orbiting;
	private bool _facadeActive;
	private Tween? _automationTween;
	private Action? _automationComplete;

	public float Distance => _pose.Distance;
	public OrbitPose CurrentPose => _pose;
	public OrbitPose CapturedPose => _capturedPose;
	public bool IsAnimating => _automationTween is not null;
	public bool IsFacadeActive => _facadeActive;

	public override void _Ready()
	{
		Projection = ProjectionType.Perspective;
		Fov = FovDegrees;
		Near = 0.2f;
		Far = 400f;
		Current = true;
	}

	public void Configure(Vector3 center, float boundsHalfX, float boundsHalfZ)
	{
		_center = center;
		_boundsHalfX = boundsHalfX;
		_boundsHalfZ = boundsHalfZ;
		_pose.Pivot = center;
		ClampPivotToMap();
		ApplyTransform();
	}

	public void CapturePose() => _capturedPose = _pose;

	public void SetCapturedPose(OrbitPose pose) => _capturedPose = pose;

	public void SetFacadeActive(bool active) => _facadeActive = active;

	public void SnapToPose(OrbitPose target)
	{
		CancelAutomation();
		target.Clamp(Limits);
		_pose = target;
		ApplyTransform();
	}

	public void TweenToPose(OrbitPose target, float duration, Action? onComplete = null)
	{
		CancelAutomation();
		target.Clamp(Limits);
		BeginPoseTween(_pose, target, duration, onComplete);
	}

	public void RestoreCapturedPose(float duration, Action? onComplete = null, float minDistance = 0f)
	{
		CancelAutomation();
		var target = _capturedPose;
		if (target.Distance <= 0.001f)
		{
			onComplete?.Invoke();
			return;
		}

		if (minDistance > 0f && target.Distance < minDistance)
			target.Distance = minDistance;
		BeginPoseTween(_pose, target, duration, onComplete);
	}

	public void CancelAutomation()
	{
		if (_automationTween is null)
			return;

		_automationTween.Kill();
		_automationTween = null;
		_automationComplete = null;
	}

	public override void _Process(double delta)
	{
		if (IsAnimating || _facadeActive)
			return;

		var pan = Vector2.Zero;
		if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up))
			pan.Y += 1f;
		if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down))
			pan.Y -= 1f;
		if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left))
			pan.X -= 1f;
		if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right))
			pan.X += 1f;

		if (pan == Vector2.Zero)
			return;

		pan = pan.Normalized();
		var (right, forward) = OrbitPose.FlatPanAxes(GlobalTransform.Basis);
		_pose.FlatPan(pan, right, forward, OrbitControls.KeyboardPanSpeed, (float)delta);
		ClampPivotToMap();
		ApplyTransform();
	}

	public override void _Input(InputEvent @event)
	{
		if (IsAnimating)
			return;

		switch (@event)
		{
			case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouseButton:
				if (IsMouseOverUi() || _facadeActive)
					break;
				_orbiting = true;
				_lastMousePosition = mouseButton.Position;
				GetViewport().SetInputAsHandled();
				break;

			case InputEventMouseButton { Pressed: false, ButtonIndex: MouseButton.Left }:
				_orbiting = false;
				break;

			case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp }:
				if (IsMouseOverUi() || _facadeActive)
					break;
				_pose.Zoom(-OrbitControls.ZoomStep, Limits);
				ApplyTransform();
				GetViewport().SetInputAsHandled();
				break;

			case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelDown }:
				if (IsMouseOverUi())
					break;
				_pose.Zoom(OrbitControls.ZoomStep, Limits);
				ApplyTransform();
				GetViewport().SetInputAsHandled();
				break;

			case InputEventMouseMotion motion when _orbiting && !_facadeActive:
			{
				var delta = motion.Position - _lastMousePosition;
				_lastMousePosition = motion.Position;
				_pose.Orbit(delta, OrbitControls.OrbitSensitivity, Limits);
				ApplyTransform();
				GetViewport().SetInputAsHandled();
				break;
			}
		}
	}

	private void BeginPoseTween(OrbitPose start, OrbitPose target, float duration, Action? onComplete)
	{
		var startPivot = start.Pivot;
		var startDistance = start.Distance;
		var startYaw = start.Yaw;
		var startPitch = start.Pitch;
		_automationComplete = onComplete;

		_automationTween = CreateTween();
		_automationTween.TweenMethod(
				Callable.From<float>(BlendPose),
				0f,
				1f,
				duration)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
		_automationTween.Finished += OnAutomationFinished;

		void BlendPose(float t)
		{
			_pose.Pivot = startPivot.Lerp(target.Pivot, t);
			_pose.Distance = Mathf.Lerp(startDistance, target.Distance, t);
			_pose.Yaw = Mathf.LerpAngle(startYaw, target.Yaw, t);
			_pose.Pitch = Mathf.Lerp(startPitch, target.Pitch, t);
			ApplyTransform();
		}
	}

	private void OnAutomationFinished()
	{
		_automationTween = null;
		var complete = _automationComplete;
		_automationComplete = null;
		complete?.Invoke();
	}

	private bool IsMouseOverUi() => GetViewport().GuiGetHoveredControl() is not null;

	private void ClampPivotToMap()
	{
		_pose.Pivot = new Vector3(
			Mathf.Clamp(_pose.Pivot.X, _center.X - _boundsHalfX, _center.X + _boundsHalfX),
			0f,
			Mathf.Clamp(_pose.Pivot.Z, _center.Z - _boundsHalfZ, _center.Z + _boundsHalfZ));
	}

	private void ApplyTransform()
	{
		GlobalPosition = _pose.CameraPosition();
		LookAt(_pose.Pivot, Vector3.Up);
	}
}
