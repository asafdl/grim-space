using Godot;
using GrimSpace.Battle.Presentation.Graphics;

namespace GrimSpace.Battle.Presentation.Camera;

public partial class Controller : Camera3D, ICameraRig
{
	public event Action? ManualInputStarted;

	private const float OrbitSensitivity = 0.004f;
	private const float PanSensitivity = 0.025f;
	private const float KeyboardPanSpeed = 28f;
	private const float ZoomStep = 1.5f;
	private const float DefaultDistance = 22f;
	private const float MinDistance = 8f;
	private const float MaxDistance = 280f;
	private const float MinPitch = -0.3f;
	private const float MaxPitch = 1.25f;
	private const float PivotEpsilon = 0.01f;
	private const float MaxCorrectionShift = 28f;

	private Vector3 _pivot;
	private float _yaw;
	private float _pitch;
	private float _defaultYaw;
	private float _defaultPitch;
	private float _distance;
	private Vector2 _lastMousePosition;
	private bool _orbiting;
	private bool _panning;
	private Tween? _pivotTween;
	private Vector3? _automationTarget;

	public Vector3 Pivot => _pivot;
	public bool IsAutomationActive => _pivotTween is not null;
	public Vector3? AutomationTarget => _automationTarget;

	public void SetPivot(Vector3 pivot)
	{
		CancelAutomation();
		_pivot = pivot;
		ApplyTransform();
	}

	public bool AreVisible(IReadOnlyList<Vector3> points, float margin) =>
		AreVisibleWithCurrentTransform(points, margin);

	public bool AreVisibleAtPivot(Vector3 pivot, IReadOnlyList<Vector3> points, float margin)
	{
		var savedPivot = _pivot;
		ApplyPose(pivot);
		var visible = AreVisibleWithCurrentTransform(points, margin);
		ApplyPose(savedPivot);
		return visible;
	}

	public bool TryCalculateCorrection(
		IReadOnlyList<Vector3> points,
		float margin,
		out Vector3 targetPivot)
	{
		targetPivot = _pivot;

		if (points.Count == 0)
			return false;

		if (AreVisibleWithCurrentTransform(points, margin))
			return false;

		targetPivot = CapPivotShift(ComputeCentroid(points));
		return (_pivot - targetPivot).LengthSquared() > PivotEpsilon * PivotEpsilon;
	}

	public void TweenPivotTo(Vector3 target, float duration)
	{
		CancelAutomation();

		if ((_pivot - target).LengthSquared() <= PivotEpsilon * PivotEpsilon)
			return;

		_automationTarget = target;
		_pivotTween = CreateTween();
		_pivotTween.TweenMethod(
			Callable.From<Vector3>(SetPivotInternal),
			_pivot,
			target,
			duration)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
		_pivotTween.Finished += OnPivotTweenFinished;
	}

	public void MovePivotToward(Vector3 target, float delta, float responseTime)
	{
		if (responseTime <= 0f)
		{
			_pivot = target;
			ApplyTransform();
			return;
		}

		var t = Mathf.Clamp(delta / responseTime, 0f, 1f);
		_pivot += (target - _pivot) * t;
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

		_pivot = new Vector3(
			WorldMapping.CellSize * 4f,
			WorldMapping.CellSize * 4f,
			WorldMapping.CellSize * 4f);
		SyncFromTransform();
		_distance = DefaultDistance;
		_defaultYaw = _yaw;
		_defaultPitch = _pitch;
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
		var right = GlobalTransform.Basis.X;
		var forward = -GlobalTransform.Basis.Z;
		forward.Y = 0f;
		right.Y = 0f;
		if (forward.LengthSquared() > 0.001f)
			forward = forward.Normalized();
		if (right.LengthSquared() > 0.001f)
			right = right.Normalized();

		_pivot += (right * pan.X + forward * pan.Y) * KeyboardPanSpeed * (float)delta;
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
				Zoom(-ZoomStep);
				GetViewport().SetInputAsHandled();
				break;

			case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelDown }:
				NotifyManualInput();
				Zoom(ZoomStep);
				GetViewport().SetInputAsHandled();
				break;

			case InputEventKey { Pressed: true, Echo: false, Keycode: Key.Equal or Key.KpAdd }:
				NotifyManualInput();
				Zoom(-ZoomStep);
				GetViewport().SetInputAsHandled();
				break;

			case InputEventKey { Pressed: true, Echo: false, Keycode: Key.Minus or Key.KpSubtract }:
				NotifyManualInput();
				Zoom(ZoomStep);
				GetViewport().SetInputAsHandled();
				break;

			case InputEventMouseMotion motion when _orbiting || _panning:
				var delta = motion.Position - _lastMousePosition;
				_lastMousePosition = motion.Position;
				NotifyManualInput();

				if (_orbiting)
					Orbit(delta);
				else
					Pan(delta);

				GetViewport().SetInputAsHandled();
				break;
		}
	}

	private void SetPivotInternal(Vector3 pivot)
	{
		_pivot = pivot;
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
		var offset = desiredPivot - _pivot;
		var length = offset.Length();
		if (length <= MaxCorrectionShift || length < 0.001f)
			return desiredPivot;

		return _pivot + offset / length * MaxCorrectionShift;
	}

	private void ApplyPose(Vector3 pivot)
	{
		_pivot = pivot;
		ApplyTransform();
	}

	private Vector3 OrbitOffset() =>
		new Vector3(
			Mathf.Cos(_pitch) * Mathf.Sin(_yaw),
			Mathf.Sin(_pitch),
			Mathf.Cos(_pitch) * Mathf.Cos(_yaw)) * _distance;

	private void SyncFromTransform()
	{
		var offset = GlobalPosition - _pivot;
		_distance = Mathf.Clamp(offset.Length(), MinDistance, MaxDistance);

		if (_distance < 0.001f)
		{
			_distance = 25f;
			_pitch = -0.5f;
			_yaw = 0.8f;
		}
		else
		{
			var dir = offset / _distance;
			_pitch = Mathf.Asin(dir.Y);
			_yaw = Mathf.Atan2(dir.X, dir.Z);
		}

		ApplyTransform();
	}

	private void Orbit(Vector2 delta)
	{
		_yaw -= delta.X * OrbitSensitivity;
		_pitch = Mathf.Clamp(_pitch - delta.Y * OrbitSensitivity, MinPitch, MaxPitch);
		ApplyTransform();
	}

	private void Pan(Vector2 delta)
	{
		var right = GlobalTransform.Basis.X;
		var up = GlobalTransform.Basis.Y;
		_pivot -= right * delta.X * PanSensitivity;
		_pivot += up * delta.Y * PanSensitivity;
		ApplyTransform();
	}

	private void Zoom(float amount)
	{
		_distance = Mathf.Clamp(_distance + amount, MinDistance, MaxDistance);
		ApplyTransform();
	}

	private void ApplyTransform()
	{
		GlobalPosition = _pivot + OrbitOffset();
		LookAt(_pivot, Vector3.Up);
	}
}
