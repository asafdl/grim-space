using GrimSpace.Battle.Encounter;
using GrimSpace.Run;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using RunState = GrimSpace.Run.State;
using BattleUnitType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.Tests.World.StarSystem.Engagement;

public sealed class BeginEngagementTests
{
	[Fact]
	public void BeginEngagement_SetsActiveBattleFromCommittedEngagement()
	{
		var run = RunState.CreateDevDefault(42);
		var playerId = RunState.PlayerFleetUnitId;
		var pirateId = "pirate-a";
		run.StarSystem.Map.UnitRegistry.Add(Factory.CreatePirateFleet(
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

		var encounter = EngagementBattleFactory.Create(run.PlayerParty, 7);

		Assert.True(EngagementQueries.TryGetCommittedPlayerEngagement(run.StarSystem.Map, playerId, out var committed));
		Assert.Equal(playerId, committed.InitiatorUnitId);
		Assert.Equal(4, encounter.Spawns.Count);
		Assert.All(encounter.Spawns.Skip(1), spawn => Assert.Equal(BattleUnitType.Patrol, spawn.Unit.Type));
	}
}
