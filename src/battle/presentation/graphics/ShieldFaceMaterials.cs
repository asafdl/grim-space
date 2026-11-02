using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

internal static class ShieldFaceMaterials
{
	private static readonly Color Full = new(0.35f, 0.75f, 1f, 0.82f);
	private static readonly Color Depleted = new(0.16f, 0.24f, 0.32f, 0.6f);
	private static readonly Color Temporary = new(0.7f, 0.4f, 1f, 0.86f);

	public static Color TintFor(int points, int maxPoints, int temporaryPoints)
	{
		if (temporaryPoints > 0)
			return Temporary;

		var fraction = maxPoints > 0 ? Mathf.Clamp((float)points / maxPoints, 0f, 1f) : 0f;
		return Depleted.Lerp(Full, fraction);
	}

	public static StandardMaterial3D Create()
	{
		var material = WeaponPreviewMaterials.CreateWireframe(Full);
		material.EmissionEnabled = true;
		material.EmissionEnergyMultiplier = 0f;
		return material;
	}
}
