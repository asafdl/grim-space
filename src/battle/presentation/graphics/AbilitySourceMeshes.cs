using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

internal static class AbilitySourceMeshes
{
	public static Mesh CreateFlakBurst() =>
		new SphereMesh
		{
			Radius = 0.42f,
			Height = 0.84f,
			RadialSegments = 12,
			Rings = 6,
		};

	public static Mesh CreateRailgun() =>
		new BoxMesh
		{
			Size = new Vector3(0.18f, 0.18f, 1.35f),
		};

	public static Mesh CreateTorpedo() => TorpedoMesh.CreateHull();

	public static Mesh CreatePatrol() => PatrolMesh.CreatePreviewHull();

	public static Mesh CreateDetonation() =>
		new SphereMesh
		{
			Radius = 0.68f,
			Height = 1.36f,
			RadialSegments = 16,
			Rings = 8,
		};
}
