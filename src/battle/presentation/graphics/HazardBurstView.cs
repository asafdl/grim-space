using System.Collections.Generic;
using Godot;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>
/// Faint markers showing where hazards resolved during turn replay.
/// </summary>
public partial class HazardBurstView : Node3D
{
	private const float CellAlpha = 0.38f;
	private const float CellScale = 0.88f;
	private const float CenterRadius = 0.22f;

	private readonly List<Node3D> _markers = new();

	public void Clear()
	{
		foreach (var marker in _markers)
			marker.Free();

		_markers.Clear();
	}

	public void RecordBurst(EHazardKind kind, IReadOnlyCollection<Coord> cells)
	{
		if (cells.Count == 0)
			return;

		var fill = kind switch
		{
			EHazardKind.MissileZone => new Color(0.95f, 0.28f, 0.12f, CellAlpha),
			EHazardKind.FlakBurst => new Color(0.95f, 0.68f, 0.14f, CellAlpha),
			EHazardKind.RailgunBurst => new Color(0.85f, 0.35f, 1f, CellAlpha),
			EHazardKind.TorpedoBlast => new Color(0.2f, 0.85f, 0.55f, CellAlpha),
			_ => new Color(0.9f, 0.4f, 0.2f, CellAlpha),
		};

		foreach (var cell in cells)
			AddCellMarker(cell, fill);

		AddCenterMarker(Centroid(cells), fill with { A = 0.9f });
	}

	private void AddCellMarker(Coord cell, Color color)
	{
		var marker = new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = Vector3.One * WorldMapping.CellSize * CellScale },
			Position = WorldMapping.ToWorld(cell),
			MaterialOverride = CreateMaterial(color, emissionEnergy: 0.25f),
		};
		PresentationLayers.MarkUx(marker);
		AddChild(marker);
		_markers.Add(marker);
	}

	private void AddCenterMarker(Coord center, Color color)
	{
		var marker = new MeshInstance3D
		{
			Mesh = new SphereMesh { Radius = CenterRadius, Height = CenterRadius * 2f },
			Position = WorldMapping.ToWorld(center) + Vector3.Up * 0.15f,
			MaterialOverride = CreateMaterial(color, emissionEnergy: 0.8f),
		};
		PresentationLayers.MarkUx(marker);
		AddChild(marker);
		_markers.Add(marker);
	}

	private static StandardMaterial3D CreateMaterial(Color color, float emissionEnergy) =>
		new()
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			AlbedoColor = color,
			EmissionEnabled = true,
			Emission = color with { A = 1f },
			EmissionEnergyMultiplier = emissionEnergy,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
		};

	private static Coord Centroid(IEnumerable<Coord> cells)
	{
		long x = 0;
		long y = 0;
		long z = 0;
		var count = 0;

		foreach (var cell in cells)
		{
			x += cell.X;
			y += cell.Y;
			z += cell.Z;
			count++;
		}

		if (count == 0)
			return Coord.Zero;

		return new Coord((int)(x / count), (int)(y / count), (int)(z / count));
	}
}
