using GrimSpace.Battle;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Effects;

[BattleTestSuite]
public sealed class ResetCooldownOnKillEffectTests
{
	[Theory]
	[InlineData(50, true)]
	[InlineData(500, false)]
	[InlineData(0, false)]
	public void ResetsOnlyOnNewKillAndUndoRestoresCooldownAndDamage(int hull, bool resets)
	{
		var (battle, player, enemy) = CreateBattle();
		enemy.State.HullPoints = hull;
		enemy.State.ShieldPoints.Fill(0);
		var mount = player.State.FindInstalled(EAbilityKind.RoadsWeDontNeedRoads)!.Mount;
		player.State.MountRuntimeFor(mount).CooldownRemaining = 3;
		var effect = new ResetCooldownOnKillEffect(new ApplyAreaDamageEffect(
			EImpactCause.HighSpeedRod,
			new HashSet<Coord> { enemy.State.Position },
			100,
			player.State.Position));
		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);

		var records = effect.Apply(battle.Engine.World, runtime, player.State.Id);

		Assert.Equal(resets ? 0 : 3, player.State.CooldownRemaining(mount));
		Assert.Equal(resets ? 1 : 0, records.OfType<Record<CooldownResetOnKillFacts>>().Count());
		effect.Undo(battle.Engine.World, runtime, player.State.Id);
		Assert.Equal(3, player.State.CooldownRemaining(mount));
		Assert.Equal(hull, enemy.State.HullPoints);
	}

	[Fact]
	public void NonOptedInCooldownIsUnchanged()
	{
		var (battle, player, enemy) = CreateBattle(EType.Fighter);
		enemy.State.HullPoints = 50;
		enemy.State.ShieldPoints.Fill(0);
		var mount = player.State.FindInstalled(EAbilityKind.VoidBombLauncher)!.Mount;
		player.State.MountRuntimeFor(mount).CooldownRemaining = 3;
		var effect = new ResetCooldownOnKillEffect(new ApplyAreaDamageEffect(
			EImpactCause.LightningCannonBurst,
			new HashSet<Coord> { enemy.State.Position },
			100,
			player.State.Position));

		var records = effect.Apply(
			battle.Engine.World, battle.Engine.ActorRuntimes.For(player.State.Id), player.State.Id);

		Assert.False(enemy.State.IsAlive);
		Assert.Equal(3, player.State.CooldownRemaining(mount));
		Assert.Empty(records.OfType<Record<CooldownResetOnKillFacts>>());
	}

	[Fact]
	public void OwnedChildKillResetsOwnerCooldown()
	{
		var (battle, player, enemy) = CreateBattle();
		var mount = player.State.FindInstalled(EAbilityKind.RoadsWeDontNeedRoads)!.Mount;
		player.State.MountRuntimeFor(mount).CooldownRemaining = 2;
		enemy.State.ParentId = player.State.Id;
		enemy.State.ShieldPoints.Fill(0);
		var effect = new ResetCooldownOnKillEffect(new ApplyAreaDamageEffect(
			EImpactCause.VoidBombBlast,
			new HashSet<Coord> { enemy.State.Position },
			1000,
			enemy.State.Position));

		var records = effect.Apply(
			battle.Engine.World, battle.Engine.ActorRuntimes.For(enemy.State.Id), enemy.State.Id);

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
