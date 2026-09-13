using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Encounter;
using BattleUnitType = GrimSpace.Units.Enums.EType;
using FleetType = GrimSpace.World.StarSystem.Units.EType;

namespace GrimSpace.World.StarSystem.Contracts.Objectives;

public sealed record FleetSpawnSpec(
	FleetType Type,
	EFaction Faction,
	EDangerLevel Danger,
	int Seed,
	IReadOnlyList<BattleUnitType> MemberTypes);
