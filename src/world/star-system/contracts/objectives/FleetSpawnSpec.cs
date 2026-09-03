using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Encounter;

namespace GrimSpace.World.StarSystem.Contracts.Objectives;

public sealed record FleetSpawnSpec(EFaction Faction, EDangerLevel Danger, int Seed);
