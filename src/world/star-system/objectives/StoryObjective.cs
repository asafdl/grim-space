namespace GrimSpace.World.StarSystem.Objectives;

public sealed record StoryObjective(string Id, string Title, string Summary)
{
	public static StoryObjective FirstContract { get; } = new(
		"first-contract",
		"Get your first contract",
		"Go to the Administrative Core and accept your first contract.");
}
