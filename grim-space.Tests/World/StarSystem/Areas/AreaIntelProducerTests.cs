using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Areas;

namespace GrimSpace.Tests.World.StarSystem.Areas;

[StarSystemTestSuite]
public sealed class AreaIntelProducerTests
{
	private static readonly Dictionary<string, Coord> References = new()
	{
		["west"] = new Coord(0, 0, 0),
		["east"] = new Coord(600, 0, 0),
		["north"] = new Coord(300, 0, 600),
		["nearby"] = new Coord(300, 0, 290),
	};

	private static readonly AreaIntelContext Triangle = new("west", "east", "north");

	[Fact]
	public void Produce_UsesNearestEligibleReferenceAfterSearchPointIsPicked()
	{
		var intel = Produce(new Coord(300, 0, 300));

		Assert.Equal("Somewhere near {A}.", intel.Template);
		Assert.Equal("nearby", intel.LandmarkAId);
	}

	[Fact]
	public void Produce_FarFromReferencesButNearConnectingSegment_UsesBetween()
	{
		var intel = Produce(new Coord(300, 0, 150), ["west", "east"]);

		Assert.Equal("Somewhere between {A} and {B}.", intel.Template);
		Assert.Equal("west", intel.LandmarkAId);
		Assert.Equal("east", intel.LandmarkBId);
	}

	[Fact]
	public void Produce_OutsideSegmentProximity_DoesNotSayBetween()
	{
		var intel = Produce(new Coord(300, 0, 160), ["west", "east"]);

		Assert.Equal("Somewhere in the general area between {A}, {B}, and {C}.", intel.Template);
	}

	[Fact]
	public void Produce_FarFromReferencesAndSegments_UsesGeneralArea()
	{
		var intel = Produce(new Coord(300, 0, 250), ["west", "east", "north"]);

		Assert.Equal("Somewhere in the general area between {A}, {B}, and {C}.", intel.Template);
		Assert.Equal("west", intel.LandmarkAId);
		Assert.Equal("east", intel.LandmarkBId);
		Assert.Equal("north", intel.LandmarkCId);
	}

	[Fact]
	public void Produce_BorderTriangleFallback_UsesNavigationLandmarksOnly()
	{
		var rimA = AreaBorderAnchor.Id(new Coord(0, 0, 0));
		var rimB = AreaBorderAnchor.Id(new Coord(600, 0, 0));
		var positions = new Dictionary<string, Coord>(References)
		{
			[rimA] = new Coord(0, 0, 0),
			[rimB] = new Coord(600, 0, 0),
		};

		var intel = AreaIntelProducer.Produce(
			new AreaIntelContext(rimA, "north", rimB),
			new Coord(300, 0, 250),
			["north", "west", "east", rimA, rimB],
			id => positions[id],
			1024);

		Assert.Equal("Somewhere in the general area between {A}, {B}, and {C}.", intel.Template);
		foreach (var id in new[] { intel.LandmarkAId, intel.LandmarkBId, intel.LandmarkCId })
			Assert.False(AreaBorderAnchor.TryParseId(id, out _));
	}

	private static AreaIntel Produce(Coord position, IReadOnlyList<string>? referenceIds = null) =>
		AreaIntelProducer.Produce(
			Triangle,
			position,
			referenceIds ?? ["west", "east", "north", "nearby"],
			id => References[id],
			1024);
}
