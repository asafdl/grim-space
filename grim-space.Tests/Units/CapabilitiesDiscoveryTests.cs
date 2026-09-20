using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Units;

public sealed class CapabilitiesDiscoveryTests
{
	[Fact]
	public void FighterWithoutRailgun_DoesNotDiscoverRailgun()
	{
		var installed = ShipCatalog.DefaultInstalledAbilitiesFor(EType.Fighter)
			.Where(ability => ability.Spec.Kind != EAbilityKind.Railgun)
			.ToArray();
		var spec = ShipSpec.Create(EType.Fighter, 2, installed);
		var ship = ShipInstance.FromSpec("fighter-a", spec);
		var player = Factory.Create(ship, ETeam.Player, Coord.Zero, new UserExecutionAgent());
		var enemy = BattleTestFixture.Enemy(Coord.Forward * 6);
		var battle = BattleTestFixture.BeginSimulation(player, enemy);
		BattleTestFixture.GrantPlayerPlanning(battle);

		var legal = Capabilities.LegalCapabilities(battle.PlayerAgent.Sim, player.State.Id);

		Assert.DoesNotContain(legal, action => action is RailgunAction);
		Assert.Contains(legal, action => action is FlakAction);
	}

	[Fact]
	public void DefForKind_ThrowsForUnmappedKind()
	{
		var kind = unchecked((EAbilityKind)(-1));

		Assert.Throws<InvalidOperationException>(() => Capabilities.DefForKind(kind));
	}
}
