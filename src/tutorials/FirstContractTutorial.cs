using GrimSpace.Education;
using GrimSpace.World.StarSystem;

namespace GrimSpace.Tutorials;

public static class FirstContractTutorial
{
	public const string Id = "first-contract";

	public static TutorialFlow Create(StarMap map)
	{
		ArgumentNullException.ThrowIfNull(map);
		return new TutorialFlow(
			Id,
			map.Blueprint.SupplyPlan.AdministrativePoiId,
			new TutorialDialogContent(
				"Your first contract",
				$"Right-click the [url={map.Blueprint.SupplyPlan.AdministrativePoiId}]Administrative Core[/url] on the map to move your fleet there."));
	}
}
