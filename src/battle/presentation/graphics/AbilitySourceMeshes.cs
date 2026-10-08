using System.Collections.Generic;
using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

internal static class AbilitySourceMeshes
{
	private static ArrayMesh? _lightningBoltMesh;

	public static Node3D CreateScrapDroneSwarmBurst() => ScrapDroneMesh.CreateMountPreview();

	public static Node3D CreateLightningCannon() => Ghost(GetLightningBoltMesh());

	public static Node3D CreateGoopGun() =>
		Ghost(new SphereMesh
		{
			Radius = 0.34f,
			Height = 0.68f,
			RadialSegments = 12,
			Rings = 6,
		});

	public static Node3D CreateVoidBomb() => VoidBombMesh.CreateHullInstance();

	public static Node3D CreateRepurposedMiner() => Ghost(RepurposedMinerMesh.CreatePreviewHull());

	public static Node3D CreateDetonation() =>
		Ghost(new SphereMesh
		{
			Radius = 0.68f,
			Height = 1.36f,
			RadialSegments = 16,
			Rings = 8,
		});

	private static Node3D Ghost(Mesh mesh) => new MeshInstance3D { Mesh = mesh };

	private static Mesh GetLightningBoltMesh()
	{
		if (_lightningBoltMesh is not null)
			return _lightningBoltMesh;

		var profile = new (float X, float Z)[]
		{
			(0.000f, 0.650f),   // P0: front tip
			(0.040f, 0.160f),   // P1: left inner notch
			(-0.200f, 0.040f),  // P2: left elbow
			(0.000f, -0.650f),  // P3: rear tail
			(0.040f, -0.160f),  // P4: right inner notch
			(0.240f, -0.040f),  // P5: right elbow
		};

		const float halfHeight = 0.08f;
		var vertices = new List<Vector3>(60);
		var normals = new List<Vector3>(60);

		void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
		{
			vertices.Add(a);
			vertices.Add(b);
			vertices.Add(c);
			normals.Add(normal);
			normals.Add(normal);
			normals.Add(normal);
		}

		// Top face (normal = (0, 1, 0))
		var topTriangles = new (int I0, int I1, int I2)[]
		{
			(5, 1, 0),
			(5, 2, 1),
			(2, 4, 3),
			(2, 5, 4),
		};
		foreach (var (i0, i1, i2) in topTriangles)
		{
			AddTriangle(
				new Vector3(profile[i0].X, halfHeight, profile[i0].Z),
				new Vector3(profile[i1].X, halfHeight, profile[i1].Z),
				new Vector3(profile[i2].X, halfHeight, profile[i2].Z),
				Vector3.Up);
		}

		// Bottom face (normal = (0, -1, 0))
		var botTriangles = new (int I0, int I1, int I2)[]
		{
			(5, 0, 1),
			(5, 1, 2),
			(2, 3, 4),
			(2, 4, 5),
		};
		foreach (var (i0, i1, i2) in botTriangles)
		{
			AddTriangle(
				new Vector3(profile[i0].X, -halfHeight, profile[i0].Z),
				new Vector3(profile[i1].X, -halfHeight, profile[i1].Z),
				new Vector3(profile[i2].X, -halfHeight, profile[i2].Z),
				Vector3.Down);
		}

		// Side walls (6 quads = 12 triangles)
		for (var i = 0; i < profile.Length; i++)
		{
			var j = (i + 1) % profile.Length;
			var topA = new Vector3(profile[i].X, halfHeight, profile[i].Z);
			var topB = new Vector3(profile[j].X, halfHeight, profile[j].Z);
			var botA = new Vector3(profile[i].X, -halfHeight, profile[i].Z);
			var botB = new Vector3(profile[j].X, -halfHeight, profile[j].Z);

			var edgeDir = topB - topA;
			var outwardNormal = Vector3.Up.Cross(edgeDir).Normalized();

			AddTriangle(topA, topB, botA, outwardNormal);
			AddTriangle(topB, botB, botA, outwardNormal);
		}

		var roll = new Basis(new Vector3(0f, 0f, 1f), Mathf.Pi * 0.5f);
		for (var i = 0; i < vertices.Count; i++)
		{
			vertices[i] = roll * vertices[i];
			normals[i] = roll * normals[i];
		}

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
		arrays[(int)Mesh.ArrayType.Normal] = normals.ToArray();

		var mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		_lightningBoltMesh = mesh;
		return mesh;
	}
}
