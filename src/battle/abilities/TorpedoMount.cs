using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Abilities;

public static class TorpedoMount
{
	public static (Coord Position, Coord Fore, Coord Dorsal) LaunchPose(State ship, ESpatialOrientation mountedOn)
	{
		var frame = BodyFrame.From(ship);
		return mountedOn switch
		{
			ESpatialOrientation.Retro => (
				ship.Position + frame.Step(ESpatialOrientation.Retro),
				Coord.Zero - ship.Fore,
				ship.Dorsal),
			ESpatialOrientation.Ventral => (
				ship.Position + frame.Step(ESpatialOrientation.Ventral),
				Coord.Zero - ship.Dorsal,
				ship.Fore),
			ESpatialOrientation.Dorsal => (
				ship.Position + frame.Step(ESpatialOrientation.Dorsal),
				ship.Dorsal,
				Coord.Zero - ship.Fore),
			_ => throw new ArgumentOutOfRangeException(nameof(mountedOn), mountedOn, null),
		};
	}
}
