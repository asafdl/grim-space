using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

internal static class ShieldFaceMaterials
{
	private static readonly StandardMaterial3D Full =
		WeaponPreviewMaterials.CreateWireframe(new Color(0.2f, 0.62f, 1f, 0.82f));

	private static readonly StandardMaterial3D Damaged =
		WeaponPreviewMaterials.CreateWireframe(new Color(1f, 0.42f, 0.08f, 0.9f));

	public static StandardMaterial3D For(int points, int maxPoints) =>
		points >= maxPoints ? Full : Damaged;
}
