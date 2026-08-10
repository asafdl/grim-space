using Godot;

namespace GrimSpace.Battle.Presentation.Camera;

public interface ICameraRig
{
	Vector3 Pivot { get; }
	bool IsAutomationActive { get; }
	Vector3? AutomationTarget { get; }

	bool AreVisible(IReadOnlyList<Vector3> points, float margin);
	bool AreVisibleAtPivot(Vector3 pivot, IReadOnlyList<Vector3> points, float margin);

	bool TryCalculateCorrection(
		IReadOnlyList<Vector3> points,
		float margin,
		out Vector3 targetPivot);

	void TweenPivotTo(Vector3 target, float duration);
	void MovePivotToward(Vector3 target, float delta, float responseTime);
	void CancelAutomation();
}
