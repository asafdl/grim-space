using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class VoidBombActionTests
{
	private const string PlayerId = "player";

	[Fact]
	public void FireSpawnsTorpedoWithFuelAndSetsShipCooldown()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		var shipFore = battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId).Fore;
		var action = VoidBombDef.Instance.Bind(PlayerId, ESpatialOrientation.Retro);

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(action));

		var ship = battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId);
		Assert.Equal(
			CatalogExpectations.DefaultVoidBombLauncherSpec().CooldownTurns,
			StateMountTestKit.CooldownRemaining(
				ship,
				EAbilityKind.VoidBombLauncher,
				ESpatialOrientation.Retro));
		Assert.Equal(
			0,
			StateMountTestKit.CooldownRemaining(
				ship,
				EAbilityKind.VoidBombLauncher,
				ESpatialOrientation.Ventral));

		var torpedo = Assert.Single(UnitRegistry.For(battle.PlayerAgent.Sim.World).All, unit => unit.State.Type == EType.VoidBomb);
		Assert.Equal(CatalogExpectations.DefaultVoidBombLauncher().FuelTurns, torpedo.State.FuelRemaining);
		Assert.Equal(origin + (Coord.Zero - shipFore), torpedo.State.Position);
		Assert.Equal(Coord.Zero - shipFore, torpedo.State.Fore);
		Assert.Equal(ETeam.Player, torpedo.Team);
		Assert.Equal(PlayerId, torpedo.State.ParentId);
	}

	[Fact]
	public void CooldownAppliesOnlyToFiredLauncher()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(
			VoidBombDef.Instance.Bind(PlayerId, ESpatialOrientation.Dorsal)));
		Assert.False(battle.PlayerAgent.Sim.TryEnqueue(
			VoidBombDef.Instance.Bind(PlayerId, ESpatialOrientation.Dorsal)));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(
			VoidBombDef.Instance.Bind(PlayerId, ESpatialOrientation.Ventral)));
	}

	[Theory]
	[InlineData(ESpatialOrientation.Forward)]
	[InlineData(ESpatialOrientation.Port)]
	[InlineData(ESpatialOrientation.Starboard)]
	public void FireIllegalFromUnsupportedMount(ESpatialOrientation mountedOn)
	{
		var battle = TurnOrchestrationTests.CreateOrchestrator(new Coord(5, 5, 5), new Coord(0, 0, 0));

		Assert.False(battle.PlayerAgent.Sim.TryEnqueue(VoidBombDef.Instance.Bind(PlayerId, mountedOn)));
	}

	[Fact]
	public void FighterCapabilitiesIncludeTorpedo()
	{
		var weapons = Capabilities.AbilitiesFor(EType.Fighter);

		Assert.Contains(weapons, def => def is VoidBombDef);
	}

	[Fact]
	public void RoundUpkeepDecrementsTorpedoCooldown()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		StateMountTestKit.SetCooldownRemaining(battle.Engine.World.StateOf(PlayerId), EAbilityKind.VoidBombLauncher, 2);

		BattleTestActions.CommitAndResolve(battle);

		Assert.Equal(1, StateMountTestKit.CooldownRemaining(battle.Engine.World.StateOf(PlayerId), EAbilityKind.VoidBombLauncher));
	}

	[Fact]
	public void ResolveTurnActivatesSpawnedTorpedoSameCycle()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		var shipFore = battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId).Fore;
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(
			VoidBombDef.Instance.Bind(PlayerId, ESpatialOrientation.Retro)));

		var replay = BattleTestActions.CommitAndResolve(battle);

		var torpedo = Assert.Single(UnitRegistry.For(battle.Engine.World).All, unit => unit.State.Type == EType.VoidBomb);
		Assert.Equal(CatalogExpectations.DefaultVoidBombLauncher().FuelTurns - 1, torpedo.State.FuelRemaining);
		Assert.NotEqual(origin - shipFore, torpedo.State.Position);
		Assert.Contains(replay.Actions, action => action is VoidBombAction);
		Assert.Contains(
			replay.History,
			entry => entry is Record<SpawnFacts> { Value: var spawn }
				&& spawn.TargetId == torpedo.State.Id);
		Assert.Contains(
			replay.Actions,
			action => action is EndOfPhaseAction && action.ActorId == torpedo.State.Id);
		Assert.Contains(
			replay.Actions,
			action => action is VoidBombMoveStepAction && action.ActorId == torpedo.State.Id);
		Assert.Contains(
			replay.Actions,
			action => action is FuelBurnAction && action.ActorId == torpedo.State.Id);
	}

	[Fact]
	public void QueuedTorpedoKeepsSpawnedIdAcrossReevaluationForkAndReplay()
	{
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			new Coord(5, 5, 5),
			new Coord(0, 0, 0));
		var sim = battle.PlayerAgent.Sim;
		var action = VoidBombDef.Instance.Bind(PlayerId, ESpatialOrientation.Retro);

		Assert.True(sim.TryEnqueue(action));
		AssertSpawned(sim.World, action.SpawnedUnitId);

		sim.Reevaluate();
		AssertSpawned(sim.World, action.SpawnedUnitId);
		AssertSpawned(sim.Fork().World, action.SpawnedUnitId);
		AssertSpawned(sim.ReplayWorld(sim.Actions.Count), action.SpawnedUnitId);
	}

	private static void AssertSpawned(GrimSpace.Battle.World.BattleWorld world, string unitId) =>
		Assert.True(UnitRegistry.For(world).TryGet(unitId, out _));
}
