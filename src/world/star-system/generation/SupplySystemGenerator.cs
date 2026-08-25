using GrimSpace.Core.Ids;
using GrimSpace.Math;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Generation;

public static class SupplySystemGenerator
{
	public static StarSystemBlueprint CreateBlueprint(int seed)
	{
		var plan = SupplySystemPlan.Copper;
		var unitSpawns = new[]
		{
			SpawnUnits(seed, EType.MiningBarge, 4, MinerSpawn),
			SpawnUnits(seed, EType.RefineryHauler, 2, HaulerSpawn),
			SpawnUnits(seed, EType.ExportFreighter, 2, FreighterSpawn),
			SpawnUnits(seed, EType.ComplianceVessel, 8, ComplianceSpawn),
			SpawnUnits(seed, EType.CargoShuttle, 4, MerchantSpawn),
			SpawnUnits(seed, EType.ServiceVessel, 6, CivilianSpawn),
		}.SelectMany(spawns => spawns).ToArray();

		return new StarSystemBlueprint(
			seed,
			StarMap.DevMapWidth,
			StarMap.DevMapHeight,
			EStarSystemClass.Supply,
			plan,
			plan.CreatePoiTemplates(seed),
			unitSpawns);

		static (string StartPoiId, string[] ChorePoiIds) MinerSpawn(
			SupplySystemPlan plan,
			StableRandom random)
		{
			if (random.NextDouble() < 0.5)
				return (plan.ExtractionPoiId, [plan.RefineryPoiId, plan.ExtractionPoiId]);

			return (plan.RefineryPoiId, [plan.ExtractionPoiId, plan.RefineryPoiId]);
		}

		static (string StartPoiId, string[] ChorePoiIds) HaulerSpawn(
			SupplySystemPlan plan,
			StableRandom random)
		{
			if (random.NextDouble() < 0.5)
				return (plan.StoragePoiId, [plan.RefineryPoiId, plan.StoragePoiId]);

			return (plan.RefineryPoiId, [plan.StoragePoiId, plan.RefineryPoiId]);
		}

		static (string StartPoiId, string[] ChorePoiIds) FreighterSpawn(
			SupplySystemPlan plan,
			StableRandom random)
		{
			if (random.NextDouble() < 0.5)
				return (plan.ExitPoiId, [plan.StoragePoiId, plan.ExitPoiId]);

			return (plan.StoragePoiId, [plan.ExitPoiId, plan.StoragePoiId]);
		}

		static (string StartPoiId, string[] ChorePoiIds) ComplianceSpawn(
			SupplySystemPlan plan,
			StableRandom random)
		{
			var visits = random.NextDouble() < 0.5
				? new[]
				{
					plan.ExtractionPoiId,
					plan.RefineryPoiId,
					plan.StoragePoiId,
					plan.ExitPoiId,
				}
				: new[]
				{
					plan.ExitPoiId,
					plan.StoragePoiId,
					plan.RefineryPoiId,
					plan.ExtractionPoiId,
				};

			return (plan.AdministrativePoiId, [..visits, plan.AdministrativePoiId]);
		}

		static (string StartPoiId, string[] ChorePoiIds) MerchantSpawn(
			SupplySystemPlan plan,
			StableRandom random) =>
			(plan.TradeHubPoiId, TradeChoreBuilder.BuildVisitChore(plan, random, plan.TradeHubPoiId));

		static (string StartPoiId, string[] ChorePoiIds) CivilianSpawn(
			SupplySystemPlan plan,
			StableRandom random) =>
			(plan.TradeHubPoiId, TradeChoreBuilder.BuildVisitChore(plan, random, plan.TradeHubPoiId));
	}

	private static IEnumerable<UnitSpawnIntent> SpawnUnits(
		int seed,
		EType type,
		int count,
		Func<SupplySystemPlan, StableRandom, (string StartPoiId, string[] ChorePoiIds)> planChore)
	{
		var plan = SupplySystemPlan.Copper;
		var typeSlug = StarSystemTypeSlug.For(type);
		for (var i = 0; i < count; i++)
		{
			var random = new StableRandom(
				StableSeedMixer.From(seed)
					.Add("unit-chore")
					.Add(typeSlug)
					.Add(i)
					.Value);
			var (startPoiId, chorePoiIds) = planChore(plan, random);
			yield return new UnitSpawnIntent(TypedIdGenerator.NextId(typeSlug), type, startPoiId, chorePoiIds);
		}
	}
}
