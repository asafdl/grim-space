using Godot;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class BoardHazardView : Node3D
{
	private const float CellFit = 0.94f;

	private static readonly Coord[] PositiveFaceOffsets =
	[
		new(1, 0, 0),
		new(0, 1, 0),
		new(0, 0, 1),
	];

	// Strong value/chroma separation so differences survive warm key light.
	private static readonly Color[] RockBases =
	[
		new(0.58f, 0.56f, 0.54f),
		new(0.2f, 0.2f, 0.22f),
		new(0.32f, 0.4f, 0.48f),
		new(0.36f, 0.42f, 0.3f),
		new(0.5f, 0.3f, 0.22f),
		new(0.42f, 0.4f, 0.45f),
		new(0.55f, 0.48f, 0.4f),
		new(0.28f, 0.34f, 0.4f),
		new(0.45f, 0.44f, 0.4f),
		new(0.3f, 0.36f, 0.32f),
	];

	public void Build(IReadOnlyList<Hazard> hazards)
	{
		foreach (var hazard in hazards)
		{
			if (hazard.Kind == EHazardKind.Asteroid)
				AddChild(CreateAsteroid(hazard));
		}
	}

	private static Node3D CreateAsteroid(Hazard hazard)
	{
		var familyRng = RngFor(hazard.Center);
		var baseColor = RockBases[familyRng.RandiRange(0, RockBases.Length - 1)];

		var root = new Node3D { Name = hazard.Id };
		foreach (var cell in hazard.Cells)
			root.AddChild(CreateCellRock(cell, baseColor));

		foreach (var cell in hazard.Cells)
		{
			foreach (var offset in PositiveFaceOffsets)
			{
				var other = cell + offset;
				if (hazard.Cells.Contains(other))
					root.AddChild(CreateCellBridge(cell, other, baseColor));
			}
		}

		return root;
	}

	private static MeshInstance3D CreateCellRock(Coord cell, Color baseColor)
	{
		var rng = RngFor(cell);
		var fit = WorldMapping.CellSize * CellFit;
		var size = new Vector3(
			fit * rng.RandfRange(0.88f, 1f),
			fit * rng.RandfRange(0.88f, 1f),
			fit * rng.RandfRange(0.88f, 1f));

		return new MeshInstance3D
		{
			Name = $"rock_{cell.X}_{cell.Y}_{cell.Z}",
			Position = WorldMapping.ToWorld(cell),
			Mesh = AsteroidMesh.Create(size, rng),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = CreateRockMaterial(baseColor, rng),
		};
	}

	private static MeshInstance3D CreateCellBridge(Coord a, Coord b, Color baseColor)
	{
		var rng = RngFor(a + b);
		var from = WorldMapping.ToWorld(a);
		var to = WorldMapping.ToWorld(b);
		var mid = (from + to) * 0.5f;
		var delta = to - from;
		var fit = WorldMapping.CellSize * CellFit;
		var size = new Vector3(
			fit * rng.RandfRange(0.9f, 1f),
			fit * rng.RandfRange(0.9f, 1f),
			delta.Length() * 2f * CellFit * rng.RandfRange(0.92f, 1f));

		var direction = delta.Normalized();
		var up = Mathf.Abs(direction.Dot(Vector3.Up)) > 0.95f ? Vector3.Right : Vector3.Up;

		return new MeshInstance3D
		{
			Name = $"bridge_{a.X}_{a.Y}_{a.Z}_{b.X}_{b.Y}_{b.Z}",
			Position = mid,
			Basis = Basis.LookingAt(direction, up),
			Mesh = AsteroidMesh.Create(size, rng),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = CreateRockMaterial(baseColor, rng),
		};
	}

	private static StandardMaterial3D CreateRockMaterial(Color baseColor, RandomNumberGenerator rng)
	{
		var albedo = new Color(
			Mathf.Clamp(baseColor.R + rng.RandfRange(-0.1f, 0.1f), 0.12f, 0.72f),
			Mathf.Clamp(baseColor.G + rng.RandfRange(-0.1f, 0.1f), 0.12f, 0.72f),
			Mathf.Clamp(baseColor.B + rng.RandfRange(-0.1f, 0.1f), 0.12f, 0.72f));

		return new StandardMaterial3D
		{
			AlbedoColor = albedo,
			Roughness = rng.RandfRange(0.78f, 0.98f),
			Metallic = rng.RandfRange(0.01f, 0.1f),
		};
	}

	private static RandomNumberGenerator RngFor(Coord center)
	{
		var rng = new RandomNumberGenerator();
		rng.Seed = (ulong)(center.X * 73856093 ^ center.Y * 19349663 ^ center.Z * 83492791);
		return rng;
	}
}
