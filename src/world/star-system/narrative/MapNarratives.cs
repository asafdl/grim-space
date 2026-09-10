namespace GrimSpace.World.StarSystem.Narrative;

public static class MapNarratives
{
	public const string OpeningId = "map-opening";

	public static bool TryGet(string id, StarMap world, out NarrativeDefinition definition)
	{
		if (id == OpeningId)
		{
			var administrativeCoreId = world.Blueprint.SupplyPlan.AdministrativePoiId;
			definition = new NarrativeDefinition(
				OpeningId,
				[
					"Oh, Syndi's beard, WHAT. A. SHITHOLE. this system is, how did I even end up here?",
					$"Oh right, the drugs...\nWell I need credits unless I want trouble, lets head to the [url={administrativeCoreId}]Administrative Core[/url] and see if they have any bounties on offer.",
				]);
			return true;
		}

		definition = null!;
		return false;
	}
}
