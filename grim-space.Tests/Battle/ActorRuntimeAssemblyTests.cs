using GrimSpace.Battle;
using GrimSpace.Pilots;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.BattleRuntime;

[BattleTestSuite]
public sealed class ActorRuntimeAssemblyTests
{
	[Fact]
	public void From_EmptyPilot_HasNoModifiers()
	{
		var runtime = ActorRuntime.From(null);

		Assert.Null(runtime.PilotId);
		Assert.Empty(runtime.PilotCapabilities);
	}

	[Fact]
	public void From_IonizedSnapshot_RegistersModifierThroughResolve()
	{
		var player = Factory.Create(
			ShipInstance.FromCatalog("player", EType.Fighter),
			ETeam.Player,
			new Coord(5, 5, 5),
			new GrimSpace.Battle.Player.UserExecutionAgent());
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var pilots = new Dictionary<string, PilotBattleSnapshot?>(StringComparer.Ordinal)
		{
			[player.State.Id] = PilotBattleSnapshot.FromSkills(
				"pilot-1",
				[EPilotSkillId.IonizedPayload]),
		};
		var battle = BattleTestFixture.BeginSimulation(player, enemy, pilotsByActorId: pilots);
		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);
		var effects = LightningCannonDef.Instance.Resolve(
			new LightningCannonAction(player.State.Id),
			battle.Engine.World,
			runtime);

		Assert.Equal(
			PilotSkillPercents.IonizedPayload.ShieldDamageBonusPercent,
			((GrimSpace.Battle.Effects.ApplyAreaDamageEffect)effects[0]).ShieldDamageBonusPercent);
	}

	[Fact]
	public void FromEncounterAndFromSavedWorld_MatchPilotRuntime()
	{
		var snapshot = PilotBattleSnapshot.FromSkills(
			"pilot-1",
			[EPilotSkillId.IonizedPayload]);
		var encounter = new BattleEncounter
		{
			Seed = 1,
			Id = "runtime-parity",
			Objective = GrimSpace.Battle.Objectives.EObjective.EliminateOpponents,
			Spawns =
			[
				new BattleSpawn
				{
					Ship = ShipInstance.FromCatalog("player", EType.Fighter),
					Pilot = snapshot,
					Team = ETeam.Player,
					Position = new Coord(5, 5, 5),
					ExecutionAgent = new GrimSpace.Battle.Player.UserExecutionAgent(),
				},
				new BattleSpawn
				{
					Ship = ShipInstance.FromCatalog("enemy", EType.Fighter),
					Team = ETeam.Enemy,
					Position = new Coord(0, 0, 0),
					ExecutionAgent = BattleAgentFactory.Create(EBattleAgentKind.Ai),
				},
			],
		};

		using var fromEncounter = BattleOrchestrator.FromEncounter(encounter, gridSize: 12);
		var saved = fromEncounter.Engine.World.Fork();
		using var fromSaved = BattleOrchestrator.FromSavedWorld(saved, "player", encounter);

		var encounterRuntime = fromEncounter.Engine.ActorRuntimes.For("player");
		var savedRuntime = fromSaved.Engine.ActorRuntimes.For("player");

		Assert.Equal(encounterRuntime.PilotId, savedRuntime.PilotId);
		Assert.Equal(encounterRuntime.PilotCapabilities, savedRuntime.PilotCapabilities);
	}

	[Fact]
	public void LazyRuntime_ForUnknownActor_IsEmptyPilot()
	{
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(new Coord(5, 5, 5)),
			BattleTestFixture.Enemy(new Coord(0, 0, 0)));

		var childRuntime = battle.Engine.ActorRuntimes.For("spawn-child");
		Assert.Empty(childRuntime.PilotCapabilities);
	}
}
