using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Weapons;

[BattleTestSuite]
public sealed class WeaponSpecOverrideTests
{
	private const string PlayerId = "custom-fighter";

	[Fact]
	public void ScrapDroneSwarmRangeUpgradeExpandsAffectedCells()
	{
		var installed = WithScrapDroneSwarmRangeUpgrade(rangeUpgradeTier: 1);
		var player = Factory.Create(
			BattleSpawnTestKit.FighterWithInstalledAbilities(PlayerId, installed),
			ETeam.Player,
			new Coord(10, 10, 10),
			new UserExecutionAgent());
		var battle = BattleTestFixture.BeginSimulation(
			player,
			BattleTestFixture.Enemy(Coord.Zero),
			BattleTestFixture.Grid(size: 30));
		var world = battle.PlayerAgent.Sim.World;
		var cells = ScrapDroneSwarmDef.Instance.AffectedCells(
			new ScrapDroneSwarmAction(PlayerId, ESpatialOrientation.Port),
			world);

		var spec = (ScrapDroneSwarmSpec)world.StateOf(PlayerId)
			.FindInstalled(EAbilityKind.ScrapDroneSwarm, ESpatialOrientation.Port)!.Spec;
		Assert.Equal(ScrapDroneSwarmSpec.Baseline.BurstRange + 1, spec.BurstRange);
		Assert.Equal(44, cells.Count);
	}

	[Fact]
	public void LightningCannonRangeUpgradeChangesReachEnvelope()
	{
		var installed = WithLightningCannonRangeUpgrade(rangeUpgradeTier: 1);
		var player = Factory.Create(
			BattleSpawnTestKit.FighterWithInstalledAbilities(PlayerId, installed),
			ETeam.Player,
			new Coord(10, 10, 10),
			new UserExecutionAgent());
		var battle = BattleTestFixture.BeginSimulation(
			player,
			BattleTestFixture.Enemy(Coord.Zero),
			BattleTestFixture.Grid(size: 30));
		var world = battle.PlayerAgent.Sim.World;
		var spec = (LightningCannonSpec)world.StateOf(PlayerId).FindInstalled(EAbilityKind.LightningCannon)!.Spec;
		var cells = LightningCannonDef.Instance.AffectedCells(new LightningCannonAction(PlayerId), world);
		var frame = BodyFrame.From(world.StateOf(PlayerId));

		Assert.Equal(LightningCannonSpec.Baseline.LineLength + 1, spec.LineLength);
		Assert.Contains(frame.ToWorld(6, 0, 0), cells);
		Assert.DoesNotContain(frame.ToWorld(9, 0, 0), cells);
	}

	private static IReadOnlyList<InstalledAbility> WithScrapDroneSwarmRangeUpgrade(int rangeUpgradeTier) =>
		ShipCatalog.FullFighterLoadout().InstalledAbilities
			.Select(ability => ability.Kind == EAbilityKind.ScrapDroneSwarm
				? new InstalledAbility(
					ability.Kind,
					ability.MountedOn,
					rangeUpgradeTier: rangeUpgradeTier)
				: ability)
			.ToArray();

	private static IReadOnlyList<InstalledAbility> WithLightningCannonRangeUpgrade(int rangeUpgradeTier) =>
		ShipCatalog.NewRunLoadoutFor(EType.Fighter).InstalledAbilities
			.Select(ability => ability.Kind == EAbilityKind.LightningCannon
				? new InstalledAbility(
					ability.Kind,
					ability.MountedOn,
					rangeUpgradeTier: rangeUpgradeTier)
				: ability)
			.ToArray();
}
