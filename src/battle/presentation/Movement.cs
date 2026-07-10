using System.Collections.Generic;
using Godot;
using GrimSpace.Domain.Grid;
using GrimSpace.Battle.Movement;
using GridView = GrimSpace.Battle.Grid.View;

namespace GrimSpace.Battle.Presentation;

public sealed class Selection
{
	public int? HoveredIndex { get; private set; }
	public int? SelectedIndex { get; private set; }

	public void SetHover(int? index, int optionCount) =>
		HoveredIndex = ClampIndex(index, optionCount);

	public ClickResult OnClick(int? pickedIndex, int optionCount)
	{
		var picked = ClampIndex(pickedIndex, optionCount);
		if (picked is null)
			return ClickResult.Ignored;

		if (SelectedIndex != picked)
		{
			SelectedIndex = picked;
			return ClickResult.Selected;
		}

		return ClickResult.Confirm;
	}

	public void Clear()
	{
		SelectedIndex = null;
		HoveredIndex = null;
	}

	public void ClampToCount(int optionCount)
	{
		HoveredIndex = ClampIndex(HoveredIndex, optionCount);
		SelectedIndex = ClampIndex(SelectedIndex, optionCount);
	}

	private static int? ClampIndex(int? index, int optionCount)
	{
		if (index is not int i || i < 0 || i >= optionCount)
			return null;

		return i;
	}
}

public enum ClickResult
{
	Ignored,
	Selected,
	Confirm,
}

public static class Movement
{
	public static (IReadOnlyList<Coord> Path, Coord? Target) GetHighlights(
		IReadOnlyList<Option> options,
		int? selectedIndex,
		int? hoveredIndex)
	{
		var active = selectedIndex ?? hoveredIndex;
		if (active is not int i)
			return ([], null);

		return (options[i].Path, options[i].EndPosition);
	}

	public static string FormatOption(Option option) =>
		$"{option.Path.Count} cells ({option.ApCost} AP)";

	public static string BuildHint(
		IReadOnlyList<Option> options,
		int? selectedIndex,
		int? hoveredIndex,
		int currentAp)
	{
		if (selectedIndex is int selected)
			return $"Selected: {FormatOption(options[selected])}  |  Click again to confirm  |  AP: {currentAp}";

		if (hoveredIndex is int hovered)
			return $"Hover: {FormatOption(options[hovered])}  |  Click to select  |  AP: {currentAp}";

		return $"Move preview  |  min 3 AP, no opposing steps  |  forward/lateral 1 AP/step, retro 2 AP/step  |  AP: {currentAp}  |  scroll/+/-: zoom  |  RMB: orbit";
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
			var world = GridView.ToWorld(options[i].EndPosition);
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
