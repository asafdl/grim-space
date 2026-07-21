using System.Collections.Generic;
using Godot;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed class Selection
{
	public int? HoveredIndex { get; private set; }

	public void SetHover(int? index, int optionCount) =>
		HoveredIndex = ClampIndex(index, optionCount);

	public void Clear() => HoveredIndex = null;

	public void ClampToCount(int optionCount) =>
		HoveredIndex = ClampIndex(HoveredIndex, optionCount);

	private static int? ClampIndex(int? index, int optionCount)
	{
		if (index is not int i || i < 0 || i >= optionCount)
			return null;

		return i;
	}
}

public static class MovementSelection
{
	public static (IReadOnlyList<Coord> Path, Coord? Target) GetHighlights(
		IReadOnlyList<Option> options,
		int? hoveredIndex)
	{
		if (hoveredIndex is not int i)
			return ([], null);

		return (options[i].Path, options[i].EndPosition);
	}

	public static (IReadOnlyList<Coord> Path, Coord? Target) WithCommittedMove(
		IReadOnlyList<IAction> actions,
		IReadOnlyList<Coord> path,
		Coord? target)
	{
		if (path.Count > 0 || target is not null)
			return (path, target);

		var movePath = actions.OfType<MovePathAction>().FirstOrDefault();
		if (movePath is not null)
			return (movePath.Option.Path, movePath.Option.EndPosition);

		var moveSteps = actions.OfType<MoveStepAction>().ToList();
		if (moveSteps.Count == 0)
			return (path, target);

		var committedPath = moveSteps.Select(step => step.To).ToList();
		return (committedPath, committedPath[^1]);
	}

	public static string FormatMomentum(State unit)
	{
		var config = MomentumConfig.ForLevel(unit.MomentumLevel);
		var evasion = (int)(config.Evasion * 100);
		return $"M{unit.MomentumLevel} ({evasion}% eva)";
	}

	private const float PickRadius = 1.4f;

	public static int? PickOptionIndex(Camera3D camera, Vector2 screenPos, IReadOnlyList<Option> options)
	{
		if (options.Count == 0)
			return null;

		var origin = camera.ProjectRayOrigin(screenPos);
		var direction = camera.ProjectRayNormal(screenPos);

		int? bestIndex = null;
		var bestDistance = PickRadius;

		for (var i = 0; i < options.Count; i++)
		{
			var world = WorldMapping.ToWorld(options[i].EndPosition);
			var distance = DistanceRayToPoint(origin, direction, world);
			if (distance >= bestDistance)
				continue;

			bestDistance = distance;
			bestIndex = i;
		}

		return bestIndex;
	}

	private static float DistanceRayToPoint(Vector3 origin, Vector3 direction, Vector3 point)
	{
		var toPoint = point - origin;
		var t = Mathf.Clamp(toPoint.Dot(direction), 0f, 200f);
		var closest = origin + direction * t;
		return closest.DistanceTo(point);
	}
}
