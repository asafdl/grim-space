using GrimSpace.Math;

namespace GrimSpace.World.StarSystem.Generation;

public static class TradeChoreBuilder
{
	private static readonly string[][] VisitTemplates =
	[
		["poi-refinery", "poi-extraction", "poi-admin", "poi-exit", "poi-storage"],
		["poi-exit", "poi-storage", "poi-refinery", "poi-extraction", "poi-admin", "poi-exit"],
		["poi-storage", "poi-exit", "poi-admin", "poi-extraction", "poi-refinery"],
	];

	public static string[] BuildVisitChore(SupplySystemPlan plan, StableRandom random, string homePoiId)
	{
		var template = VisitTemplates[(int)(random.NextDouble() * VisitTemplates.Length)];
		var chore = new string[template.Length + 1];
		for (var i = 0; i < template.Length; i++)
			chore[i] = ResolvePoiId(plan, template[i]);

		chore[^1] = homePoiId;
		ValidateChore(plan, homePoiId, chore);
		return chore;
	}

	private static string ResolvePoiId(SupplySystemPlan plan, string templatePoiId) =>
		templatePoiId switch
		{
			"poi-extraction" => plan.ExtractionPoiId,
			"poi-refinery" => plan.RefineryPoiId,
			"poi-storage" => plan.StoragePoiId,
			"poi-exit" => plan.ExitPoiId,
			"poi-admin" => plan.AdministrativePoiId,
			_ => throw new InvalidOperationException($"Unknown trade chore template POI '{templatePoiId}'."),
		};

	private static void ValidateChore(SupplySystemPlan plan, string homePoiId, IReadOnlyList<string> chore)
	{
		var current = homePoiId;
		foreach (var destination in chore)
		{
			if (!plan.HasRoute(current, destination))
			{
				throw new InvalidOperationException(
					$"Trade chore is missing route {current} -> {destination}.");
			}

			current = destination;
		}
	}
}
