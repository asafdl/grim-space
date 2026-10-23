using Godot;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>Ship-local axes: X starboard, Y dorsal, Z fore (matches <see cref="UnitView"/> basis).</summary>
internal static class UnitFaceLocalGeometry
{
	private const float FaceSurfaceInset = 0.92f;

	public static Vector3 OutwardNormal(ESpatialOrientation face) =>
		face switch
		{
			ESpatialOrientation.Forward => new Vector3(0f, 0f, 1f),
			ESpatialOrientation.Retro => new Vector3(0f, 0f, -1f),
			ESpatialOrientation.Dorsal => Vector3.Up,
			ESpatialOrientation.Ventral => Vector3.Down,
			ESpatialOrientation.Starboard => Vector3.Right,
			ESpatialOrientation.Port => Vector3.Left,
			_ => Vector3.Up,
		};

	public static Vector3 EmitPointOnFace(Aabb bounds, ESpatialOrientation exitFace)
	{
		var normal = OutwardNormal(exitFace);
		var center = bounds.GetCenter();
		var half = bounds.Size * 0.5f;
		return center + new Vector3(
			normal.X * half.X,
			normal.Y * half.Y,
			normal.Z * half.Z) * FaceSurfaceInset;
	}

	public static ESpatialOrientation SparkExitFace(ESpatialOrientation hitFace) =>
		DirectionRules.Opposite(hitFace);

	public static Basis BasisWithLocalYAxis(Vector3 yAxis)
	{
		yAxis = yAxis.Normalized();
		if (yAxis.LengthSquared() < Mathf.Epsilon)
			return Basis.Identity;

		if (yAxis.Dot(Vector3.Up) < -0.999f)
			return new Basis(Vector3.Right, Mathf.Pi);

		var forward = Mathf.Abs(yAxis.Dot(Vector3.Forward)) > 0.99f
			? Vector3.Right
			: Vector3.Forward;
		var right = yAxis.Cross(forward).Normalized();
		forward = right.Cross(yAxis).Normalized();
		return new Basis(right, yAxis, forward);
	}
}
