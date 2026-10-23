using Godot;
using GrimSpace.World.StarSystem.Objectives;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

internal static class ObjectivePresentationPolicy
{
	public static bool ShowsReward(EObjectiveSource source) =>
		source == EObjectiveSource.Contract;

	public static bool CanDismiss(EObjectiveSource source) =>
		source is EObjectiveSource.Contract or EObjectiveSource.Lead;

	public static string DismissTooltip(EObjectiveSource source) =>
		source switch
		{
			EObjectiveSource.Contract => "Dismiss contract",
			EObjectiveSource.Lead => "Abandon lead",
			_ => throw new ArgumentException(
				$"Objective source '{source}' cannot be dismissed.",
				nameof(source)),
		};

	public static Color AccentColor(EObjectiveKind kind) =>
		kind switch
		{
			EObjectiveKind.Hunt => Color.FromHtml("#F05F5F"),
			EObjectiveKind.Wreckage => Color.FromHtml("#D7A93B"),
			EObjectiveKind.Delivery => Color.FromHtml("#72D17C"),
			EObjectiveKind.Lead => Color.FromHtml("#8A9199"),
			EObjectiveKind.Story => Color.FromHtml("#5794E6"),
			_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
		};
}
