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

	public Coord Step(EStepDirection direction) =>
		direction switch
		{
			EStepDirection.Forward => Fore,
			EStepDirection.Retro => Coord.Zero - Fore,
			EStepDirection.Dorsal => Dorsal,
			EStepDirection.Ventral => Coord.Zero - Dorsal,
			EStepDirection.Port => Coord.Zero - Starboard,
			EStepDirection.Starboard => Starboard,
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

	public EStepDirection? DirectionOfStep(Coord from, Coord to)
	{
		var delta = to - from;

		foreach (var direction in Enum.GetValues<EStepDirection>())
		{
			if (delta == Step(direction))
				return direction;
		}

		return null;
	}

	private GridBasis ToBasis() => GridBasis.From(Fore, Dorsal, Starboard);
}
