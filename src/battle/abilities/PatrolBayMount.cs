using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Abilities;

public static class PatrolBayMount
{
	public static (Coord Position, Coord Fore, Coord Dorsal) LaunchPose(State ship)
	{
		var frame = BodyFrame.From(ship);
		return (
			ship.Position + frame.Step(ESpatialOrientation.Ventral),
			ship.Fore,
			ship.Dorsal);
	}

	public static int SpawnMomentum(State ship) =>
		System.Math.Clamp(ship.MomentumLevel, 0, MomentumConfig.MaxLevel);
}
