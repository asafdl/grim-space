using Godot;
using GrimSpace.Battle.Presentation.Graphics;

namespace GrimSpace.Battle.Presentation.Camera;

public partial class Controller : Camera3D
{
	private const float OrbitSensitivity = 0.004f;
	private const float PanSensitivity = 0.025f;
	private const float KeyboardPanSpeed = 28f;
	private const float ZoomStep = 1.5f;
	private const float DefaultDistance = 22f;
	private const float MinDistance = 8f;
	private const float MaxDistance = 280f;
	private const float MinPitch = -0.3f;
	private const float MaxPitch = 1.25f;

	private Vector3 _pivot;
	private float _yaw;
	private float _pitch;
	private float _defaultYaw;
	private float _defaultPitch;
	private float _distance;
	private Vector2 _lastMousePosition;
	private bool _orbiting;
	private bool _panning;

	public void SetPivot(Vector3 pivot)
	{
		_pivot = pivot;
		ApplyTransform();
	}

	public void FocusOn(Vector3 worldPosition)
	{
		_pivot = worldPosition;
		_yaw = _defaultYaw;
		_pitch = _defaultPitch;
		ApplyTransform();
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
				GetViewport().SetInputAsHandled();
				break;

			case InputEventMouseButton { Pressed: false, ButtonIndex: MouseButton.Right }:
				_orbiting = false;
				break;

			case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Middle } mouseButton:
				_panning = true;
				_lastMousePosition = mouseButton.Position;
				GetViewport().SetInputAsHandled();
				break;

			case InputEventMouseButton { Pressed: false, ButtonIndex: MouseButton.Middle }:
				_panning = false;
				break;

			case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp }:
				Zoom(-ZoomStep);
				GetViewport().SetInputAsHandled();
				break;

			case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelDown }:
				Zoom(ZoomStep);
				GetViewport().SetInputAsHandled();
				break;

			case InputEventKey { Pressed: true, Echo: false, Keycode: Key.Equal or Key.KpAdd }:
				Zoom(-ZoomStep);
				GetViewport().SetInputAsHandled();
				break;

			case InputEventKey { Pressed: true, Echo: false, Keycode: Key.Minus or Key.KpSubtract }:
				Zoom(ZoomStep);
				GetViewport().SetInputAsHandled();
				break;

			case InputEventMouseMotion motion when _orbiting || _panning:
				var delta = motion.Position - _lastMousePosition;
				_lastMousePosition = motion.Position;

				if (_orbiting)
					Orbit(delta);
				else
					Pan(delta);

				GetViewport().SetInputAsHandled();
				break;
		}
	}

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
		var offset = new Vector3(
			Mathf.Cos(_pitch) * Mathf.Sin(_yaw),
			Mathf.Sin(_pitch),
			Mathf.Cos(_pitch) * Mathf.Cos(_yaw)) * _distance;

		GlobalPosition = _pivot + offset;
		LookAt(_pivot, Vector3.Up);
	}
}
