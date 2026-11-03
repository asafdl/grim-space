using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class HighVelocityRodEffect : Node3D
{
	public const double ChargeSeconds = 1;
	public const double RevealSeconds = 0.01;
	public const double FadeSeconds = 0.25;
	public const double ImpactSeconds = ChargeSeconds + RevealSeconds;
	public float ShaftLength { get; set; } = 3.24f;
	private Tween? _animation;
	private AudioStreamPlayer3D? _fireSound;
	private Sprite3D? _ping;

	public void ShowPing()
	{
		if (IsInstanceValid(_ping))
			_ping!.QueueFree();
		const string path = "res://assets/vfx/high_velocity_rod_ping.png";
		var camera = GetViewport().GetCamera3D()
			?? throw new InvalidOperationException("Rod PING requires an active camera.");
		var sprite = new Sprite3D
		{
			Name = "RodPing",
			Texture = GD.Load<Texture2D>(path)
				?? throw new InvalidOperationException($"Could not load rod artwork '{path}'."),
			TopLevel = true,
			PixelSize = 1.8f / 584f,
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			Shaded = false,
			NoDepthTest = false,
			TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps,
			Scale = Vector3.One * 0.75f,
		};
		PresentationLayers.MarkWorld(sprite);
		AddChild(sprite);
		sprite.GlobalPosition = ToGlobal(Vector3.Left * ShaftLength * 0.5f)
			+ camera.GlobalBasis.Y * 0.65f;
		_ping = sprite;
		var pop = sprite.CreateTween();
		pop.TweenProperty(sprite, "scale", Vector3.One, 0.08)
			.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
	}
	private readonly List<ShaderMaterial> _rodMaterials = [];
	private StandardMaterial3D _flareMaterial = null!;

	public void SetFlareGlow(float strength)
	{
		var color = new Color("#ff7828");
		var brightness = Mathf.Lerp(0.35f, 4f, Mathf.Clamp(strength, 0, 1));
		_flareMaterial.AlbedoColor = new Color(color.R * brightness, color.G * brightness, color.B * brightness);
		if (IsInstanceValid(_ping))
			_ping!.Modulate = new Color(1, 1, 1, Mathf.Clamp(strength, 0, 1));
	}

	private static readonly Shader RevealShader = new()
	{
		Code = """
			shader_type spatial;
			render_mode unshaded, cull_disabled;
			uniform vec4 stroke_color : source_color;
			uniform float reveal = 1.0;
			uniform float shaft_length = 3.24;
			varying float stroke_x;
			void vertex() {
				stroke_x = VERTEX.x / shaft_length;
			}
			void fragment() {
				if (reveal <= 0.0 || (reveal < 1.0 && stroke_x < mix(0.01, -1.01, reveal))) {
					discard;
				}
				ALBEDO = stroke_color.rgb;
			}
			""",
	};

	public void SetRodReveal(float progress)
	{
		foreach (var material in _rodMaterials)
			material.SetShaderParameter("reveal", Mathf.Clamp(progress, 0, 1));
	}

	public void Play()
	{
		_animation?.Kill();
		SetRodReveal(0);
		SetFlareGlow(0);
		_animation = CreateTween();
		_animation.TweenMethod(Callable.From<float>(SetFlareGlow), 0f, 1f, ChargeSeconds)
			.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
		_animation.TweenCallback(Callable.From(() =>
			_fireSound = PresentationSfx.PlayWorldOneShot(this, Vector3.Zero, PresentationSfx.HighVelocityRodFirePath)));
		_animation.TweenCallback(Callable.From(ShowPing));
		_animation.TweenMethod(Callable.From<float>(SetRodReveal), 0f, 1f, RevealSeconds);
		_animation.TweenMethod(Callable.From<float>(SetFlareGlow), 1f, 0f, FadeSeconds);
		_animation.TweenCallback(Callable.From(() =>
		{
			foreach (var mesh in GetChildren().OfType<MeshInstance3D>())
				mesh.Visible = false;
		}));
		_animation.TweenInterval(0.15);
		_animation.TweenCallback(Callable.From(() =>
		{
			if (IsInstanceValid(_fireSound) && _fireSound!.Playing)
				_fireSound.Finished += QueueFree;
			else
				QueueFree();
		}));
	}

	public void Cancel()
	{
		_animation?.Kill();
		Visible = false;
		QueueFree();
	}

	public override void _ExitTree() => _animation?.Kill();

	public override void _Ready()
	{
		// Drawing coordinates: left is the leading tip, right is the trailing flare.
		Vector2[][] rod =
		[
			[new(10, 38), new(66, 41), new(161, 39), new(253, 37), new(321, 39)],
			[new(19, 42), new(95, 44), new(192, 42), new(285, 44), new(334, 44)],
			[new(48, 46), new(27, 51), new(107, 53), new(209, 49), new(319, 48)],
			[new(58, 52), new(115, 59), new(199, 61), new(289, 57), new(325, 53)],
			[new(88, 55), new(145, 65), new(224, 66), new(300, 61), new(327, 56)],
			[new(65, 48), new(130, 48), new(204, 46), new(283, 45)],
			[new(120, 58), new(191, 55), new(264, 53), new(313, 51)],
		];
		Vector2[][] flare =
		[
			[new(316, 10), new(337, 18), new(384, 26), new(352, 29), new(309, 32)],
			[new(293, 35), new(345, 32), new(397, 35), new(354, 39), new(333, 46)],
			[new(362, 42), new(333, 51), new(369, 47), new(331, 59), new(358, 56)],
			[new(358, 56), new(334, 65), new(369, 65), new(344, 71)],
			[new(353, 70), new(334, 76), new(397, 71), new(360, 77)],
		];
		AddStrokes([rod[0], rod[5]], new Color("#33383d"), 0.012f);
		AddStrokes([rod[1], rod[2], rod[3], rod[4], rod[6]], new Color("#6e757c"), 0.012f);
		AddStrokes(flare, new Color("#ff7828"), 0.014f, glow: true);
	}

	private void AddStrokes(Vector2[][] strokes, Color color, float radius, bool glow = false)
	{
		Material material;
		if (!glow)
		{
			var reveal = new ShaderMaterial { Shader = RevealShader };
			reveal.SetShaderParameter("stroke_color", color);
			reveal.SetShaderParameter("shaft_length", ShaftLength);
			_rodMaterials.Add(reveal);
			material = reveal;
		}
		else
		{
			_flareMaterial = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				AlbedoColor = new Color(color.R * 3, color.G * 3, color.B * 3),
				CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			};
			material = _flareMaterial;
		}
		for (var i = 0; i < strokes.Length; i++)
		{
			var depth = (i % 3 - 1) * 0.025f;
			var points = strokes[i].Select(point =>
				new Vector3((point.X - 334) * 0.01f * (glow ? 1 : ShaftLength / 3.24f),
					(43 - point.Y) * 0.01f, depth)).ToArray();
			var vertices = new List<Vector3>();
			var indices = new List<int>();
			for (var j = 0; j < points.Length; j++)
			{
				var tangent = (points[System.Math.Min(j + 1, points.Length - 1)]
					- points[System.Math.Max(j - 1, 0)]).Normalized();
				var across = tangent.Cross(Vector3.Back).Normalized();
				for (var side = 0; side < 6; side++)
				{
					var angle = Mathf.Tau * side / 6;
					vertices.Add(points[j] + radius * (across * Mathf.Cos(angle) + Vector3.Back * Mathf.Sin(angle)));
				}
				if (j == 0)
					continue;
				for (var side = 0; side < 6; side++)
				{
					var a = (j - 1) * 6 + side;
					var b = (j - 1) * 6 + (side + 1) % 6;
					var c = j * 6 + side;
					var d = j * 6 + (side + 1) % 6;
					indices.AddRange([a, c, b, b, c, d]);
				}
			}
			var arrays = new Godot.Collections.Array();
			arrays.Resize((int)Mesh.ArrayType.Max);
			arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
			arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();
			var mesh = new ArrayMesh();
			mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
			var stroke = new MeshInstance3D
			{
				Name = $"Stroke{GetChildCount()}",
				Mesh = mesh,
				MaterialOverride = material,
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			};
			PresentationLayers.MarkWorld(stroke);
			AddChild(stroke);
		}
	}
}
