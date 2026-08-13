using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Actions;

public sealed class CarrierPatrolIntegrationTests
{
	[Fact]
	public void ResolveTurn_CarrierDeploysPatrolSameCycle()
	{
		var battle = BattleTestFixture.BeginCarrierVsPlayer(new Coord(0, 5, 5), new Coord(8, 5, 5));
		var carrierId = BattleTestFixture.FirstEnemyId(battle);

		var replay = BattleTestActions.CommitAndResolve(battle);

		var patrol = Assert.Single(
			UnitRegistry.For(battle.Engine.World).All,
			unit => unit.State.Type == EType.Patrol);
		Assert.Equal(carrierId, patrol.State.ParentId);

		Assert.Contains(replay.History, entry => entry is SpawnPatrolAction { ActorId: var actorId } && actorId == carrierId);
		Assert.Contains(
			replay.History,
			entry => entry is Record<SpawnFacts> { Value: var spawn }
				&& spawn.SourceId == carrierId
				&& spawn.TargetId == patrol.State.Id
				&& spawn.EntityType == EType.Patrol);
		Assert.Contains(
			replay.Actions,
			action => action is EndOfPhaseAction { ActorId: var actorId } && actorId == patrol.State.Id);
	}

	[Fact]
	public void ResolveTurn_SpawnedPatrolFiresFlakWhenPlayerInBurst()
	{
		var grid = BattleTestFixture.Grid();
		var carrierPos = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(new Coord(0, 5, 5));
		var carrier = BattleTestFixture.Carrier(carrierPos);
		carrier.State.Fore = new Coord(1, 0, 0);
		carrier.State.Dorsal = Coord.Up;
		carrier.State.Starboard = Coord.Cross(carrier.State.Dorsal, carrier.State.Fore);

		var (_, fore, dorsal) = PatrolBayMount.LaunchPose(carrier.State);
		var patrolFrame = new BodyFrame(
			carrierPos + BodyFrame.From(carrier.State).Step(ESpatialOrientation.Ventral),
			fore,
			dorsal,
			Coord.Cross(dorsal, fore));
		var burstCells = WeaponBursts.FlakBurstCells(
			patrolFrame,
			ESpatialOrientation.Port,
			grid.IsInBounds);
		Assert.NotEmpty(burstCells);
		player.State.Position = burstCells.First();

		var battle = BattleTestFixture.BeginSimulation(player, carrier, grid);
		var replay = BattleTestActions.CommitAndResolve(battle);

		var patrol = Assert.Single(
			UnitRegistry.For(battle.Engine.World).All,
			unit => unit.State.Type == EType.Patrol);
		Assert.Contains(
			replay.Actions,
			action => action is FlakAction { ActorId: var actorId } && actorId == patrol.State.Id);
	}
}
