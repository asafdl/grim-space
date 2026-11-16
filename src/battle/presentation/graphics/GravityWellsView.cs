using Godot;
using GrimSpace.Battle.Presentation.Ui;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class GravityWellsView : Node3D
{
	private const float CoreRadius = 0.9f;
	private const float MaskRadius = 1.4f;
	private static readonly Shader InterferenceShader = new()
	{
		Code = """
			shader_type canvas_item;
			render_mode unshaded;
			uniform sampler2D scene : hint_screen_texture, repeat_disable, filter_linear;
			uniform vec2 center_uv;
			uniform float radius_uv;

			float hash(vec2 value) {
				return fract(sin(dot(value, vec2(127.1, 311.7))) * 43758.5453);
			}

			void fragment() {
				vec2 pixel = SCREEN_UV / SCREEN_PIXEL_SIZE;
				vec2 center_px = center_uv / SCREEN_PIXEL_SIZE;
				float radius_px = radius_uv / SCREEN_PIXEL_SIZE.x;
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
	};

	private readonly Dictionary<string, (GravityWellPresentation Well, GravityWellView View, ColorRect Overlay)> _wells = [];
	private Camera3D _camera = null!;
	private CanvasLayer _layer = null!;
	private BackBufferCopy _copy = null!;
	private bool _simulationEnabled;

	public void Configure(Camera3D camera) => _camera = camera;

	public void SetSimulationEnabled(bool enabled)
	{
		_simulationEnabled = enabled;
		foreach (var (_, view, _) in _wells.Values)
			view.SetSimulationEnabled(enabled);
	}

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		_layer = new CanvasLayer { Name = "GravityInterference", Layer = 1 };
		AddChild(_layer);
		_copy = new BackBufferCopy { CopyMode = BackBufferCopy.CopyModeEnum.Viewport, Visible = false };
		_layer.AddChild(_copy);
	}

	public void Apply(IReadOnlyList<GravityWellPresentation> wells)
	{
		var ids = wells.Select(well => well.Id).ToHashSet(StringComparer.Ordinal);
		foreach (var id in _wells.Keys.Where(id => !ids.Contains(id)).ToArray())
			Remove(id);

		foreach (var well in wells)
		{
			if (_wells.TryGetValue(well.Id, out var current))
			{
				if (current.Well == well)
					continue;
				Remove(well.Id);
			}

			var view = new GravityWellView { Name = "GravityWell" };
			view.Build(well);
			view.SetSimulationEnabled(_simulationEnabled);
			AddChild(view);
			var overlay = new ColorRect
			{
				Name = "CoreInterference",
				Material = new ShaderMaterial { Shader = InterferenceShader },
				MouseFilter = Control.MouseFilterEnum.Ignore,
				Visible = false,
			};
			_layer.AddChild(overlay);
			_wells.Add(well.Id, (well, view, overlay));
		}
	}

	public override void _Process(double delta)
	{
		var viewportSize = GetViewport().GetVisibleRect().Size;
		var viewportRect = new Rect2(Vector2.Zero, viewportSize);
		var anyVisible = false;
		foreach (var (_, view, overlay) in _wells.Values)
		{
			var center = view.GlobalPosition;
			var depth = -_camera.GlobalBasis.Z.Dot(center - _camera.GlobalPosition);
			if (depth < _camera.Near || depth > _camera.Far || viewportSize.X <= 0f || viewportSize.Y <= 0f)
			{
				overlay.Hide();
				continue;
			}

			var screenCenter = _camera.UnprojectPosition(center);
			var screenEdge = _camera.UnprojectPosition(center + _camera.GlobalBasis.X * CoreRadius);
			var radius = screenCenter.DistanceTo(screenEdge);
			var bounds = new Rect2(screenCenter - Vector2.One * radius * MaskRadius,
				Vector2.One * radius * MaskRadius * 2f);
			if (radius <= 0f || !bounds.Intersects(viewportRect))
			{
				overlay.Hide();
				continue;
			}

			overlay.Position = bounds.Position;
			overlay.Size = bounds.Size;
			var material = (ShaderMaterial)overlay.Material;
			material.SetShaderParameter("center_uv", screenCenter / viewportSize);
			material.SetShaderParameter("radius_uv", radius / viewportSize.X);
			overlay.Show();
			anyVisible = true;
		}
		_copy.Visible = anyVisible;
	}

	private void Remove(string id)
	{
		var (_, view, overlay) = _wells[id];
		view.Hide();
		overlay.Hide();
		view.QueueFree();
		overlay.QueueFree();
		_wells.Remove(id);
	}
}
