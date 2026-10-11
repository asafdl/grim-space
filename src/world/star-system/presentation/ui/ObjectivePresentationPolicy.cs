using GrimSpace.World.StarSystem.Objectives;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

internal static class ObjectivePresentationPolicy
{
	public static bool ShowsReward(EObjectiveSource source) =>
		source == EObjectiveSource.Contract;

	public static bool CanDismiss(EObjectiveSource source) =>
		source is EObjectiveSource.Contract or EObjectiveSource.BlackMarketLead;

	public static string DismissTooltip(EObjectiveSource source) =>
		source switch
		{
			EObjectiveSource.Contract => "Dismiss contract",
			EObjectiveSource.BlackMarketLead => "Abandon black market lead",
			_ => throw new ArgumentException(
				$"Objective source '{source}' cannot be dismissed.",
				nameof(source)),
		};

	public static string AccentHex(EObjectiveKind kind) =>
		kind switch
		{
			EObjectiveKind.Hunt => "#F05F5F",
			EObjectiveKind.Wreckage => "#D7A93B",
			EObjectiveKind.Delivery => "#72D17C",
			EObjectiveKind.BlackMarketLead => "#8A9199",
			EObjectiveKind.Story => "#5794E6",
			_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
		};
}
