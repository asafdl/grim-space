using GrimSpace.Battle.Player;
using GrimSpace.Math.Grid;
using Godot;

namespace GrimSpace.Battle.Presentation.Picking;

public readonly record struct MovementHoverSnapshot(
	Vector2 PointerPosition,
	Coord Coordinate,
	GridBasis EndBasis,
	int StepCount,
	Transform3D CameraTransform,
	Vector2I ViewportSize);

public static class MovementHoverReuse
{
	public static bool CanReuseDisplayedHover(
		MovementHoverSnapshot? displayed,
		Vector2 pointerPosition,
		Transform3D cameraTransform,
		Vector2I viewportSize,
		MovePathOption? displayedOption)
	{
		if (displayed is not MovementHoverSnapshot snapshot || displayedOption is null)
			return false;

		if (snapshot.PointerPosition != pointerPosition)
			return false;

		if (snapshot.CameraTransform != cameraTransform)
			return false;

		if (snapshot.ViewportSize != viewportSize)
			return false;

		return snapshot.Coordinate == displayedOption.EndPosition
			&& snapshot.EndBasis == displayedOption.EndBasis
			&& snapshot.StepCount == displayedOption.Steps.Count;
	}
}
