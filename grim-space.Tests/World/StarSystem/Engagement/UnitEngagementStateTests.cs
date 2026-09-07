using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.Tests.World.StarSystem.Engagement;

public sealed class UnitEngagementStateTests
{
	[Fact]
	public void FromSpawn_SetsContactRadiusFromSpawn()
	{
		var player = State.FromSpawn(new Spawn(
			"player",
			EType.PlayerFleet,
			"dock",
			default,
			UnitDefaults.SpeedPerTick(EType.PlayerFleet),
			UnitDefaults.ContactRadius(EType.PlayerFleet),
			[]));
		var pirate = State.FromSpawn(new Spawn(
			"pirate",
			EType.PirateFleet,
			"",
			new GrimSpace.Math.Grid.Coord(1, 0, 1),
			UnitDefaults.SpeedPerTick(EType.PirateFleet),
			UnitDefaults.ContactRadius(EType.PirateFleet),
			[]));

		Assert.Equal(6, player.ContactRadius);
		Assert.Equal(6, pirate.ContactRadius);
	}

	[Fact]
	public void SetEngagementIntentEffect_SetsBidirectionalHuntLink()
	{
		var map = StarMap.CreateDevDefault(42);
		var runtime = new ActorRuntime();
		var hunter = map.UnitRegistry.Ids.First();
		var target = AddPirate(map, "pirate-a", new GrimSpace.Math.Grid.Coord(10, 0, 10));

		new SetEngagementIntentEffect(hunter, target).Apply(map, runtime, hunter);

		Assert.Equal(target, map.StateOf(hunter).EngagementTargetUnitId);
		Assert.Equal(hunter, map.StateOf(target).HuntedByUnitId);
	}

	[Fact]
	public void ClearEngagementIntentEffect_ClearsBidirectionalHuntLink()
	{
		var map = StarMap.CreateDevDefault(42);
		var runtime = new ActorRuntime();
		var hunter = map.UnitRegistry.Ids.First();
		var target = AddPirate(map, "pirate-a", new GrimSpace.Math.Grid.Coord(10, 0, 10));
		new SetEngagementIntentEffect(hunter, target).Apply(map, runtime, hunter);

		new ClearEngagementIntentEffect(hunter).Apply(map, runtime, hunter);

		Assert.Null(map.StateOf(hunter).EngagementTargetUnitId);
		Assert.Null(map.StateOf(target).HuntedByUnitId);
	}

	[Fact]
	public void SetEngagementIntentEffect_ReplaceTarget_ClearsPreviousTargetBackReference()
	{
		var map = StarMap.CreateDevDefault(42);
		var runtime = new ActorRuntime();
		var hunter = map.UnitRegistry.Ids.First();
		var firstTarget = AddPirate(map, "pirate-a", new GrimSpace.Math.Grid.Coord(10, 0, 10));
		var secondTarget = AddPirate(map, "pirate-b", new GrimSpace.Math.Grid.Coord(20, 0, 20));

		new SetEngagementIntentEffect(hunter, firstTarget).Apply(map, runtime, hunter);
		new SetEngagementIntentEffect(hunter, secondTarget).Apply(map, runtime, hunter);

		Assert.Equal(secondTarget, map.StateOf(hunter).EngagementTargetUnitId);
		Assert.Null(map.StateOf(firstTarget).HuntedByUnitId);
		Assert.Equal(hunter, map.StateOf(secondTarget).HuntedByUnitId);
	}

	[Fact]
	public void Clone_CopiesEngagementFieldsIndependently()
	{
		var map = StarMap.CreateDevDefault(42);
		var runtime = new ActorRuntime();
		var hunter = map.UnitRegistry.Ids.First();
		var target = AddPirate(map, "pirate-a", new GrimSpace.Math.Grid.Coord(10, 0, 10));
		new SetEngagementIntentEffect(hunter, target).Apply(map, runtime, hunter);

		var snapshot = map.StateOf(hunter).Clone();
		new ClearEngagementIntentEffect(hunter).Apply(map, runtime, hunter);

		Assert.Null(map.StateOf(hunter).EngagementTargetUnitId);
		Assert.Equal(target, snapshot.EngagementTargetUnitId);
	}

	private static string AddPirate(StarMap map, string id, GrimSpace.Math.Grid.Coord coord)
	{
		map.UnitRegistry.Add(Factory.CreatePirateFleet(
			id,
			coord,
			GrimSpace.World.Factions.EFaction.Pirates,
			new GrimSpace.World.StarSystem.Encounter.CombatProfile(
				GrimSpace.World.StarSystem.Encounter.EDangerLevel.VeryLow,
				1)));
		return id;
	}
}
