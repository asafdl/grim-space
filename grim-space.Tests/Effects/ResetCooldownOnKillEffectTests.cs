using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Effects;

[BattleTestSuite]
public sealed class ResetCooldownOnKillEffectTests
{
	[Theory]
	[InlineData(50, 1)]
	[InlineData(500, 0)]
	[InlineData(0, 0)]
	public void ThirstForScrap_NewEnemyKillGrantsStackAndUndoRestoresIt(int hull, int addedStacks)
	{
		var (battle, player, enemy) = CreateBattle();
		using var cleanup = battle;
		var world = battle.Engine.World;
		var runtime = ActorRuntime.From(PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.ThirstForScrap]));
		BattleStatusMaterializer.Materialize(world, player.State.Id, runtime);
		player.State.StatusOf<ThirstForScrap>()!.Stacks = 2;
		enemy.State.CurrentHullPoints = hull;
		enemy.State.CurrentShieldPoints.Fill(0);
		var action = new HighVelocityRodAction(player.State.Id, ESpatialOrientation.Forward);
		var records = new List<IRecord>();
		var applied = BattleEffectTestKit.ApplyEffects(
			action,
			world,
			runtime,
			[
				new DamageUnitEffect(
					EImpactCause.HighVelocityRod,
					enemy.State.Id,
					player.State.Position,
					100),
			],
			records);

		Assert.Equal(2 + addedStacks, player.State.StatusOf<ThirstForScrap>()!.Stacks);
		Assert.Equal(addedStacks, records.OfType<Record<ThirstForScrapChangedFacts>>().Count());
		var prepared = BattleEffectTestKit.PrepareDamage(
			action,
			world,
			runtime,
			new DamageUnitEffect(
				EImpactCause.HighVelocityRod,
				enemy.State.Id,
				player.State.Position,
				100));
		Assert.Equal((2 + addedStacks) * 10, prepared.BonusPercent);
		Assert.Empty(world.ActivationRoundLists.EntriesFor(player.State.Id));

		ExecutionHelper.UndoEffects(applied, action, world, runtime);

		Assert.Equal(2, player.State.StatusOf<ThirstForScrap>()!.Stacks);
		Assert.Equal(hull, enemy.State.CurrentHullPoints);
	}

	[Theory]
	[InlineData(50, true)]
	[InlineData(500, false)]
	[InlineData(0, false)]
	public void ResetsOnlyOnNewKillAndUndoRestoresCooldownAndDamage(int hull, bool resets)
	{
		var (battle, player, enemy) = CreateBattle();
		enemy.State.CurrentHullPoints = hull;
		enemy.State.CurrentShieldPoints.Fill(0);
		var mount = player.State.FindInstalled(EAbilityKind.RoadsWeDontNeedRoads)!.Mount;
		player.State.MountRuntimeFor(mount).CooldownRemaining = 3;
		var action = new HighVelocityRodAction(player.State.Id, ESpatialOrientation.Forward);
		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);
		var records = new List<IRecord>();
		var applied = BattleEffectTestKit.ApplyEffects(
			action,
			battle.Engine.World,
			runtime,
			[
				new DamageUnitEffect(
					EImpactCause.HighVelocityRod,
					enemy.State.Id,
					player.State.Position,
					100),
			],
			records);

		Assert.Equal(resets ? 0 : 3, player.State.CooldownRemaining(mount));
		Assert.Equal(resets ? 1 : 0, records.OfType<Record<CooldownResetOnKillFacts>>().Count());
		var lethalImpacts = records.OfType<Record<ImpactFacts>>()
			.Where(record => record.Value.IsLethal).ToArray();
		Assert.Equal(resets ? 1 : 0, lethalImpacts.Length);
		if (resets)
		{
			Assert.Equal(player.State.Id, lethalImpacts[0].Value.SourceId);
			Assert.Equal(enemy.State.Id, lethalImpacts[0].Value.TargetId);
		}
		ExecutionHelper.UndoEffects(applied, action, battle.Engine.World, runtime);
		Assert.Equal(3, player.State.CooldownRemaining(mount));
		Assert.Equal(hull, enemy.State.CurrentHullPoints);
	}

	[Fact]
	public void NonOptedInCooldownIsUnchanged()
	{
		var (battle, player, enemy) = CreateBattle(EType.Fighter);
		enemy.State.CurrentHullPoints = 50;
		enemy.State.CurrentShieldPoints.Fill(0);
		var mount = player.State.FindInstalled(EAbilityKind.VoidBombLauncher)!.Mount;
		player.State.MountRuntimeFor(mount).CooldownRemaining = 3;
		var records = new List<IRecord>();
		BattleEffectTestKit.ApplyEffects(
			new LightningCannonAction(player.State.Id),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(player.State.Id),
			[
				new DamageUnitEffect(
					EImpactCause.LightningCannonBurst,
					enemy.State.Id,
					player.State.Position,
					100),
			],
			records);

		Assert.False(enemy.State.IsAlive);
		Assert.Equal(3, player.State.CooldownRemaining(mount));
		Assert.Empty(records.OfType<Record<CooldownResetOnKillFacts>>());
		Assert.True(Assert.Single(records.OfType<Record<ImpactFacts>>()).Value.IsLethal);
	}

	[Fact]
	public void OwnedChildKillResetsOwnerCooldown()
	{
		var (battle, player, enemy) = CreateBattle();
		var mount = player.State.FindInstalled(EAbilityKind.RoadsWeDontNeedRoads)!.Mount;
		player.State.MountRuntimeFor(mount).CooldownRemaining = 2;
		enemy.State.ParentId = player.State.Id;
		enemy.State.CurrentShieldPoints.Fill(0);
		var records = new List<IRecord>();
		BattleEffectTestKit.ApplyEffects(
			new DetonateAction(enemy.State.Id),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(enemy.State.Id),
			[
				new DamageUnitEffect(
					EImpactCause.VoidBombBlast,
					enemy.State.Id,
					enemy.State.Position,
					1000),
			],
			records);

		Assert.Equal(0, player.State.CooldownRemaining(mount));
		Assert.Equal(player.State.Id, Assert.Single(records.OfType<Record<CooldownResetOnKillFacts>>()).Value.ActorId);
	}

	[Fact]
	public void ResetFactUpdatesReplayWithoutChangingLiveState()
	{
		var (_, player, _) = CreateBattle();
		var mount = player.State.FindInstalled(EAbilityKind.RoadsWeDontNeedRoads)!.Mount;
		player.State.MountRuntimeFor(mount).CooldownRemaining = 3;
		var replay = new ReplayState(new Dictionary<string, State> { [player.State.Id] = player.State });

		replay.ApplyCooldownResetOnKill(new CooldownResetOnKillFacts(player.State.Id, mount));

		Assert.Equal(0, replay.StateOf(player.State.Id).CooldownRemaining(mount));
		Assert.Equal(3, player.State.CooldownRemaining(mount));
	}

	[Fact]
	public void OnlyJumpSpecOptsIntoKillReset()
	{
		var cooldownAbilities = Enum.GetValues<EAbilityKind>()
			.Select(AbilitySpec.BaselineFor)
			.OfType<ICooldownAbility>();

		Assert.IsType<RoadsWeDontNeedRoadsSpec>(
			Assert.Single(cooldownAbilities, ability => ability.ResetsCooldownOnKill));
	}

	private static (BattleOrchestrator Battle, Unit Player, Unit Enemy) CreateBattle(EType chassis = EType.Rogue)
	{
		var player = Factory.Create(
			ShipInstance.FromCatalog("player", chassis), ETeam.Player,
			new Coord(5, 5, 5), new UserExecutionAgent());
		var enemy = BattleTestFixture.Enemy(new Coord(5, 7, 5));
		return (BattleTestFixture.BeginSimulation(player, enemy), player, enemy);
	}
}
