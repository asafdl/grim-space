using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Spatial;

public readonly record struct BodyFrame(Coord Origin, Coord Fore, Coord Dorsal, Coord Starboard)
{
	public static BodyFrame From(State state) =>
		new(state.Position, state.Fore, state.Dorsal, state.Starboard);

	public static BodyFrame WorldAligned(Coord origin) =>
		new(origin, Coord.Forward, Coord.Up, Coord.Cross(Coord.Up, Coord.Forward));

	public Coord Step(ESpatialOrientation direction) =>
		direction switch
		{
			ESpatialOrientation.Forward => Fore,
			ESpatialOrientation.Retro => Coord.Zero - Fore,
			ESpatialOrientation.Dorsal => Dorsal,
			ESpatialOrientation.Ventral => Coord.Zero - Dorsal,
			ESpatialOrientation.Port => Coord.Zero - Starboard,
			ESpatialOrientation.Starboard => Starboard,
			_ => Coord.Zero,
		};

	public Coord ToWorld(int fore, int port, int dorsal) =>
		Origin + ToBasis().ToWorldOffset(fore, -port, dorsal);

	public bool TryFromWorld(Coord cell, out int fore, out int port, out int dorsal)
	{
		var delta = cell - Origin;
		if (!ToBasis().TryToLocal(delta, out fore, out var localRight, out dorsal))
		{
			port = 0;
			return false;
		}

		port = -localRight;
		return true;
	}

	public ESpatialOrientation HitFaceFrom(Coord attackOrigin)
	{
		var delta = attackOrigin - Origin;
		if (delta == Coord.Zero)
			return ESpatialOrientation.Forward;

		if (!ToBasis().TryToLocal(delta, out var fore, out var localRight, out var dorsal))
			return ESpatialOrientation.Forward;

		var port = -localRight;
		var maxAbs = System.Math.Max(System.Math.Abs(fore), System.Math.Max(System.Math.Abs(port), System.Math.Abs(dorsal)));

		if (System.Math.Abs(fore) == maxAbs)
			return fore >= 0 ? ESpatialOrientation.Forward : ESpatialOrientation.Retro;
		if (System.Math.Abs(port) == maxAbs)
			return port >= 0 ? ESpatialOrientation.Port : ESpatialOrientation.Starboard;
		return dorsal >= 0 ? ESpatialOrientation.Dorsal : ESpatialOrientation.Ventral;
	}

	public ESpatialOrientation? DirectionOfStep(Coord from, Coord to)
	{
		var delta = to - from;

		foreach (var direction in Enum.GetValues<ESpatialOrientation>())
		{
			if (delta == Step(direction))
				return direction;
		}

		return null;
	}

	private GridBasis ToBasis() => GridBasis.From(Fore, Dorsal, Starboard);
}
