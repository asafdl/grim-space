using Godot;

namespace GrimSpace.World.StarSystem.Presentation;

public sealed record PlayerTravelSample(
	Vector3 WorldPosition,
	Vector3? TravelDirection,
	bool IsTravelActiveOrPending);
