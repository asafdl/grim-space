using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Engagement;

public sealed class UnitEngagementStateTests(StarMapFixture maps)
{
	[Fact]
	public void FromSpawn_SetsEngageRadiusFromSpawn()
	{
		var player = State.FromSpawn(new Spawn(
			"player",
			EType.PlayerFleet,
			"dock",
			default,
			UnitDefaults.SpeedPerTick(EType.PlayerFleet),
			UnitDefaults.EngageRadius(EType.PlayerFleet),
			[]));
		var pirate = State.FromSpawn(new Spawn(
			"pirate",
			EType.PirateFleet,
			"",
			new GrimSpace.Math.Grid.Coord(1, 0, 1),
			UnitDefaults.SpeedPerTick(EType.PirateFleet),
			UnitDefaults.EngageRadius(EType.PirateFleet),
			[]));

		Assert.Equal(6, player.EngageRadius);
		Assert.Equal(6, pirate.EngageRadius);
	}

	[Fact]
	public void SetEngagementIntentEffect_SetsBidirectionalHuntLink()
	{
		var map = maps.Fresh(42);
		var runtime = new ActorRuntime();
		var hunter = map.FleetRegistry.Ids.First();
		var target = AddPirate(map, "pirate-a", new GrimSpace.Math.Grid.Coord(10, 0, 10));

		new SetEngagementIntentEffect(hunter, target).Apply(map, runtime, hunter);

		Assert.Equal(target, EngagementAssertions.Hunting(map.StateOf(hunter)));
		Assert.Equal(EEngagementPhase.Pursuing, EngagementAssertions.Phase(map.StateOf(hunter)));
		Assert.Equal(hunter, EngagementAssertions.HuntedBy(map.StateOf(target)));
	}

	[Fact]
	public void ClearEngagementIntentEffect_ClearsBidirectionalHuntLink()
	{
		var map = maps.Fresh(42);
		var runtime = new ActorRuntime();
		var hunter = map.FleetRegistry.Ids.First();
		var target = AddPirate(map, "pirate-a", new GrimSpace.Math.Grid.Coord(10, 0, 10));
		new SetEngagementIntentEffect(hunter, target).Apply(map, runtime, hunter);

		new ClearEngagementIntentEffect(hunter).Apply(map, runtime, hunter);

		Assert.Null(EngagementAssertions.Hunting(map.StateOf(hunter)));
		Assert.Null(EngagementAssertions.HuntedBy(map.StateOf(target)));
	}

	[Fact]
	public void SetEngagementIntentEffect_ReplaceTarget_ClearsPreviousTargetBackReference()
	{
		var map = maps.Fresh(42);
		var runtime = new ActorRuntime();
		var hunter = map.FleetRegistry.Ids.First();
		var firstTarget = AddPirate(map, "pirate-a", new GrimSpace.Math.Grid.Coord(10, 0, 10));
		var secondTarget = AddPirate(map, "pirate-b", new GrimSpace.Math.Grid.Coord(20, 0, 20));

		new SetEngagementIntentEffect(hunter, firstTarget).Apply(map, runtime, hunter);
		new SetEngagementIntentEffect(hunter, secondTarget).Apply(map, runtime, hunter);

		Assert.Equal(secondTarget, EngagementAssertions.Hunting(map.StateOf(hunter)));
		Assert.Null(EngagementAssertions.HuntedBy(map.StateOf(firstTarget)));
		Assert.Equal(hunter, EngagementAssertions.HuntedBy(map.StateOf(secondTarget)));
	}

	[Fact]
	public void Clone_CopiesEngagementFieldsIndependently()
	{
		var map = maps.Fresh(42);
		var runtime = new ActorRuntime();
		var hunter = map.FleetRegistry.Ids.First();
		var target = AddPirate(map, "pirate-a", new GrimSpace.Math.Grid.Coord(10, 0, 10));
		new SetEngagementIntentEffect(hunter, target).Apply(map, runtime, hunter);

		var snapshot = map.StateOf(hunter).Clone();
		new ClearEngagementIntentEffect(hunter).Apply(map, runtime, hunter);

		Assert.Null(EngagementAssertions.Hunting(map.StateOf(hunter)));
		Assert.Equal(target, EngagementAssertions.Hunting(snapshot));
	}

	private static string AddPirate(StarMap map, string id, GrimSpace.Math.Grid.Coord coord)
	{
		map.FleetRegistry.Add(StarSystemTestHarness.CreatePirateFleet(
			id,
			coord,
			GrimSpace.World.Factions.EFaction.Pirates,
			new GrimSpace.World.StarSystem.Encounter.CombatProfile(
				GrimSpace.World.StarSystem.Encounter.EDangerLevel.VeryLow,
				1)));
		return id;
	}
}
