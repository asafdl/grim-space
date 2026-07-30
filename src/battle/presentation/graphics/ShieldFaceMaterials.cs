using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

internal static class ShieldFaceMaterials
{
	private static readonly Dictionary<MaterialKey, StandardMaterial3D> Cache = new();

	public static StandardMaterial3D For(Color hullColor, int points, int maxPoints)
	{
		var key = new MaterialKey(hullColor, points, maxPoints);
		if (Cache.TryGetValue(key, out var material))
			return material;

		material = Create(hullColor, points, maxPoints);
		Cache[key] = material;
		return material;
	}

	private static StandardMaterial3D Create(Color hullColor, int points, int maxPoints)
	{
		var material = new StandardMaterial3D
		{
			Roughness = 0.45f,
			Metallic = 0.15f,
		};

		if (points <= 0 || maxPoints <= 0)
		{
			material.AlbedoColor = hullColor;
			return material;
		}

		var warn = new Color(1f, 0.82f, 0.3f);
		var isFull = points >= maxPoints;
		material.AlbedoColor = isFull
			? hullColor.Lightened(0.18f)
			: hullColor.Lerp(warn, 0.4f);

		return material;
	}

	private readonly record struct MaterialKey(Color HullColor, int Points, int MaxPoints);
}
