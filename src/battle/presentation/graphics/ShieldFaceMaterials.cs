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
		var warn = new Color(1f, 0.88f, 0.15f);

		var material = new StandardMaterial3D
		{
			Roughness = 0.45f,
			Metallic = 0.1f,
		};

		if (maxPoints <= 0)
		{
			material.AlbedoColor = hullColor;
			material.EmissionEnabled = false;
			return material;
		}

		if (points <= 0)
		{
			material.AlbedoColor = hullColor.Lerp(warn, 0.55f);
			material.EmissionEnabled = true;
			material.Emission = warn;
			material.EmissionEnergyMultiplier = 0.85f;
			return material;
		}

		if (points >= maxPoints)
		{
			material.AlbedoColor = hullColor.Lightened(0.12f);
			material.EmissionEnabled = true;
			material.Emission = hullColor.Lightened(0.35f);
			material.EmissionEnergyMultiplier = 0.25f;
			return material;
		}

		var damage = 1f - (float)points / maxPoints;
		material.AlbedoColor = hullColor.Lerp(warn, 0.35f + damage * 0.25f);
		material.EmissionEnabled = true;
		material.Emission = warn;
		material.EmissionEnergyMultiplier = 0.45f + damage * 0.35f;

		return material;
	}

	private readonly record struct MaterialKey(Color HullColor, int Points, int MaxPoints);
}
