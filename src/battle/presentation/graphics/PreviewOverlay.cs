using Godot;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>
/// Planning aids layered over the battle view: grid highlights.
/// </summary>
public partial class PreviewOverlay : Node3D
{
	private GridView _gridView = null!;

	public void Configure(GridView gridView) => _gridView = gridView;

	public void Apply(PresentationFrame frame)
	{
		if (frame.ActiveUnit is null || frame.ShowOutcomeOverlay)
		{
			Clear();
			return;
		}

		switch (frame.Mode)
		{
			case EPlayerMode.Move:
				_gridView.SetMoveHighlights(
					frame.MoveOptions,
					frame.MovePath,
					frame.MoveTarget,
					frame.PreviewHazardCells);
				break;

			case EPlayerMode.Flak:
				_gridView.SetFlakHighlights(
					frame.PreviewHazardCells,
					frame.ValidFlakPortCells,
					frame.ValidFlakStarboardCells,
					frame.FlakPreviewCells);
				break;

			case EPlayerMode.Railgun:
				_gridView.SetRailgunHighlights(
					frame.RailgunCells,
					frame.RailgunPreviewCells,
					frame.PreviewHazardCells);
				break;
		}
	}

	public void Clear() => _gridView.ClearHighlights();

	public void ApplyMoveHover(
		IReadOnlyList<Option> options,
		IReadOnlyList<Coord> path,
		Coord? target,
		IReadOnlySet<Coord> hazardCells) =>
		_gridView.SetMoveHighlights(options, path, target, hazardCells);
}
