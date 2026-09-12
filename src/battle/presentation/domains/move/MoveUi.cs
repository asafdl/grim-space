using GrimSpace.Battle.Player;
using GrimSpace.Battle.Movement;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Domains.Move;

public static class MoveUi
{
	public static (IReadOnlyList<MoveCheckpoint> Checkpoints, Coord? Target) GetPathHighlights(
		IReadOnlyList<MovePathOption> paths,
		int? hoveredIndex,
		IReadOnlyList<MoveCheckpoint> committedPath,
		MovePathOption? selected = null)
	{
		if (selected is not null)
			return (selected.Checkpoints.Skip(1).ToList(), selected.EndPosition);

		if (hoveredIndex is int i)
			return (paths[i].Checkpoints.Skip(1).ToList(), paths[i].EndPosition);

		if (committedPath.Count > 0)
			return (committedPath, committedPath[^1].Position);

		return ([], null);
	}
}
