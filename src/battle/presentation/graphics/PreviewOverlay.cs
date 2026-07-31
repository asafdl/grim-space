using Godot;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>
/// Planning aids layered over the battle view: grid highlights and missile range shell.
/// </summary>
public partial class PreviewOverlay : Node3D
{
	private GridView _gridView = null!;
	private MissileRangeIndicator _missileRangeIndicator = null!;

	public void Configure(GridView gridView, MissileRangeIndicator missileRangeIndicator)
	{
		_gridView = gridView;
		_missileRangeIndicator = missileRangeIndicator;
	}

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
				_missileRangeIndicator.SetActive(null, 0);
				_gridView.SetMoveHighlights(
					frame.MoveOptions,
					frame.MovePath,
					frame.MoveTarget,
					frame.PreviewHazardCells);
				break;

			case EPlayerMode.Missile:
				_missileRangeIndicator.SetActive(frame.ActorState.Position, frame.MissileRange);
				_gridView.SetMissileHighlights(
					frame.PreviewHazardCells,
					frame.ValidMissileCells,
					frame.MissilePreviewCells);
				break;

			case EPlayerMode.Flak:
				_missileRangeIndicator.SetActive(null, 0);
				_gridView.SetFlakHighlights(
					frame.PreviewHazardCells,
					frame.ValidFlakPortCells,
					frame.ValidFlakStarboardCells,
					frame.FlakPreviewCells);
				break;

			case EPlayerMode.Railgun:
				_missileRangeIndicator.SetActive(null, 0);
				_gridView.SetRailgunHighlights(
					frame.RailgunTargetCells,
					frame.RailgunHoveredCell,
					frame.PreviewHazardCells);
				break;
		}
	}

	public void Clear()
	{
		_gridView.ClearHighlights();
		_missileRangeIndicator.SetActive(null, 0);
	}

	public void ApplyMoveHover(
		IReadOnlyList<Option> options,
		IReadOnlyList<Coord> path,
		Coord? target,
		IReadOnlySet<Coord> hazardCells)
	{
		_missileRangeIndicator.SetActive(null, 0);
		_gridView.SetMoveHighlights(options, path, target, hazardCells);
	}
}
