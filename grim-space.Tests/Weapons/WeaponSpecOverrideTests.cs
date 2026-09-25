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
	public void CustomFlakBurstRangeShrinksAffectedCells()
	{
		var installed = ReplaceFlakSpec(new FlakSpec(UsesPerTurn: 1, Damage: 1, BurstRange: 1));
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
		var cells = FlakDef.Instance.AffectedCells(
			new FlakAction(PlayerId, ESpatialOrientation.Port),
			world);

		Assert.Equal(6, cells.Count);
	}

	[Fact]
	public void CustomRailgunLineLengthChangesReachEnvelope()
	{
		var installed = ReplaceRailgunSpec(new RailgunSpec(UsesPerTurn: 1, Damage: 1, LineLength: 3, PyramidRange: 1));
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
		var spec = (RailgunSpec)world.StateOf(PlayerId).FindInstalled(EAbilityKind.Railgun)!.Spec;
		var cells = RailgunDef.Instance.AffectedCells(new RailgunAction(PlayerId), world);
		var frame = BodyFrame.From(world.StateOf(PlayerId));

		Assert.Equal(3, spec.LineLength);
		Assert.DoesNotContain(frame.ToWorld(5, 0, 0), cells);
		Assert.Contains(frame.ToWorld(3, 0, 0), cells);
	}

	private static IReadOnlyList<InstalledAbility> ReplaceFlakSpec(FlakSpec flak) =>
		ShipCatalog.FullFighterLoadout().InstalledAbilities
			.Select(ability => ability.Spec.Kind == EAbilityKind.Flak
				? ability with { Spec = flak }
				: ability)
			.ToArray();

	private static IReadOnlyList<InstalledAbility> ReplaceRailgunSpec(RailgunSpec railgun) =>
		ShipCatalog.NewRunLoadoutFor(EType.Fighter).InstalledAbilities
			.Select(ability => ability.Spec.Kind == EAbilityKind.Railgun
				? ability with { Spec = railgun }
				: ability)
			.ToArray();
}
