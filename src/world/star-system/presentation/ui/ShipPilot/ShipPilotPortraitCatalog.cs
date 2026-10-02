using Godot;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public static class ShipPilotPortraitCatalog
{
	private static readonly IReadOnlyDictionary<string, string> Paths =
		new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["pilot_01"] = "res://assets/ui/unit_portraits/pilots/pilot_01.png",
			["pilot_02"] = "res://assets/ui/unit_portraits/pilots/pilot_02.png",
			["pilot_03"] = "res://assets/ui/unit_portraits/pilots/pilot_03.png",
			["pilot_04"] = "res://assets/ui/unit_portraits/pilots/pilot_04.png",
			["pilot_05"] = "res://assets/ui/unit_portraits/pilots/pilot_05.png",
			["pilot_06"] = "res://assets/ui/unit_portraits/pilots/pilot_06.png",
			["pilot_07"] = "res://assets/ui/unit_portraits/pilots/pilot_07.png",
			["pilot_08"] = "res://assets/ui/unit_portraits/pilots/pilot_08.png",
		};

	public static Texture2D TextureFor(string portraitId) =>
		GD.Load<Texture2D>(Paths.TryGetValue(portraitId, out var path)
			? path
			: throw new ArgumentException($"Unknown player portrait '{portraitId}'.", nameof(portraitId)));
}
