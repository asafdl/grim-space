using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Actions;

public sealed class TorpedoActionTests
{
	private const string PlayerId = "player";

	[Fact]
	public void FireSpawnsTorpedoWithFuelAndSetsShipCooldown()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		var shipFore = battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId).Fore;
		var action = new TorpedoAction(PlayerId, ESpatialOrientation.Retro);

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(action));

		var ship = battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId);
		Assert.Equal(TorpedoConfig.CooldownTurns, ship.TorpedoCooldownRemaining);

		var torpedo = Assert.Single(UnitRegistry.For(battle.PlayerAgent.Sim.World).All, unit => unit.State.Type == EType.Torpedo);
		Assert.Equal(TorpedoConfig.Fuel, torpedo.State.FuelRemaining);
		Assert.Equal(TorpedoConfig.SpawnMomentum, torpedo.State.MomentumLevel);
		Assert.Equal(origin + (Coord.Zero - shipFore), torpedo.State.Position);
		Assert.Equal(Coord.Zero - shipFore, torpedo.State.Fore);
		Assert.Equal(ETeam.Player, torpedo.Alliance.Team);
		Assert.Equal(PlayerId, torpedo.State.ParentId);
	}

	[Fact]
	public void FireIllegalWhileCooldownActive()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new TorpedoAction(PlayerId, ESpatialOrientation.Dorsal)));
		Assert.False(battle.PlayerAgent.Sim.TryEnqueue(new TorpedoAction(PlayerId, ESpatialOrientation.Ventral)));
	}

	[Theory]
	[InlineData(ESpatialOrientation.Forward)]
	[InlineData(ESpatialOrientation.Port)]
	[InlineData(ESpatialOrientation.Starboard)]
	public void FireIllegalFromUnsupportedMount(ESpatialOrientation mountedOn)
	{
		var battle = TurnOrchestrationTests.CreateOrchestrator(new Coord(5, 5, 5), new Coord(0, 0, 0));

		Assert.False(battle.PlayerAgent.Sim.TryEnqueue(new TorpedoAction(PlayerId, mountedOn)));
	}

	[Fact]
	public void FighterCapabilitiesIncludeTorpedo()
	{
		var weapons = Capabilities.AbilitiesFor(EType.Fighter);

		Assert.Contains(weapons, def => def is TorpedoDef);
	}

	[Fact]
	public void RoundUpkeepDecrementsTorpedoCooldown()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		battle.Engine.World.StateOf(PlayerId).TorpedoCooldownRemaining = 2;

		BattleTestActions.CommitAndResolve(battle);

		Assert.Equal(1, battle.Engine.World.StateOf(PlayerId).TorpedoCooldownRemaining);
	}

	[Fact]
	public void ResolveTurnDoesNotActivateSpawnedTorpedoSameCycle()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		var shipFore = battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId).Fore;
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new TorpedoAction(PlayerId, ESpatialOrientation.Retro)));

		var replay = BattleTestActions.CommitAndResolve(battle);

		var torpedo = Assert.Single(UnitRegistry.For(battle.Engine.World).All, unit => unit.State.Type == EType.Torpedo);
		Assert.Equal(TorpedoConfig.Fuel, torpedo.State.FuelRemaining);
		Assert.Equal(origin - shipFore, torpedo.State.Position);
		Assert.Contains(replay.Actions, action => action is TorpedoAction);
		Assert.Contains(
			replay.History,
			entry => entry is Record<SpawnFacts> { Value: var spawn }
				&& spawn.TargetId == torpedo.State.Id);
		Assert.DoesNotContain(
			replay.Actions,
			action => action is EndOfPhaseAction && action.ActorId == torpedo.State.Id);
		Assert.DoesNotContain(
			replay.Actions,
			action => action is MoveStepAction && action.ActorId == torpedo.State.Id);
		Assert.DoesNotContain(
			replay.Actions,
			action => action is FuelBurnAction && action.ActorId == torpedo.State.Id);

		var nextReplay = BattleTestActions.CommitAndResolve(battle);
		Assert.Contains(
			nextReplay.Actions,
			action => action is EndOfPhaseAction && action.ActorId == torpedo.State.Id);
	}
}
