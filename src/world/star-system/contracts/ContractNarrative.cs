namespace GrimSpace.World.StarSystem.Contracts;

public sealed record ContractNarrative(string Title, string Briefing)
{
	public static ContractNarrative ForHunt(string title) =>
		new(
			title,
			"Pirate activity is reducing route efficiency. The Optimality requires them disassembled.");
}
