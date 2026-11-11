using Godot;
using GrimSpace.Application;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class SimulationModeTransitionView : Node
{
	private const double TransitionSeconds = 0.7;
	private int _generation;
	private bool _isCapturing;
	private bool _isTransitioning;
	private TextureRect _overlay = null!;
	private ShaderMaterial _transitionMaterial = null!;
	private AudioStreamPlayer _transitionSound = null!;
	private Tween? _transitionTween;

	public bool IsCapturing => _isCapturing;
	public bool IsTransitioning => _isTransitioning;

	public override void _Ready()
	{
		_transitionMaterial = new ShaderMaterial
		{
			Shader = new Shader
			{
				Code = """
					shader_type canvas_item;
					render_mode unshaded;
					uniform sampler2D incoming : hint_screen_texture, repeat_disable, filter_linear;
					uniform float progress = 0.0;
					float hash(vec2 value) {
						return fract(sin(dot(value, vec2(127.1, 311.7))) * 43758.5453);
					}
					void fragment() {
						float envelope = smoothstep(0.0, 0.15, progress)
							* (1.0 - smoothstep(0.65, 1.0, progress));
						float tick = floor(progress * 12.0);
						float row = floor(UV.y * 85.0);
						float band = step(0.74, hash(vec2(floor(row / 3.0), tick)));
						float shift = (hash(vec2(row, tick + 19.0)) - 0.5)
							* (0.008 + band * 0.16) * envelope;
						vec2 offset = vec2(shift, 0.0);
						vec2 sample_uv = clamp(UV + offset, vec2(0.0), vec2(1.0));
						float split = (0.0015 + band * 0.006) * envelope;
						vec2 red_uv = clamp(sample_uv + vec2(split, 0.0), vec2(0.0), vec2(1.0));
						vec2 blue_uv = clamp(sample_uv - vec2(split, 0.0), vec2(0.0), vec2(1.0));
						vec3 old_color = vec3(texture(TEXTURE, red_uv).r,
							texture(TEXTURE, sample_uv).g, texture(TEXTURE, blue_uv).b);
						vec3 new_color = vec3(texture(incoming, red_uv).r,
							texture(incoming, sample_uv).g, texture(incoming, blue_uv).b);
						float threshold = mix(0.08, 0.92, hash(vec2(floor(row / 3.0), 7.0)));
						float reveal = smoothstep(threshold - 0.06, threshold + 0.06, progress);
						vec3 color = mix(old_color, new_color, reveal);
						vec2 block = vec2(floor((UV.x + shift) * 18.0), row);
						float fragment = step(0.84, hash(block + vec2(tick * 3.0, tick)));
						float streak = band * fragment * envelope;
						vec3 interference = mix(vec3(0.08, 0.75, 0.95), vec3(0.75, 0.9, 0.98),
							step(0.65, hash(block + 29.0)));
						color = mix(color, interference, streak * 0.75);
						float scanline = 0.5 + 0.5 * sin(UV.y / SCREEN_PIXEL_SIZE.y * 1.57);
						color *= 1.0 - scanline * envelope * 0.08;
						COLOR = vec4(color, 1.0);
					}
					""",
			},
		};
		var transitionLayer = new CanvasLayer { Name = "SimulationTransition", Layer = 90 };
		AddChild(transitionLayer);
		_transitionSound = new AudioStreamPlayer
		{
			Name = "SimulationGlitchSound",
			Stream = GD.Load<AudioStream>("res://assets/sfx/ui/simulation_glitch.wav")
				?? throw new InvalidOperationException("Could not load simulation transition SFX."),
			Bus = AudioBuses.Sfx,
		};
		AddChild(_transitionSound);
		_overlay = new TextureRect
		{
			Name = "Glitch",
			Material = _transitionMaterial,
			Visible = false,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		transitionLayer.AddChild(_overlay);
	}

	public void Transition(Action applyPresentation)
	{
		ArgumentNullException.ThrowIfNull(applyPresentation);
		_transitionTween?.Kill();
		var generation = ++_generation;
		_isTransitioning = true;
		CaptureAndTransition(generation, applyPresentation);
	}

	public void ApplyImmediately(Action applyPresentation)
	{
		ArgumentNullException.ThrowIfNull(applyPresentation);
		_generation++;
		_transitionTween?.Kill();
		_transitionTween = null;
		_transitionSound.Stop();
		_overlay.Hide();
		_overlay.Texture = null;
		_isCapturing = false;
		_isTransitioning = false;
		applyPresentation();
	}

	public override void _ExitTree()
	{
		_generation++;
		_transitionTween?.Kill();
		_transitionSound?.Stop();
		base._ExitTree();
	}

	private async void CaptureAndTransition(int generation, Action applyPresentation)
	{
		_isCapturing = true;
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		if (!IsInsideTree() || generation != _generation)
			return;

		var snapshot = ImageTexture.CreateFromImage(GetViewport().GetTexture().GetImage());
		try
		{
			applyPresentation();
		}
		finally
		{
			if (generation == _generation)
				_isCapturing = false;
		}

		_overlay.Texture = snapshot;
		_transitionMaterial.SetShaderParameter("progress", 0f);
		_overlay.Show();
		_transitionSound.Play();
		_isCapturing = false;
		_transitionTween = CreateTween();
		_transitionTween.TweenMethod(
			Callable.From<float>(progress =>
				_transitionMaterial.SetShaderParameter("progress", progress)),
			0f,
			1f,
			TransitionSeconds);
		_transitionTween.TweenCallback(Callable.From(() => FinishTransition(generation)));
	}

	private void FinishTransition(int generation)
	{
		if (generation != _generation)
			return;
		_overlay.Hide();
		_overlay.Texture = null;
		_transitionTween = null;
		_isTransitioning = false;
	}
}
