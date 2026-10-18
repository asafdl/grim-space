using System;
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
		
		// Camera direction: primarily behind the ship (-fore), slightly above
		// If dorsal is parallel/anti-parallel to fore (invalid orientation), use world up
		var foreDorsalDot = System.Math.Abs(fore.X * dorsal.X + fore.Y * dorsal.Y + fore.Z * dorsal.Z);
		var upOffset = foreDorsalDot > 0.99f ? Vector3.Up : dorsal;
		
		// Ensure the offset is perpendicular to fore
		var upOffsetPerp = (upOffset - fore * (fore.X * upOffset.X + fore.Y * upOffset.Y + fore.Z * upOffset.Z)).Normalized();
		if (upOffsetPerp.LengthSquared() < 0.01f)
			upOffsetPerp = Vector3.Up;
		
		var direction = (-fore + upOffsetPerp * PlayerAftDorsalOffset).Normalized();
		
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
