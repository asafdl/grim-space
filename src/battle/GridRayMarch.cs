using System;
using System.Collections.Generic;

namespace GrimSpace.Battle;

public static class GridRayMarch
{
	public static List<GridCoord> March(BattleGrid grid, float originX, float originY, float originZ, float dirX, float dirY, float dirZ, float cellSize)
	{
		var cells = new List<GridCoord>();
		var lengthSq = dirX * dirX + dirY * dirY + dirZ * dirZ;
		if (lengthSq < 1e-8f)
			return cells;

		var invLen = 1f / MathF.Sqrt(lengthSq);
		dirX *= invLen;
		dirY *= invLen;
		dirZ *= invLen;

		var x = (int)MathF.Floor(originX / cellSize);
		var y = (int)MathF.Floor(originY / cellSize);
		var z = (int)MathF.Floor(originZ / cellSize);

		var stepX = dirX > 0 ? 1 : dirX < 0 ? -1 : 0;
		var stepY = dirY > 0 ? 1 : dirY < 0 ? -1 : 0;
		var stepZ = dirZ > 0 ? 1 : dirZ < 0 ? -1 : 0;

		var tMaxX = NextBoundaryT(originX, dirX, x, cellSize);
		var tMaxY = NextBoundaryT(originY, dirY, y, cellSize);
		var tMaxZ = NextBoundaryT(originZ, dirZ, z, cellSize);

		var tDeltaX = stepX == 0 ? float.PositiveInfinity : cellSize / MathF.Abs(dirX);
		var tDeltaY = stepY == 0 ? float.PositiveInfinity : cellSize / MathF.Abs(dirY);
		var tDeltaZ = stepZ == 0 ? float.PositiveInfinity : cellSize / MathF.Abs(dirZ);

		const int maxSteps = 128;
		for (var i = 0; i < maxSteps; i++)
		{
			if (grid.IsInBounds(new GridCoord(x, y, z)))
				cells.Add(new GridCoord(x, y, z));
			else if (cells.Count > 0)
				break;

			if (tMaxX < tMaxY)
			{
				if (tMaxX < tMaxZ)
				{
					x += stepX;
					tMaxX += tDeltaX;
				}
				else
				{
					z += stepZ;
					tMaxZ += tDeltaZ;
				}
			}
			else
			{
				if (tMaxY < tMaxZ)
				{
					y += stepY;
					tMaxY += tDeltaY;
				}
				else
				{
					z += stepZ;
					tMaxZ += tDeltaZ;
				}
			}
		}

		return cells;
	}

	private static float NextBoundaryT(float origin, float direction, int voxel, float cellSize)
	{
		if (MathF.Abs(direction) < 1e-8f)
			return float.PositiveInfinity;

		var boundary = direction > 0 ? (voxel + 1) * cellSize : voxel * cellSize;
		return (boundary - origin) / direction;
	}
}
