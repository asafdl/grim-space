using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Abilities;

public static class TorpedoMount
{
	public static (Coord Position, Coord Fore, Coord Dorsal) LaunchPose(State ship, ETorpedoMount mount)
	{
		var frame = BodyFrame.From(ship);
		return mount switch
		{
			ETorpedoMount.Aft => (
				ship.Position + frame.Step(ESpatialOrientation.Retro),
				Coord.Zero - ship.Fore,
				ship.Dorsal),
			ETorpedoMount.Ventral => (
				ship.Position + frame.Step(ESpatialOrientation.Ventral),
				Coord.Zero - ship.Dorsal,
				ship.Fore),
			ETorpedoMount.Dorsal => (
				ship.Position + frame.Step(ESpatialOrientation.Dorsal),
				ship.Dorsal,
				Coord.Zero - ship.Fore),
			_ => throw new ArgumentOutOfRangeException(nameof(mount), mount, null),
		};
	}
}
