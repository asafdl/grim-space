using Godot;

namespace GrimSpace.World.StarSystem.Presentation;

public partial class MapCamera : Camera3D
{
	private const float MinSize = 8f;
	private const float MaxSize = 48f;
	private const float ZoomStep = 1.5f;
	private const float PanSpeed = 18f;
	private const float DragPanScale = 0.02f;

	private readonly float _yaw = Mathf.DegToRad(-55f);
	private readonly float _pitch = Mathf.DegToRad(-28f);
	private Vector3 _center;
	private Vector3 _pivot;
	private float _boundsHalfX;
	private float _boundsHalfZ;
	private bool _dragging;

	public override void _Ready()
	{
		Projection = ProjectionType.Orthogonal;
		Size = 22f;
		Current = true;
	}

	public void Configure(Vector3 center, float boundsHalfX, float boundsHalfZ)
	{
		_center = center;
		_pivot = center;
		_boundsHalfX = boundsHalfX;
		_boundsHalfZ = boundsHalfZ;
		ApplyTransform();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		switch (@event)
		{
			case InputEventMouseButton { ButtonIndex: MouseButton.Middle, Pressed: true }:
			case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }:
				_dragging = true;
				GetViewport().SetInputAsHandled();
				break;
			case InputEventMouseButton { ButtonIndex: MouseButton.Middle, Pressed: false }:
			case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }:
				_dragging = false;
				break;
			case InputEventMouseMotion motion when _dragging:
			{
				var right = Flat(GlobalTransform.Basis.X);
				var forward = Flat(GlobalTransform.Basis.Z);
				_pivot -= right * motion.Relative.X * Size * DragPanScale;
				_pivot -= forward * motion.Relative.Y * Size * DragPanScale;
				ApplyTransform();
				GetViewport().SetInputAsHandled();
				break;
			}
			case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true }:
				Size = Mathf.Max(MinSize, Size - ZoomStep);
				ApplyTransform();
				GetViewport().SetInputAsHandled();
				break;
			case InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, Pressed: true }:
				Size = Mathf.Min(MaxSize, Size + ZoomStep);
				ApplyTransform();
				GetViewport().SetInputAsHandled();
				break;
		}
	}

	public override void _Process(double delta)
	{
		var move = Vector3.Zero;
		if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left))
			move.X -= 1f;
		if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right))
			move.X += 1f;
		if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up))
			move.Z -= 1f;
		if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down))
			move.Z += 1f;

		if (move == Vector3.Zero)
			return;

		var right = Flat(GlobalTransform.Basis.X);
		var forward = Flat(-GlobalTransform.Basis.Z);
		_pivot += (right * move.X + forward * move.Z).Normalized() * PanSpeed * (float)delta * (Size / 20f);
		ApplyTransform();
	}

	private void ApplyTransform()
	{
		_pivot.X = Mathf.Clamp(_pivot.X, _center.X - _boundsHalfX, _center.X + _boundsHalfX);
		_pivot.Z = Mathf.Clamp(_pivot.Z, _center.Z - _boundsHalfZ, _center.Z + _boundsHalfZ);
		_pivot.Y = 0f;

		var offset = new Vector3(0f, 0f, Size * 1.35f);
		var basis = Basis.Identity.Rotated(Vector3.Up, _yaw).Rotated(Vector3.Right, _pitch);
		GlobalPosition = _pivot + basis * offset;
		LookAt(_pivot, Vector3.Up);
	}

	private static Vector3 Flat(Vector3 v)
	{
		v.Y = 0f;
		return v.LengthSquared() < 0.0001f ? Vector3.Forward : v.Normalized();
	}
}
