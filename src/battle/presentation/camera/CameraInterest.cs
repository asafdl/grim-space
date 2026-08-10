using Godot;

namespace GrimSpace.Battle.Presentation.Camera;

public readonly record struct CameraInterest(
	IReadOnlyList<Vector3> Points,
	CameraImportance Importance);
