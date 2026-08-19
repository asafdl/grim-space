using Godot;
using GrimSpace.Math.Camera;

namespace GrimSpace.World.StarSystem.Presentation;

/// <summary>
/// Map orbit camera. Shared orbit/pan/zoom math; pivot clamped to the navigational XZ plane.
/// </summary>
public partial class MapCamera : Camera3D
{
	private static readonly OrbitLimits Limits = new(
		MinDistance: 18f,
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

	private Vector3 _center;
	private float _boundsHalfX;
	private float _boundsHalfZ;
	private Vector2 _lastMousePosition;
	private bool _orbiting;
	private bool _panning;

	public float Distance => _pose.Distance;

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

	public override void _Process(double delta)
	{
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
				_pose.Zoom(-OrbitControls.ZoomStep, Limits);
				ApplyTransform();
				GetViewport().SetInputAsHandled();
				break;

			case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelDown }:
				_pose.Zoom(OrbitControls.ZoomStep, Limits);
				ApplyTransform();
				GetViewport().SetInputAsHandled();
				break;

			case InputEventMouseMotion motion when _orbiting || _panning:
			{
				var delta = motion.Position - _lastMousePosition;
				_lastMousePosition = motion.Position;

				if (_orbiting)
					_pose.Orbit(delta, OrbitControls.OrbitSensitivity, Limits);
				else
				{
					_pose.ScreenPan(
						delta,
						GlobalTransform.Basis.X,
						GlobalTransform.Basis.Y,
						OrbitControls.PanSensitivity);
					ClampPivotToMap();
				}

				ApplyTransform();
				GetViewport().SetInputAsHandled();
				break;
			}
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
		GlobalPosition = _pose.CameraPosition();
		LookAt(_pose.Pivot, Vector3.Up);
	}
}
