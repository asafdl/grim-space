using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class GoopSprayEffect : Node3D
{
	public const double SpraySeconds = 0.72;
	public const double ImpactSeconds = SpraySeconds * 0.94;
	public const double CaptionSeconds = 0.4;

	private static readonly Vector2[][] Strokes =
	[
		[new(10, 316), new(92, 205), new(182, 102), new(252, 67), new(348, 61), new(413, 39), new(415, 10)],
		[new(23, 327), new(110, 210), new(210, 116), new(320, 44), new(425, 21)],
		[new(41, 327), new(117, 245), new(230, 168), new(334, 106), new(401, 65), new(415, 16)],
		[new(50, 316), new(143, 196), new(249, 145), new(349, 133), new(407, 176)],
		[new(47, 328), new(158, 212), new(267, 142), new(353, 118), new(403, 111), new(426, 118)],
		[new(65, 319), new(168, 230), new(287, 159), new(388, 122), new(433, 125)],
		[new(58, 327), new(179, 227), new(272, 135), new(354, 111), new(412, 87), new(416, 72)],
		[new(54, 318), new(155, 219), new(254, 160), new(354, 123), new(421, 125), new(443, 158)],
	];
	private static readonly ArrayMesh?[] Meshes = new ArrayMesh?[Strokes.Length];
	private static StandardMaterial3D? _material;
	private Tween? _flight;
	private Tween? _captionAnimation;

	public static void Play(Node host, Vector3 sourceWorldPosition, Vector3 targetWorldPosition, ulong seed)
	{
		ArgumentNullException.ThrowIfNull(host);
		var travel = targetWorldPosition - sourceWorldPosition;
		var distance = travel.Length();
		if (distance <= 0.001f)
			throw new ArgumentException("Goop spray source and target must be distinct positions.");

		var direction = travel / distance;
		var up = Vector3.Up - direction * direction.Dot(Vector3.Up);
		if (up.LengthSquared() < 0.01f)
			up = Vector3.Right - direction * direction.Dot(Vector3.Right);
		up = up.Normalized();
		var arcHeight = Mathf.Min(distance * 0.09f, 0.65f);
		var effect = new GoopSprayEffect { Name = "GoopSprayEffect" };
		host.AddChild(effect);
		effect.TopLevel = true;
		effect.GlobalPosition = sourceWorldPosition;
		for (var i = 0; i < Strokes.Length; i++)
		{
			var stroke = new MeshInstance3D
			{
				Name = $"SludgeStroke{i}",
				Mesh = Meshes[i] ??= CreateStroke(i),
				MaterialOverride = _material ??= new StandardMaterial3D
				{
					ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
					AlbedoColor = new Color(0.08f, 0.53f, 0.16f),
					CullMode = BaseMaterial3D.CullModeEnum.Disabled,
				},
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			};
			PresentationLayers.MarkWorld(stroke);
			effect.AddChild(stroke);
		}
		var variation = (seed % 17) / 16f;
		effect._flight = effect.CreateTween();
		effect._flight.TweenMethod(Callable.From<float>(progress =>
		{
			effect.GlobalPosition = sourceWorldPosition + travel * progress
				+ up * (4 * arcHeight * progress * (1 - progress));
			var tangent = (travel + up * (4 * arcHeight * (1 - 2 * progress))).Normalized();
			var right = up.Cross(tangent).Normalized();
			effect.GlobalBasis = new Basis(right, tangent.Cross(right).Normalized(), tangent);
			effect.Scale = new Vector3(0.9f + variation * 0.2f, 1, 1)
				* Mathf.Lerp(0.12f, 1, Mathf.Clamp(progress / 0.16f, 0, 1));
		}), 0f, 1f, ImpactSeconds);
		effect._flight.TweenCallback(Callable.From(() => effect.ShowCaption(targetWorldPosition)));
		effect._flight.TweenMethod(Callable.From<float>(fade =>
		{
			foreach (var stroke in effect.GetChildren().OfType<MeshInstance3D>())
				stroke.Transparency = fade;
		}), 0f, 1f, SpraySeconds - ImpactSeconds);
		effect._flight.TweenInterval(CaptionSeconds - (SpraySeconds - ImpactSeconds));
		effect._flight.TweenCallback(Callable.From(effect.QueueFree));
	}

	private void ShowCaption(Vector3 target)
	{
		var caption = new Sprite3D
		{
			Name = "SplooshCaption",
			Texture = GD.Load<Texture2D>("res://assets/vfx/goop_sploosh.png")
				?? throw new InvalidOperationException("Could not load Goop Gun SPLOOSH artwork."),
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			Shaded = false,
			NoDepthTest = false,
			PixelSize = 3f / 919f,
			TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps,
		};
		PresentationLayers.MarkWorld(caption);
		AddChild(caption);
		caption.TopLevel = true;
		caption.GlobalTransform = new Transform3D(Basis.Identity.Scaled(Vector3.One * 0.75f),
			target + Vector3.Up * 3.5f);
		_captionAnimation = caption.CreateTween();
		_captionAnimation.TweenProperty(caption, "scale", Vector3.One, 0.08)
			.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		_captionAnimation.TweenProperty(caption, "modulate", Colors.Transparent, CaptionSeconds - 0.08);
	}

	public void Cancel()
	{
		_captionAnimation?.Kill();
		_captionAnimation = null;
		_flight?.Kill();
		_flight = null;
		Visible = false;
		QueueFree();
	}

	public override void _ExitTree()
	{
		_captionAnimation?.Kill();
		_captionAnimation = null;
		_flight?.Kill();
		_flight = null;
		base._ExitTree();
	}

	private static ArrayMesh CreateStroke(int index)
	{
		var controls = Strokes[index];
		var points = new List<Vector3>();
		for (var segment = 0; segment < controls.Length - 1; segment++)
		{
			var a = controls[System.Math.Max(0, segment - 1)];
			var b = controls[segment];
			var c = controls[segment + 1];
			var d = controls[System.Math.Min(controls.Length - 1, segment + 2)];
			for (var sample = 0; sample < 8; sample++)
			{
				var t = sample / 8f;
				var pixel = 0.5f * (2 * b + (-a + c) * t
					+ (2 * a - 5 * b + 4 * c - d) * t * t
					+ (-a + 3 * b - 3 * c + d) * t * t * t);
				points.Add(ToSpatial(pixel, index));
			}
		}
		points.Add(ToSpatial(controls[^1], index));
		var vertices = new List<Vector3>();
		var rings = new Vector3[points.Count, 7];
		for (var i = 0; i < points.Count; i++)
		{
			var tangent = (points[System.Math.Min(i + 1, points.Count - 1)]
				- points[System.Math.Max(0, i - 1)]).Normalized();
			var reference = Mathf.Abs(tangent.Dot(Vector3.Up)) > 0.95f ? Vector3.Right : Vector3.Up;
			var normal = tangent.Cross(reference).Normalized();
			var binormal = tangent.Cross(normal).Normalized();
			var radius = (0.016f + index % 3 * 0.005f)
				* (0.85f + 0.18f * Mathf.Sin(i * 0.7f + index));
			for (var side = 0; side < 7; side++)
			{
				var angle = Mathf.Tau * side / 6;
				rings[i, side] = points[i]
					+ (normal * Mathf.Cos(angle) + binormal * Mathf.Sin(angle)) * radius;
			}
		}
		for (var i = 0; i < points.Count - 1; i++)
			for (var side = 0; side < 6; side++)
				vertices.AddRange(
				[
					rings[i, side], rings[i + 1, side], rings[i, side + 1],
					rings[i, side + 1], rings[i + 1, side], rings[i + 1, side + 1],
				]);
		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
		var mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		return mesh;
	}

	private static Vector3 ToSpatial(Vector2 pixel, int index)
	{
		var offset = pixel - new Vector2(410, 100);
		return new Vector3(
			(index - 3.5f) * 0.022f + 0.025f * Mathf.Sin(pixel.X * 0.02f + index),
			-offset.Dot(new Vector2(0.524f, 0.852f)) * 0.0055f,
			offset.Dot(new Vector2(0.852f, -0.524f)) * 0.0055f);
	}
}
