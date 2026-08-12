using GrimSpace.Battle.Player;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Domains.Move;

public static class MoveUi
{
	public static (IReadOnlyList<Coord> Path, Coord? Target) GetPathHighlights(
		IReadOnlyList<MovePathOption> paths,
		int? hoveredIndex,
		IReadOnlyList<Coord> committedPath)
	{
		if (hoveredIndex is int i)
			return (paths[i].Cells, paths[i].EndPosition);

		if (committedPath.Count > 0)
			return (committedPath, committedPath[^1]);

		return ([], null);
	}
}
