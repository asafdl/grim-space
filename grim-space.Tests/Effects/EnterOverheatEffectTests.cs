using GrimSpace.Battle.Effects;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Heat;

namespace GrimSpace.Tests.Effects;

[BattleTestSuite]
public sealed class EnterOverheatEffectTests
{
	private const string PlayerId = "player";

	[Fact]
	public void ApplyClearsOverdriveAndSetsOverheat()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Heat = 100;
		state.Overdrive = true;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);

		new EnterOverheatEffect().Apply(battle.Engine.World, runtime, PlayerId);

		Assert.Equal(100, state.Heat);
		Assert.False(state.Overdrive);
		Assert.True(state.Overheat);
	}

	[Fact]
	public void ApplyIsNoOpWhenProfileDoesNotParticipate()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.HeatProfile = HeatSpec.None;
		state.Overdrive = true;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);

		new EnterOverheatEffect().Apply(battle.Engine.World, runtime, PlayerId);

		Assert.True(state.Overdrive);
		Assert.False(state.Overheat);
	}

	[Fact]
	public void UndoRestoresFlags()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Overdrive = true;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		var effect = new EnterOverheatEffect();

		effect.Apply(battle.Engine.World, runtime, PlayerId);
		effect.Undo(battle.Engine.World, runtime, PlayerId);

		Assert.True(state.Overdrive);
		Assert.False(state.Overheat);
	}
}
