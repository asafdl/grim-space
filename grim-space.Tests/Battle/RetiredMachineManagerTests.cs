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
		var enemy = BattleTestFixture.Enemy(new Coord(1, 0, 0));
		var world = BattleWorldFor(parent, enemy);
		InstallManager(world, parent);

		Assert.True(parent.State.Has<SpawnedAllyDamageBonus>());
		IAction action = new HighVelocityRodAction(parent.State.Id, ESpatialOrientation.Forward);
		var modified = BattleEffectTestKit.PrepareDamage(
			action,
			world,
			new ActorRuntime(),
			new DamageUnitEffect(
				EImpactCause.HighVelocityRod,
				enemy.State.Id,
				Coord.Zero,
				100));

		Assert.Equal(0, modified.BonusPercent);
	}

	[Fact]
	public void SpawnedChild_ReceivesDamageBuffStatus()
	{
		var parent = BattleTestFixture.Player(Coord.Zero);
		var child = BattleTestFixture.Enemy(new Coord(1, 0, 0));
		var enemy = BattleTestFixture.RepurposedMiner(new Coord(2, 0, 0), "target");
		child.State.ParentId = parent.State.Id;
		var world = BattleWorldFor(parent, child, enemy);
		InstallManager(world, parent);
		SpawnedAllyDamageBuff.GrantFrom(parent.State, child.State);

		Assert.True(child.State.Has<SpawnedAllyDamageBuff>());
		Assert.False(child.State.Has<SpawnedAllyDamageBonus>());

		IAction action = new HighVelocityRodAction(child.State.Id, ESpatialOrientation.Forward);
		var modified = BattleEffectTestKit.PrepareDamage(
			action,
			world,
			new ActorRuntime(),
			new DamageUnitEffect(
				EImpactCause.HighVelocityRod,
				enemy.State.Id,
				child.State.Position,
				100));

		Assert.Equal(
			PilotSkillPercents.RetiredMachineManager.SpawnedAllyDamageBonusPercent,
			modified.BonusPercent);
	}

	[Fact]
	public void ChildWithoutManagerParent_DoesNotReceiveBuff()
	{
		var parent = BattleTestFixture.Player(Coord.Zero);
		var child = BattleTestFixture.Enemy(new Coord(1, 0, 0));
		var enemy = BattleTestFixture.RepurposedMiner(new Coord(2, 0, 0), "target");
		child.State.ParentId = parent.State.Id;
		var world = BattleWorldFor(parent, child, enemy);
		SpawnedAllyDamageBuff.GrantFrom(parent.State, child.State);

		Assert.Empty(child.State.Statuses);
		IAction action = new HighVelocityRodAction(child.State.Id, ESpatialOrientation.Forward);
		var modified = BattleEffectTestKit.PrepareDamage(
			action,
			world,
			new ActorRuntime(),
			new DamageUnitEffect(
				EImpactCause.HighVelocityRod,
				enemy.State.Id,
				child.State.Position,
				100));

		Assert.Equal(0, modified.BonusPercent);
	}

	private static void InstallManager(BattleWorld world, Unit parent) =>
		BattleStatusMaterializer.Materialize(
			world,
			parent.State.Id,
			ActorRuntime.From(new PilotBattleSnapshot(
				"pilot",
				PilotCapabilityGrant.FromBackground(
					"pilot",
					EPilotBackgroundId.RetiredMachineManager,
					BattlePilotCapabilities.Supports))));

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
