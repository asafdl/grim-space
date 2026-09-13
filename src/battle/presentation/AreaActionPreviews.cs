using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation;

public sealed record CellVolumePreview(
	Coord Origin,
	IReadOnlySet<Coord> Cells);

public sealed record AreaActionPreview(
	IAction Action,
	CellVolumePreview Volume);

public sealed record AreaActionPreviews(
	IReadOnlyList<AreaActionPreview> Aim,
	IReadOnlyList<AreaActionPreview> Queued)
{
	public static AreaActionPreviews Empty { get; } = new([], []);
}
