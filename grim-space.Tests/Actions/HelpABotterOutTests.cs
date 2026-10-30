using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.PilotCapabilities;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Battle.Player;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class HelpABotterOutTests
{
	private const string PlayerId = "player";

	[Fact]
	public void ScrapDroneSwarm_IsTaggedHeal()
	{
		IAction action = new ScrapDroneSwarmAction(PlayerId, ESpatialOrientation.Port);
		Assert.Contains(BattleActionTags.Heal, action.Tags);
	}

	[Fact]
	public void AffectedCells_GainOneBurstRangeWhenHealingSkillBonusActive()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BeginBattleWithSkill(origin);
		var action = new ScrapDroneSwarmAction(PlayerId, ESpatialOrientation.Starboard);
		var world = battle.Engine.World;

		world.StateOf(PlayerId).HealingSkillRangeBonusActive = false;
		var baseline = ScrapDroneSwarmDef.Instance.AffectedCells(action, world).Count;

		world.StateOf(PlayerId).HealingSkillRangeBonusActive = true;
		var boosted = ScrapDroneSwarmDef.Instance.AffectedCells(action, world).Count;

		Assert.True(boosted > baseline);
	}

	[Fact]
	public void ApplyRangeBonus_ExtendsScrapDroneSwarmBurstByOne()
	{
		var player = BattleTestFixture.Player(Coord.Zero);
		player.State.HealingSkillRangeBonusActive = true;
		var boosted = (ScrapDroneSwarmSpec)HealingSkillRange.ApplyRangeBonus(
			ScrapDroneSwarmSpec.Baseline,
			player.State,
			[BattleActionTags.Heal]);

		Assert.Equal(
			ScrapDroneSwarmSpec.Baseline.BurstRange + PilotSkillPercents.HelpABotterOut.HealingRangeBonusCells,
			boosted.BurstRange);
	}

	[Fact]
	public void Resolve_HealsAllyInExtendedBurstRing()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var mountedOn = ESpatialOrientation.Starboard;
		var extraCell = ExtraBurstCell(player.State, mountedOn);
		var ally = Factory.Create(
			ShipInstance.FromCatalog("ally", EType.Fighter),
			ETeam.Player,
			extraCell,
			new UserExecutionAgent());
		ally.State.HullPoints = 50;
		var battle = BeginBattleWithSkill(
			player,
			[ally, BattleTestFixture.Enemy(origin + Coord.Forward * 8)]);
		var action = new ScrapDroneSwarmAction(PlayerId, mountedOn);

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(action));

		Assert.Equal(150, battle.PlayerAgent.Sim.StateOf<State>("ally").HullPoints);
	}

	private static Coord ExtraBurstCell(State firer, ESpatialOrientation mountedOn)
	{
		var frame = BodyFrame.From(firer);
		var burstDirection = frame.Step(mountedOn);
		var baseline = ScrapDroneSwarmSpec.Baseline.GetArea(
			frame.Origin,
			burstDirection,
			frame.Fore,
			frame.Dorsal);
		var extended = ScrapDroneSwarmSpec.Baseline with
		{
			BurstRange = ScrapDroneSwarmSpec.Baseline.BurstRange
				+ PilotSkillPercents.HelpABotterOut.HealingRangeBonusCells,
		};
		var boosted = extended.GetArea(frame.Origin, burstDirection, frame.Fore, frame.Dorsal);
		return boosted.First(cell => !baseline.Contains(cell));
	}

	private static BattleOrchestrator BeginBattleWithSkill(Coord origin) =>
		BeginBattleWithSkill(BattleTestFixture.Player(origin), []);

	private static BattleOrchestrator BeginBattleWithSkill(Unit player, IReadOnlyList<Unit> extras)
	{
		var pilots = new Dictionary<string, PilotBattleSnapshot?>(StringComparer.Ordinal)
		{
			[player.State.Id] = PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.HelpABotterOut]),
		};
		return BattleTestFixture.BeginSimulation(
			player,
			extras,
			pilotsByActorId: pilots);
	}
}
