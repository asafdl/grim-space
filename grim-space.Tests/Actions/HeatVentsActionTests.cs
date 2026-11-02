using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class HeatVentsActionTests
{
	private const string PlayerId = "player";

	[Fact]
	public void HeatVents_IsLegalWhenReady()
	{
		var (_, battle) = PlayerWithHeatVentsBattle();
		var simState = battle.PlayerAgent.Sim.World.StateOf(PlayerId);
		simState.Heat = 50;
		simState.ActionPoints = PilotSkillPercents.HeatVents.ApCost;

		var legal = Capabilities.LegalCapabilities(battle.PlayerAgent.Sim, PlayerId);

		Assert.Contains(legal, action => action is HeatVentsAction);
	}

	[Fact]
	public void HeatVents_ReleasesHeatSpendsApAndMarksUsed()
	{
		var (player, battle) = PlayerWithHeatVentsBattle();
		player.State.Heat = 70;
		player.State.ActionPoints = 4;

		BattleTestApply.TryApplyOne(
			HeatVentsDef.Instance.Bind(PlayerId),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId);

		Assert.Equal(70 - PilotSkillPercents.HeatVents.HeatReleased, player.State.Heat);
		Assert.Equal(4 - PilotSkillPercents.HeatVents.ApCost, player.State.ActionPoints);
		Assert.True(player.State.HeatVentsUsedThisTurn);
		Assert.True(battle.Engine.World.Timeline.ContainsPending(
			action => action is ResetHeatVentsUsedThisTurnAction reset
				&& reset.ActorId == PlayerId));
	}

	[Fact]
	public void HeatVents_DoesNotAddAbilityHeat()
	{
		var (player, battle) = PlayerWithHeatVentsBattle();
		player.State.Heat = 10;
		player.State.ActionPoints = PilotSkillPercents.HeatVents.ApCost;

		BattleTestApply.TryApplyOne(
			HeatVentsDef.Instance.Bind(PlayerId),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId);

		Assert.Equal(0, player.State.Heat);
	}

	[Fact]
	public void HeatVents_IllegalWithoutApOrHeatOrAfterUse()
	{
		var (player, battle) = PlayerWithHeatVentsBattle();
		var action = HeatVentsDef.Instance.Bind(PlayerId);
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);

		player.State.Heat = 40;
		player.State.ActionPoints = 0;
		Assert.False(HeatVentsDef.Instance.IsLegal(action, world, runtime));

		player.State.ActionPoints = PilotSkillPercents.HeatVents.ApCost;
		player.State.Heat = 0;
		Assert.False(HeatVentsDef.Instance.IsLegal(action, world, runtime));

		player.State.Heat = 40;
		Assert.True(HeatVentsDef.Instance.IsLegal(action, world, runtime));

		player.State.HeatVentsUsedThisTurn = true;
		Assert.False(HeatVentsDef.Instance.IsLegal(action, world, runtime));
	}

	[Fact]
	public void HeatVents_IllegalWhileOverheated()
	{
		var (player, battle) = PlayerWithHeatVentsBattle();
		player.State.Heat = 100;
		player.State.ActionPoints = PilotSkillPercents.HeatVents.ApCost;
		player.State.Overheat = true;
		BattleStatuses.SyncOverheat(player.State);

		Assert.False(HeatVentsDef.Instance.IsLegal(
			HeatVentsDef.Instance.Bind(PlayerId),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId)));
	}

	[Fact]
	public void AbilityDefsFor_IncludesHeatVentsWhenEquipped()
	{
		var (player, battle) = PlayerWithHeatVentsBattle();

		var defs = Capabilities.AbilityDefsFor(
			player.State,
			battle.Engine.ActorRuntimes.For(player.State.Id));

		Assert.Contains(HeatVentsDef.Instance, defs);
	}

	private static (Unit Player, BattleOrchestrator Battle) PlayerWithHeatVentsBattle()
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
				[EPilotSkillId.HeatVents]),
		};
		var battle = BattleTestFixture.BeginSimulation(player, enemy, pilotsByActorId: pilots);
		return (player, battle);
	}
}
