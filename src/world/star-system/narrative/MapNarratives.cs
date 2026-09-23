namespace GrimSpace.World.StarSystem.Narrative;

public static class MapNarratives
{
	public const string OpeningId = "map-opening";

	public static bool TryGet(string id, StarMap world, out NarrativeDefinition definition)
	{
		if (id == OpeningId)
		{
			definition = new NarrativeDefinition(
				OpeningId,
				[
					"Oh, Syndi's beard, WHAT. A. SHITHOLE. this system is, how did I even end up here?",
					$"Oh right, the drugs...\nWell I need credits unless I want trouble. Good thing I \"reclaimed\" that Class Z-Fighter on my way out.",
				]);
			return true;
		}

		definition = null!;
		return false;
	}
}
