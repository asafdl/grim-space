namespace GrimSpace.World.StarSystem.Narrative;

public static class MapNarratives
{
	public const string OpeningId = "map-opening";

	private static readonly NarrativeDefinition Opening = new(
		OpeningId,
		[
			"Oh, by Syndi's cores, WHAT. A. SHITHOLE this system is, how did I even end up here?",
			"Oh right, the drugs...\nWell I need credits unless I want trouble, lets head to the Administrative Core and see if they have any bounties on offer.",
		]);

	public static bool TryGet(string id, out NarrativeDefinition definition)
	{
		if (id == OpeningId)
		{
			definition = Opening;
			return true;
		}

		definition = null!;
		return false;
	}
}
