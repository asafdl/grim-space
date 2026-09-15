using Godot;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Camera;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Camera;

internal static class BattleCameraPoses
{
	private const float PlayerAftDistance = 18f;
	private const float PlayerAftDorsalOffset = 0.45f;

	public static OrbitPose PlayerAft(State state)
	{
		var fore = ToVector(state.Fore);
		var dorsal = ToVector(state.Dorsal);
		var direction = (-fore + dorsal * PlayerAftDorsalOffset).Normalized();
		var (yaw, pitch) = Controller.OrbitAnglesForDirection(direction);
		return new OrbitPose
		{
			Pivot = WorldMapping.ToWorld(state.Position),
			Distance = PlayerAftDistance,
			Yaw = yaw,
			Pitch = pitch,
		};
	}

	private static Vector3 ToVector(Coord coord) =>
		new(coord.X, coord.Y, coord.Z);
}
