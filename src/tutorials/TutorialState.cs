namespace GrimSpace.Tutorials;

public sealed class TutorialState
{
	public TutorialBeat CurrentBeat { get; set; } = TutorialBeat.None;

	public string? BeatAContractId { get; set; }

	public bool BeatBOffered { get; set; }

	public string? BeatBContractId { get; set; }

	public string? ActiveFlowId { get; set; }

	public int ActiveStepIndex { get; set; } = -1;
}
