using GrimSpace.Battle.Effects;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Heat;

namespace GrimSpace.Tests.Effects;

[BattleTestSuite]
public sealed class ClearOverheatEffectTests
{
	private const string PlayerId = "player";

	[Fact]
	public void ApplyClearsOverheatAndResetsHeat()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Heat = 100;
		state.Overheat = true;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);

		new ClearOverheatEffect().Apply(battle.Engine.World, runtime, PlayerId);

		Assert.Equal(0, state.Heat);
		Assert.False(state.Overheat);
	}

	[Fact]
	public void ApplyIsNoOpWhenProfileDoesNotParticipate()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.HeatProfile = HeatSpec.None;
		state.Heat = 40;
		state.Overheat = true;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);

		new ClearOverheatEffect().Apply(battle.Engine.World, runtime, PlayerId);

		Assert.Equal(40, state.Heat);
		Assert.True(state.Overheat);
	}

	[Fact]
	public void UndoRestoresHeatAndFlag()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Heat = 100;
		state.Overheat = true;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		var effect = new ClearOverheatEffect();

		effect.Apply(battle.Engine.World, runtime, PlayerId);
		effect.Undo(battle.Engine.World, runtime, PlayerId);

		Assert.Equal(100, state.Heat);
		Assert.True(state.Overheat);
	}
}
