using Godot;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Ui;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class MoveRouteRibbonView : MeshInstance3D
{
	private static readonly Shader RouteShader = new()
	{
		Code = """
			shader_type spatial;
			render_mode unshaded, cull_disabled, depth_test_disabled, depth_draw_never, shadows_disabled;

			void vertex() {
				vec3 point = (MODELVIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
				vec3 tangent = normalize((MODELVIEW_MATRIX * vec4(NORMAL, 0.0)).xyz);
				vec3 side = cross(tangent, normalize(-point));
				if (length(side) < 0.001) {
					side = cross(tangent, vec3(0.0, 1.0, 0.0));
				}
				POSITION = PROJECTION_MATRIX * vec4(point + normalize(side) * UV.y, 1.0);
			}

			void fragment() {
				float pulse = pow(max(0.0, cos((UV.x - TIME * 2.5) * 2.0944)), 12.0);
				float along = mod(UV.x + 0.16, 0.32) - 0.16;
				float stroke = length(vec2(max(abs(along) - 0.04, 0.0), UV.y));
				float edge = fwidth(stroke);
				ALBEDO = COLOR.rgb * (1.0 + pulse * 0.35);
				ALPHA = (0.75 + pulse * 0.25)
					* (1.0 - smoothstep(0.04 - edge, 0.04 + edge, stroke));
			}
			""",
	};

	private readonly ImmediateMesh _mesh = new();

	public override void _Ready()
	{
		Mesh = _mesh;
		MaterialOverride = new ShaderMaterial { Shader = RouteShader };
		CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		PresentationLayers.MarkUx(this);
		Visible = false;
	}

	public void ApplyFrame(PresentationFrame frame, bool simulationEnabled)
	{
		_mesh.ClearSurfaces();
		var option = frame.SelectedMove ?? frame.HoveredMove;
		Visible = simulationEnabled
			&& frame.ShowMovePreview
			&& !frame.ShowOutcomeOverlay
			&& frame.Mode == EPlayerMode.Move
			&& option is not null;
		if (!Visible || option is null)
			return;

		var previous = frame.FocusState.Position;
		var distance = 0f;
		var hasSurface = false;
		foreach (var checkpoint in option.Checkpoints)
		{
			var cell = checkpoint.Position;
			if (cell == previous)
				continue;

			if (!hasSurface)
			{
				_mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
				hasSurface = true;
			}
			var from = WorldMapping.ToWorld(previous);
			var to = WorldMapping.ToWorld(cell);
			var heading = cell - previous;
			var nextDistance = distance + from.DistanceTo(to);
			_mesh.SurfaceSetColor(HeadingDirectionColors.For(heading));
			// The shader expands each segment toward the camera using this tangent.
			_mesh.SurfaceSetNormal((to - from).Normalized());
			AddVertex(from, distance, -0.05f);
			AddVertex(from, distance, 0.05f);
			AddVertex(to, nextDistance, 0.05f);
			AddVertex(from, distance, -0.05f);
			AddVertex(to, nextDistance, 0.05f);
			AddVertex(to, nextDistance, -0.05f);
			previous = cell;
			distance = nextDistance;
		}
		if (hasSurface)
			_mesh.SurfaceEnd();
	}

	private void AddVertex(Vector3 position, float distance, float width)
	{
		_mesh.SurfaceSetUV(new Vector2(distance, width));
		_mesh.SurfaceAddVertex(position);
	}
}
