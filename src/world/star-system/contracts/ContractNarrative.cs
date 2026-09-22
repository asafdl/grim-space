namespace GrimSpace.World.StarSystem.Contracts;

public sealed record ContractNarrative(string Title, string Briefing, string TurnInDialog = "")
{
	public static ContractNarrative ForHunt(string title) =>
		new(
			title,
			"Pirate activity is reducing route efficiency. The Optimality decrees them as SCRAP!");

	public static ContractNarrative ForDelivery(string title, string issuerBriefing, string turnInDialog) =>
		new(title, issuerBriefing, turnInDialog);
}
