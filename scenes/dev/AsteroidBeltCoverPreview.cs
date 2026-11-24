using Godot;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Math.Grid;

namespace GrimSpace.Dev;

public partial class AsteroidBeltCoverPreview : Node3D
{
	private static readonly Vector3 Center = WorldMapping.ToWorld(Coord.Zero);

	private Camera3D _camera = null!;
	private AsteroidBeltCoverView _lowCover = null!;
	private AsteroidBeltCoverView _highCover = null!;
	private int? _lowResistance;
	private int? _highResistance;
	private int _encounterSeed = 19;
	private float _angle;
	private float _elevation;
	private float _distance = 8.5f;

	public override void _Ready()
	{
		AddChild(SpaceBackdrop.CreateWorldEnvironment());
		AddChild(new DirectionalLight3D
		{
			RotationDegrees = new Vector3(-48f, -28f, 0f),
			LightEnergy = 1.8f,
		});

		_camera = new Camera3D { Current = true, Fov = 40f };
		AddChild(_camera);
		UpdateCamera();
		_lowCover = AddSample(AsteroidBelt.LowResistancePercent, new Vector3(-1.6f, 0f, 0f));
		_highCover = AddSample(AsteroidBelt.HighResistancePercent, new Vector3(1.6f, 0f, 0f));
		RefreshSamples();

		var overlay = new CanvasLayer();
		AddChild(overlay);
		var instructions = new Label
		{
			Position = new Vector2(24f, 24f),
			Text = "Asteroid belt cover\n"
				+ "Left/Right: orbit | Up/Down: elevation | Mouse wheel: zoom | H: hit | R: regenerate",
		};
		instructions.AddThemeFontSizeOverride("font_size", 22);
		overlay.AddChild(instructions);
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } key)
			return;

		switch (key.Keycode)
		{
			case Key.Left: _angle -= 0.3f; break;
			case Key.Right: _angle += 0.3f; break;
			case Key.Up: _elevation = Mathf.Min(1.3f, _elevation + 0.15f); break;
			case Key.Down: _elevation = Mathf.Max(-1.3f, _elevation - 0.15f); break;
			case Key.R:
				_encounterSeed = unchecked(_encounterSeed + 1);
				RefreshSamples();
				break;
			case Key.H:
				// Shots travel across the screen so the exit-side scatter stays readable.
				HitSample(_lowCover, ref _lowResistance, _camera.Basis.X);
				HitSample(_highCover, ref _highResistance, _camera.Basis.X);
				break;
			default: return;
		}

		UpdateCamera();
		GetViewport().SetInputAsHandled();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton { Pressed: true } mouse)
			return;

		switch (mouse.ButtonIndex)
		{
			case MouseButton.WheelUp: _distance = Mathf.Max(3f, _distance * 0.85f); break;
			case MouseButton.WheelDown: _distance = Mathf.Min(30f, _distance / 0.85f); break;
			default: return;
		}

		UpdateCamera();
		GetViewport().SetInputAsHandled();
	}

	private AsteroidBeltCoverView AddSample(int resistancePercent, Vector3 offset)
	{
		var cover = new AsteroidBeltCoverView
		{
			Name = $"Cover{resistancePercent}",
			Position = Center + offset,
		};
		AddChild(cover);
		return cover;
	}

	private void RefreshSamples()
	{
		_lowResistance = AsteroidBelt.LowResistancePercent;
		_highResistance = AsteroidBelt.HighResistancePercent;
		_lowCover.Apply(_lowResistance,
			CloudPattern.SeedFor(_encounterSeed, new Coord(-1, 0, 0)));
		_highCover.Apply(_highResistance,
			CloudPattern.SeedFor(_encounterSeed, new Coord(1, 0, 0)));
	}

	private static void HitSample(AsteroidBeltCoverView cover, ref int? resistance, Vector3 hitDirection)
	{
		if (resistance is null)
			return;

		var next = resistance == AsteroidBelt.HighResistancePercent
			? AsteroidBelt.LowResistancePercent
			: (int?)null;
		cover.AnimateHit(next, hitDirection);
		resistance = next;
	}

	private void UpdateCamera()
	{
		_camera.Position = Center + new Vector3(
			Mathf.Sin(_angle) * Mathf.Cos(_elevation),
			Mathf.Sin(_elevation),
			Mathf.Cos(_angle) * Mathf.Cos(_elevation)) * _distance;
		_camera.LookAt(Center);
	}
}
