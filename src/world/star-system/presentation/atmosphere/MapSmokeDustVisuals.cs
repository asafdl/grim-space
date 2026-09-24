using Godot;
using GrimSpace.Math;

namespace GrimSpace.World.StarSystem.Presentation.Atmosphere;

public static class MapSmokeDustVisuals
{
	public const string DefaultSmokeTexturePath =
		"res://assets/textures/particles/smoke_02.png";

	private static Texture2D? _defaultTexture;

	public static Texture2D LoadDefaultSmokeTexture() =>
		_defaultTexture ??= GD.Load<Texture2D>(DefaultSmokeTexturePath);

	public static MultiMeshInstance3D CreateLayer(
		Texture2D texture,
		float quadSize,
		IReadOnlyList<SmokeDustCard> cards,
		string nodeName = "SmokeDust")
	{
		var drawMaterial = CreateBillboardMaterial(texture);
		var multiMesh = new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			UseColors = true,
			InstanceCount = cards.Count,
			Mesh = new QuadMesh
			{
				Size = Vector2.One * quadSize,
				Material = drawMaterial,
			},
		};

		for (var i = 0; i < cards.Count; i++)
		{
			var card = cards[i];
			multiMesh.SetInstanceTransform(
				i,
				new Transform3D(Basis.Identity.Scaled(Vector3.One * card.Scale), card.Position));
			multiMesh.SetInstanceColor(i, card.VertexColor);
		}

		return new MultiMeshInstance3D
		{
			Name = nodeName,
			Multimesh = multiMesh,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
	}

	public static MultiMeshInstance3D CreateRegionalMiningLayer(
		Texture2D texture,
		float worldRadius,
		int amount,
		float opacity,
		StableRandom random)
	{
		var quadSize = 0.55f + worldRadius * 0.22f;
		var cards = new List<SmokeDustCard>(amount);
		for (var i = 0; i < amount; i++)
		{
			var angle = random.NextDouble() * System.Math.Tau;
			var distance = random.NextDouble() * worldRadius * 0.88;
			var lift = (random.NextDouble() - 0.5) * worldRadius * 0.18;
			var position = new Vector3(
				(float)(System.Math.Cos(angle) * distance),
				(float)lift,
				(float)(System.Math.Sin(angle) * distance));
			var scale = 0.7f + (float)random.NextDouble() * 1.1f;
			var tone = 0.42f + (float)random.NextDouble() * 0.18f;
			var alpha = opacity * (0.45f + (float)random.NextDouble() * 0.55f);
			cards.Add(new SmokeDustCard(
				position,
				scale,
				new Color(tone * 0.72f, tone * 0.66f, tone * 0.58f, alpha)));
		}

		return CreateLayer(texture, quadSize, cards, "RegionalDust");
	}

	public static MultiMeshInstance3D CreateSingleBillboardLayer(
		Texture2D texture,
		float quadSize,
		float opacityMultiplier = 0.12f,
		Color? tint = null)
	{
		var tone = tint ?? new Color(0.42f, 0.38f, 0.34f, 1f);
		SmokeDustCard[] cards =
		[
			new(
				Vector3.Zero,
				1f,
				new Color(tone.R, tone.G, tone.B, opacityMultiplier)),
		];
		return CreateLayer(texture, quadSize, cards, "SmokeBillboard");
	}

	private static StandardMaterial3D CreateBillboardMaterial(Texture2D texture) =>
		new()
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			BlendMode = BaseMaterial3D.BlendModeEnum.Mix,
			DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
			AlbedoTexture = texture,
			VertexColorUseAsAlbedo = true,
			BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
			BillboardKeepScale = true,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
		};

	public readonly record struct SmokeDustCard(Vector3 Position, float Scale, Color VertexColor);
}
