using Godot;
using GrimSpace.Battle;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Specs;

namespace GrimSpace.Dev;

public partial class ShieldBubblePreview : Node3D
{
	private static readonly string[] Options = ["Original", "Quiet wireframe", "Octagonal panels", "Triangular facets", "Smooth energy shell", "Stylized energy"];
	private static readonly string[] Coverage = ["Full coverage", "Forward depleted", "Forward / starboard / dorsal depleted", "No shields"];
	private static readonly Color Ally = new(0.52f, 0.68f, 0.76f);
	private static readonly Color Enemy = new(0.76f, 0.59f, 0.49f);
	private readonly List<(Node3D Node, bool Covered)> _shields = [];
	private readonly List<Node3D> _groups = [];
	private readonly List<(Node3D Node, int Coverage)> _rows = [];
	private readonly List<MeshInstance3D> _powerLinks = [];
	private readonly List<ShaderMaterial> _linkedShields = [];
	private bool _showPowerLinks = true;
	private Camera3D _camera = null!;
	private Label _status = null!;
	private bool _showShields = true;
	private int _option = 5;
	private int _coverage;
	private float _azimuth;
	private float _elevation = 0.65f;
	private float _distance = 14f;

	public override void _Ready()
	{
		AddChild(new WorldEnvironment
		{
			Environment = new Godot.Environment
			{
				BackgroundMode = Godot.Environment.BGMode.Color,
				BackgroundColor = new Color(0.008f, 0.012f, 0.022f),
				AmbientLightSource = Godot.Environment.AmbientSource.Color,
				AmbientLightColor = new Color(0.55f, 0.64f, 0.78f),
				AmbientLightEnergy = 0.75f,
				TonemapMode = Godot.Environment.ToneMapper.Filmic,
				GlowEnabled = true,
				GlowIntensity = 0.5f,
				GlowHdrThreshold = 1.1f,
			},
		});
		AddChild(new DirectionalLight3D
		{
			RotationDegrees = new Vector3(-48f, -28f, 0f),
			LightEnergy = 1.8f,
		});
		AddChild(new DirectionalLight3D
		{
			RotationDegrees = new Vector3(-25f, 130f, 0f),
			LightColor = new Color(0.65f, 0.75f, 0.9f),
			LightEnergy = 0.65f,
		});
		_camera = new Camera3D
		{
			Current = true,
			Fov = 40f,
		};
		AddChild(_camera);
		var shader = GD.Load<Shader>("res://scenes/dev/shield_bubble_preview.gdshader")
			?? throw new InvalidOperationException("Could not load the shield bubble preview shader.");
		var smoothShader = GD.Load<Shader>("res://scenes/dev/shield_energy_preview.gdshader")
			?? throw new InvalidOperationException("Could not load the smooth energy shield preview shader.");
		for (var option = 0; option < Options.Length; option++)
		{
			var group = new Node3D { Position = new Vector3(ColumnX(option), 0, 0) };
			AddChild(group);
			_groups.Add(group);
			AddLabel(group, $"{option + 1}. {Options[option]}", new Vector3(0, 1, -12f), 38);
			for (var coverage = 0; coverage < Coverage.Length; coverage++)
			{
				var z = (coverage - 1.5f) * 6f;
				var row = new Node3D { Position = new Vector3(0, 0, z) };
				group.AddChild(row);
				_rows.Add((row, coverage));
				AddLabel(row, Coverage[coverage], new Vector3(0, 0, -2f), 24);
				var activeShader = option == 4 ? smoothShader : shader;
				AddShip(row, option, coverage, new Vector3(-2.5f, 0, 0), Ally, activeShader, "ally");
				AddShip(row, option, coverage, new Vector3(2.5f, 0, 0), Enemy, activeShader, "enemy");
			}
		}
		var layer = new CanvasLayer();
		AddChild(layer);
		_status = new Label { Position = new Vector2(24, 24) };
		_status.AddThemeFontSizeOverride("font_size", 20);
		layer.AddChild(_status);
		UpdateCamera();
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } key)
			return;
		if (key.Keycode >= Key.Key1 && key.Keycode <= Key.Key6)
		{
			_option = (int)key.Keycode - (int)Key.Key1;
			_distance = _coverage < 0 ? 36f : 14f;
		}
		else if (key.Keycode == Key.O)
		{
			_option = -1;
			_coverage = -1;
			_distance = 78f;
		}
		else if (key.Keycode == Key.C)
		{
			_coverage = (_coverage + 2) % (Coverage.Length + 1) - 1;
			if (_option < 0)
				_option = 5;
			_distance = _coverage < 0 ? 36f : 14f;
		}
		else if (key.Keycode == Key.F)
			_distance = 85f;
		else if (key.Keycode == Key.E)
		{
			_showPowerLinks = !_showPowerLinks;
			foreach (var link in _powerLinks)
				link.Visible = _showPowerLinks;
			foreach (var shield in _linkedShields)
				shield.SetShaderParameter("link_glow", _showPowerLinks ? 1f : 0f);
		}
		else if (key.Keycode == Key.Space)
		{
			_showShields = !_showShields;
			foreach (var shield in _shields)
				shield.Node.Visible = _showShields && shield.Covered;
		}
		else
			return;
		UpdateCamera();
		GetViewport().SetInputAsHandled();
	}

	public override void _Process(double delta)
	{
		var horizontal = (Input.IsPhysicalKeyPressed(Key.Right) ? 1 : 0)
			- (Input.IsPhysicalKeyPressed(Key.Left) ? 1 : 0);
		var vertical = (Input.IsPhysicalKeyPressed(Key.Up) ? 1 : 0)
			- (Input.IsPhysicalKeyPressed(Key.Down) ? 1 : 0);
		if (horizontal == 0 && vertical == 0)
			return;
		_azimuth += horizontal * (float)delta;
		_elevation = Mathf.Clamp(_elevation + vertical * (float)delta, 0.1f, 1.4f);
		UpdateCamera();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton { Pressed: true } mouse
			|| mouse.ButtonIndex is not (MouseButton.WheelUp or MouseButton.WheelDown))
			return;
		_distance = Mathf.Clamp(_distance * (mouse.ButtonIndex == MouseButton.WheelUp ? 0.9f : 1.1f), 12f, 110f);
		UpdateCamera();
		GetViewport().SetInputAsHandled();
	}

	private void AddShip(Node3D group, int option, int coverage, Vector3 position, Color tint, Shader shader, string team)
	{
		var loadout = ShipCatalog.FullFighterLoadout() with
		{
			BaseShieldPoints = FighterSpec.Instance.DefaultBaseShieldPoints,
		};
		var state = State.FromShipInstance(
			ShipInstance.FromSpec($"bubble-{option}-{coverage}-{team}", FighterSpec.Instance, loadout),
			Coord.Zero);
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
		{
			if (coverage == 3
				|| (coverage >= 1 && face == ESpatialOrientation.Forward)
				|| (coverage == 2 && face is ESpatialOrientation.Starboard or ESpatialOrientation.Dorsal))
				state.CurrentShieldPoints[face] = 0;
		}
		var view = new UnitView();
		group.AddChild(view);
		view.Bind(state, tint, tint);
		view.Position = position;
		view.SetProcess(false);
		var faces = Enum.GetValues<ESpatialOrientation>()
			.Select(face => view.FindChild($"Shield{face}", true, false))
			.OfType<MeshInstance3D>()
			.ToArray();
		if (faces.Length == 0)
			throw new InvalidOperationException("Shield comparison requires a ship with shield meshes.");
		var links = faces.SelectMany(face => face.GetChildren().OfType<MeshInstance3D>()).ToArray();
		if (option == 5)
		{
			_shields.AddRange(faces.Select(face => ((Node3D)face, face.Visible)));
			_powerLinks.AddRange(links);
			_linkedShields.AddRange(faces.Select(face => (ShaderMaterial)face.MaterialOverride));
			return;
		}
		foreach (var link in links)
			link.Visible = false;
		if (option is 0 or 1)
		{
			foreach (var orientation in Enum.GetValues<ESpatialOrientation>())
			{
				var face = view.FindChild($"Shield{orientation}", true, false) as MeshInstance3D
					?? throw new InvalidOperationException($"Shield comparison is missing {orientation}.");
				face.Mesh = ShieldBubbleMesh.CreateWireframeFace(view.HullBounds, orientation);
				face.MaterialOverride = WeaponPreviewMaterials.CreateWireframe(
					option == 0 ? new Color(0.35f, 0.75f, 1f, 0.82f) : new Color(tint, 0.29f));
				_shields.Add((face, face.Visible));
			}
			return;
		}
		var bounds = faces[0].Mesh.GetAabb();
		foreach (var face in faces)
			bounds = bounds.Merge(face.Mesh.GetAabb());
		foreach (var face in faces)
		{
			var covered = face.Visible;
			// Derive the panel direction and envelope from the production face geometry.
			var offset = (face.Mesh.GetAabb().GetCenter() - bounds.GetCenter()) / bounds.Size;
			var axis = (int)offset.Abs().MaxAxisIndex();
			var direction = Vector3.Zero;
			direction[axis] = Mathf.Sign(offset[axis]);
			face.Visible = false;
			var material = new ShaderMaterial { Shader = shader };
			material.SetShaderParameter("team_color", tint);
			if (option == 4)
			{
				material.SetShaderParameter("face_axis", direction);
			}
			var bubble = new MeshInstance3D
			{
				Name = $"Preview{face.Name}",
				Mesh = option == 4
					? new SphereMesh { Radius = 1f, Height = 2f, RadialSegments = 96, Rings = 64 }
					: CreatePanelMesh(direction, octagons: option == 2),
				Position = bounds.GetCenter(),
				Scale = bounds.Size * 0.5f,
				MaterialOverride = material,
				Visible = covered,
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			};
			face.GetParent().AddChild(bubble);
			_shields.Add((bubble, covered));
		}
	}

	private void UpdateCamera()
	{
		for (var i = 0; i < _groups.Count; i++)
			_groups[i].Visible = _option < 0 || _option == i;
		foreach (var row in _rows)
			row.Node.Visible = _coverage < 0 || row.Coverage == _coverage;
		var target = new Vector3(_option < 0 ? 0 : ColumnX(_option), 0,
			_coverage < 0 ? 0 : (_coverage - 1.5f) * 6f);
		_camera.Position = target + new Vector3(
			Mathf.Sin(_azimuth) * Mathf.Cos(_elevation),
			Mathf.Sin(_elevation),
			Mathf.Cos(_azimuth) * Mathf.Cos(_elevation)) * _distance;
		_camera.LookAt(target);
		_status.Text = $"Shield comparison - {(_option < 0 ? "Overview" : Options[_option])}\n"
			+ "Each pair: ally left (muted blue), enemy right (muted amber); original keeps its blue\n"
			+ "1-6: style | C: cycle coverage / all rows | O: overview | F: far | Arrows: orbit | Wheel: zoom | Space: shields\n"
			+ "Rows: all six faces / forward depleted / three faces depleted / none. Static coverage; no replay.";
		_status.Text += $"\nE: power links {(_showPowerLinks ? "ON" : "OFF")} (style 6 only); soft drift + outward pulse";
	}

	private static void AddLabel(Node3D parent, string text, Vector3 position, int fontSize) =>
		parent.AddChild(new Label3D
		{
			Text = text,
			Position = position,
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			FontSize = fontSize,
			PixelSize = 0.01f,
			OutlineSize = 6,
			NoDepthTest = true,
		});

	private static ArrayMesh CreatePanelMesh(Vector3 direction, bool octagons)
	{
		const int cells = 3;
		var horizontal = direction.X != 0 ? Vector3.Up : Vector3.Right;
		var vertical = direction.Cross(horizontal);
		var vertices = new List<Vector3>();
		var normals = new List<Vector3>();
		var uv = new List<Vector2>();
		for (var y = 0; y < cells; y++)
		for (var x = 0; x < cells; x++)
		{
			var a = new Vector2(x, y);
			Vector2[] corners = [a, a + Vector2.Right, a + Vector2.One, a + Vector2.Down];
			if (octagons)
			{
				var outline = new List<Vector2>();
				for (var i = 0; i < corners.Length; i++)
				{
					var corner = corners[i];
					var next = corners[(i + 1) % corners.Length];
					outline.Add(corner.Lerp(next, 0.28f));
					outline.Add(corner.Lerp(next, 0.72f));
					AddPanel([corner, corner.Lerp(next, 0.28f),
						corner.Lerp(corners[(i + 3) % corners.Length], 0.28f)]);
				}
				AddPanel(outline);
			}
			else
			{
				AddPanel([corners[0], corners[1], corners[2]]);
				AddPanel([corners[0], corners[2], corners[3]]);
			}
		}
		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
		arrays[(int)Mesh.ArrayType.Normal] = normals.ToArray();
		arrays[(int)Mesh.ArrayType.TexUV] = uv.ToArray();
		var mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		return mesh;

		void AddPanel(IReadOnlyList<Vector2> outline)
		{
			var points = outline.Select(point =>
				(direction + horizontal * (point.X * 2f / cells - 1f)
					+ vertical * (point.Y * 2f / cells - 1f)).Normalized()).ToArray();
			var center = points.Aggregate(Vector3.Zero, (sum, point) => sum + point) / points.Length;
			var normal = center.Normalized();
			// Flat panels provide real highlight changes rather than a texture on a smooth sphere.
			for (var i = 0; i < points.Length; i++)
				points[i] = center + (points[i] - center).Slide(normal) * 0.985f;
			for (var i = 0; i < points.Length; i++)
			{
				var first = points[i];
				var second = points[(i + 1) % points.Length];
				if ((first - center).Cross(second - center).Dot(normal) > 0)
					(first, second) = (second, first);
				vertices.AddRange([center, first, second]);
				normals.AddRange([normal, normal, normal]);
				// One fan triangle's outer edge is a panel seam; its radial edges stay invisible.
				uv.AddRange([new Vector2(1f, 0f), Vector2.Zero, Vector2.Zero]);
			}
		}
	}

	private static float ColumnX(int option) => (option - (Options.Length - 1) * 0.5f) * 13f;
}
