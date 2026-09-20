using GrimSpace.Battle;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Actions;

public sealed class FlakActionTests
{
	private const string PlayerId = "player";

	private static int TotalShieldPoints(GrimSpace.Battle.Units.State state)
	{
		var total = 0;
		foreach (var face in Enum.GetValues<ESpatialOrientation>())
			total += state.ShieldPoints[face];
		return total;
	}

	[Fact]
	public void FlakAppliesResolveImmediately()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var flak = new FlakAction(PlayerId, ESpatialOrientation.Port);

		Assert.Equal(CatalogExpectations.UsesPerTurn(EType.Fighter, EAbilityKind.Flak), StateMountTestKit.UsesRemaining(battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId), EAbilityKind.Flak));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(flak));
		Assert.Equal(CatalogExpectations.UsesPerTurn(EType.Fighter, EAbilityKind.Flak) - 1, StateMountTestKit.UsesRemaining(battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId), EAbilityKind.Flak));
		Assert.False(battle.PlayerAgent.Sim.TryEnqueue(flak));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new FlakAction(PlayerId, ESpatialOrientation.Starboard)));
		Assert.Equal(0, StateMountTestKit.UsesRemaining(
			battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId),
			EAbilityKind.Flak));
	}

	[Fact]
	public void FlakAppliesDamageWithoutApPenalty()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var action = new FlakAction(PlayerId, ESpatialOrientation.Starboard);
		var cells = FlakDef.Instance.AffectedCells(action, battle.PlayerAgent.Sim.World);
		var enemy = UnitRegistry.For(battle.PlayerAgent.Sim.World).All.First(unit => unit.State.Id != PlayerId);
		enemy.State.Position = cells.First();
		var shieldsBefore = TotalShieldPoints(enemy.State);

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(action));

		Assert.Equal(shieldsBefore - CatalogExpectations.FlakDamage(), TotalShieldPoints(enemy.State));
		Assert.False(enemy.State.ApPenaltyNextTurn);
	}
}
