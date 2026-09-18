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
			[
				new TutorialStep(
					map.Blueprint.SupplyPlan.AdministrativePoiId,
					new TutorialDialogContent(
						"Your first contract",
						TutorialCopy.MapMoveToPoi(
							map.Blueprint.SupplyPlan.AdministrativePoiId,
							"Administrative Core"),
						AcceptText: null),
					FocusTarget: false,
					AdvanceOnAccept: false)
			]);
	}
}
