using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Encounter;
using BattleUnitType = GrimSpace.Units.Enums.EType;
using FleetType = GrimSpace.World.StarSystem.Units.EType;

namespace GrimSpace.World.StarSystem.Contracts;

/// <summary>
/// Hunt encounter composition and difficulty. Spawn seed is assigned when the contract is created.
/// </summary>
public sealed record HuntEncounterArgs(
	FleetType FleetType,
	EFaction Faction,
	EDangerLevel Danger,
	IReadOnlyList<BattleUnitType> MemberTypes);
