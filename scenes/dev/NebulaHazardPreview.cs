using Godot;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Specs;

namespace GrimSpace.Dev;

public partial class NebulaHazardPreview : Node3D
{
	private const string NebulaPath = "res://assets/textures/messier_17.jpg";
	private const string CloudPath = "res://assets/models/spaceobjects/cloud.glb";
	private static readonly Vector3 Center = WorldMapping.ToWorld(Coord.Zero);

	private Camera3D _camera = null!;
	private float _angle;
	private float _elevation;
	private float _distance = 12f;

	public override void _Ready()
	{
		AddChild(SpaceBackdrop.CreateWorldEnvironment());
		AddChild(new DirectionalLight3D
		{
			RotationDegrees = new Vector3(-48, -28, 0),
			LightEnergy = 1.8f,
		});
		_camera = new Camera3D { Current = true, Fov = 40 };
		AddChild(_camera);
		UpdateCamera();

		var nebula = GD.Load<Texture2D>(NebulaPath)
			?? throw new InvalidOperationException($"Could not load nebula texture '{NebulaPath}'.");
		var purple = GD.Load<Texture2D>(CryoPlumeView.PurpleNebulaPath)
			?? throw new InvalidOperationException($"Could not load nebula texture '{CryoPlumeView.PurpleNebulaPath}'.");
		var textures = new[] { nebula, purple };
		var model = GD.Load<PackedScene>(CloudPath)
			?? throw new InvalidOperationException($"Could not load cloud model '{CloudPath}'.");
		var materials = new List<ShaderMaterial>();
		var clouds = new List<(MeshInstance3D Instance, CloudFragmentMesh Fragments, int Slot, ShaderMaterial[] Materials)>();
		var fragmentMeshes = new Dictionary<Mesh, CloudFragmentMesh>();
		var seedLabels = new List<Label3D>();
		for (var slot = 0; slot < 6; slot++)
		{
			var cloudCenter = Center + new Vector3((slot % 3 - 1) * 3.2f, (0.5f - slot / 3) * 2.8f, 0f);
			var scene = model.Instantiate<Node3D>();
			try
			{
				var meshes = scene.FindChildren("*", "MeshInstance3D", true, false)
					.OfType<MeshInstance3D>().ToArray();
				if (meshes.Length == 0)
					throw new InvalidOperationException($"Cloud model '{CloudPath}' has no meshes.");

				var bounds = default(Aabb);
				for (var i = 0; i < meshes.Length; i++)
				{
					if (meshes[i].Mesh is not { } mesh)
						throw new InvalidOperationException($"Cloud model '{CloudPath}' has an empty mesh.");
					var transform = Transform3D.Identity;
					for (Node3D? node = meshes[i]; node is not null; node = node.GetParent() as Node3D)
					{
						transform = node.Transform * transform;
						if (node == scene)
							break;
					}
					var partBounds = transform * mesh.GetAabb();
					bounds = i == 0 ? partBounds : bounds.Merge(partBounds);
				}
				var extent = Mathf.Max(bounds.Size.X, Mathf.Max(bounds.Size.Y, bounds.Size.Z));
				if (extent <= 0f)
					throw new InvalidOperationException($"Cloud model '{CloudPath}' has no extent.");

				foreach (var meshInstance in meshes)
				{
					var mesh = meshInstance.Mesh;
					var meshMaterials = new List<ShaderMaterial>();
					for (var surface = 0; surface < mesh.GetSurfaceCount(); surface++)
					{
						if (meshInstance.GetActiveMaterial(surface) is not StandardMaterial3D source)
							throw new InvalidOperationException($"Cloud model '{CloudPath}' has an unsupported material.");
						var material = new ShaderMaterial { Shader = CryoPlumeView.IonShader };
						material.SetShaderParameter("nebula_texture", textures[0]);
						material.SetShaderParameter("cloud_texture", source.AlbedoTexture);
						material.SetShaderParameter("cloud_color", source.AlbedoColor);
						material.SetShaderParameter("cloud_center", cloudCenter);
						material.SetShaderParameter("cell_size", WorldMapping.CellSize);
						material.SetShaderParameter("flash_period", 2.8f + slot * 0.37f);
						material.SetShaderParameter("flash_offset", slot * 0.61f);
						meshInstance.SetSurfaceOverrideMaterial(surface, material);
						materials.Add(material);
						meshMaterials.Add(material);
					}
					meshInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
					if (!fragmentMeshes.TryGetValue(mesh, out var fragments))
					{
						fragments = new CloudFragmentMesh(mesh, bounds.GetCenter(), extent);
						fragmentMeshes.Add(mesh, fragments);
					}
					clouds.Add((meshInstance, fragments, slot, meshMaterials.ToArray()));
				}

				var scale = WorldMapping.CellSize / extent;
				var cloud = new Node3D
				{
					Name = $"NebulaCloud{slot}",
					Position = cloudCenter - bounds.GetCenter() * scale,
					Scale = Vector3.One * scale,
				};
				cloud.AddChild(scene);
				AddChild(cloud);
			}
			finally
			{
				if (scene.GetParent() is null)
					scene.Free();
			}

			var state = State.FromShipInstance(
				ShipInstance.FromSpec($"nebula-preview-{slot}", FighterSpec.Instance, ShipCatalog.FullFighterLoadout()),
				Coord.Zero);
			var ship = new UnitView();
			AddChild(ship);
			ship.Bind(state, Colors.White);
			ship.Position += cloudCenter - Center;

			var seedLabel = new Label3D
			{
				Position = cloudCenter + new Vector3(0f, -1.15f, 0f),
				Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
				FontSize = 32,
				PixelSize = 0.006f,
			};
			AddChild(seedLabel);
			seedLabels.Add(seedLabel);
		}

		void Generate(int firstSeed)
		{
			var regions = Enumerable.Range(0, 6).Select(i => CloudPattern.Create(firstSeed + i)).ToArray();
			foreach (var (instance, fragments, slot, meshMaterials) in clouds)
			{
				instance.Mesh = fragments.Build(regions[slot]);
				for (var surface = 0; surface < meshMaterials.Length; surface++)
					instance.SetSurfaceOverrideMaterial(surface, meshMaterials[surface]);
			}
			for (var slot = 0; slot < seedLabels.Count; slot++)
				seedLabels[slot].Text = $"Seed {firstSeed + slot}";
		}

		var layer = new CanvasLayer();
		AddChild(layer);
		var label = new Label
		{
			Position = new Vector2(24, 24),
			Text = "Seeded 3D cloud variations\n"
				+ "Left/Right: orbit | Up/Down: elevation | Mouse wheel: zoom",
		};
		label.AddThemeFontSizeOverride("font_size", 22);
		layer.AddChild(label);

		var controls = new VBoxContainer { Position = new Vector2(24, 105) };
		layer.AddChild(controls);
		controls.AddChild(new Label { Text = "Starting seed" });
		var seedInput = new SpinBox { MinValue = 0, MaxValue = int.MaxValue - 5, Step = 1, Value = 0 };
		controls.AddChild(seedInput);
		var generateButton = new Button { Text = "Generate six" };
		generateButton.Pressed += () => Generate((int)seedInput.Value);
		controls.AddChild(generateButton);

		var texturePicker = new OptionButton { CustomMinimumSize = new Vector2(260, 0) };
		texturePicker.AddItem("Messier 17");
		texturePicker.AddItem("Purple Nebula 1");
		texturePicker.ItemSelected += index =>
		{
			foreach (var material in materials)
				material.SetShaderParameter("nebula_texture", textures[(int)index]);
		};
		controls.AddChild(texturePicker);

		var lightning = new CheckButton { Text = "Lightning flashes", ButtonPressed = true };
		lightning.Toggled += enabled =>
		{
			foreach (var material in materials)
				material.SetShaderParameter("lightning_enabled", enabled);
		};
		controls.AddChild(lightning);

		controls.AddChild(new Label { Text = "Cloud opacity" });
		var opacity = new HSlider { MinValue = 0.01, MaxValue = 0.35, Step = 0.01, Value = 0.1 };
		opacity.ValueChanged += value =>
		{
			foreach (var material in materials)
				material.SetShaderParameter("opacity", (float)value);
		};
		controls.AddChild(opacity);
		Generate(0);
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

	private void UpdateCamera()
	{
		_camera.Position = Center + new Vector3(
			Mathf.Sin(_angle) * Mathf.Cos(_elevation),
			Mathf.Sin(_elevation),
			Mathf.Cos(_angle) * Mathf.Cos(_elevation)) * _distance;
		_camera.LookAt(Center);
	}
}
