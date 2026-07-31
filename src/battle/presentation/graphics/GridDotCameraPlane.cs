using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

public enum GridDotPlacementMode
{
	Volume,
	CameraPlane,
}

/// <summary>World axis held constant for an axis-aligned slice (e.g. X → YZ plane).</summary>
public enum GridDotSliceAxis
{
	X,
	Y,
	Z,
}

public static class GridDotCameraPlane
{
	public static GridDotSliceAxis ClosestAxisAlignedPlane(Camera3D camera)
	{
		var forward = -camera.GlobalTransform.Basis.Z;
		var absX = System.Math.Abs(forward.X);
		var absY = System.Math.Abs(forward.Y);
		var absZ = System.Math.Abs(forward.Z);

		if (absX >= absY && absX >= absZ)
			return GridDotSliceAxis.X;

		if (absY >= absX && absY >= absZ)
			return GridDotSliceAxis.Y;

		return GridDotSliceAxis.Z;
	}

	public static string PlaneLabel(GridDotSliceAxis lockedAxis) =>
		lockedAxis switch
		{
			GridDotSliceAxis.X => "YZ",
			GridDotSliceAxis.Y => "XZ",
			GridDotSliceAxis.Z => "XY",
			_ => "?",
		};
}
