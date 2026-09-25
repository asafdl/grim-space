using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Specs;

namespace GrimSpace.Tests;

internal static class BattleSpawnTestKit
{
	public static BattleSpawn Create(
		string id,
		EType chassis,
		ETeam team,
		Coord position,
		ExecutionAgent<BattleWorld, ActorRuntime> executionAgent) =>
		Create(ShipInstance.FromCatalog(id, chassis), team, position, executionAgent);

	public static BattleSpawn Create(
		ShipInstance ship,
		ETeam team,
		Coord position,
		ExecutionAgent<BattleWorld, ActorRuntime> executionAgent) =>
		new()
		{
			Ship = ship,
			Team = team,
			Position = position,
			ExecutionAgent = executionAgent,
		};

	public static ShipInstance FighterWithInstalledAbilities(
		string id,
		IReadOnlyList<InstalledAbility> installed) =>
		ShipInstance.FromSpec(
			id,
			FighterSpec.Instance,
			ShipLoadout.Create(
				FighterSpec.Instance,
				ShipCatalog.NewRunLoadoutFor(EType.Fighter).MaxHullPoints,
				ShipCatalog.NewRunLoadoutFor(EType.Fighter).MaxShieldPoints,
				installed));
}
