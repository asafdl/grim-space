using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
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
		var shipFore = battle.Sim.StateOf<ActorState>(PlayerId).Fore;
		var action = new TorpedoAction(PlayerId, ETorpedoMount.Aft);

		Assert.True(battle.Sim.TryEnqueue(action));

		var ship = battle.Sim.StateOf<ActorState>(PlayerId);
		Assert.Equal(TorpedoConfig.CooldownTurns, ship.TorpedoCooldownRemaining);

		var torpedo = Assert.Single(UnitRegistry.For(battle.Sim.World).All, unit => unit.State.Type == EType.Torpedo);
		Assert.Equal(TorpedoConfig.Fuel, torpedo.State.FuelRemaining);
		Assert.Equal(TorpedoConfig.SpawnMomentum, torpedo.State.MomentumLevel);
		Assert.Equal(origin + (Coord.Zero - shipFore), torpedo.State.Position);
		Assert.Equal(Coord.Zero - shipFore, torpedo.State.Fore);
		Assert.Equal(ETeam.Player, torpedo.Alliance.Team);
	}

	[Fact]
	public void FireIllegalWhileCooldownActive()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));

		Assert.True(battle.Sim.TryEnqueue(new TorpedoAction(PlayerId, ETorpedoMount.Dorsal)));
		Assert.False(battle.Sim.TryEnqueue(new TorpedoAction(PlayerId, ETorpedoMount.Ventral)));
	}

	[Fact]
	public void FighterCapabilitiesIncludeEnabledTorpedoMounts()
	{
		var weapons = Capabilities.WeaponsFor(EType.Fighter);
		foreach (var mount in TorpedoConfig.EnabledMounts)
			Assert.Contains(weapons, def => def is TorpedoDef torpedo && torpedo.Mount == mount);
	}

	[Fact]
	public void RoundUpkeepDecrementsTorpedoCooldown()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		battle.Engine.World.StateOf(PlayerId).TorpedoCooldownRemaining = 2;

		battle.ResolveTurn();

		Assert.Equal(1, battle.Engine.World.StateOf(PlayerId).TorpedoCooldownRemaining);
	}

	[Fact]
	public void ResolveTurnActivatesSpawnedTorpedoSameCycle()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		Assert.True(battle.Sim.TryEnqueue(new TorpedoAction(PlayerId, ETorpedoMount.Aft)));

		var replay = battle.ResolveTurn();

		var torpedo = Assert.Single(UnitRegistry.For(battle.Engine.World).All, unit => unit.State.Type == EType.Torpedo);
		Assert.Contains(replay.Actions, action => action is TorpedoAction);
		Assert.Contains(
			replay.History,
			entry => entry is Record<SpawnFacts> { Value: var spawn }
				&& spawn.TargetId == torpedo.State.Id);
		Assert.Contains(
			replay.Actions,
			action => action is EndOfPhaseAction && action.ActorId == torpedo.State.Id);
	}
}
