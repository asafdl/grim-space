using System.Text.Json;
using GrimSpace.Math.Grid;
using GrimSpace.Run.Persistence;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using FleetType = GrimSpace.World.StarSystem.Units.EType;
using BattleUnitType = GrimSpace.Units.Enums.EType;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class PersistenceAggressionTests
{
	[Fact]
	public void RuntimeCooldowns_ForkAndResetAreIsolated()
	{
		var runtime = new ActorRuntime();
		runtime.ActionCooldownUntilTick = 20;
		runtime.IgnoreUntilTickByTargetId["target"] = 30;

		var fork = runtime.Fork();
		fork.ActionCooldownUntilTick = 40;
		fork.IgnoreUntilTickByTargetId["target"] = 60;
		fork.IgnoreUntilTickByTargetId["other"] = 90;

		Assert.Equal(20, runtime.ActionCooldownUntilTick);
		Assert.Equal(30, runtime.IgnoreUntilTickByTargetId["target"]);
		Assert.DoesNotContain("other", runtime.IgnoreUntilTickByTargetId.Keys);

		runtime.Reset();
		Assert.Equal(0, runtime.ActionCooldownUntilTick);
		Assert.Empty(runtime.IgnoreUntilTickByTargetId);
	}

	[Fact]
	public void FleetAggression_RoundTrips()
	{
		var map = StarMap.Create(42);
		map.FleetRegistry.Add(CreatePirateFleet(7));
		var registry = PersistenceRegistry.CreateDefault();
		var captured = SaveDtoMapper.CaptureStarMap(map, registry);

		var restored = SaveDtoMapper.RestoreStarMap(captured, registry);
		Assert.Equal(
			7,
			restored.FleetRegistry.FleetOf("aggressive-pirate").State.AggressionRating);
	}

	private static Fleet CreatePirateFleet(int aggressionRating) =>
		ContractEnemySpawner.CreateAmbushFleet(
			new FleetSpawnSpec(
				FleetType.PirateFleet,
				EFaction.Pirates,
				42,
				[(BattleUnitType.RepurposedMiner, EShipGearTier.T0)],
				AggressionRatingOverride: aggressionRating),
			new Coord(10, 0, 10),
			"aggressive-pirate",
			"persistence");
}
