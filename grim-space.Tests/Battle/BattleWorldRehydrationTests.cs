using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.World;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests;

[BattleTestSuite]
public sealed class BattleWorldRehydrationTests
{
	[Fact]
	public void FromWorld_PreservesAuthoritativeBattleState()
	{
		using var original = BattleOrchestrator.FromEncounter(
			BattleEncounter.DevDefault(seed: 17, gridSize: 12),
			gridSize: 12);
		var liveWorld = original.Engine.World;
		var player = liveWorld.UnitRegistry.UnitOf(original.PlayerId);
		var mount = player.State.MountRuntime.Keys.First();
		player.State.Position = new Coord(3, 4, 5);
		player.State.HullPoints = 7;
		player.State.MountRuntime[mount].UsesRemaining = 0;
		player.State.MountRuntime[mount].CooldownRemaining = 2;
		liveWorld.Timeline.Clock.Set(4);
		original.Engine.Schedule(3, new EndOfPhaseAction(original.PlayerId));

		var savedWorld = liveWorld.Fork();
		original.Dispose();

		using var restored = BattleOrchestrator.FromWorld(savedWorld, player.State.Id);
		var restoredWorld = restored.Engine.World;
		var restoredPlayer = restoredWorld.UnitRegistry.UnitOf(player.State.Id);

		Assert.Equal(4, restored.TurnNumber);
		Assert.True(restored.AcceptsPlayerInput);
		Assert.Equal(player.State.Position, restoredPlayer.State.Position);
		Assert.Equal(player.State.HullPoints, restoredPlayer.State.HullPoints);
		Assert.True(
			restoredPlayer.State.ShieldPoints.Matches(player.State.ShieldPoints));
		Assert.Equal(
			player.State.MountRuntime[mount].UsesRemaining,
			restoredPlayer.State.MountRuntime[mount].UsesRemaining);
		Assert.Equal(
			player.State.MountRuntime[mount].CooldownRemaining,
			restoredPlayer.State.MountRuntime[mount].CooldownRemaining);
		Assert.Equal(
			liveWorld.UnitRegistry.Ids.OrderBy(id => id),
			restoredWorld.UnitRegistry.Ids.OrderBy(id => id));
		Assert.Equal(
			liveWorld.Asteroids.Select(AsteroidSnapshot),
			restoredWorld.Asteroids.Select(AsteroidSnapshot));
		Assert.True(restoredWorld.Timeline.ContainsPending(
			action => action is EndOfPhaseAction end && end.ActorId == player.State.Id));
	}

	[Fact]
	public void FromSavedWorld_RecreatesAgentsInsteadOfReusingInstances()
	{
		using var original = BattleOrchestrator.FromEncounter(
			BattleEncounter.DevDefault(seed: 18, gridSize: 12),
			gridSize: 12);
		var savedWorld = original.Engine.World.Fork();
		var savedAgents = savedWorld.UnitRegistry.All
			.ToDictionary(unit => unit.State.Id, unit => unit.ExecutionAgent);

		using var restored = BattleOrchestrator.FromSavedWorld(savedWorld, original.PlayerId);

		foreach (var unit in restored.Engine.World.UnitRegistry.All)
		{
			Assert.NotSame(savedAgents[unit.State.Id], unit.ExecutionAgent);
			Assert.True(unit.ExecutionAgent.IsInitialized);
		}
		Assert.IsType<UserExecutionAgent>(
			restored.Engine.World.UnitRegistry.UnitOf(original.PlayerId).ExecutionAgent);
	}

	private static object AsteroidSnapshot(Asteroid asteroid) =>
		(
			asteroid.Id,
			asteroid.ActorId,
			asteroid.Center,
			asteroid.Passable,
			asteroid.BlocksAbilities,
			asteroid.Cells.OrderBy(cell => cell.X).ThenBy(cell => cell.Y).ThenBy(cell => cell.Z));
}
