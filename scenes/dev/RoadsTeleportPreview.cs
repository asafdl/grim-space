using Godot;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Dev;

public partial class RoadsTeleportPreview : Node3D
{
	private PixelTeleportEffect _effect = null!;
	private Node3D _ship = null!;
	private Label _status = null!;
	private Tween? _animation;
	private bool _paused;
	private static readonly Vector3 Departure = new(-2, 0, 0);
	private static readonly Vector3 Arrival = new(2, 0, 0);

	public override void _Ready()
	{
		AddChild(new WorldEnvironment
		{
			Environment = new Godot.Environment
			{
				BackgroundMode = Godot.Environment.BGMode.Color,
				BackgroundColor = new Color(0.012f, 0.018f, 0.035f),
				AmbientLightSource = Godot.Environment.AmbientSource.Color,
				AmbientLightColor = new Color(0.65f, 0.72f, 0.85f),
				AmbientLightEnergy = 0.8f,
				GlowEnabled = true,
				GlowIntensity = 0.5f,
			},
		});
		AddChild(new DirectionalLight3D
		{
			RotationDegrees = new Vector3(-48, -28, 0),
			LightEnergy = 1.8f,
		});
		var camera = new Camera3D
		{
			Position = new Vector3(3, 4, 9),
			Current = true,
			Fov = 40,
		};
		AddChild(camera);
		camera.LookAt(Vector3.Zero);
		AddLandingCell(Departure);
		AddLandingCell(Arrival);
		_ship = new Node3D { Name = "TeleportingRogue", Scale = Vector3.One * 2 };
		AddChild(_ship);
		_ship.AddChild(ShipMesh.CreateHull(EType.Rogue));
		var bounds = BoundsUnder(_ship);
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
		{
			if (face == ESpatialOrientation.Retro)
				continue;
			_ship.AddChild(new MeshInstance3D
			{
				Name = $"Shield{face}",
				Mesh = ShieldBubbleMesh.CreateFace(bounds, face),
				MaterialOverride = ShieldFaceMaterials.Create(bounds, ShieldFaceMaterials.TeamTint(ETeam.Player)),
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			});
		}
		_effect = new PixelTeleportEffect();
		AddChild(_effect);
		var layer = new CanvasLayer();
		AddChild(layer);
		_status = new Label { Position = new Vector2(24, 24) };
		_status.AddThemeFontSizeOverride("font_size", 22);
		layer.AddChild(_status);
		Play();
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } key)
			return;
		switch (key.Keycode)
		{
			case Key.Space:
				Play();
				break;
			case Key.P:
				_paused = !_paused;
				if (_paused)
					_animation?.Pause();
				else
					_animation?.Play();
				_effect.SetPaused(_paused);
				break;
			default:
				return;
		}
		GetViewport().SetInputAsHandled();
	}

	private void Play()
	{
		_animation?.Kill();
		_effect.Stop();
		_paused = false;
		_ship.Position = Departure;
		SetPhase("Ready");
		_animation = CreateTween().SetLoops();
		_animation.TweenInterval(0.4);
		_animation.TweenCallback(Callable.From(() => Teleport(Arrival)));
		_animation.TweenInterval(PixelTeleportEffect.DurationSeconds + 0.45);
		_animation.TweenCallback(Callable.From(() => Teleport(Departure)));
		_animation.TweenInterval(PixelTeleportEffect.DurationSeconds);
	}

	private void Teleport(Vector3 destination)
	{
		SetPhase("Pixel dissolve");
		_effect.Play(_ship, () =>
		{
			_ship.Position = destination;
			SetPhase("Pixel reveal");
		});
	}

	private void SetPhase(string phase) =>
		_status.Text = "Roads: pixel teleport prototype (presentation only)\n"
			+ "Space: restart | P: pause/resume\n" + phase;

	private static Aabb BoundsUnder(Node3D root)
	{
		var found = false;
		var bounds = default(Aabb);
		foreach (var child in root.FindChildren("*", "MeshInstance3D", true, false))
		{
			if (child is not MeshInstance3D { Mesh: { } mesh } instance)
				continue;
			var transform = Transform3D.Identity;
			for (Node3D? node = instance; node is not null && node != root; node = node.GetParent() as Node3D)
				transform = node.Transform * transform;
			var meshBounds = transform * mesh.GetAabb();
			bounds = found ? bounds.Merge(meshBounds) : meshBounds;
			found = true;
		}

		return found
			? bounds
			: throw new InvalidOperationException("Teleport preview hull has no mesh.");
	}

	private void AddLandingCell(Vector3 position)
	{
		var material = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			AlbedoColor = new Color(0.45f, 0.3f, 0.8f),
		};
		var cell = new MeshInstance3D
		{
			Position = position + Vector3.Down * 0.45f,
			Mesh = new TorusMesh { InnerRadius = 0.65f, OuterRadius = 0.68f },
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		AddChild(cell);
	}
}
