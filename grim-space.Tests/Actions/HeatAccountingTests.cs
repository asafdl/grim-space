using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Heat;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Units.Maneuvering;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class HeatAccountingTests
{
	private const string PlayerId = "player";

	[Fact]
	public void MoveSpendAddsTenHeatPerAp()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);

		Assert.True(BattleTestApply.TryApplyOne(
			MoveDef.Instance.Bind(PlayerId, ESpatialOrientation.Forward),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId));

		Assert.Equal(HeatChangeEffect.PerApSpent, state.Heat);
		Assert.False(state.Overdrive);
	}

	[Fact]
	public void HeadingSpendAddsFiveHeatPerMp()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);

		Assert.True(BattleTestApply.TryApplyOne(
			HeadingDef.Instance.Bind(PlayerId, EHeadingTurn.YawRight),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId));

		Assert.Equal(HeatChangeEffect.PerMpSpent, state.Heat);
	}

	[Fact]
	public void RollSpendAddsFiveHeatPerMp()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);

		Assert.True(BattleTestApply.TryApplyOne(
			RollDef.Instance.Bind(PlayerId, ERollDirection.Clockwise),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId));

		Assert.Equal(HeatChangeEffect.PerMpSpent, state.Heat);
	}

	[Fact]
	public void AbilityUseAddsTwentyHeat()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		var mountedOn = state.FindInstalled(EAbilityKind.ScrapDroneSwarm)!.MountedOn;

		Assert.True(BattleTestApply.TryApplyOne(
			ScrapDroneSwarmDef.Instance.Bind(PlayerId, mountedOn),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId));

		Assert.Equal(HeatChangeEffect.PerAbilityUse, state.Heat);
	}

	[Fact]
	public void HeatGainMultiplierDoublesSpendHeat()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.HeatProfile = new HeatSpec(100, 20, 2f);

		Assert.True(BattleTestApply.TryApplyOne(
			MoveDef.Instance.Bind(PlayerId, ESpatialOrientation.Forward),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId));

		Assert.Equal(HeatChangeEffect.PerApSpent * 2, state.Heat);
	}

	[Fact]
	public void CrossingCapacitySetsOverdriveAndCapsHeat()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Heat = 95;

		Assert.True(BattleTestApply.TryApplyOne(
			MoveDef.Instance.Bind(PlayerId, ESpatialOrientation.Forward),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId));

		Assert.Equal(100, state.Heat);
		Assert.True(state.Overdrive);
	}

	[Fact]
	public void OverheatBlocksHeatGainFromMovement()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.Heat = 100;
		state.Overheat = true;

		Assert.True(BattleTestApply.TryApplyOne(
			MoveDef.Instance.Bind(PlayerId, ESpatialOrientation.Forward),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId));

		Assert.Equal(100, state.Heat);
		Assert.False(state.Overdrive);
	}

	[Fact]
	public void NonParticipatingProfileGainsNoHeatFromMove()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.HeatProfile = HeatSpec.None;

		Assert.True(BattleTestApply.TryApplyOne(
			MoveDef.Instance.Bind(PlayerId, ESpatialOrientation.Forward),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId));

		Assert.Equal(0, state.Heat);
	}

	[Fact]
	public void ApDeltaPenaltyDoesNotGenerateHeat()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var state = battle.Engine.World.StateOf(PlayerId);
		state.ActionPoints = 4;

		Assert.True(BattleTestApply.TryApplyOne(
			ApDeltaDef.Instance.Bind(PlayerId, -2),
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(PlayerId),
			PlayerId));

		Assert.Equal(2, state.ActionPoints);
		Assert.Equal(0, state.Heat);
	}
}
