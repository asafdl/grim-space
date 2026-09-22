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
}
