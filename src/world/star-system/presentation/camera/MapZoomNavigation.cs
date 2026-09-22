using GrimSpace.Math.Camera;

namespace GrimSpace.World.StarSystem.Presentation.Camera;

public enum ZoomStepCurve
{
	LinearBand,
	Multiplicative,
}

/// <summary>
/// Pure zoom-step and cutoff-crossing math for presentation-mode wheel navigation.
/// </summary>
public static class MapZoomNavigation
{
	public const int DefaultDetentsPerBand = 12;
	public const int DefaultInteriorDetents = 3;
	public const float DefaultMultiplicativeFactor = 0.10f;

	public static ZoomStepPolicy LinearBandStepPolicy => new(ZoomStepCurve.LinearBand);

	public static ZoomStepPolicy OverviewStepPolicy => new(
		ZoomStepCurve.Multiplicative,
		MultiplicativeFactor: DefaultMultiplicativeFactor);

	public static float ProportionalStep(
		float distance,
		in OrbitLimits limits,
		int direction,
		ZoomStepPolicy policy = default)
	{
		var signedStep = UncappedProportionalStep(distance, limits, direction, policy);
		return CapStepToBand(distance, signedStep, limits);
	}

	public static float UncappedProportionalStep(
		float distance,
		in OrbitLimits limits,
		int direction,
		ZoomStepPolicy policy = default)
	{
		policy = NormalizePolicy(policy);
		if (direction == 0)
			return 0f;

		if (policy.Curve == ZoomStepCurve.Multiplicative)
		{
			if (distance <= 0f || policy.MultiplicativeFactor <= 0f)
				return 0f;

			var magnitude = distance * policy.MultiplicativeFactor;
			return direction > 0 ? -magnitude : magnitude;
		}

		if (policy.DetentsPerBand <= 0)
			return 0f;

		var linearMagnitude = DetentSize(limits, policy.DetentsPerBand);
		if (linearMagnitude <= 0f)
			return 0f;

		return direction > 0 ? -linearMagnitude : linearMagnitude;
	}

	public static bool WouldCrossOutward(
		float distance,
		in OrbitLimits limits,
		int direction,
		ZoomStepPolicy policy = default)
	{
		var signedStep = UncappedProportionalStep(distance, limits, direction, policy);
		return signedStep > 0f && distance + signedStep > limits.MaxDistance;
	}

	public static bool WouldCrossInward(
		float distance,
		in OrbitLimits limits,
		int direction,
		ZoomStepPolicy policy = default)
	{
		var signedStep = UncappedProportionalStep(distance, limits, direction, policy);
		return signedStep < 0f && distance + signedStep < limits.MinDistance;
	}

	public static float InteriorDistance(
		in OrbitLimits limits,
		bool fromMinSide,
		int detentsFromCutoff = DefaultInteriorDetents,
		int detentsPerBand = DefaultDetentsPerBand)
	{
		var inset = DetentSize(limits, detentsPerBand) * detentsFromCutoff;
		return fromMinSide
			? limits.MinDistance + inset
			: limits.MaxDistance - inset;
	}

	public static float ClampSavedDistanceToInterior(
		float distance,
		in OrbitLimits limits,
		int detentsFromCutoff = DefaultInteriorDetents,
		int detentsPerBand = DefaultDetentsPerBand)
	{
		var minInterior = InteriorDistance(limits, fromMinSide: true, detentsFromCutoff, detentsPerBand);
		var maxInterior = InteriorDistance(limits, fromMinSide: false, detentsFromCutoff, detentsPerBand);
		return System.Math.Clamp(distance, minInterior, maxInterior);
	}

	private static ZoomStepPolicy NormalizePolicy(ZoomStepPolicy policy) =>
		policy == default ? LinearBandStepPolicy : policy;

	private static float DetentSize(in OrbitLimits limits, int detentsPerBand)
	{
		if (detentsPerBand <= 0)
			return 0f;

		return (limits.MaxDistance - limits.MinDistance) / detentsPerBand;
	}

	private static float CapStepToBand(float distance, float signedStep, in OrbitLimits limits)
	{
		if (signedStep < 0f)
		{
			var remaining = distance - limits.MinDistance;
			if (-signedStep > remaining)
				return -remaining;
		}
		else if (signedStep > 0f)
		{
			var remaining = limits.MaxDistance - distance;
			if (signedStep > remaining)
				return remaining;
		}

		return signedStep;
	}
}

public readonly record struct ZoomStepPolicy(
	ZoomStepCurve Curve = ZoomStepCurve.LinearBand,
	int DetentsPerBand = MapZoomNavigation.DefaultDetentsPerBand,
	float MultiplicativeFactor = MapZoomNavigation.DefaultMultiplicativeFactor);
