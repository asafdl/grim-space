using Godot;
using GrimSpace.Application;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Math.Grid;

namespace GrimSpace.Scenes.Dev;

public partial class GravityWellLab : Node3D
{
	private const int GridSize = 3;
	private Controller _camera = null!;
	private readonly HashSet<Coord> _cells = [];
	private readonly List<(Vector3 Start, Vector3 End, Vector3 Normal, Coord Heading)> _roadSegments = [];
	private readonly List<(Vector3 Start, Vector3 End, Vector3 Normal, Coord Heading)> _chevronRuns = [];
	private readonly ImmediateMesh _chevrons = new();
	private ShaderMaterial _coreInterference = null!;
	private float _flowOffset;

	public override void _Ready()
	{
		AddChild(new WorldEnvironment
		{
			Environment = new Godot.Environment
			{
				BackgroundMode = Godot.Environment.BGMode.Color,
				BackgroundColor = new Color(0.008f, 0.012f, 0.022f),
			},
		});

		_camera = new Controller { Name = "Camera3D", Current = true };
		AddChild(_camera);
		ResetCamera();

		AddWarpedSpace();
		AddCoreInterference();

		for (var x = 0; x < GridSize; x++)
		for (var y = 0; y < GridSize; y++)
		for (var z = 0; z < GridSize; z++)
			_cells.Add(new Coord(x, y, z));
		BuildSurfaceSegments();
		BuildChevronRuns();
		AddChild(new MeshInstance3D
		{
			Name = "Chevrons",
			Mesh = _chevrons,
			MaterialOverride = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				VertexColorUseAsAlbedo = true,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			},
		});
		UpdateChevrons();

		var overlay = new CanvasLayer();
		AddChild(overlay);
		overlay.AddChild(new Label
		{
			Position = new Vector2(24, 24),
			Text = $"GRAVITY WELL LAB — SURFACE FLOW (Y IS UP)\n"
				+ $"Hold {GameInputBindings.Label("battle_camera_orbit")} and drag to orbit  ·  "
				+ $"{GameInputBindings.Label("battle_zoom_in")} / {GameInputBindings.Label("battle_zoom_out")} to zoom  ·  R to reset\n"
				+ "Mirrored flow on both sides, using the standard heading colors.",
		});
	}

	private void AddWarpedSpace()
	{
		const int subdivisions = 48;
		var extent = GridSize * WorldMapping.CellSize;
		var center = Vector3.One * (extent * 0.5f);
		var axes = new[] { Vector3.Right, Vector3.Up, Vector3.Back };
		var lattice = new ImmediateMesh();
		lattice.SurfaceBegin(Mesh.PrimitiveType.Lines);
		for (var axis = 0; axis < axes.Length; axis++)
		for (var a = -1; a <= GridSize + 1; a++)
		for (var b = -1; b <= GridSize + 1; b++)
		{
			var start = (axes[(axis + 1) % 3] * a + axes[(axis + 2) % 3] * b - axes[axis]) * WorldMapping.CellSize;
			for (var segment = 0; segment < subdivisions; segment++)
			for (var endpoint = 0; endpoint < 2; endpoint++)
			{
				var t = (segment + endpoint) / (float)subdivisions;
				var point = start + axes[axis] * ((extent + WorldMapping.CellSize * 2f) * t);
				var offset = point - center;
				var pull = 0.82f * Mathf.Exp(-offset.LengthSquared() / 14f);
				var warped = center + offset * (1f - pull);
				var fade = Mathf.Clamp((4.5f - offset.Length()) / 1.4f, 0f, 1f);
				fade = fade * fade * (3f - 2f * fade);
				lattice.SurfaceSetColor(new Color(0.42f, 0.51f, 0.72f, fade * 0.48f));
				lattice.SurfaceAddVertex(warped);
			}
		}
		lattice.SurfaceEnd();
		AddChild(new MeshInstance3D
		{
			Name = "WarpedSpace",
			Mesh = lattice,
			MaterialOverride = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				VertexColorUseAsAlbedo = true,
			},
		});
		AddChild(new MeshInstance3D
		{
			Name = "GravityCore",
			Position = center,
			Mesh = new SphereMesh { Radius = 0.72f, Height = 1.44f, RadialSegments = 64, Rings = 32 },
			MaterialOverride = new ShaderMaterial
			{
				Shader = new Shader
				{
					Code = """
						shader_type spatial;
						render_mode unshaded;

						void fragment() {
							float rim = pow(1.0 - clamp(dot(NORMAL, VIEW), 0.0, 1.0), 5.0);
							ALBEDO = mix(vec3(0.001, 0.001, 0.003), vec3(0.45, 0.38, 0.7), rim);
						}
						""",
				},
			},
		});
	}

	private void AddCoreInterference()
	{
		_coreInterference = new ShaderMaterial
		{
			Shader = new Shader
			{
				Code = """
					shader_type canvas_item;
					render_mode unshaded;
					uniform sampler2D scene : hint_screen_texture, repeat_disable, filter_linear;
					uniform vec2 center_px;
					uniform float radius_px = 1.0;

					float hash(vec2 value) {
						return fract(sin(dot(value, vec2(127.1, 311.7))) * 43758.5453);
					}

					void fragment() {
						vec2 pixel = SCREEN_UV / SCREEN_PIXEL_SIZE;
						float radius = length(pixel - center_px) / max(radius_px, 1.0);
						float mask = 1.0 - smoothstep(0.65, 1.4, radius);
						float tick = floor(TIME * 12.0);
						float burst = step(0.65, hash(vec2(tick, 17.0)));
						float row = floor((pixel.y - center_px.y) / 3.0);
						float band = step(0.8, hash(vec2(row, tick)));
						float shift = (hash(vec2(row, tick + 19.0)) - 0.5)
							* 22.0 * band * burst * mask;
						vec2 sample_uv = clamp(SCREEN_UV + vec2(shift, 0.0) * SCREEN_PIXEL_SIZE,
							vec2(0.0), vec2(1.0));
						vec3 color = texture(scene, sample_uv).rgb;
						float fragment = step(0.86, hash(vec2(floor(pixel.x / 16.0), row) + tick));
						color = mix(color, vec3(0.12, 0.1, 0.17), fragment * band * burst * mask * 0.35);
						COLOR = vec4(color, mask * band * burst);
					}
					""",
			},
		};
		var layer = new CanvasLayer { Name = "CoreInterference", Layer = 1 };
		AddChild(layer);
		var overlay = new ColorRect
		{
			Material = _coreInterference,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		layer.AddChild(overlay);
		overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
	}

	private static int SideOf(Coord cell)
	{
		const int center = GridSize / 2;
		var dx = cell.X - center;
		var dy = cell.Y - center;
		var dz = cell.Z - center;
		return System.Math.Sign(dx != 0 ? dx : dz != 0 ? dz : dy);
	}

	private void BuildSurfaceSegments()
	{
		const float surfaceOffset = 0.02f;
		var axes = new[] { new Coord(1, 0, 0), Coord.Up, Coord.Forward };
		var vectors = new[] { Vector3.Right, Vector3.Up, Vector3.Back };
		for (var faceAxis = 0; faceAxis < 3; faceAxis++)
		for (var faceSign = -1; faceSign <= 1; faceSign += 2)
		foreach (var cell in _cells)
		{
			if (SideOf(cell) <= 0 || !IsOnFace(cell, faceAxis, faceSign))
				continue;

			var normal = vectors[faceAxis] * faceSign;
			var point = WorldMapping.ToWorld(cell) + normal * (WorldMapping.CellSize * 0.5f + surfaceOffset);
			for (var tangent = 0; tangent < 3; tangent++)
			{
				if (tangent == faceAxis)
					continue;

				var direction = vectors[tangent];
				var neighbor = cell + axes[tangent];
				if (_cells.Contains(neighbor) && SideOf(neighbor) > 0)
					_roadSegments.Add((point, point + direction * WorldMapping.CellSize, normal, axes[tangent]));

				for (var edgeSign = -1; edgeSign <= 1; edgeSign += 2)
				{
					if (IsOnFace(cell, tangent, edgeSign))
					{
						var edge = point + direction * edgeSign * (WorldMapping.CellSize * 0.5f + surfaceOffset);
						_roadSegments.Add((edgeSign > 0 ? point : edge, edgeSign > 0 ? edge : point, normal, axes[tangent]));
					}
				}

			}
		}

		var mirroredCount = _roadSegments.Count;
		var extent = Vector3.One * (GridSize * WorldMapping.CellSize);
		for (var i = 0; i < mirroredCount; i++)
		{
			var segment = _roadSegments[i];
			_roadSegments.Add((extent - segment.Start, extent - segment.End, -segment.Normal, -segment.Heading));
		}
	}

	private void BuildChevronRuns()
	{
		foreach (var segment in _roadSegments)
		{
			if (_roadSegments.Any(other =>
				other.Heading == segment.Heading
				&& other.Normal.IsEqualApprox(segment.Normal)
				&& other.End.IsEqualApprox(segment.Start)))
				continue;

			var end = segment.End;
			while (true)
			{
				var next = _roadSegments.FindIndex(other =>
					other.Heading == segment.Heading
					&& other.Normal.IsEqualApprox(segment.Normal)
					&& other.Start.IsEqualApprox(end));
				if (next < 0)
					break;
				end = _roadSegments[next].End;
			}
			_chevronRuns.Add((segment.Start, end, segment.Normal, segment.Heading));
		}
	}

	private static bool IsOnFace(Coord cell, int axis, int sign)
	{
		var value = axis switch { 0 => cell.X, 1 => cell.Y, _ => cell.Z };
		return value == (sign < 0 ? 0 : GridSize - 1);
	}

	private static void AddRoadSegment(ImmediateMesh mesh, Vector3 start, Vector3 end, Vector3 normal, float halfWidth)
	{
		var side = normal.Cross((end - start).Normalized()) * halfWidth;
		mesh.SurfaceAddVertex(start - side);
		mesh.SurfaceAddVertex(start + side);
		mesh.SurfaceAddVertex(end + side);
		mesh.SurfaceAddVertex(start - side);
		mesh.SurfaceAddVertex(end + side);
		mesh.SurfaceAddVertex(end - side);
	}

	public override void _Process(double delta)
	{
		_flowOffset = (_flowOffset + (float)delta * 0.7f) % 1f;
		UpdateChevrons();
		var center = Vector3.One * (GridSize * WorldMapping.CellSize * 0.5f);
		var screenCenter = _camera.UnprojectPosition(center);
		var screenEdge = _camera.UnprojectPosition(center + _camera.GlobalBasis.X * 0.9f);
		_coreInterference.SetShaderParameter("center_px", screenCenter);
		_coreInterference.SetShaderParameter("radius_px",
			_camera.IsPositionBehind(center) ? 0f : screenCenter.DistanceTo(screenEdge));
	}

	private void UpdateChevrons()
	{
		_chevrons.ClearSurfaces();
		_chevrons.SurfaceBegin(Mesh.PrimitiveType.Triangles);
		foreach (var (start, end, normal, heading) in _chevronRuns)
		{
			var direction = (end - start).Normalized();
			var lateral = normal.Cross(direction);
			var length = start.DistanceTo(end);
			var first = Mathf.PosMod(_flowOffset - start.Dot(direction), 1f);
			for (var distance = first; distance < length; distance += 1f)
			{
				var color = HeadingDirectionColors.For(heading);
				var fade = Mathf.Clamp(Mathf.Min(distance, length - distance) / 0.4f, 0f, 1f);
				color.A = fade * fade * (3f - 2f * fade);
				_chevrons.SurfaceSetColor(color);
				var point = start + direction * distance + normal * 0.01f;
				var tip = point + direction * 0.12f;
				var back = point - direction * 0.12f;
				AddRoadSegment(_chevrons, back + lateral * 0.12f, tip, normal, 0.025f);
				AddRoadSegment(_chevrons, back - lateral * 0.12f, tip, normal, 0.025f);
			}
		}
		_chevrons.SurfaceEnd();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.R })
		{
			ResetCamera();
			GetViewport().SetInputAsHandled();
		}
	}

	private void ResetCamera()
	{
		var center = Vector3.One * (GridSize * WorldMapping.CellSize * 0.5f);
		_camera.SetOrbitFocus(center);
		_camera.SetFocus(center, 14f, Mathf.Pi / 4f, 0.45f);
	}
}
