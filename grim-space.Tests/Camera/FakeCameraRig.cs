using Godot;
using GrimSpace.Battle.Presentation.Camera;

namespace GrimSpace.Tests.Camera;

/// <summary>
/// Stand-in camera rig for director tests. The real rig is a Godot <c>Camera3D</c>, which
/// cannot be constructed inside this test project (AGENTS.md forbids touching the Godot
/// framework from tests; doing so aborts the test host with a native access violation).
/// </summary>
internal sealed class FakeCameraRig : ICameraRig
{
	public Vector3 Pivot { get; private set; }
	public bool IsAutomationActive { get; private set; }
	public Vector3? AutomationTarget { get; private set; }

	/// <summary>Points the rig reports as already on screen.</summary>
	public bool EverythingVisible { get; set; } = true;

	public int CancelAutomationCalls { get; private set; }
	public int MovePivotCalls { get; private set; }
	public int TweenPivotCalls { get; private set; }

	public bool AreVisible(IReadOnlyList<Vector3> points, float margin) => EverythingVisible;

	public bool AreVisibleAtPivot(Vector3 pivot, IReadOnlyList<Vector3> points, float margin) =>
		EverythingVisible;

	public bool TryCalculateCorrection(
		IReadOnlyList<Vector3> points,
		float margin,
		out Vector3 targetPivot)
	{
		targetPivot = Pivot;
		return false;
	}

	public void TweenPivotTo(Vector3 target)
	{
		TweenPivotCalls++;
		Pivot = target;
		AutomationTarget = target;
		IsAutomationActive = true;
	}

	public void TweenPivotTo(Vector3 target, float duration) => TweenPivotTo(target);

	public void MovePivotToward(Vector3 target, float delta, float responseTime)
	{
		MovePivotCalls++;
		Pivot = target;
	}

	public void CancelAutomation()
	{
		CancelAutomationCalls++;
		IsAutomationActive = false;
		AutomationTarget = null;
	}
}
