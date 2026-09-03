using GrimSpace.World.StarSystem.Areas;

namespace GrimSpace.World.StarSystem.Contracts;

public sealed record ContractNarrative(string Title, string Briefing)
{
	public static ContractNarrative ForHunt(string title, AreaPick searchArea) =>
		new(title, $"Hunt targets in the indicated sector. {searchArea.Description}");
}
