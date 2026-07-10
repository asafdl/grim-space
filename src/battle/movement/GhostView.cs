using System.Collections.Generic;
using Godot;
using GrimSpace.Domain.Grid;
using GridView = GrimSpace.Battle.Grid.View;

namespace GrimSpace.Battle.Movement;

public partial class GhostView : Node3D
{
	private const float PickRadius = 1.4f;

	private readonly List<MeshInstance3D> _ghosts = new();
	private StandardMaterial3D? _ghostMaterial;
	private StandardMaterial3D? _hoverMaterial;
	private StandardMaterial3D? _selectedMaterial;

	public void ShowOptions(
		IReadOnlyList<Option> options,
		int? hoveredIndex,
		int? selectedIndex)
	{
		EnsureMaterials();
		ClearGhosts();

		for (var i = 0; i < options.Count; i++)
		{
			var material = i == selectedIndex
				? _selectedMaterial
				: i == hoveredIndex
					? _hoverMaterial
					: _ghostMaterial;

			var ghost = new MeshInstance3D
			{
				Mesh = new BoxMesh { Size = new Vector3(1.2f, 0.8f, 1.8f) },
				Position = GridView.ToWorld(options[i].EndPosition),
				MaterialOverride = material,
			};
			AddChild(ghost);
			_ghosts.Add(ghost);
		}
	}

	public int? PickOptionIndex(Camera3D camera, Vector2 screenPos, IReadOnlyList<Option> options)
	{
		if (options.Count == 0)
			return null;

		var origin = camera.ProjectRayOrigin(screenPos);
		var direction = camera.ProjectRayNormal(screenPos);

		int? bestIndex = null;
		var bestDistance = PickRadius;

		for (var i = 0; i < options.Count; i++)
		{
			var world = GridView.ToWorld(options[i].EndPosition);
			var distance = DistanceRayToPoint(origin, direction, world);
			if (distance >= bestDistance)
				continue;

			bestDistance = distance;
			bestIndex = i;
		}

		return bestIndex;
	}

	private void EnsureMaterials()
	{
		_ghostMaterial ??= new StandardMaterial3D
		{
			AlbedoColor = new Color(0.35f, 0.75f, 1f, 0.35f),
			Roughness = 0.6f,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
		};

		_hoverMaterial ??= new StandardMaterial3D
		{
			AlbedoColor = new Color(0.5f, 0.9f, 1f, 0.55f),
			Roughness = 0.6f,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
		};

		_selectedMaterial ??= new StandardMaterial3D
		{
			AlbedoColor = new Color(0.2f, 1f, 0.55f, 0.6f),
			Roughness = 0.6f,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
		};
	}

	private static float DistanceRayToPoint(Vector3 origin, Vector3 direction, Vector3 point)
	{
		var toPoint = point - origin;
		var t = Mathf.Clamp(toPoint.Dot(direction), 0f, 200f);
		var closest = origin + direction * t;
		return closest.DistanceTo(point);
	}

	private void ClearGhosts()
	{
		foreach (var ghost in _ghosts)
			ghost.QueueFree();

		_ghosts.Clear();
	}
}
