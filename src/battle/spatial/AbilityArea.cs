using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;
using Microsoft.Extensions.Caching.Memory;

namespace GrimSpace.Battle.Spatial;

public static class AbilityArea
{
	private const int CacheSizeLimit = 256;

	private static readonly MemoryCache GeometryCache =
		new(new MemoryCacheOptions { SizeLimit = CacheSizeLimit });

	private static readonly MemoryCache BlockingCache =
		new(new MemoryCacheOptions { SizeLimit = CacheSizeLimit });

	public static FrozenSet<Coord> CellsInBounds(
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

		return GeometryCache.GetOrCreate(key, entry =>
		{
			entry.Size = 1;
			return Compute(area, frame, direction, grid);
		})!;
	}

	public static FrozenSet<Coord> ManhattanBallInBounds(Coord origin, int radius, Grid grid)
	{
		var key = (origin, radius, grid.Width, grid.Height, grid.Depth);
		return GeometryCache.GetOrCreate(key, entry =>
		{
			entry.Size = 1;
			return Manhattan.EnumerateBall(origin, radius)
				.Where(grid.IsInBounds)
				.ToFrozenSet();
		})!;
	}

	public static IReadOnlySet<Coord> ApplyBlocking(
		Coord origin,
		FrozenSet<Coord> candidates,
		bool isBlockable,
		FrozenSet<Coord> blockingCells)
	{
		if (!isBlockable || blockingCells.Count == 0)
			return candidates;

		var key = new BlockingCacheKey(origin, candidates, blockingCells);
		return BlockingCache.GetOrCreate(key, entry =>
		{
			entry.Size = 1;
			return candidates
				.Where(cell => !IsShadowed(origin, cell, blockingCells))
				.ToFrozenSet();
		})!;
	}

	private static bool IsShadowed(Coord origin, Coord target, IReadOnlySet<Coord> blockers)
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

	private static FrozenSet<Coord> Compute(
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

		return result.ToFrozenSet();
	}

	private readonly struct BlockingCacheKey : IEquatable<BlockingCacheKey>
	{
		private readonly Coord _origin;
		private readonly FrozenSet<Coord> _candidates;
		private readonly FrozenSet<Coord> _blockingCells;

		public BlockingCacheKey(
			Coord origin,
			FrozenSet<Coord> candidates,
			FrozenSet<Coord> blockingCells)
		{
			_origin = origin;
			_candidates = candidates;
			_blockingCells = blockingCells;
		}

		public bool Equals(BlockingCacheKey other) =>
			_origin.Equals(other._origin)
			&& ReferenceEquals(_candidates, other._candidates)
			&& ReferenceEquals(_blockingCells, other._blockingCells);

		public override bool Equals(object? obj) =>
			obj is BlockingCacheKey other && Equals(other);

		public override int GetHashCode() =>
			HashCode.Combine(
				_origin,
				RuntimeHelpers.GetHashCode(_candidates),
				RuntimeHelpers.GetHashCode(_blockingCells));
	}
}
