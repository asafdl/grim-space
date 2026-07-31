namespace GrimSpace.Battle.Presentation.Ui;

public enum ERingBandPreset
{
	ApMomentumThrust,
	ApMomentum,
	ApThrust,
}

public static class RingBandPresetLabels
{
	public static readonly IReadOnlyList<ERingBandPreset> All =
	[
		ERingBandPreset.ApMomentumThrust,
		ERingBandPreset.ApMomentum,
		ERingBandPreset.ApThrust,
	];

	public static string Label(ERingBandPreset preset) =>
		preset switch
		{
			ERingBandPreset.ApThrust => "AP + thrust",
			ERingBandPreset.ApMomentum => "AP + momentum",
			ERingBandPreset.ApMomentumThrust => "AP + momentum + thrust",
			_ => preset.ToString(),
		};

	public static ERingFacet GroupingFacets(ERingBandPreset preset) =>
		preset switch
		{
			ERingBandPreset.ApThrust => ERingFacet.ApTier | ERingFacet.ThrustClass,
			ERingBandPreset.ApMomentum => ERingFacet.ApTier | ERingFacet.MomentumOutcome,
			ERingBandPreset.ApMomentumThrust =>
				ERingFacet.ApTier | ERingFacet.MomentumOutcome | ERingFacet.ThrustClass,
			_ => ERingFacet.ApTier | ERingFacet.ThrustClass,
		};
}
