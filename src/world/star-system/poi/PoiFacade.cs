namespace GrimSpace.World.StarSystem.Poi;

/// <summary>
/// Orbit-camera framing for a placed POI facade view. Godot-free; presentation converts to world pose.
/// </summary>
public readonly record struct PoiFacade(
	float PivotOffsetX,
	float PivotOffsetY,
	float PivotOffsetZ,
	float YawDegrees,
	float PitchDegrees,
	float Distance,
	EFacadeLayout Layout = EFacadeLayout.Default)
{
	public static PoiFacade Default => new(0f, 0.25f, 0f, -38f, 32f, 6.5f, EFacadeLayout.Default);

	public static PoiFacade Planet => new(0f, 0.35f, 0f, -38f, 28f, 7.5f, EFacadeLayout.Planet);

	public static PoiFacade LargeStation => new(0f, 0.15f, 0f, 7f, 25f, 6f, EFacadeLayout.Station);

	public PoiFacade WithYawFromApproach(double dirX, double dirZ)
	{
		if (dirX * dirX + dirZ * dirZ < 0.0001)
			return this;

		var yawDegrees = (float)(System.Math.Atan2(dirX, dirZ) * 180.0 / System.Math.PI);
		return this with { YawDegrees = yawDegrees };
	}
}
