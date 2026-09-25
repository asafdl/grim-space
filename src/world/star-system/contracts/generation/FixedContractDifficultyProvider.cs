using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Encounter;
using BattleUnitType = GrimSpace.Units.Enums.EType;
using FleetType = GrimSpace.World.StarSystem.Units.EType;

namespace GrimSpace.World.StarSystem.Contracts.Generation;

public sealed class FixedContractDifficultyProvider
{
	public static FixedContractDifficultyProvider Alpha { get; } = new();

	private readonly ContractDifficultyProfile _profile = new(
		new HuntEncounterArgs(
			FleetType.PirateFleet,
			EFaction.Pirates,
			EDangerLevel.VeryLow,
			[BattleUnitType.Patrol]),
		HuntRewardCredits: 75,
		DeliveryRewardCredits: 50);

	public ContractDifficultyProfile Get(StarMap map, int tick)
	{
		ArgumentNullException.ThrowIfNull(map);
		return _profile;
	}
}
