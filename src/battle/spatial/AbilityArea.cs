using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;
using Microsoft.Extensions.Caching.Memory;

namespace GrimSpace.Battle.Spatial;

public static class AbilityArea
{
	private const int CacheSizeLimit = 256;

	private static readonly MemoryCache Cache =
		new(new MemoryCacheOptions { SizeLimit = CacheSizeLimit });

	public static HashSet<Coord> CellsInBounds(
		IAreaDamage area,
		BodyFrame frame,
		ESpatialOrientation direction,
		Grid grid)
	{
		if (area is not AbilitySpec spec)
			return Compute(area, frame, direction, grid);

		var key = (
			frame.Origin,
			frame.Fore,
			frame.Dorsal,
			frame.Starboard,
			direction,
			grid.Width,
			grid.Height,
			grid.Depth,
			spec);

		return Cache.GetOrCreate(key, entry =>
		{
			entry.Size = 1;
			return Compute(area, frame, direction, grid);
		})!;
	}

	public static HashSet<Coord> ApplyBlocking(
		Coord origin,
		HashSet<Coord> candidates,
		bool isBlockable,
		IEnumerable<Hazard> hazards)
	{
		var result = new HashSet<Coord>(candidates);
		if (!isBlockable)
			return result;

		var blockers = CollectAbilityBlockingCells(hazards);
		if (blockers.Count == 0)
			return result;

		result.RemoveWhere(cell => IsShadowed(origin, cell, blockers));
		return result;
	}

	private static HashSet<Coord> CollectAbilityBlockingCells(IEnumerable<Hazard> hazards)
	{
		var blockers = new HashSet<Coord>();
		foreach (var hazard in hazards)
		{
			if (!hazard.BlocksAbilities)
				continue;
			foreach (var cell in hazard.Cells)
				blockers.Add(cell);
		}

		return blockers;
	}

	private static bool IsShadowed(Coord origin, Coord target, HashSet<Coord> blockers)
	{
		if (origin.Equals(target))
			return false;

		var x = origin.X;
		var y = origin.Y;
		var z = origin.Z;

		var dx = target.X - x;
		var dy = target.Y - y;
		var dz = target.Z - z;

		var stepX = System.Math.Sign(dx);
		var stepY = System.Math.Sign(dy);
		var stepZ = System.Math.Sign(dz);

		var absDx = System.Math.Abs(dx);
		var absDy = System.Math.Abs(dy);
		var absDz = System.Math.Abs(dz);

		var tDeltaX = absDx == 0 ? double.PositiveInfinity : 1.0 / absDx;
		var tDeltaY = absDy == 0 ? double.PositiveInfinity : 1.0 / absDy;
		var tDeltaZ = absDz == 0 ? double.PositiveInfinity : 1.0 / absDz;

		var tMaxX = absDx == 0 ? double.PositiveInfinity : tDeltaX * 0.5;
		var tMaxY = absDy == 0 ? double.PositiveInfinity : tDeltaY * 0.5;
		var tMaxZ = absDz == 0 ? double.PositiveInfinity : tDeltaZ * 0.5;

		while (x != target.X || y != target.Y || z != target.Z)
		{
			var minT = System.Math.Min(tMaxX, System.Math.Min(tMaxY, tMaxZ));
			var tieX = tMaxX <= minT + 1e-12;
			var tieY = tMaxY <= minT + 1e-12;
			var tieZ = tMaxZ <= minT + 1e-12;
			var tiedMask = (tieX ? 1 : 0) | (tieY ? 2 : 0) | (tieZ ? 4 : 0);

			// Supercover: at edge/corner ties, every voxel touched by the seam must block the ray.
			for (var combination = 1; combination <= tiedMask; combination++)
			{
				if ((combination & tiedMask) != combination)
					continue;

				var nx = x + ((combination & 1) != 0 ? stepX : 0);
				var ny = y + ((combination & 2) != 0 ? stepY : 0);
				var nz = z + ((combination & 4) != 0 ? stepZ : 0);
				var cell = new Coord(nx, ny, nz);
				if (cell.Equals(origin))
					continue;
				if (blockers.Contains(cell))
					return true;
			}

			if (tieX)
			{
				x += stepX;
				tMaxX += tDeltaX;
			}

			if (tieY)
			{
				y += stepY;
				tMaxY += tDeltaY;
			}

			if (tieZ)
			{
				z += stepZ;
				tMaxZ += tDeltaZ;
			}
		}

		return blockers.Contains(target);
	}

	private static HashSet<Coord> Compute(
		IAreaDamage area,
		BodyFrame frame,
		ESpatialOrientation direction,
		Grid grid)
	{
		var burstDirection = frame.Step(direction);
		var result = new HashSet<Coord>();
		foreach (var cell in area.GetArea(frame.Origin, burstDirection, frame.Fore, frame.Dorsal))
		{
			if (grid.IsInBounds(cell))
				result.Add(cell);
		}

		return result;
	}
}
