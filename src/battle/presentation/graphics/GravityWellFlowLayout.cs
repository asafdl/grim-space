using GrimSpace.Battle.NonUnits;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

internal readonly record struct GravityWellFlowRun(
	Coord StartHalfCells,
	Coord EndHalfCells,
	Coord Normal,
	Coord Heading);

internal static class GravityWellFlowLayout
{
	public static IReadOnlyList<GravityWellFlowRun> Build(IEnumerable<GravityWellFace> faces, Coord center)
	{
		var surface = new HashSet<(Coord Cell, Coord Normal)>();
		var flow = new HashSet<(Coord Cell, Coord Normal, Coord Heading)>();
		foreach (var face in faces)
		{
			surface.Add((face.Cell, face.Normal));
			flow.Add((face.Cell, face.Normal, face.FirstHeading));
			flow.Add((face.Cell, face.Normal, face.SecondHeading));
		}

		var runs = new List<GravityWellFlowRun>();
		foreach (var (cell, normal, heading) in flow)
		{
			var previous = cell - heading;
			if (flow.Contains((previous, normal, heading)))
				continue;

			var start = (cell - center) * 2;
			if (!surface.Contains((previous, normal)))
				start -= heading;

			var last = cell;
			while (flow.Contains((last + heading, normal, heading)))
				last += heading;
			var end = (last - center) * 2;
			if (!surface.Contains((last + heading, normal)))
				end += heading;

			if (start != end)
				runs.Add(new GravityWellFlowRun(start, end, normal, heading));
		}
		return runs;
	}
}
