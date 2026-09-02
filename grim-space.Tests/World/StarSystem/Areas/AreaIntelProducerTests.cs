using GrimSpace.World.StarSystem.Areas;

namespace GrimSpace.Tests.World.StarSystem.Areas;

public sealed class AreaIntelProducerTests
{
	[Theory]
	[InlineData(EAreaDistance.Low)]
	[InlineData(EAreaDistance.Med)]
	[InlineData(EAreaDistance.High)]
	public void Produce_IncludesLandmarkNames(EAreaDistance distance)
	{
		var context = new AreaIntelContext("Copper Refinery", "Storage Depot", distance);

		for (var i = 0; i < 16; i++)
		{
			var intel = AreaIntelProducer.Produce(context);
			Assert.Contains("Copper Refinery", intel);
			Assert.Contains("Storage Depot", intel);
		}
	}

	[Fact]
	public void Produce_ToneFilter_UsesOnlyRequestedTone()
	{
		var context = new AreaIntelContext("Copper Refinery", "Storage Depot", EAreaDistance.Low);

		for (var i = 0; i < 24; i++)
		{
			var intel = AreaIntelProducer.Produce(context, [EAreaIntelTone.Fragmentary]);
			Assert.True(
				intel.Contains("Word is", StringComparison.Ordinal)
				|| intel.Contains("Contact last seen", StringComparison.Ordinal)
				|| intel.Contains("They said", StringComparison.Ordinal));
		}
	}

	[Fact]
	public void Produce_ImpossibleToneFilter_Throws()
	{
		var context = new AreaIntelContext("Copper Refinery", "Storage Depot", EAreaDistance.Low);

		Assert.Throws<ArgumentException>(() =>
			AreaIntelProducer.Produce(context, []));
	}

	[Fact]
	public void Produce_HasVariationAcrossSamples()
	{
		var context = new AreaIntelContext("Copper Refinery", "Storage Depot", EAreaDistance.Med);
		var lines = new HashSet<string>();

		for (var i = 0; i < 48; i++)
			lines.Add(AreaIntelProducer.Produce(context));

		Assert.True(lines.Count > 1);
	}
}
