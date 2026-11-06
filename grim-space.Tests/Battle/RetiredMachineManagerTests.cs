using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.PilotCapabilities;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.PilotCapabilities;

public sealed class RetiredMachineManagerTests
{
	[Fact]
	public void Parent_InstallsMarkerPassiveWithoutDamageRewrite()
	{
		var parent = BattleTestFixture.Player(Coord.Zero);
		var world = BattleWorldFor(parent);
		InstallManager(world, parent);

		Assert.True(BattleStatuses.HasSpawnedAllyDamageBonus(parent.State));
		IAction action = new ScrapDroneSwarmAction(parent.State.Id, ESpatialOrientation.Port);
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.ScrapDroneSwarmBurst,
			new HashSet<Coord> { new(1, 0, 0) },
			10 * 100,
			Coord.Zero);

		var modified = Assert.IsType<ApplyAreaDamageEffect>(
			Assert.Single(BattleStatuses.RewriteOutgoing(action, world, [effect])));

		Assert.Equal(0, modified.DamageBonusPercent);
	}

	[Fact]
	public void SpawnedChild_ReceivesDamageBuffStatus()
	{
		var parent = BattleTestFixture.Player(Coord.Zero);
		var child = BattleTestFixture.Enemy(new Coord(1, 0, 0));
		child.State.ParentId = parent.State.Id;
		var world = BattleWorldFor(parent, child);
		InstallManager(world, parent);
		BattleStatuses.GrantSpawnedAllyDamageBuff(parent.State, child.State);

		var buffId = BattleStatuses.BuffInstanceId(
			nameof(EPilotCapabilityId.SpawnedAllyDamageBonus),
			child.State.Id);
		Assert.Contains(child.State.Statuses, status => status.InstanceId == buffId);
		Assert.DoesNotContain(
			child.State.Statuses,
			status => status.InstanceId == BattleStatuses.PassiveInstanceId(
				nameof(EPilotCapabilityId.SpawnedAllyDamageBonus),
				child.State.Id));

		IAction action = new ScrapDroneSwarmAction(child.State.Id, ESpatialOrientation.Port);
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.ScrapDroneSwarmBurst,
			new HashSet<Coord> { new(2, 0, 0) },
			10 * 100,
			new Coord(1, 0, 0));

		var modified = Assert.IsType<ApplyAreaDamageEffect>(
			Assert.Single(BattleStatuses.RewriteOutgoing(action, world, [effect])));

		Assert.Equal(
			PilotSkillPercents.RetiredMachineManager.SpawnedAllyDamageBonusPercent,
			modified.DamageBonusPercent);
	}

	[Fact]
	public void ChildWithoutManagerParent_DoesNotReceiveBuff()
	{
		var parent = BattleTestFixture.Player(Coord.Zero);
		var child = BattleTestFixture.Enemy(new Coord(1, 0, 0));
		child.State.ParentId = parent.State.Id;
		var world = BattleWorldFor(parent, child);
		BattleStatuses.GrantSpawnedAllyDamageBuff(parent.State, child.State);

		Assert.Empty(child.State.Statuses);
		IAction action = new ScrapDroneSwarmAction(child.State.Id, ESpatialOrientation.Port);
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.ScrapDroneSwarmBurst,
			new HashSet<Coord> { new(2, 0, 0) },
			10 * 100,
			new Coord(1, 0, 0));

		var modified = Assert.IsType<ApplyAreaDamageEffect>(
			Assert.Single(BattleStatuses.RewriteOutgoing(action, world, [effect])));

		Assert.Equal(0, modified.DamageBonusPercent);
	}

	private static void InstallManager(BattleWorld world, Unit parent) =>
		BattlePassiveStatusInstaller.InstallFromCapabilities(
			world,
			parent.State.Id,
			ActorRuntime.From(new PilotBattleSnapshot(
				"pilot",
				PilotCapabilityGrant.FromBackground(
					"pilot",
					EPilotBackgroundId.RetiredMachineManager,
					BattlePilotCapabilityPipelineFactory.Supports))));

	private static BattleWorld BattleWorldFor(params Unit[] units) =>
		BattleWorld.FromLive(
			units,
			new Dictionary<string, NonUnit>(),
			BattleTestFixture.Grid(),
			new HashSet<Coord>(),
			"retired-machine-manager-test",
			EObjective.EliminateOpponents,
			units.Select(unit => unit.State.Id).ToHashSet(StringComparer.Ordinal),
			new Timeline());
}
