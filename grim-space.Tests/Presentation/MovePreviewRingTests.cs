using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;

namespace grim_space.Tests.Presentation;

public sealed class MovePreviewRingTests
{
	private static readonly Coord Actor = Coord.Zero;
	private static readonly BodyFrame Frame = BodyFrame.WorldAligned(Actor);

	private static Option ForwardOption(int steps, int apCost) =>
		ForwardOption(Actor, steps, apCost, startMomentum: 0, endMomentum: 0);

	private static Option ForwardOption(
		Coord origin,
		int steps,
		int apCost,
		int startMomentum,
		int endMomentum)
	{
		var path = new List<Coord>(steps);
		var position = origin;
		for (var step = 0; step < steps; step++)
		{
			position += Frame.Fore;
			path.Add(position);
		}

		return new Option
		{
			ApCost = apCost,
			Path = path,
			StartMomentumLevel = startMomentum,
			EndMomentumLevel = endMomentum,
		};
	}

	[Fact]
	public void Classify_aheadBurn_onForwardPath()
	{
		var option = ForwardOption(2, 3);
		var facets = MoveRingFacets.OptionFacets.Classify(Frame, Actor, option);

		Assert.Equal(EThrustClass.Ahead, facets.ThrustClass);
		Assert.Equal(EBodyWedge.Fore, facets.BodyWedge);
	}

	private static Option LateralOption(int steps, int apCost)
	{
		var path = new List<Coord>(steps);
		var position = Actor;
		for (var step = 0; step < steps; step++)
		{
			position += Frame.Starboard;
			path.Add(position);
		}

		return new Option
		{
			ApCost = apCost,
			Path = path,
		};
	}

	[Fact]
	public void BuildRingTable_defaultPreset_groupsByThrustAndApTier()
	{
		var options = new List<Option>
		{
			ForwardOption(1, 3),
			ForwardOption(2, 4),
		};

		var table = MovePreviewRings.BuildRingTable(Frame, Actor, options);

		Assert.Equal(ERingBandPreset.ApMomentum, table.Preset);
		Assert.Equal(2, table.RingCount);
		Assert.Contains("Ahead burn", table.FormatRingHint(0));
		Assert.Contains("3 AP", table.FormatRingHint(0));
	}

	[Fact]
	public void BuildRingTable_apThrust_sortsByApThenAheadBeforeDrift()
	{
		var options = new List<Option>
		{
			LateralOption(1, 4),
			ForwardOption(2, 4),
			ForwardOption(1, 3),
		};

		var table = MovePreviewRings.BuildRingTable(Frame, Actor, options, ERingBandPreset.ApThrust);

		Assert.Equal(3, table.RingCount);
		Assert.Contains("3 AP", table.FormatRingHint(0));
		Assert.Contains("Ahead burn", table.FormatRingHint(0));
		Assert.Contains("4 AP", table.FormatRingHint(1));
		Assert.Contains("Ahead burn", table.FormatRingHint(1));
		Assert.Contains("Drift", table.FormatRingHint(2));
	}

	[Fact]
	public void BuildRingTable_apMomentum_groupsByApAndMomentum()
	{
		var options = new List<Option>
		{
			ForwardOption(Actor, 1, 3, 0, 0),
			ForwardOption(Actor, 2, 3, 0, 1),
		};

		var table = MovePreviewRings.BuildRingTable(Frame, Actor, options, ERingBandPreset.ApMomentum);

		Assert.Equal(2, table.RingCount);
		Assert.Contains("M hold", table.FormatRingHint(0));
		Assert.Contains("M gain", table.FormatRingHint(1));
	}

	[Fact]
	public void BuildRingTable_apMomentumThrust_includesAllFacetsInHint()
	{
		var options = new List<Option>
		{
			ForwardOption(1, 3),
			LateralOption(1, 4),
		};

		var table = MovePreviewRings.BuildRingTable(Frame, Actor, options, ERingBandPreset.ApMomentumThrust);

		Assert.Equal(2, table.RingCount);
		Assert.Contains("Ahead burn", table.FormatRingHint(0));
		Assert.Contains("Drift", table.FormatRingHint(1));
	}

	[Fact]
	public void BuildRingTable_emptyOptions_zeroRings()
	{
		var table = MovePreviewRings.BuildRingTable(Frame, Actor, Array.Empty<Option>());

		Assert.Equal(0, table.RingCount);
	}

	[Fact]
	public void OptionsForRing_returnsOnlyActiveRingEndpoints()
	{
		var options = new List<Option>
		{
			ForwardOption(1, 3),
			ForwardOption(2, 4),
		};

		var table = MovePreviewRings.BuildRingTable(Frame, Actor, options, ERingBandPreset.ApThrust);
		var ring0 = MovePreviewRings.OptionsForRing(options, table, 0);

		Assert.Single(ring0);
	}

	[Fact]
	public void DevDefault_turnStart_hasMultipleFacetRings()
	{
		var encounter = GrimSpace.Run.Encounter.DevDefault(42);
		var battle = GrimSpace.Battle.BattleOrchestrator.FromEncounter(encounter);
		var table = new GrimSpace.Battle.Presentation.BattleUi(battle).BuildFrame().MovePreviewRingTable;

		Assert.True(table.RingCount > 1);
	}
}
