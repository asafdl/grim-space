using GrimSpace.World.StarSystem.Areas;

namespace GrimSpace.Tests.World.StarSystem.Areas;

public sealed class AreaIntelProducerTests
{
	[Theory]
	[InlineData(EAreaDistance.Low)]
	[InlineData(EAreaDistance.Med)]
	[InlineData(EAreaDistance.High)]
	public void Produce_PreservesLandmarkIds(EAreaDistance distance)
	{
		var context = new AreaIntelContext("poi-refinery", "poi-storage", distance);

		for (var i = 0; i < 16; i++)
		{
			var intel = AreaIntelProducer.Produce(context);
			Assert.Equal("poi-refinery", intel.LandmarkAId);
			Assert.Equal("poi-storage", intel.LandmarkBId);
			Assert.Contains("{A}", intel.Template);
			Assert.Contains("{B}", intel.Template);
		}
	}

	[Fact]
	public void Produce_ToneFilter_UsesOnlyRequestedTone()
	{
		var context = new AreaIntelContext("poi-refinery", "poi-storage", EAreaDistance.Low);

		for (var i = 0; i < 24; i++)
		{
			var intel = AreaIntelProducer.Produce(context, [EAreaIntelTone.Fragmentary]);
			Assert.True(
				intel.Template.Contains("Word is", StringComparison.Ordinal)
				|| intel.Template.Contains("Contact last seen", StringComparison.Ordinal)
				|| intel.Template.Contains("They said", StringComparison.Ordinal));
		}
	}

	[Fact]
	public void Produce_ImpossibleToneFilter_Throws()
	{
		var context = new AreaIntelContext("poi-refinery", "poi-storage", EAreaDistance.Low);

		Assert.Throws<ArgumentException>(() =>
			AreaIntelProducer.Produce(context, []));
	}

	[Fact]
	public void Produce_HasVariationAcrossSamples()
	{
		var context = new AreaIntelContext("poi-refinery", "poi-storage", EAreaDistance.Med);
		var lines = new HashSet<string>();

		for (var i = 0; i < 48; i++)
			lines.Add(AreaIntelProducer.Produce(context).Template);

		Assert.True(lines.Count > 1);
	}
}
