using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Ui;

public static class MovePreviewRings
{
	public readonly struct MovePreviewRingTable
	{
		public int RingCount { get; }
		public ERingBandPreset Preset { get; }
		private readonly MoveRingFacets.OptionFacets[] _groupFacets;
		private readonly int[][] _optionIndicesOnRing;

		public MovePreviewRingTable(
			ERingBandPreset preset,
			int ringCount,
			MoveRingFacets.OptionFacets[] groupFacets,
			int[][] optionIndicesOnRing)
		{
			Preset = preset;
			RingCount = ringCount;
			_groupFacets = groupFacets;
			_optionIndicesOnRing = optionIndicesOnRing;
		}

		public IReadOnlyList<int> OptionIndicesOnRing(int ringIndex) => _optionIndicesOnRing[ringIndex];

		public MoveRingFacets.OptionFacets RingFacetsAt(int ringIndex) => _groupFacets[ringIndex];

		public string FormatRingHint(int ringIndex) =>
			_groupFacets[ringIndex].FormatHint(
				Preset,
				OptionIndicesOnRing(ringIndex).Count);
	}

	public static MovePreviewRingTable BuildRingTable(
		BodyFrame bodyFrame,
		Coord actor,
		IReadOnlyList<Option> options,
		ERingBandPreset preset = ERingBandPreset.ApMomentum)
	{
		var grouping = RingBandPresetLabels.GroupingFacets(preset);

		var bestApCost = new Dictionary<Coord, int>();
		var bestOption = new Dictionary<Coord, int>();
		for (var i = 0; i < options.Count; i++)
		{
			var option = options[i];
			var apCost = option.ApCost;
			if (!bestOption.TryGetValue(option.EndPosition, out var bestIndex)
				|| apCost < bestApCost[option.EndPosition]
				|| (apCost == bestApCost[option.EndPosition] && i < bestIndex))
			{
				bestApCost[option.EndPosition] = apCost;
				bestOption[option.EndPosition] = i;
			}
		}

		var groups = new List<(MoveRingFacets.OptionFacets Key, List<int> Indices)>();
		foreach (var optionIndex in bestOption.Values)
		{
			var classified = MoveRingFacets.OptionFacets.Classify(bodyFrame, actor, options[optionIndex]);
			var groupIndex = FindGroup(groups, classified, grouping);
			if (groupIndex < 0)
			{
				groups.Add((classified, [optionIndex]));
				continue;
			}

			var entry = groups[groupIndex];
			entry.Indices.Add(optionIndex);
			groups[groupIndex] = entry;
		}

		groups.Sort((left, right) => left.Key.CompareForPreset(right.Key, preset));

		var groupFacets = new MoveRingFacets.OptionFacets[groups.Count];
		var optionIndicesOnRing = new int[groups.Count][];
		for (var ringIndex = 0; ringIndex < groups.Count; ringIndex++)
		{
			var indices = groups[ringIndex].Indices;
			indices.Sort();
			optionIndicesOnRing[ringIndex] = indices.ToArray();
			groupFacets[ringIndex] = groups[ringIndex].Key;
		}

		return new MovePreviewRingTable(preset, groups.Count, groupFacets, optionIndicesOnRing);
	}

	public static IReadOnlyList<Option> OptionsForRing(
		IReadOnlyList<Option> options,
		MovePreviewRingTable table,
		int ringIndex)
	{
		if (table.RingCount == 0 || ringIndex < 0 || ringIndex >= table.RingCount)
			return Array.Empty<Option>();

		var indices = table.OptionIndicesOnRing(ringIndex);
		if (indices.Count == 0)
			return Array.Empty<Option>();

		var ringOptions = new Option[indices.Count];
		for (var i = 0; i < indices.Count; i++)
			ringOptions[i] = options[indices[i]];

		return ringOptions;
	}

	private static int FindGroup(
		List<(MoveRingFacets.OptionFacets Key, List<int> Indices)> groups,
		MoveRingFacets.OptionFacets classified,
		ERingFacet grouping)
	{
		for (var i = 0; i < groups.Count; i++)
		{
			if (classified.MatchesGrouping(groups[i].Key, grouping))
				return i;
		}

		return -1;
	}
}
