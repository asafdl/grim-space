using Godot;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Math.Grid;

namespace GrimSpace.Dev;

public partial class GoopVfxPreview : Node3D
{
	private static readonly IReadOnlySet<Coord> PreviewCells = Enumerable
		.Range(0, 3)
		.SelectMany(x => Enumerable.Range(0, 3).Select(y => new Coord(x, y, 1)))
		.ToHashSet();
	private static readonly Coord Center = new(1, 1, 1);
	private static readonly Vector3 Target = WorldMapping.ToWorld(Center);
	private static readonly Vector3 Source = Target + Vector3.Back * 8f;

	private GoopBlobFieldView _field = null!;
	private Label _status = null!;
	private ulong _seed = 0x6A09E667F3BCC909UL;

	public override void _Ready()
	{
		var camera = GetNode<Camera3D>("Camera3D");
		camera.LookAt(Target, Vector3.Up);
		_status = GetNode<Label>("UI/Status");

		AddHazardGuide();
		AddSourceMarker();
		_field = new GoopBlobFieldView { Name = "PersistentGoopField" };
		AddChild(_field);
		UpdateStatus();
		CallDeferred(MethodName.Fire);
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } key)
			return;

		switch (key.Keycode)
		{
			case Key.Space:
				Fire();
				break;
			case Key.R:
				Reseed();
				break;
			default:
				return;
		}

		GetViewport().SetInputAsHandled();
	}

	private void Fire()
	{
		var seed = _seed;
		GoopSprayEffect.Play(this, Source, Target, seed);
		GetTree().CreateTimer(GoopSprayEffect.ImpactSeconds).Timeout +=
			() => _field.Reveal(PreviewCells, seed, Center);
	}

	private void Reseed()
	{
		_seed = unchecked(_seed + 0x9E3779B97F4A7C15UL);
		_field.Reveal(PreviewCells, _seed, Center);
		UpdateStatus();
	}

	private void UpdateStatus() =>
		_status.Text =
			$"Floating goop weapon preview  |  Perpendicular 3x3 hazard  |  Seed: 0x{_seed:X16}\n" +
			"Space: fire spray and reveal on impact    R: reveal a new deterministic layout";

	private void AddHazardGuide()
	{
		var material = new StandardMaterial3D
		{
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			AlbedoColor = new Color(0.12f, 0.24f, 0.32f, 0.2f),
			Roughness = 1f,
		};
		var mesh = new BoxMesh
		{
			Size = new Vector3(
				WorldMapping.CellSize * 3f - 0.08f,
				WorldMapping.CellSize * 3f - 0.08f,
				0.025f),
		};

		var guide = new MeshInstance3D
		{
			Name = "HazardPlaneGuide",
			Position = new Vector3(
				Target.X,
				Target.Y,
				Target.Z - WorldMapping.CellSize * 0.5f),
			Mesh = mesh,
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		PresentationLayers.MarkWorld(guide);
		AddChild(guide);
	}

	private void AddSourceMarker()
	{
		var marker = new MeshInstance3D
		{
			Name = "FiringSource",
			Position = Source,
			Mesh = new CapsuleMesh
			{
				Radius = 0.28f,
				Height = 1.2f,
				RadialSegments = 12,
				Rings = 4,
			},
			RotationDegrees = new Vector3(90f, 0f, 0f),
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = new Color(0.18f, 0.64f, 0.82f),
				Roughness = 0.35f,
				EmissionEnabled = true,
				Emission = new Color(0.08f, 0.28f, 0.4f),
				EmissionEnergyMultiplier = 0.3f,
			},
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		PresentationLayers.MarkWorld(marker);
		AddChild(marker);
	}
}
