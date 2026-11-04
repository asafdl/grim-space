using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class ProtectionDroneView : Node3D
{
	public const double FlightSeconds = 0.75;
	public const double FadeSeconds = 0.3;

	private UnitView _target = null!;
	private Vector3 _offset;
	private Vector3 _origin;
	private GeometryInstance3D[] _geometry = [];
	private double _elapsed;
	private double _flightElapsed = FlightSeconds;
	private double _fadeElapsed = -1;

	public void Attach(UnitView target, Vector3 offset)
	{
		_target = target;
		_offset = offset;
		TopLevel = true;
		var model = ScrapDroneMesh.CreateMountPreview();
		model.Name = "DroneModel";
		AddChild(model);
		_geometry = model.FindChildren("*", "GeometryInstance3D", true, false)
			.OfType<GeometryInstance3D>().ToArray();
		foreach (var geometry in _geometry)
			PresentationLayers.MarkWorld(geometry);
		SnapToHover();
	}

	public void LaunchFrom(Vector3 origin)
	{
		_origin = origin;
		_flightElapsed = 0;
		GlobalPosition = origin;
	}

	public void SnapToHover()
	{
		_flightElapsed = FlightSeconds;
		_fadeElapsed = -1;
		foreach (var geometry in _geometry)
			geometry.Transparency = 0;
		UpdatePose();
	}

	public void FadeOut()
	{
		if (_fadeElapsed < 0)
			_fadeElapsed = 0;
	}

	public override void _Process(double delta)
	{
		_elapsed += delta;
		_flightElapsed = System.Math.Min(FlightSeconds, _flightElapsed + delta);
		UpdatePose();
		if (_fadeElapsed < 0)
			return;

		_fadeElapsed += delta;
		var fade = (float)System.Math.Min(1, _fadeElapsed / FadeSeconds);
		foreach (var geometry in _geometry)
			geometry.Transparency = fade;
		if (fade >= 1)
			QueueFree();
	}

	private void UpdatePose()
	{
		// Move ghosts bind before entering the tree; _Process applies the pose afterward.
		if (!IsInsideTree() || !_target.IsInsideTree())
			return;

		var time = (float)_elapsed;
		var destination = _target.GlobalTransform * _offset
			+ Vector3.Up * (0.08f * Mathf.Sin(time * 3f));
		var progress = (float)(_flightElapsed / FlightSeconds);
		var weight = Mathf.SmoothStep(0, 1, progress);
		var position = _origin.Lerp(destination, weight)
			+ Vector3.Up * (0.6f * Mathf.Sin(Mathf.Pi * progress));
		GlobalPosition = position;
		var direction = destination - position;
		GlobalBasis = progress < 0.98f && direction.LengthSquared() > 0.001f
			&& System.Math.Abs(direction.Normalized().Dot(Vector3.Up)) < 0.99f
			? Basis.LookingAt(direction.Normalized(), Vector3.Up)
			: _target.GlobalBasis.Orthonormalized()
				* new Basis(Vector3.Up, 0.12f * Mathf.Sin(time * 1.7f));
	}
}
