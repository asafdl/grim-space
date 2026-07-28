using System.Collections.Generic;
using Godot;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>
/// Movement trail segments for the last resolved turn.
/// </summary>
public partial class TurnHistoryView : Node3D
{
	private const float TrailYOffset = 0.18f;
	private const float TrailThickness = 0.07f;

	private readonly Dictionary<string, Coord> _lastPositions = new();
	private readonly List<Node3D> _ownedNodes = new();

	public void BeginTurn(IReadOnlyDictionary<string, Coord> startPositions)
	{
		Clear();
		foreach (var (actorId, position) in startPositions)
			_lastPositions[actorId] = position;
	}

	public void Clear()
	{
		foreach (var node in _ownedNodes)
			node.Free();

		_ownedNodes.Clear();
		_lastPositions.Clear();
	}

	public void RecordMove(string actorId, Coord from, Coord to, Color color)
	{
		if (from == to)
			return;

		AddTrailSegment(from, to, color);
		_lastPositions[actorId] = to;
	}

	private void AddTrailSegment(Coord from, Coord to, Color color)
	{
		var start = WorldMapping.ToWorld(from) + Vector3.Up * TrailYOffset;
		var end = WorldMapping.ToWorld(to) + Vector3.Up * TrailYOffset;
		var delta = end - start;
		var length = delta.Length();
		if (length < 0.001f)
			return;

		var segment = new MeshInstance3D
		{
			Mesh = new CylinderMesh
			{
				TopRadius = TrailThickness,
				BottomRadius = TrailThickness,
				Height = length,
			},
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = color,
				EmissionEnabled = true,
				Emission = color,
				EmissionEnergyMultiplier = 0.35f,
				Roughness = 0.5f,
			},
		};

		segment.Position = (start + end) * 0.5f;
		segment.Basis = BasisAlignedToDirection(delta);
		AddChild(segment);
		_ownedNodes.Add(segment);
	}

	private static Basis BasisAlignedToDirection(Vector3 direction)
	{
		var axis = direction.Normalized();
		if (axis.LengthSquared() < 0.0001f)
			return Basis.Identity;

		var reference = Mathf.Abs(axis.Dot(Vector3.Up)) > 0.99f ? Vector3.Forward : Vector3.Up;
		var tangent = reference.Cross(axis).Normalized();
		var bitangent = axis.Cross(tangent);
		return new Basis(tangent, axis, bitangent);
	}
}
