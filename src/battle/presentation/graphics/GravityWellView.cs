using Godot;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class GravityWellView : Node3D
{
	private const float SurfaceOffset = 0.02f;
	private const float FlowHalfWidth = 0.16f;
	private MeshInstance3D _chevrons = null!;
	private MeshInstance3D _lattice = null!;
	private static readonly Shader FlowShader = new()
	{
		Code = """
			shader_type spatial;
			render_mode unshaded, cull_back, depth_draw_never, shadows_disabled;

			float line_distance(vec2 p, vec2 a, vec2 b) {
				vec2 segment = b - a;
				float t = clamp(dot(p - a, segment) / dot(segment, segment), 0.0, 1.0);
				return length(p - (a + t * segment));
			}

			void fragment() {
				float distance_along = UV.x - UV2.x;
				float length_along = UV2.y;
				float fade = smoothstep(0.0, 0.4, min(distance_along, length_along - distance_along));
				vec2 point = vec2(mod(UV.x - TIME * 0.35 + 0.5, 1.0) - 0.5, UV.y);
				float stroke = min(
					line_distance(point, vec2(-0.12, -0.12), vec2(0.12, 0.0)),
					line_distance(point, vec2(-0.12, 0.12), vec2(0.12, 0.0)));
				ALBEDO = COLOR.rgb;
				ALPHA = COLOR.a * fade * (1.0 - smoothstep(0.02, 0.03, stroke));
			}
			""",
	};
	private static readonly Shader CoreShader = new()
	{
		Code = """
			shader_type spatial;
			render_mode unshaded;

			void fragment() {
				float rim = pow(1.0 - clamp(dot(NORMAL, VIEW), 0.0, 1.0), 5.0);
				ALBEDO = mix(vec3(0.001, 0.001, 0.003), vec3(0.45, 0.38, 0.7), rim);
			}
			""",
	};

	public void Build(GravityWellPresentation well)
	{
		Position = WorldMapping.ToWorld(well.Center);
		AddFlow(well);
		AddWarpedSpace();
		AddVisual("GravityCore",
			new SphereMesh { Radius = 0.72f, Height = 1.44f, RadialSegments = 64, Rings = 32 },
			new ShaderMaterial { Shader = CoreShader });
	}

	public void SetSimulationEnabled(bool enabled)
	{
		_chevrons.Visible = enabled;
		_lattice.Visible = enabled;
	}

	private void AddFlow(GravityWellPresentation well)
	{
		var mesh = new ImmediateMesh();
		mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
		foreach (var run in GravityWellFlowLayout.Build(well.Faces, well.Center))
		{
			var normal = ToVector(run.Normal);
			var direction = ToVector(run.Heading);
			var start = ToVector(run.StartHalfCells) * (WorldMapping.CellSize * 0.5f)
				+ normal * (WorldMapping.CellSize * 0.5f + SurfaceOffset + 0.01f);
			var end = ToVector(run.EndHalfCells) * (WorldMapping.CellSize * 0.5f)
				+ normal * (WorldMapping.CellSize * 0.5f + SurfaceOffset + 0.01f);
			var lateral = normal.Cross(direction) * FlowHalfWidth;
			var length = start.DistanceTo(end);
			var phase = (start + Position).Dot(direction);
			mesh.SurfaceSetColor(HeadingDirectionColors.For(run.Heading));
			mesh.SurfaceSetUV2(new Vector2(phase, length));
			AddFlowVertex(mesh, start - lateral, phase, -FlowHalfWidth);
			AddFlowVertex(mesh, start + lateral, phase, FlowHalfWidth);
			AddFlowVertex(mesh, end + lateral, phase + length, FlowHalfWidth);
			AddFlowVertex(mesh, start - lateral, phase, -FlowHalfWidth);
			AddFlowVertex(mesh, end + lateral, phase + length, FlowHalfWidth);
			AddFlowVertex(mesh, end - lateral, phase + length, -FlowHalfWidth);
		}
		mesh.SurfaceEnd();
		_chevrons = AddVisual("Chevrons", mesh, new ShaderMaterial { Shader = FlowShader });
	}

	private static void AddFlowVertex(ImmediateMesh mesh, Vector3 position, float distance, float lateral)
	{
		mesh.SurfaceSetUV(new Vector2(distance, lateral));
		mesh.SurfaceAddVertex(position);
	}

	private void AddWarpedSpace()
	{
		const int subdivisions = 48;
		const int gridSize = 3;
		var extent = gridSize * WorldMapping.CellSize;
		var center = Vector3.One * (extent * 0.5f);
		var axes = new[] { Vector3.Right, Vector3.Up, Vector3.Back };
		var lattice = new ImmediateMesh();
		lattice.SurfaceBegin(Mesh.PrimitiveType.Lines);
		for (var axis = 0; axis < axes.Length; axis++)
		for (var a = -1; a <= gridSize + 1; a++)
		for (var b = -1; b <= gridSize + 1; b++)
		{
			var start = (axes[(axis + 1) % 3] * a + axes[(axis + 2) % 3] * b - axes[axis])
				* WorldMapping.CellSize;
			for (var segment = 0; segment < subdivisions; segment++)
			for (var endpoint = 0; endpoint < 2; endpoint++)
			{
				var t = (segment + endpoint) / (float)subdivisions;
				var point = start + axes[axis] * ((extent + WorldMapping.CellSize * 2f) * t);
				var offset = point - center;
				var pull = 0.82f * Mathf.Exp(-offset.LengthSquared() / 14f);
				var fade = Mathf.Clamp((4.5f - offset.Length()) / 1.4f, 0f, 1f);
				fade = fade * fade * (3f - 2f * fade);
				lattice.SurfaceSetColor(new Color(0.42f, 0.51f, 0.72f, fade * 0.48f));
				lattice.SurfaceAddVertex(offset * (1f - pull));
			}
		}
		lattice.SurfaceEnd();
		_lattice = AddVisual("WarpedSpace", lattice, new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			VertexColorUseAsAlbedo = true,
		});
	}

	private MeshInstance3D AddVisual(string name, Mesh mesh, Material material)
	{
		var visual = new MeshInstance3D
		{
			Name = name,
			Mesh = mesh,
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		PresentationLayers.MarkWorld(visual);
		AddChild(visual);
		return visual;
	}

	private static Vector3 ToVector(Coord coord) => new(coord.X, coord.Y, coord.Z);
}
