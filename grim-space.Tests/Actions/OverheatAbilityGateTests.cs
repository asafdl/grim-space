using GrimSpace.Battle.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class OverheatAbilityGateTests
{
	private const string PlayerId = "player";

	[Fact]
	public void Overheat_BlocksScrapDroneAbility()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Overheat = true;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		var mountedOn = state.FindInstalled(EAbilityKind.ScrapDroneSwarm)!.MountedOn;
		var action = new ScrapDroneSwarmAction(PlayerId, mountedOn);

		Assert.False(ScrapDroneSwarmDef.Instance.IsLegal(action, battle.Engine.World, runtime));
	}

	[Fact]
	public void Overheat_AllowsMovement()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Overheat = true;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		var action = MoveDef.Instance.Bind(PlayerId, ESpatialOrientation.Forward);

		Assert.True(MoveDef.Instance.IsLegal(action, battle.Engine.World, runtime));
	}

	[Fact]
	public void Overheat_BlocksLightningAbility()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Overheat = true;
		var runtime = battle.Engine.ActorRuntimes.For(PlayerId);
		var action = new LightningCannonAction(PlayerId, ESpatialOrientation.Forward);

		Assert.False(LightningCannonDef.Instance.IsLegal(action, battle.Engine.World, runtime));
	}
}
