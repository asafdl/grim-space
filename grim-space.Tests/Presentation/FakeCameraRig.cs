using Godot;
using GrimSpace.Battle.Presentation.Camera;

namespace GrimSpace.Tests.Presentation;

internal sealed class FakeCameraRig : ICameraRig
{
	private Vector3 _pivot;
	private Vector3? _automationTarget;
	private bool _automationActive;

	public List<(Vector3 Target, float Duration)> TweenCalls { get; } = [];
	public List<(Vector3 Target, float Delta, float Response)> MoveCalls { get; } = [];
	public int CancelCalls { get; private set; }

	public Func<IReadOnlyList<Vector3>, float, bool>? VisibleAtPivotOverride { get; set; }
	public Func<IReadOnlyList<Vector3>, float, bool>? CorrectionOverride { get; set; }
	public Func<IReadOnlyList<Vector3>, float, Vector3>? CorrectionTargetOverride { get; set; }

	public Vector3 Pivot => _pivot;
	public bool IsAutomationActive => _automationActive;
	public Vector3? AutomationTarget => _automationTarget;

	public void SetPivot(Vector3 pivot) => _pivot = pivot;

	public bool AreVisible(IReadOnlyList<Vector3> points, float margin) =>
		AreVisibleAtPivot(_pivot, points, margin);

	public bool AreVisibleAtPivot(Vector3 pivot, IReadOnlyList<Vector3> points, float margin)
	{
		if (VisibleAtPivotOverride is not null)
			return VisibleAtPivotOverride(points, margin);

		foreach (var point in points)
		{
			var offset = point - pivot;
			if (offset.Length() > 30f)
				return false;
		}

		return true;
	}

	public bool TryCalculateCorrection(
		IReadOnlyList<Vector3> points,
		float margin,
		out Vector3 targetPivot)
	{
		if (CorrectionOverride is not null)
		{
			var needs = CorrectionOverride(points, margin);
			targetPivot = CorrectionTargetOverride?.Invoke(points, margin) ?? _pivot;
			return needs;
		}

		targetPivot = _pivot;
		foreach (var point in points)
		{
			var offset = point - _pivot;
			if (offset.Length() > 30f)
			{
				targetPivot = point;
				return true;
			}
		}

		return false;
	}

	public void TweenPivotTo(Vector3 target, float duration)
	{
		TweenCalls.Add((target, duration));
		_automationActive = true;
		_automationTarget = target;
		_pivot = target;
	}

	public void MovePivotToward(Vector3 target, float delta, float responseTime) =>
		MoveCalls.Add((target, delta, responseTime));

	public void CancelAutomation()
	{
		CancelCalls++;
		_automationActive = false;
		_automationTarget = null;
	}

	public void CompleteAutomation()
	{
		_automationActive = false;
		_automationTarget = null;
	}
}
