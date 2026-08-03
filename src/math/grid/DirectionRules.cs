namespace GrimSpace.Math.Grid;

public static class DirectionRules
{
	public static int DirectionBit(ESpatialOrientation direction) => 1 << (int)direction;

	public static bool UsesOpposite(int usedMask, ESpatialOrientation direction) =>
		(usedMask & DirectionBit(Opposite(direction))) != 0;

	public static ESpatialOrientation Opposite(ESpatialOrientation direction) =>
		direction switch
		{
			ESpatialOrientation.Forward => ESpatialOrientation.Retro,
			ESpatialOrientation.Retro => ESpatialOrientation.Forward,
			ESpatialOrientation.Dorsal => ESpatialOrientation.Ventral,
			ESpatialOrientation.Ventral => ESpatialOrientation.Dorsal,
			ESpatialOrientation.Port => ESpatialOrientation.Starboard,
			ESpatialOrientation.Starboard => ESpatialOrientation.Port,
			_ => direction,
		};
}
