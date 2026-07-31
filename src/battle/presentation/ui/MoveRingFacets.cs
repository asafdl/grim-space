using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Ui;

public enum EThrustClass
{
	None,
	Ahead,
	Drift,
	AheadAndDrift,
	Retro,
}

public enum EBodyWedge
{
	Here,
	Fore,
	Aft,
	Port,
	Starboard,
	Dorsal,
	Ventral,
	OffAxis,
}

public enum EApTier
{
	Other,
	MinSpend,
	FullSpend,
}

public enum EMomentumOutcome
{
	Hold,
	Gain,
	Lose,
}

public static class MoveRingFacets
{
	public readonly struct OptionFacets : IEquatable<OptionFacets>
	{
		public EThrustClass ThrustClass { get; init; }
		public EBodyWedge BodyWedge { get; init; }
		public EApTier ApTier { get; init; }
		public int ApCost { get; init; }
		public EMomentumOutcome MomentumOutcome { get; init; }
		public int ManhattanReach { get; init; }

		public static OptionFacets Classify(BodyFrame frame, Coord actor, Option option)
		{
			var thrust = ClassifyThrust(frame, actor, option.Path);
			var wedge = ClassifyWedge(frame, option.EndPosition);
			var apTier = ClassifyApTier(option.ApCost);
			var momentum = ClassifyMomentum(option.StartMomentumLevel, option.EndMomentumLevel);
			var reach = actor.ManhattanDistanceTo(option.EndPosition);

			return new OptionFacets
			{
				ThrustClass = thrust,
				BodyWedge = wedge,
				ApTier = apTier,
				ApCost = option.ApCost,
				MomentumOutcome = momentum,
				ManhattanReach = reach,
			};
		}

		public bool MatchesGrouping(in OptionFacets other, ERingFacet activeFacets)
		{
			if (activeFacets.HasFlag(ERingFacet.ThrustClass) && ThrustClass != other.ThrustClass)
				return false;
			if (activeFacets.HasFlag(ERingFacet.BodyWedge) && BodyWedge != other.BodyWedge)
				return false;
			if (activeFacets.HasFlag(ERingFacet.ApTier) && ApCost != other.ApCost)
				return false;
			if (activeFacets.HasFlag(ERingFacet.MomentumOutcome) && MomentumOutcome != other.MomentumOutcome)
				return false;
			if (activeFacets.HasFlag(ERingFacet.ManhattanReach) && ManhattanReach != other.ManhattanReach)
				return false;

			return true;
		}

		public int CompareForSort(in OptionFacets other, ERingFacet activeFacets)
		{
			if (activeFacets.HasFlag(ERingFacet.ThrustClass))
			{
				var cmp = ThrustSortKey(ThrustClass).CompareTo(ThrustSortKey(other.ThrustClass));
				if (cmp != 0)
					return cmp;
			}

			if (activeFacets.HasFlag(ERingFacet.ApTier))
			{
				var cmp = ApCost.CompareTo(other.ApCost);
				if (cmp != 0)
					return cmp;
			}

			if (activeFacets.HasFlag(ERingFacet.BodyWedge))
			{
				var cmp = BodyWedgeSortKey(BodyWedge).CompareTo(BodyWedgeSortKey(other.BodyWedge));
				if (cmp != 0)
					return cmp;
			}

			if (activeFacets.HasFlag(ERingFacet.MomentumOutcome))
			{
				var cmp = MomentumSortKey(MomentumOutcome).CompareTo(MomentumSortKey(other.MomentumOutcome));
				if (cmp != 0)
					return cmp;
			}

			if (activeFacets.HasFlag(ERingFacet.ManhattanReach))
			{
				var cmp = ManhattanReach.CompareTo(other.ManhattanReach);
				if (cmp != 0)
					return cmp;
			}

			return 0;
		}

		public int CompareForPreset(in OptionFacets other, ERingBandPreset preset) =>
			preset switch
			{
				ERingBandPreset.ApThrust => CompareApThenThrust(other),
				ERingBandPreset.ApMomentum => CompareApThenMomentum(other),
				ERingBandPreset.ApMomentumThrust => CompareApMomentumThrust(other),
				_ => CompareApThenThrust(other),
			};

		private int CompareApThenThrust(in OptionFacets other)
		{
			var cmp = ApCost.CompareTo(other.ApCost);
			if (cmp != 0)
				return cmp;

			return ThrustSortKey(ThrustClass).CompareTo(ThrustSortKey(other.ThrustClass));
		}

		private int CompareApThenMomentum(in OptionFacets other)
		{
			var cmp = ApCost.CompareTo(other.ApCost);
			if (cmp != 0)
				return cmp;

			return MomentumSortKey(MomentumOutcome).CompareTo(MomentumSortKey(other.MomentumOutcome));
		}

		private int CompareApMomentumThrust(in OptionFacets other)
		{
			var cmp = ApCost.CompareTo(other.ApCost);
			if (cmp != 0)
				return cmp;

			cmp = MomentumSortKey(MomentumOutcome).CompareTo(MomentumSortKey(other.MomentumOutcome));
			if (cmp != 0)
				return cmp;

			return ThrustSortKey(ThrustClass).CompareTo(ThrustSortKey(other.ThrustClass));
		}

		public string FormatHint(ERingBandPreset preset, int optionCount)
		{
			var activeFacets = RingBandPresetLabels.GroupingFacets(preset);
			return FormatHint(activeFacets, optionCount);
		}

		public string FormatHint(ERingFacet activeFacets, int optionCount)
		{
			var parts = new List<string>();
			if (activeFacets.HasFlag(ERingFacet.ThrustClass))
				parts.Add(ThrustLabel(ThrustClass));
			if (activeFacets.HasFlag(ERingFacet.BodyWedge))
				parts.Add(WedgeLabel(BodyWedge));
			if (activeFacets.HasFlag(ERingFacet.ApTier))
				parts.Add(ApCostLabel(ApCost));
			if (activeFacets.HasFlag(ERingFacet.MomentumOutcome))
				parts.Add(MomentumLabel(MomentumOutcome));
			if (activeFacets.HasFlag(ERingFacet.ManhattanReach))
				parts.Add($"reach k={ManhattanReach}");

			if (parts.Count == 0)
				parts.Add("ring");

			return string.Join(" · ", parts) + $" · {optionCount} opts";
		}

		public bool Equals(OptionFacets other) =>
			ThrustClass == other.ThrustClass
			&& BodyWedge == other.BodyWedge
			&& ApTier == other.ApTier
			&& ApCost == other.ApCost
			&& MomentumOutcome == other.MomentumOutcome
			&& ManhattanReach == other.ManhattanReach;

		public override bool Equals(object? obj) => obj is OptionFacets other && Equals(other);

		public override int GetHashCode() =>
			HashCode.Combine(ThrustClass, BodyWedge, ApTier, ApCost, MomentumOutcome, ManhattanReach);
	}

	private static EThrustClass ClassifyThrust(BodyFrame frame, Coord origin, IReadOnlyList<Coord> path)
	{
		var hasForward = false;
		var hasRetro = false;
		var hasLateral = false;
		var position = origin;

		foreach (var to in path)
		{
			var direction = frame.DirectionOfStep(position, to);
			position = to;
			switch (direction)
			{
				case Movement.Enums.EStepDirection.Forward:
					hasForward = true;
					break;
				case Movement.Enums.EStepDirection.Retro:
					hasRetro = true;
					break;
				case Movement.Enums.EStepDirection.Port:
				case Movement.Enums.EStepDirection.Starboard:
				case Movement.Enums.EStepDirection.Dorsal:
				case Movement.Enums.EStepDirection.Ventral:
					hasLateral = true;
					break;
			}
		}

		if (hasRetro)
			return EThrustClass.Retro;
		if (hasForward && hasLateral)
			return EThrustClass.AheadAndDrift;
		if (hasForward)
			return EThrustClass.Ahead;
		if (hasLateral)
			return EThrustClass.Drift;

		return EThrustClass.None;
	}

	private static EBodyWedge ClassifyWedge(BodyFrame frame, Coord end)
	{
		if (!frame.TryFromWorld(end, out var fore, out var port, out var dorsal))
			return EBodyWedge.OffAxis;

		if (fore == 0 && port == 0 && dorsal == 0)
			return EBodyWedge.Here;

		var absFore = System.Math.Abs(fore);
		var absPort = System.Math.Abs(port);
		var absDorsal = System.Math.Abs(dorsal);

		if (absFore >= absPort && absFore >= absDorsal)
			return fore > 0 ? EBodyWedge.Fore : EBodyWedge.Aft;
		if (absPort >= absFore && absPort >= absDorsal)
			return port > 0 ? EBodyWedge.Port : EBodyWedge.Starboard;

		return dorsal > 0 ? EBodyWedge.Dorsal : EBodyWedge.Ventral;
	}

	private static EApTier ClassifyApTier(int apCost) =>
		apCost switch
		{
			3 => EApTier.MinSpend,
			4 => EApTier.FullSpend,
			_ => EApTier.Other,
		};

	private static EMomentumOutcome ClassifyMomentum(int start, int end) =>
		end > start ? EMomentumOutcome.Gain : end < start ? EMomentumOutcome.Lose : EMomentumOutcome.Hold;

	public static string ThrustDisplayLabel(EThrustClass thrust) => ThrustLabel(thrust);

	public static string MomentumOutcomeDisplayLabel(EMomentumOutcome outcome) => MomentumLabel(outcome);

	public static string ApCostDisplayLabel(int apCost) => ApCostLabel(apCost);

	public static string ApTierDisplayLabel(EApTier tier) => ApCostLabel(tier switch
	{
		EApTier.MinSpend => 3,
		EApTier.FullSpend => 4,
		_ => 0,
	});

	/// <summary>Ring cycle: hold → gain → lose.</summary>
	private static int MomentumSortKey(EMomentumOutcome outcome) =>
		outcome switch
		{
			EMomentumOutcome.Hold => 0,
			EMomentumOutcome.Gain => 1,
			EMomentumOutcome.Lose => 2,
			_ => 3,
		};

	/// <summary>Ring cycle: straight ahead → mixed → drift → retro.</summary>
	private static int ThrustSortKey(EThrustClass thrust) =>
		thrust switch
		{
			EThrustClass.Ahead => 0,
			EThrustClass.AheadAndDrift => 1,
			EThrustClass.Drift => 2,
			EThrustClass.Retro => 3,
			_ => 4,
		};

	private static int BodyWedgeSortKey(EBodyWedge wedge) =>
		wedge switch
		{
			EBodyWedge.Fore => 0,
			EBodyWedge.Aft => 1,
			EBodyWedge.Port => 2,
			EBodyWedge.Starboard => 3,
			EBodyWedge.Dorsal => 4,
			EBodyWedge.Ventral => 5,
			EBodyWedge.Here => 6,
			_ => 7,
		};

	private static string ThrustLabel(EThrustClass thrust) =>
		thrust switch
		{
			EThrustClass.Ahead => "Ahead burn",
			EThrustClass.Drift => "Drift",
			EThrustClass.AheadAndDrift => "Ahead + drift",
			EThrustClass.Retro => "Retro",
			_ => "No thrust",
		};

	private static string WedgeLabel(EBodyWedge wedge) =>
		wedge switch
		{
			EBodyWedge.Here => "Same cell",
			EBodyWedge.Fore => "Fore wedge",
			EBodyWedge.Aft => "Aft wedge",
			EBodyWedge.Port => "Port wedge",
			EBodyWedge.Starboard => "Starboard wedge",
			EBodyWedge.Dorsal => "Dorsal wedge",
			EBodyWedge.Ventral => "Ventral wedge",
			_ => "Off-axis",
		};

	private static string ApCostLabel(int apCost) => $"{apCost} AP";

	private static string ApTierLabel(EApTier tier) => ApCostLabel(tier switch
	{
		EApTier.MinSpend => 3,
		EApTier.FullSpend => 4,
		_ => 0,
	});

	private static string MomentumLabel(EMomentumOutcome outcome) =>
		outcome switch
		{
			EMomentumOutcome.Gain => "M gain",
			EMomentumOutcome.Lose => "M lose",
			_ => "M hold",
		};
}
