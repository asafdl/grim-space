using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem.Areas;

public static class AreaBorderAnchor
{
	public const string IdPrefix = "area:border:";

	public static string Id(Coord borderPoint) => $"{IdPrefix}{borderPoint.X}:{borderPoint.Z}";

	public static bool TryParseId(string id, out Coord borderPoint)
	{
		borderPoint = default;
		if (!id.StartsWith(IdPrefix, StringComparison.Ordinal))
			return false;

		var payload = id[IdPrefix.Length..];
		var separator = payload.IndexOf(':');
		if (separator <= 0 || separator >= payload.Length - 1)
			return false;

		if (!int.TryParse(payload[..separator], out var x)
			|| !int.TryParse(payload[(separator + 1)..], out var z))
		{
			return false;
		}

		borderPoint = new Coord(x, 0, z);
		return true;
	}

	public static string? TryGetDisplayName(string id) =>
		TryParseId(id, out _) ? "the sector rim" : null;
}
