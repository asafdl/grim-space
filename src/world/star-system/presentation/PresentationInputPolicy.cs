namespace GrimSpace.World.StarSystem.Presentation;

public readonly record struct PresentationInputPolicy(
	bool AllowsOrbit,
	bool AllowsPan,
	bool AllowsWheelZoom,
	bool AllowsRmbMovement,
	bool AllowsStrategicHover)
{
	public static PresentationInputPolicy Locked =>
		new(false, false, false, false, false);
}
