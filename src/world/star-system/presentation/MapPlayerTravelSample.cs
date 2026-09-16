using Godot;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Pathfinding;

namespace GrimSpace.World.StarSystem.Presentation;

public static class MapPlayerTravelSample
{
	public static PlayerTravelSample Resolve(
		int mapWidth,
		int mapHeight,
		Coord committedPosition,
		Coord? tangent,
		PendingCourse? pendingCourse,
		double speedPerTick)
	{
		var worldPosition = MapMapping.ToWorld(committedPosition, mapWidth, mapHeight);

		if (pendingCourse is { } pending)
		{
			return new PlayerTravelSample(
				worldPosition,
				ResolveDirectionFromPath(
					pending.Path,
					speedPerTick,
					pending.Destination,
					committedPosition),
				true);
		}

		if (tangent is { } sampleTangent)
		{
			return new PlayerTravelSample(
				worldPosition,
				TangentToWorldDirection(sampleTangent),
				true);
		}

		return new PlayerTravelSample(worldPosition, null, false);
	}

	private static Vector3? ResolveDirectionFromPath(
		TransitPath path,
		double speedPerTick,
		Coord destination,
		Coord origin)
	{
		var (_, tangent) = path.SampleAtElapsed(0, speedPerTick);
		var direction = TangentToWorldDirection(tangent);
		if (direction is not null)
			return direction;

		var delta = destination - origin;
		var fallback = new Vector3(delta.X, 0f, delta.Z);
		return fallback.LengthSquared() > 0.001f
			? fallback.Normalized()
			: null;
	}

	private static Vector3? TangentToWorldDirection(Coord tangent)
	{
		var direction = new Vector3(tangent.X * 0.001f, 0f, tangent.Z * 0.001f);
		return direction.LengthSquared() > 0.001f
			? direction.Normalized()
			: null;
	}
}
