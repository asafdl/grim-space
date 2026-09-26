using GrimSpace.Units.Enums;
using GrimSpace.World.Factions;
using FleetType = GrimSpace.World.StarSystem.Units.EType;

namespace GrimSpace.World.StarSystem.Contracts.Objectives;

public sealed record FleetSpawnSpec(
	FleetType Type,
	EFaction Faction,
	int Seed,
	IReadOnlyList<(EType Chassis, EShipGearTier GearTier)> Members);
