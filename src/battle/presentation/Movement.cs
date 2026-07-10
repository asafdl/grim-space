using System;
using System.Collections.Generic;
using System.Linq;
using GrimSpace.Domain.Grid;
using GrimSpace.Battle.Movement;

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
	public static (List<Coord> Endpoints, IReadOnlyList<Coord> Path, Coord? Target) GetHighlights(
		IReadOnlyList<Option> options,
		int? selectedIndex,
		int? hoveredIndex)
	{
		var endpoints = options.Select(p => p.EndPosition).ToList();
		var active = selectedIndex ?? hoveredIndex;
		if (active is not int i)
			return (endpoints, [], null);

		return (endpoints, options[i].Path, options[i].EndPosition);
	}

	public static string FormatOption(Option option) =>
		option.Lateral is null ? "4 forward" : $"3 forward + 1 {option.Lateral}";

	public static string BuildHint(
		IReadOnlyList<Option> options,
		int? selectedIndex,
		int? hoveredIndex,
		int currentAp,
		Func<Option, int> getApCost)
	{
		if (selectedIndex is int selected)
		{
			var option = options[selected];
			return $"Selected: {FormatOption(option)}  |  AP cost: {getApCost(option)}  |  Click again to confirm  |  AP: {currentAp}";
		}

		if (hoveredIndex is int hovered)
		{
			var option = options[hovered];
			return $"Hover: {FormatOption(option)}  |  AP cost: {getApCost(option)}  |  Click to select  |  AP: {currentAp}";
		}

		return $"4-step move preview  |  All forward = 0 AP, lateral = 1 AP  |  AP: {currentAp}  |  +/-: zoom  |  RMB: orbit";
	}
}
