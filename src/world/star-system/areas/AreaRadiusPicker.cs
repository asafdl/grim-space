namespace GrimSpace.World.StarSystem.Areas;

public static class AreaRadiusPicker
{
	public static int Pick(double span, AreaRadiusConfig? config = null)
	{
		config ??= new AreaRadiusConfig();
		var scaled = (int)System.Math.Round(span * config.FractionOfSpan);
		return System.Math.Clamp(scaled, config.MinRadius, config.MaxRadius);
	}
}
