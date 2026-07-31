using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public readonly record struct GridDotRegion(int MinX, int MaxX, int MinY, int MaxY, int MinZ, int MaxZ)
{
	public int CellCount =>
		(MaxX - MinX + 1) * (MaxY - MinY + 1) * (MaxZ - MinZ + 1);

	/// <summary>
	/// Axis-aligned cube containing all ship positions plus each ship's legal one-turn move endpoints.
	/// </summary>
	public static GridDotRegion FromCombatReach(
		BoundedGrid grid,
		BattleSimulation sim,
		IEnumerable<string> actorIds,
		int extraPaddingCells = 0)
	{
		var points = new List<Coord>();

		foreach (var actorId in actorIds)
		{
			var position = sim.StateOf<ActorState>(actorId).Position;
			points.Add(position);

			foreach (var option in MoveUi.GetReachOptionsForActor(sim, actorId))
				points.Add(option.EndPosition);
		}

		if (points.Count == 0)
		{
			var mid = new Coord(grid.Width / 2, grid.Height / 2, grid.Depth / 2);
			return FromCube(grid, mid, 1);
		}

		var minX = points[0].X;
		var maxX = points[0].X;
		var minY = points[0].Y;
		var maxY = points[0].Y;
		var minZ = points[0].Z;
		var maxZ = points[0].Z;

		foreach (var point in points)
		{
			minX = System.Math.Min(minX, point.X);
			maxX = System.Math.Max(maxX, point.X);
			minY = System.Math.Min(minY, point.Y);
			maxY = System.Math.Max(maxY, point.Y);
			minZ = System.Math.Min(minZ, point.Z);
			maxZ = System.Math.Max(maxZ, point.Z);
		}

		var center = new Coord(
			(minX + maxX) / 2,
			(minY + maxY) / 2,
			(minZ + maxZ) / 2);

		var halfExtent = 0;
		foreach (var point in points)
		{
			halfExtent = System.Math.Max(halfExtent, System.Math.Abs(point.X - center.X));
			halfExtent = System.Math.Max(halfExtent, System.Math.Abs(point.Y - center.Y));
			halfExtent = System.Math.Max(halfExtent, System.Math.Abs(point.Z - center.Z));
		}

		halfExtent += extraPaddingCells;
		return FromCube(grid, center, halfExtent);
	}

	private static GridDotRegion FromCube(BoundedGrid grid, Coord center, int halfExtent)
	{
		halfExtent = System.Math.Max(halfExtent, 0);

		var minX = System.Math.Clamp(center.X - halfExtent, 0, grid.Width - 1);
		var maxX = System.Math.Clamp(center.X + halfExtent, 0, grid.Width - 1);
		var minY = System.Math.Clamp(center.Y - halfExtent, 0, grid.Height - 1);
		var maxY = System.Math.Clamp(center.Y + halfExtent, 0, grid.Height - 1);
		var minZ = System.Math.Clamp(center.Z - halfExtent, 0, grid.Depth - 1);
		var maxZ = System.Math.Clamp(center.Z + halfExtent, 0, grid.Depth - 1);

		return new GridDotRegion(minX, maxX, minY, maxY, minZ, maxZ);
	}
}
