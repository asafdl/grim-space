using GrimSpace.World.StarSystem.Areas;

namespace GrimSpace.Tests.World.StarSystem.Areas;

[StarSystemTestSuite]
public sealed class AreaIntelProducerTests
{
	[Fact]
	public void Produce_PreservesLandmarkIds()
	{
		var context = new AreaIntelContext("poi-refinery", "poi-storage", "landmark:nav-a:00");

		for (var i = 0; i < 16; i++)
		{
			var intel = AreaIntelProducer.Produce(context);
			Assert.Equal("poi-refinery", intel.LandmarkAId);
			Assert.Equal("poi-storage", intel.LandmarkBId);
			Assert.Equal("landmark:nav-a:00", intel.LandmarkCId);
			Assert.Contains("{A}", intel.Template);
		}
	}

	[Fact]
	public void Produce_ToneFilter_UsesOnlyRequestedTone()
	{
		var context = new AreaIntelContext("poi-refinery", "poi-storage", "landmark:nav-a:00");

		for (var i = 0; i < 24; i++)
		{
			var intel = AreaIntelProducer.Produce(context, [EAreaIntelTone.Fragmentary]);
			Assert.True(
				intel.Template.Contains("Ping", StringComparison.Ordinal)
				|| intel.Template.Contains("Sounds near", StringComparison.Ordinal)
				|| intel.Template.Contains("Near {A}", StringComparison.Ordinal));
		}
	}

	[Fact]
	public void Produce_ImpossibleToneFilter_Throws()
	{
		var context = new AreaIntelContext("poi-refinery", "poi-storage", "landmark:nav-a:00");

		Assert.Throws<ArgumentException>(() =>
			AreaIntelProducer.Produce(context, []));
	}

	[Fact]
	public void Produce_HasVariationAcrossSamples()
	{
		var context = new AreaIntelContext("poi-refinery", "poi-storage", "landmark:nav-a:00");
		var lines = new HashSet<string>();

		for (var i = 0; i < 48; i++)
			lines.Add(AreaIntelProducer.Produce(context).Template);

		Assert.True(lines.Count > 1);
	}
}
