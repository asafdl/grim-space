using Godot;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Camera;

internal static class ImpactRecoil
{
	public static Vector3 WorldDirectionFromHit(State target, ESpatialOrientation hitFace)
	{
		var exit = DirectionRules.Opposite(hitFace);
		var step = BodyFrame.From(target).Step(exit);
		var direction = new Vector3(step.X, step.Y, step.Z);
		return direction.LengthSquared() > Mathf.Epsilon ? direction.Normalized() : Vector3.Up;
	}
}
