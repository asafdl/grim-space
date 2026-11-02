using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Run.Persistence;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Effects;

[BattleTestSuite]
public sealed class OneWithTheVoidTests
{
	private const string PlayerId = "player";

	[Fact]
	public void Spawn_CopiesImmunityFlagOntoTorpedo()
	{
		var battle = LaunchTorpedo(withSkill: true, out _);

		var player = battle.Engine.World.StateOf(PlayerId);
		Assert.True(BattleStatuses.HasOneWithTheVoid(player));
		var torpedo = Assert.Single(
			UnitRegistry.For(battle.Engine.World).All,
			unit => unit.State.Type == EType.VoidBomb);
		Assert.True(BattleStatuses.HasOneWithTheVoid(torpedo.State));
		Assert.DoesNotContain(
			torpedo.State.Statuses,
			status => status.InstanceId
				== BattleStatuses.PassiveInstanceId(nameof(EPilotCapabilityId.OneWithTheVoid), PlayerId));
	}

	[Fact]
	public void Detonate_SkipsAllyAndLauncherDamageButStillDamagesOpponents()
	{
		var battle = LaunchTorpedo(withSkill: true, out var torpedoId);
		var torpedoPos = new Coord(5, 5, 5);
		battle.Engine.World.StateOf(torpedoId).Position = torpedoPos;
		battle.Engine.World.StateOf(PlayerId).Position = torpedoPos + new Coord(1, 0, 0);
		var enemy = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.Team == ETeam.Enemy);
		enemy.State.Position = torpedoPos + new Coord(0, 1, 0);

		var playerBefore = TotalDurability(battle.Engine.World.StateOf(PlayerId));
		var enemyBefore = TotalDurability(enemy.State);

		var sim = battle.Engine.CreateSimulation();
		Assert.True(sim.TryEnqueue(new DetonateAction(torpedoId)));

		Assert.Equal(playerBefore, TotalDurability(sim.StateOf<ActorState>(PlayerId)));
		Assert.True(TotalDurability(sim.StateOf<ActorState>(enemy.State.Id)) < enemyBefore);
		Assert.False(sim.StateOf<ActorState>(torpedoId).IsAlive);
	}

	[Fact]
	public void Detonate_WithoutSkillStillDamagesAllies()
	{
		var battle = LaunchTorpedo(withSkill: false, out var torpedoId);
		var torpedoPos = new Coord(5, 5, 5);
		battle.Engine.World.StateOf(torpedoId).Position = torpedoPos;
		battle.Engine.World.StateOf(PlayerId).Position = torpedoPos + new Coord(1, 0, 0);
		var enemy = UnitRegistry.For(battle.Engine.World).All.First(unit => unit.Team == ETeam.Enemy);
		enemy.State.Position = torpedoPos + new Coord(0, 1, 0);

		var playerBefore = TotalDurability(battle.Engine.World.StateOf(PlayerId));

		var sim = battle.Engine.CreateSimulation();
		Assert.True(sim.TryEnqueue(new DetonateAction(torpedoId)));

		Assert.True(TotalDurability(sim.StateOf<ActorState>(PlayerId)) < playerBefore);
	}

	[Fact]
	public void BattleUnitStateDto_PreservesOneWithTheVoidStatus()
	{
		var state = BattleTestFixture.Player(new Coord(5, 5, 5)).State;
		state.Statuses.Add(BattlePassiveStatusInstaller.Create(EPilotCapabilityId.OneWithTheVoid, state.Id));

		var dto = SaveDtoMapper.CaptureBattleUnitState(state);
		var restored = SaveDtoMapper.RestoreBattleUnitState(dto);

		Assert.True(BattleStatuses.HasOneWithTheVoid(restored));
		Assert.True(BattleStatuses.HasOneWithTheVoid(state.Clone()));
	}

	[Fact]
	public void Detonate_StaysUntagged_SoAttackRewriteOutgoingDoesNotApply()
	{
		var battle = LaunchTorpedo(withSkill: false, out var torpedoId);
		var torpedo = battle.Engine.World.StateOf(torpedoId);
		torpedo.Statuses.Add(
			new BattleStatus
			{
				InstanceId = "test:attack-bonus",
				Priority = 1,
				ActionTags = [BattleActionTags.Attack],
				Polarity = EStatusPolarity.Buff,
				RewriteOutgoing = (_, _, effect) =>
					effect is ApplyAreaDamageEffect area
						? area.WithDamageBonusPercent(50)
						: effect,
			});

		IAction detonate = new DetonateAction(torpedoId);
		Assert.Empty(detonate.Tags);

		var effects = DetonateDef.Instance.Resolve(
			detonate,
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(torpedoId));
		var rewritten = BattleStatuses.RewriteOutgoing(
			detonate,
			battle.Engine.World,
			effects);
		var area = Assert.IsType<ApplyAreaDamageEffect>(Assert.Single(rewritten));
		Assert.Equal(0, area.DamageBonusPercent);
	}

	private static BattleOrchestrator LaunchTorpedo(bool withSkill, out string torpedoId)
	{
		var origin = new Coord(5, 5, 5);
		var playerSpawn = BattleSpawnTestKit.Create(
			PlayerId,
			EType.Fighter,
			ETeam.Player,
			origin,
			new UserExecutionAgent());
		if (withSkill)
		{
			playerSpawn = new BattleSpawn
			{
				Ship = playerSpawn.Ship,
				Pilot = PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.OneWithTheVoid]),
				Team = playerSpawn.Team,
				Position = playerSpawn.Position,
				ExecutionAgent = playerSpawn.ExecutionAgent,
			};
		}

		var encounter = new BattleEncounter
		{
			Id = "test-encounter",
			Seed = 1,
			Objective = EObjective.EliminateOpponents,
			Spawns =
			[
				playerSpawn,
				BattleSpawnTestKit.Create(
					"enemy",
					EType.Fighter,
					ETeam.Enemy,
					new Coord(0, 0, 0),
					new AiController()),
			],
		};

		var battle = BattleOrchestrator.FromEncounter(encounter, gridSize: 12);
		battle.Engine.Commit(VoidBombDef.Instance.Bind(PlayerId, ESpatialOrientation.Ventral));
		var torpedo = Assert.Single(
			UnitRegistry.For(battle.Engine.World).All,
			unit => unit.State.Type == EType.VoidBomb);
		torpedoId = torpedo.State.Id;
		ExecutionAgent<BattleWorld, ActorRuntime>.Initialize(
			torpedo.ExecutionAgent,
			torpedoId,
			battle.Engine.CreateSimulation,
			battle.WriterFor(torpedoId));
		BattleTestFixture.ResetPlayerPlanning(battle);
		return battle;
	}

	private static int TotalDurability(GrimSpace.Battle.Units.State state)
	{
		var shields = 0;
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			shields += state.ShieldPoints[face];
		return state.HullPoints + shields;
	}
}
