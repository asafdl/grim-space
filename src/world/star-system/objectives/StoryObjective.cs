namespace GrimSpace.World.StarSystem.Objectives;

public sealed record StoryObjective(
	string Id,
	string Title,
	string Summary,
	string? RequiredContractId = null)
{
	public const string FirstContractId = "first-contract";

	public static StoryObjective FirstContract(string administrativePoiId)
	{
		ArgumentException.ThrowIfNullOrEmpty(administrativePoiId);
		return new StoryObjective(
			FirstContractId,
			"Get your first contract",
			$"Go to the [url={administrativePoiId}]Administrative Core[/url] and accept your first contract.");
	}

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
