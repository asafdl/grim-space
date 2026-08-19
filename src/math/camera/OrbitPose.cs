using Godot;

namespace GrimSpace.Math.Camera;

/// <summary>Distance and pitch clamps for an orbit camera. Domain-agnostic.</summary>
public readonly record struct OrbitLimits(
	float MinDistance,
	float MaxDistance,
	float MinPitch,
	float MaxPitch);

/// <summary>Shared default sensitivities for orbit / screen-pan / keyboard-pan / zoom.</summary>
public static class OrbitControls
{
	public const float OrbitSensitivity = 0.004f;
	public const float PanSensitivity = 0.025f;
	public const float KeyboardPanSpeed = 28f;
	public const float ZoomStep = 1.5f;
}

/// <summary>
/// Pivot + spherical orbit pose. Pure math — Godot nodes apply the resulting camera position.
/// </summary>
public struct OrbitPose
{
	public Vector3 Pivot;
	public float Yaw;
	public float Pitch;
	public float Distance;

	public readonly Vector3 Offset()
	{
		var dir = new Vector3(
			Mathf.Cos(Pitch) * Mathf.Sin(Yaw),
			Mathf.Sin(Pitch),
			Mathf.Cos(Pitch) * Mathf.Cos(Yaw));
		return dir * Distance;
	}

	public readonly Vector3 CameraPosition() => Pivot + Offset();

	public void Clamp(in OrbitLimits limits)
	{
		Distance = Mathf.Clamp(Distance, limits.MinDistance, limits.MaxDistance);
		Pitch = Mathf.Clamp(Pitch, limits.MinPitch, limits.MaxPitch);
	}

	public void Orbit(Vector2 mouseDelta, float sensitivity, in OrbitLimits limits)
	{
		Yaw -= mouseDelta.X * sensitivity;
		Pitch = Mathf.Clamp(Pitch - mouseDelta.Y * sensitivity, limits.MinPitch, limits.MaxPitch);
	}

	/// <summary>Screen-space pan along the camera's right and up axes.</summary>
	public void ScreenPan(Vector2 mouseDelta, Vector3 cameraRight, Vector3 cameraUp, float sensitivity)
	{
		Pivot -= cameraRight * mouseDelta.X * sensitivity;
		Pivot += cameraUp * mouseDelta.Y * sensitivity;
	}

	/// <summary>
	/// Ground-plane pan. <paramref name="pan"/> is typically a normalized WASD vector
	/// where +Y is "into the scene" (flat -camera.forward).
	/// </summary>
	public void FlatPan(Vector2 pan, Vector3 cameraRight, Vector3 cameraForward, float speed, float delta)
	{
		if (pan == Vector2.Zero)
			return;

		var right = Flatten(cameraRight);
		var forward = Flatten(cameraForward);
		Pivot += (right * pan.X + forward * pan.Y) * speed * delta;
	}

	public void Zoom(float amount, in OrbitLimits limits) =>
		Distance = Mathf.Clamp(Distance + amount, limits.MinDistance, limits.MaxDistance);

	public void SetFocus(Vector3 pivot, float distance, float yaw, float pitch, in OrbitLimits limits)
	{
		Pivot = pivot;
		Yaw = yaw;
		Distance = distance;
		Pitch = pitch;
		Clamp(limits);
	}

	/// <summary>Yaw/pitch that place the camera along <paramref name="direction"/> from the pivot.</summary>
	public static (float Yaw, float Pitch) AnglesForDirection(Vector3 direction, in OrbitLimits limits)
	{
		var dir = direction.Normalized();
		if (dir.LengthSquared() < 0.001f)
			return (0f, Mathf.Clamp(-0.35f, limits.MinPitch, limits.MaxPitch));

		return (
			Mathf.Atan2(dir.X, dir.Z),
			Mathf.Clamp(Mathf.Asin(dir.Y), limits.MinPitch, limits.MaxPitch));
	}

	/// <summary>Recover pose from an existing camera offset (e.g. scene transform sync).</summary>
	public static OrbitPose FromOffset(
		Vector3 pivot,
		Vector3 cameraPosition,
		in OrbitLimits limits,
		float fallbackDistance = 25f,
		float fallbackPitch = -0.5f,
		float fallbackYaw = 0.8f)
	{
		var offset = cameraPosition - pivot;
		var distance = offset.Length();
		if (distance < 0.001f)
		{
			return new OrbitPose
			{
				Pivot = pivot,
				Distance = Mathf.Clamp(fallbackDistance, limits.MinDistance, limits.MaxDistance),
				Pitch = Mathf.Clamp(fallbackPitch, limits.MinPitch, limits.MaxPitch),
				Yaw = fallbackYaw,
			};
		}

		var dir = offset / distance;
		var pose = new OrbitPose
		{
			Pivot = pivot,
			Distance = Mathf.Clamp(distance, limits.MinDistance, limits.MaxDistance),
			Pitch = Mathf.Asin(dir.Y),
			Yaw = Mathf.Atan2(dir.X, dir.Z),
		};
		pose.Clamp(limits);
		return pose;
	}

	/// <summary>Flat XZ axes from a camera basis (right = Basis.X, forward = -Basis.Z).</summary>
	public static (Vector3 Right, Vector3 Forward) FlatPanAxes(Basis basis)
	{
		var right = Flatten(basis.X);
		var forward = Flatten(-basis.Z);
		return (right, forward);
	}

	private static Vector3 Flatten(Vector3 v)
	{
		v.Y = 0f;
		return v.LengthSquared() > 0.001f ? v.Normalized() : Vector3.Zero;
	}
}
