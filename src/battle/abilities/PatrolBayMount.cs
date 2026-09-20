using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Abilities;

public static class PatrolBayMount
{
	public static (Coord Position, Coord Fore, Coord Dorsal) LaunchPose(
		State ship,
		ESpatialOrientation mountedOn)
	{
		var frame = BodyFrame.From(ship);
		return (
			ship.Position + frame.Step(mountedOn),
			ship.Fore,
			ship.Dorsal);
	}

}
