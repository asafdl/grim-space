using Godot;
using GrimSpace.Education;
using GrimSpace.Math.Camera;

namespace GrimSpace.World.StarSystem.Presentation;

/// <summary>
/// Map orbit camera. Shared orbit/pan/zoom math; pivot clamped to the navigational XZ plane.
/// </summary>
public partial class MapCamera : Camera3D
{
	private static readonly OrbitLimits DefaultLimits = new(
		MinDistance: 4f,
		MaxDistance: 72f,
		MinPitch: 0.12f,
		MaxPitch: 1.15f);

	private static readonly PresentationInputPolicy DefaultInputPolicy = new(
		AllowsOrbit: true,
		AllowsPan: true,
		AllowsWheelZoom: true,
		AllowsMapMovement: true,
		AllowsStrategicHover: true);

	private const float DefaultDistance = 42f;
	private const float FovDegrees = 34f;

	private OrbitPose _pose = new()
	{
		Yaw = Mathf.DegToRad(-38f),
		Pitch = Mathf.DegToRad(25f),
		Distance = DefaultDistance,
	};

	private OrbitPose _capturedPose;
	private OrbitLimits _activeLimits = DefaultLimits;
	private OrbitLimits _tweenLimits = DefaultLimits;
	private PresentationInputPolicy _inputPolicy = DefaultInputPolicy;
	private Node3D _pivot = null!;
	private SpringArm3D _springArm = null!;
	private Vector3 _center;
	private float _boundsHalfX;
	private float _boundsHalfZ;
	private Vector2 _lastMousePosition;
	private bool _orbiting;
	private bool _facadeActive;
	private bool _domainBlocked;
	private bool _focusTween;
	private float _manualInputGraceRemaining;
	private Tween? _automationTween;
	private Action? _automationComplete;
	private readonly MapCameraFocusLeases _focusLeases = new();

	private const float ManualInputGrace = 0.75f;

	public float Distance => _pose.Distance;
	public OrbitPose CurrentPose => _pose;
	public OrbitPose CapturedPose => _capturedPose;
	public OrbitLimits ActiveLimits => _activeLimits;
	public bool IsAnimating => _automationTween is not null;
	public bool IsManualGestureActive => _orbiting || _manualInputGraceRemaining > 0f;
	public bool IsFacadeActive => _facadeActive;
	public bool ManualInputEnabled => !_domainBlocked;

	public override void _Ready()
	{
		_springArm = GetParent() as SpringArm3D
			?? throw new InvalidOperationException("MapCamera must be a direct child of SpringArm3D.");
		_pivot = _springArm.GetParent() as Node3D
			?? throw new InvalidOperationException("MapCamera SpringArm3D must be a child of a Node3D pivot.");
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

	public void ApplyDistanceDelta(float signedAmount)
	{
		if (!PrepareManualInput())
			return;

		NotifyManualInput();
		_pose.Zoom(signedAmount, _activeLimits);
		ApplyTransform();
	}

	public void CapturePose() => _capturedPose = _pose;

	public void SetCapturedPose(OrbitPose pose) => _capturedPose = pose;

	public void SetFacadeActive(bool active) => _facadeActive = active;

	public void ApplyLimits(OrbitLimits limits)
	{
		_activeLimits = limits;
		_pose.Clamp(_activeLimits);
		ApplyTransform();
	}

	public void ApplyInputPolicy(PresentationInputPolicy policy, bool domainBlocked)
	{
		_inputPolicy = policy;
		_domainBlocked = domainBlocked;
		if (!AllowsOrbitInput() && !AllowsPanInput())
			_orbiting = false;
	}

	public void SetManualInputEnabled(bool enabled) => ApplyInputPolicy(_inputPolicy, !enabled);

	public void SetOcclusionEnabled(bool enabled) =>
		_springArm.CollisionMask = enabled ? MapCameraOcclusion.CollisionMask : 0u;

	public IWorldFocusHandle BeginFocusLease(Action applyFocus)
	{
		var capturedPose = _pose;
		var generation = _focusLeases.Begin();
		applyFocus();
		return new FocusLease(this, generation, capturedPose);
	}

	public void SnapToPose(OrbitPose target, OrbitLimits limits)
	{
		SupersedeFocusLeases();
		CancelAutomation();
		_activeLimits = limits;
		target.Clamp(limits);
		_pose = target;
		ApplyTransform();
	}

	public void SnapToPose(OrbitPose target) => SnapToPose(target, _activeLimits);

	public void TweenToPose(OrbitPose target, OrbitLimits limits, Action? onComplete = null)
	{
		CancelAutomation();
		target.Clamp(limits);
		BeginPoseTween(
			_pose,
			target,
			limits,
			CameraTransition.Duration(_pose, target),
			onComplete);
	}

	public void TweenToPose(OrbitPose target, OrbitLimits limits, float duration, Action? onComplete = null)
	{
		CancelAutomation();
		target.Clamp(limits);
		BeginPoseTween(_pose, target, limits, duration, onComplete);
	}

	public void TweenToPose(OrbitPose target, Action? onComplete = null) =>
		TweenToPose(target, _activeLimits, onComplete);

	public void TweenToPose(OrbitPose target, float duration, Action? onComplete = null) =>
		TweenToPose(target, _activeLimits, duration, onComplete);

	public void FocusPivot(Vector3 pivot)
	{
		CancelAutomation();
		var target = _pose;
		target.Pivot = pivot;
		_focusTween = true;
		BeginPoseTween(
			_pose,
			target,
			_activeLimits,
			CameraTransition.Duration(_pose, target),
			null,
			supersedeFocusLeases: false);
	}

	public void FocusPivot(Vector3 pivot, float duration)
	{
		CancelAutomation();
		var target = _pose;
		target.Pivot = pivot;
		_focusTween = true;
		BeginPoseTween(
			_pose,
			target,
			_activeLimits,
			duration,
			null,
			supersedeFocusLeases: false);
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
		BeginPoseTween(_pose, target, _activeLimits, duration, onComplete);
	}

	public void RestoreCapturedPose(Action? onComplete = null, float minDistance = 0f)
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
		BeginPoseTween(
			_pose,
			target,
			_activeLimits,
			CameraTransition.Duration(_pose, target),
			onComplete);
	}

	public void CancelAutomation()
	{
		if (_automationTween is null)
			return;

		_automationTween.Kill();
		_automationTween = null;
		_automationComplete = null;
		_focusTween = false;
	}

	public void MovePivotToward(Vector3 target, float delta, float responseTime)
	{
		if (IsAnimating)
			return;

		SupersedeFocusLeases();

		if (responseTime <= 0f)
			_pose.Pivot = target;
		else
		{
			var t = Mathf.Clamp(delta / responseTime, 0f, 1f);
			_pose.Pivot += (target - _pose.Pivot) * t;
		}

		ClampPivotToMap();
		ApplyTransform();
	}

	public void MoveTowardPose(OrbitPose target, float delta, float responseTime)
	{
		if (IsAnimating)
			return;

		SupersedeFocusLeases();

		var t = responseTime <= 0f ? 1f : Mathf.Clamp(delta / responseTime, 0f, 1f);
		_pose.Pivot = _pose.Pivot.Lerp(target.Pivot, t);
		_pose.Distance = Mathf.Lerp(_pose.Distance, target.Distance, t);
		_pose.Yaw = Mathf.LerpAngle(_pose.Yaw, target.Yaw, t);
		_pose.Pitch = Mathf.Lerp(_pose.Pitch, target.Pitch, t);
		ClampPivotToMap();
		ApplyTransform();
	}

	public override void _Process(double delta)
	{
		if (_manualInputGraceRemaining > 0f)
			_manualInputGraceRemaining = Mathf.Max(0f, _manualInputGraceRemaining - (float)delta);

		if (!AllowsPanInput())
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

		if (!PrepareManualInput())
			return;

		NotifyManualInput();
		pan = pan.Normalized();
		var (right, forward) = OrbitPose.FlatPanAxes(GlobalTransform.Basis);
		_pose.FlatPan(pan, right, forward, OrbitControls.KeyboardPanSpeed, (float)delta);
		ClampPivotToMap();
		ApplyTransform();
	}

	public override void _Input(InputEvent @event)
	{
		if (_domainBlocked)
		{
			_orbiting = false;
			return;
		}

		switch (@event)
		{
			case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right } mouseButton:
				if (IsMouseOverUi() || !AllowsOrbitInput() || !PrepareManualInput())
					break;
				NotifyManualInput();
				_orbiting = true;
				_lastMousePosition = mouseButton.Position;
				GetViewport().SetInputAsHandled();
				break;

			case InputEventMouseButton { Pressed: false, ButtonIndex: MouseButton.Right }:
				_orbiting = false;
				break;

			case InputEventMouseMotion motion when _orbiting && AllowsOrbitInput():
			{
				if (!PrepareManualInput())
					break;

				NotifyManualInput();
				var delta = motion.Position - _lastMousePosition;
				_lastMousePosition = motion.Position;
				_pose.Orbit(delta, OrbitControls.OrbitSensitivity, _activeLimits);
				ApplyTransform();
				GetViewport().SetInputAsHandled();
				break;
			}
		}
	}

	private void BeginPoseTween(
		OrbitPose start,
		OrbitPose target,
		OrbitLimits limits,
		float duration,
		Action? onComplete,
		bool supersedeFocusLeases = true)
	{
		if (supersedeFocusLeases)
			SupersedeFocusLeases();

		_tweenLimits = limits;
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
		_focusTween = false;
		_pose.Clamp(_tweenLimits);
		_activeLimits = _tweenLimits;
		ApplyTransform();
		var complete = _automationComplete;
		_automationComplete = null;
		complete?.Invoke();
	}

	private bool UsesLegacyFacadeInputBlock() =>
		_facadeActive && _inputPolicy == DefaultInputPolicy;

	private bool AllowsOrbitInput() =>
		!_domainBlocked && _inputPolicy.AllowsOrbit && !UsesLegacyFacadeInputBlock();

	private bool AllowsPanInput() =>
		!_domainBlocked && _inputPolicy.AllowsPan && !UsesLegacyFacadeInputBlock();

	private bool IsMouseOverUi() => GetViewport().GuiGetHoveredControl() is not null;

	private bool PrepareManualInput()
	{
		if (!IsAnimating)
			return true;

		if (!_focusTween)
			return false;

		CancelAutomation();
		return true;
	}

	private void NotifyManualInput()
	{
		SupersedeFocusLeases();
		_manualInputGraceRemaining = ManualInputGrace;
	}

	private void SupersedeFocusLeases() => _focusLeases.Supersede();

	private sealed class FocusLease(MapCamera camera, int generation, OrbitPose capturedPose)
		: IWorldFocusHandle
	{
		private MapCamera? _camera = camera;

		public void Dispose()
		{
			if (_camera is null || !_camera._focusLeases.IsCurrent(generation))
			{
				_camera = null;
				return;
			}

			if (GodotObject.IsInstanceValid(_camera))
				_camera.TweenToPose(capturedPose);
			_camera = null;
		}
	}

	private void ClampPivotToMap()
	{
		_pose.Pivot = new Vector3(
			Mathf.Clamp(_pose.Pivot.X, _center.X - _boundsHalfX, _center.X + _boundsHalfX),
			0f,
			Mathf.Clamp(_pose.Pivot.Z, _center.Z - _boundsHalfZ, _center.Z + _boundsHalfZ));
	}

	private void ApplyTransform()
	{
		_pivot.GlobalPosition = _pose.Pivot;
		_springArm.Rotation = new Vector3(-_pose.Pitch, _pose.Yaw, 0f);
		_springArm.SpringLength = _pose.Distance;
	}
}
