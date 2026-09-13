using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Player;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem.Traffic;
using RunState = GrimSpace.Run.State;
using BattleUnitType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.Tests.World.StarSystem.Engagement;

public sealed class BeginEngagementTests
{
	[Fact]
	public void BeginEngagement_SetsActiveBattleFromCommittedEngagement()
	{
		var run = RunState.CreateNewRun(42);
		var playerId = RunState.PlayerFleetUnitId;
		var pirateId = "pirate-a";
		run.StarSystem.Map.FleetRegistry.Add(StarSystemTestHarness.CreatePirateFleet(
			pirateId,
			new GrimSpace.Math.Grid.Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.Pirates,
			new GrimSpace.World.StarSystem.Encounter.CombatProfile(
				GrimSpace.World.StarSystem.Encounter.EDangerLevel.VeryLow,
				7)));
		new SetEngagementIntentEffect(playerId, pirateId)
			.Apply(run.StarSystem.Map, new ActorRuntime(), playerId);
		new ReachContactEffect(playerId, pirateId)
			.Apply(run.StarSystem.Map, new ActorRuntime(), playerId);
		new CommitEngagementEffect(playerId, pirateId)
			.Apply(run.StarSystem.Map, new ActorRuntime(), playerId);

		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(playerId);
		var pirateFleet = run.StarSystem.Map.FleetRegistry.FleetOf(pirateId);
		var encounter = EngagementBattleFactory.Create(playerFleet, pirateFleet, 7);

		Assert.True(EngagementQueries.TryGetCommittedPlayerEngagement(run.StarSystem.Map, playerId, out var committed));
		Assert.Equal(playerId, committed.InitiatorUnitId);
		Assert.Equal(4, encounter.Spawns.Count);
		Assert.Equal(
			playerFleet.Members.Concat(pirateFleet.Members).Select(member => member.Id).Order(),
			encounter.Spawns.Select(spawn => spawn.Unit.Id).Order());
		Assert.Equal(
			playerFleet.Members.Select(member => member.Id),
			encounter.Participants
				.Single(participant => participant.ParticipantId == playerId)
				.TacticalUnitIds);
		Assert.Equal(
			pirateFleet.Members.Select(member => member.Id),
			encounter.Participants
				.Single(participant => participant.ParticipantId == pirateId)
				.TacticalUnitIds);
		Assert.All(
			encounter.Spawns,
			spawn => Assert.Equal(
				playerFleet.Members.Any(member => member.Id == spawn.Unit.Id)
					? Alliance.Player
					: Alliance.Enemy,
				spawn.Unit.Alliance));
		Assert.All(
			encounter.Spawns,
			spawn => Assert.Equal(
				playerFleet.Members.Concat(pirateFleet.Members).Single(member => member.Id == spawn.Unit.Id).Type,
				spawn.Unit.Type));
		Assert.All(
			encounter.Spawns.Where(spawn => spawn.Unit.Alliance == Alliance.Player),
			spawn => Assert.IsType<UserExecutionAgent>(spawn.ExecutionAgent));
		Assert.All(
			encounter.Spawns.Where(spawn => spawn.Unit.Alliance == Alliance.Enemy),
			spawn => Assert.IsType<AiController>(spawn.ExecutionAgent));
	}

	[Fact]
	public void Create_UsesUniquePatrolPositions()
	{
		var run = RunState.CreateNewRun(42);
		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var pirateFleet = StarSystemTestHarness.CreatePirateFleet(
			"pirate-a",
			new GrimSpace.Math.Grid.Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.Pirates,
			new GrimSpace.World.StarSystem.Encounter.CombatProfile(
				GrimSpace.World.StarSystem.Encounter.EDangerLevel.VeryLow,
				231));
		pirateFleet = new Fleet(
			pirateFleet.State,
			Enumerable.Range(0, 5)
				.Select(index => new FleetMember($"patrol-{index}", BattleUnitType.Patrol)));

		var encounter = EngagementBattleFactory.Create(playerFleet, pirateFleet, 231);
		var positions = encounter.Spawns.Select(spawn => spawn.Position).ToArray();

		Assert.Equal(playerFleet.Members.Count + pirateFleet.Members.Count, positions.Length);
		Assert.Equal(positions.Length, positions.Distinct().Count());
	}
}
