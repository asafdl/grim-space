using Godot;
using GrimSpace.Math.Camera;

namespace GrimSpace.Battle.Presentation.Camera;

public interface ICameraRig
{
	Vector3 Pivot { get; }
	OrbitPose CurrentPose { get; }
	bool IsAutomationActive { get; }
	Vector3? AutomationTarget { get; }

	bool AreVisible(IReadOnlyList<Vector3> points, float margin);
	bool AreVisibleAtPivot(Vector3 pivot, IReadOnlyList<Vector3> points, float margin);

	bool TryCalculateCorrection(
		IReadOnlyList<Vector3> points,
		float margin,
		out Vector3 targetPivot);

	void TweenPivotTo(Vector3 target);
	void TweenPivotTo(Vector3 target, float duration);
	void SetFocus(Vector3 pivot, float distance, float yaw, float pitch);
	OrbitPose? CalculateOverview(IReadOnlyList<Vector3> points, IReadOnlyList<Vector3> priorityPoints);
	void MovePivotToward(Vector3 target, float delta, float responseTime);
	void CancelAutomation();
	void PlayImpactShake(ImpactShakeScale scale);
}
