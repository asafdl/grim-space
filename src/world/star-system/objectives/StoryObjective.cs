namespace GrimSpace.World.StarSystem.Objectives;

public sealed record StoryObjective(
	string Id,
	string Title,
	string Summary,
	string? RequiredContractId = null)
{
	public static StoryObjective FirstContract { get; } = new(
		"first-contract",
		"Get your first contract",
		"Go to the Administrative Core and accept your first contract.");

	public static StoryObjective BeatBContract(string contractId)
	{
		ArgumentException.ThrowIfNullOrEmpty(contractId);
		return new StoryObjective(
			"beat-b-contract",
			"Find another contract",
			"Zoom out to overview and find the next contract offer.",
			contractId);
	}
}
