using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Tests.Movement;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class OverchargeEnginesActionTests
{
	private const string PlayerId = "player";

	[Fact]
	public void OverchargeEngines_IsLegalWhenReady()
	{
		var (_, battle) = PlayerWithOverchargeBattle();

		var legal = Capabilities.LegalCapabilities(battle.PlayerAgent.Sim, PlayerId);

		Assert.Contains(legal, action => action is OverchargeEnginesAction);
	}

	[Fact]
	public void OverchargeEngines_IsLegalAtZeroAp()
	{
		var (player, battle) = PlayerWithOverchargeBattle();
		player.State.ActionPoints = 0;
		var action = OverchargeEnginesDef.Instance.Bind(PlayerId);
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);

		Assert.True(OverchargeEnginesDef.Instance.IsLegal(action, world, runtime));
	}

	[Fact]
	public void OverchargeEngines_RefreshesApUpToMaxWithoutSpendingAp()
	{
		var (player, battle) = PlayerWithOverchargeBattle();
		player.State.ActionPoints = 2;

		BattleTestApply.TryApplyOne(
			OverchargeEnginesDef.Instance.Bind(PlayerId),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId);

		Assert.Equal(4, player.State.ActionPoints);
		Assert.Equal(PilotSkillPercents.OverchargeEngines.CooldownTurns, player.State.OverchargeEnginesCooldownRemaining);
	}

	[Fact]
	public void OverchargeEngines_DoesNotExceedMaxAp()
	{
		var (player, battle) = PlayerWithOverchargeBattle();
		player.State.ActionPoints = MovementExpectations.FighterApPerTurn;

		BattleTestApply.TryApplyOne(
			OverchargeEnginesDef.Instance.Bind(PlayerId),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId);

		Assert.Equal(MovementExpectations.FighterApPerTurn, player.State.ActionPoints);
	}

	[Fact]
	public void OverchargeEngines_SchedulesNextTurnPenaltyAndCooldownTicksDownOnUpkeep()
	{
		var (player, battle) = PlayerWithOverchargeBattle();
		player.State.ActionPoints = 2;

		BattleTestApply.TryApplyOne(
			OverchargeEnginesDef.Instance.Bind(PlayerId),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId);

		Assert.True(battle.Engine.World.Timeline.ContainsPending(
			action => action is ApDeltaAction ap
				&& ap.ActorId == PlayerId
				&& ap.Delta == -PilotSkillPercents.OverchargeEngines.NextTurnApPenalty));

		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		var world = battle.Engine.World;
		new TurnUpkeepEffect().Apply(world, runtime, PlayerId);
		Assert.Equal(PilotSkillPercents.OverchargeEngines.CooldownTurns - 1, player.State.OverchargeEnginesCooldownRemaining);

		new TurnUpkeepEffect().Apply(world, runtime, PlayerId);
		Assert.Equal(0, player.State.OverchargeEnginesCooldownRemaining);

		player.State.ActionPoints = MovementExpectations.FighterApPerTurn;
		battle.Engine.AdvanceTick();

		Assert.Equal(
			MovementExpectations.FighterApPerTurn - PilotSkillPercents.OverchargeEngines.NextTurnApPenalty,
			player.State.ActionPoints);
	}

	[Fact]
	public void AbilityDefsFor_IncludesOverchargeEnginesWhenEquipped()
	{
		var (player, battle) = PlayerWithOverchargeBattle();

		var defs = Capabilities.AbilityDefsFor(
			player.State,
			battle.Engine.ActorRuntimes.For(player.State.Id));

		Assert.Contains(OverchargeEnginesDef.Instance, defs);
	}

	private static (Unit Player, BattleOrchestrator Battle) PlayerWithOverchargeBattle()
	{
		var player = Factory.Create(
			ShipInstance.FromCatalog(PlayerId, EType.Fighter),
			ETeam.Player,
			new Coord(5, 5, 5),
			BattleAgentFactory.Create(EBattleAgentKind.Player));
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var pilots = new Dictionary<string, PilotBattleSnapshot?>(StringComparer.Ordinal)
		{
			[player.State.Id] = PilotBattleSnapshot.FromSkills(
				"pilot",
				[EPilotSkillId.OverchargeEngines]),
		};
		var battle = BattleTestFixture.BeginSimulation(player, enemy, pilotsByActorId: pilots);
		return (player, battle);
	}
}
