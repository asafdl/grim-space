namespace GrimSpace.Tutorials;

public sealed class TutorialState
{
	private readonly HashSet<string> _completedFlows = new(StringComparer.Ordinal);

	public string? BeatAContractId { get; set; }

	public string? BeatBContractId { get; set; }

	public int ActiveStepIndex { get; set; } = -1;

	public bool PendingTutorialGraduation { get; set; }

	public bool IsFlowCompleted(string flowId) => _completedFlows.Contains(flowId);

	public void CompleteFlow(string flowId) => _completedFlows.Add(flowId);

	internal IReadOnlyList<string> CaptureCompletedFlows() => _completedFlows.ToArray();

	internal void RestoreCompletedFlows(IEnumerable<string> flowIds)
	{
		_completedFlows.Clear();
		foreach (var flowId in flowIds)
			_completedFlows.Add(flowId);
	}
}
