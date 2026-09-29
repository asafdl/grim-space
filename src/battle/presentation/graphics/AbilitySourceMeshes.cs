using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

internal static class AbilitySourceMeshes
{
	public static Node3D CreateScrapDroneSwarmBurst() => ScrapDroneMesh.CreateMountPreview();

	public static Node3D CreateLightningCannon() =>
		Ghost(new BoxMesh
		{
			Size = new Vector3(0.18f, 0.18f, 1.35f),
		});

	public static Node3D CreateTorpedo() => TorpedoMesh.CreateHullInstance();

	public static Node3D CreatePatrol() => Ghost(PatrolMesh.CreatePreviewHull());

	public static Node3D CreateDetonation() =>
		Ghost(new SphereMesh
		{
			Radius = 0.68f,
			Height = 1.36f,
			RadialSegments = 16,
			Rings = 8,
		});

	private static Node3D Ghost(Mesh mesh) => new MeshInstance3D { Mesh = mesh };
}
