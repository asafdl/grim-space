using GrimSpace.Math;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Contracts.Encounter;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Contracts;

/// <summary>
/// Rolls wreckage investigate outcomes (salvage vs ambush) and salvage loot. Ambush fleets are built in <see cref="ContractFactory"/>.
/// </summary>
public static class WreckageSalvageRoller
{
	private const float SalvageOutcomeWeight = 0.6f;
	private const int WreckScrapMultiplier = 3;

	public static bool RollSalvageOutcome(int mapSeed, string contractId)
	{
		ArgumentException.ThrowIfNullOrEmpty(contractId);
		var rng = Rng(mapSeed, contractId, "wreckage-outcome");
		return rng.NextDouble() < SalvageOutcomeWeight;
	}

	public static ResourceBundle RollSalvageLoot(int mapSeed, string contractId, EDangerLevel danger)
	{
		ArgumentException.ThrowIfNullOrEmpty(contractId);
		var budget = EncounterPowerCatalog.EncounterPowerBudget(danger);
		var wreck = ShipPowerCatalog.PickRandomWithinBudget(
			Rng(mapSeed, contractId, "wreckage-salvage-tier"),
			budget);
		var salvage = LootCatalog.SalvageFromShip(wreck, Rng(mapSeed, contractId, "wreckage-salvage-loot"));
		return ResourceBundle.Create(salvage.ToDictionary(
			entry => entry.Key,
			entry => entry.Key == ResourceId.ScrapAlloy
				? entry.Value * WreckScrapMultiplier
				: entry.Value));
	}

	private static StableRandom Rng(int mapSeed, string contractId, string scope) =>
		new(StableSeedMixer.From(mapSeed).Add(contractId).Add(scope).Value);
}
