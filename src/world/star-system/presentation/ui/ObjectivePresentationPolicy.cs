using Godot;
using GrimSpace.Components;
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
			EObjectiveKind.Hunt => HudPalettes.BlueCyan.Highlight,
			EObjectiveKind.Wreckage => HudPalettes.BlueCyan.Primary,
			EObjectiveKind.Delivery => HudPalettes.BlueCyan.Secondary,
			EObjectiveKind.Lead => HudPalettes.BlueCyan.Border,
			EObjectiveKind.Story => HudPalettes.BlueCyan.Highlight,
			_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
		};

}
