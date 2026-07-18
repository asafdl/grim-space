using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Weapons;

public static class FlakTargeting
{
	public static HashSet<Coord> GetBurstCells(
		BodyFrame frame,
		FlakMountConfig config,
		Func<Coord, bool> isInBounds)
	{
		var result = new HashSet<Coord>();
		var apexPort = MountPortOffset(config);
		var outwardStep = -config.SideSign;

		for (var outward = 0; outward <= config.Range; outward++)
		{
			for (var fore = -outward; fore <= outward; fore++)
			{
				for (var dorsal = -outward; dorsal <= outward; dorsal++)
				{
					if (System.Math.Abs(fore) + System.Math.Abs(dorsal) > outward)
						continue;

					var port = apexPort + outwardStep * outward;
					var cell = frame.ToWorld(fore, port, dorsal);
					if (isInBounds(cell))
						result.Add(cell);
				}
			}
		}

		return result;
	}

	public static bool IsValidBurst(
		BodyFrame frame,
		FlakMountConfig config,
		Func<Coord, bool> isInBounds) =>
		GetBurstCells(frame, config, isInBounds).Count > 0;

	public static EFlakMount? MountForCell(BodyFrame frame, Coord cell)
	{
		if (!frame.TryFromWorld(cell, out _, out var port, out _))
			return null;

		if (port > 0)
			return EFlakMount.Port;

		if (port < 0)
			return EFlakMount.Starboard;

		return null;
	}

	private static int MountPortOffset(FlakMountConfig config) => -config.SideSign;
}
