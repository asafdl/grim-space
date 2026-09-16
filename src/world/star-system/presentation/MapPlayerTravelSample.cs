using Godot;
using GrimSpace.Math.Grid;
using GrimSpace.Math.Routes;
using GrimSpace.World.StarSystem.Pathfinding;

namespace GrimSpace.World.StarSystem.Presentation;

public static class MapPlayerTravelSample
{
	public static PlayerTravelSample Resolve(
		int mapWidth,
		int mapHeight,
		double positionX,
		double positionZ,
		double? tangentX,
		double? tangentZ,
		PendingCourse? pendingCourse,
		double speedPerTick)
	{
		var worldPosition = MapMapping.ToWorld(positionX, positionZ, mapWidth, mapHeight);

		if (pendingCourse is { } pending)
		{
			return new PlayerTravelSample(
				worldPosition,
				ResolveDirectionFromPath(
					pending.Path,
					speedPerTick,
					pending.Destination,
					new Coord((int)System.Math.Round(positionX), 0, (int)System.Math.Round(positionZ))),
				true);
		}

		if (tangentX is { } sampleTangentX && tangentZ is { } sampleTangentZ)
		{
			return new PlayerTravelSample(
				worldPosition,
				TangentToWorldDirection(sampleTangentX, sampleTangentZ),
				true);
		}

		return new PlayerTravelSample(worldPosition, null, false);
	}

	public static PlayerTravelSample Resolve(
		int mapWidth,
		int mapHeight,
		Coord committedPosition,
		Coord? tangent,
		PendingCourse? pendingCourse,
		double speedPerTick) =>
		Resolve(
			mapWidth,
			mapHeight,
			committedPosition.X,
			committedPosition.Z,
			tangent?.X * 0.001,
			tangent?.Z * 0.001,
			pendingCourse,
			speedPerTick);

	public static PlayerTravelSample Resolve(
		int mapWidth,
		int mapHeight,
		PiecewiseRouteSample? continuousPosition,
		PendingCourse? pendingCourse,
		double speedPerTick)
	{
		if (continuousPosition is { } sample)
		{
			return Resolve(
				mapWidth,
				mapHeight,
				sample.Route.X,
				sample.Route.Z,
				sample.Route.TangentX,
				sample.Route.TangentZ,
				pendingCourse,
				speedPerTick);
		}

		return Resolve(
			mapWidth,
			mapHeight,
			0,
			0,
			null,
			null,
			pendingCourse,
			speedPerTick);
	}

	private static Vector3? ResolveDirectionFromPath(
		TransitPath path,
		double speedPerTick,
		Coord destination,
		Coord origin)
	{
		var sample = path.SampleContinuousAtElapsed(0, speedPerTick).Route;
		var direction = TangentToWorldDirection(sample.TangentX, sample.TangentZ);
		if (direction is not null)
			return direction;

		var delta = destination - origin;
		var fallback = new Vector3(delta.X, 0f, delta.Z);
		return fallback.LengthSquared() > 0.001f
			? fallback.Normalized()
			: null;
	}

	private static Vector3? TangentToWorldDirection(double tangentX, double tangentZ)
	{
		var direction = new Vector3((float)tangentX, 0f, (float)tangentZ);
		return direction.LengthSquared() > 0.001f
			? direction.Normalized()
			: null;
	}
}
