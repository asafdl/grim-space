using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Actions;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Ai;

public sealed class TorpedoTargetSelectionTests
{
	private const string PlayerId = "player";

	[Fact]
	public void Classify_InTrajectoryBeatsFutureAndUnreachable()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		FaceForward(battle, torpedoId);
		var start = new Coord(5, 5, 10);
		battle.Engine.World.StateOf(torpedoId).Position = start;
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = 1;
		battle.Engine.World.StateOf(torpedoId).MomentumLevel = 0;
		battle.Engine.World.StateOf(PlayerId).Position = new Coord(0, 0, 0);

		var envelope = TorpedoReachEnvelope.Build(battle.Engine.CreateSimulation(), torpedoId);

		Assert.Equal(ETorpedoTargetClass.InTrajectory, envelope.Classify(start + Coord.Forward * 2));
		Assert.Equal(ETorpedoTargetClass.Unreachable, envelope.Classify(new Coord(5, 5, 0)));
	}

	[Fact]
	public void BestReachableOpponent_PrefersInTrajectoryOverFuture()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		FaceForward(battle, torpedoId);
		var start = new Coord(5, 5, 1);
		battle.Engine.World.StateOf(torpedoId).Position = start;
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = 3;
		battle.Engine.World.StateOf(torpedoId).MomentumLevel = 0;
		battle.Engine.World.StateOf(PlayerId).Position = new Coord(0, 0, 0);

		var inTrajectory = battle.Engine.World.Units.Values.First(unit => unit.Controller == EController.Enemy);
		inTrajectory.State.Position = start + Coord.Forward * 2;

		var future = Factory.Create(
			new Instance { Id = "future", Type = EType.Patrol, Controller = EController.Enemy },
			start + Coord.Forward * 10);
		battle.Engine.World.MutableUnits[future.State.Id] = future;

		var session = battle.Engine.CreateSimulation();
		var envelope = TorpedoReachEnvelope.Build(session, torpedoId);
		Assert.Equal(ETorpedoTargetClass.InTrajectory, envelope.Classify(inTrajectory.State.Position));
		Assert.Equal(ETorpedoTargetClass.Future, envelope.Classify(future.State.Position));

		var chosen = TorpedoSearchInput.BestReachableOpponent(session, torpedoId);
		Assert.NotNull(chosen);
		Assert.Equal(inTrajectory.State.Id, chosen.State.Id);
	}

	[Fact]
	public void BestReachableOpponent_IgnoresUnreachableBehind()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		FaceForward(battle, torpedoId);
		var start = new Coord(5, 5, 10);
		battle.Engine.World.StateOf(torpedoId).Position = start;
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = 1;
		battle.Engine.World.StateOf(torpedoId).MomentumLevel = 0;
		battle.Engine.World.StateOf(PlayerId).Position = new Coord(0, 0, 0);

		var ahead = battle.Engine.World.Units.Values.First(unit => unit.Controller == EController.Enemy);
		ahead.State.Position = start + Coord.Forward * 2;

		var behind = Factory.Create(
			new Instance { Id = "behind", Type = EType.Patrol, Controller = EController.Enemy },
			new Coord(5, 5, 0));
		battle.Engine.World.MutableUnits[behind.State.Id] = behind;

		var chosen = TorpedoSearchInput.BestReachableOpponent(battle.Engine.CreateSimulation(), torpedoId);
		Assert.NotNull(chosen);
		Assert.Equal(ahead.State.Id, chosen.State.Id);
	}

	[Fact]
	public void Plan_LocksOntoAheadTargetNotCloserBehind()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		FaceForward(battle, torpedoId);
		var torpedoPos = new Coord(5, 5, 5);
		battle.Engine.World.StateOf(torpedoId).Position = torpedoPos;
		battle.Engine.World.StateOf(torpedoId).FuelRemaining = TorpedoConfig.Fuel;
		battle.Engine.World.StateOf(PlayerId).Position = new Coord(0, 0, 0);

		var ahead = battle.Engine.World.Units.Values.First(unit => unit.Controller == EController.Enemy);
		ahead.State.Position = torpedoPos + Coord.Forward * 6;

		var behind = Factory.Create(
			new Instance { Id = "behind", Type = EType.Patrol, Controller = EController.Enemy },
			torpedoPos + Coord.Forward * -2);
		battle.Engine.World.MutableUnits[behind.State.Id] = behind;

		var torpedo = battle.Engine.World.UnitOf(torpedoId);
		var session = battle.Engine.CreateSimulation();
		TorpedoExecutionAgent.Instance.Plan(torpedo, session);

		var end = session.StateOf<ActorState>(torpedoId).Position;
		Assert.True(end.ManhattanDistanceTo(ahead.State.Position) < torpedoPos.ManhattanDistanceTo(ahead.State.Position));
		Assert.True(end.Z >= torpedoPos.Z);
	}

	private static void FaceForward(Battle.BattleOrchestrator battle, string torpedoId)
	{
		var state = battle.Engine.World.StateOf(torpedoId);
		state.Fore = Coord.Forward;
		state.Dorsal = Coord.Up;
		state.Starboard = Coord.Cross(Coord.Up, Coord.Forward);
	}

	private static Battle.BattleOrchestrator BattleWithTorpedo(out string torpedoId)
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		battle.Engine.Commit(new TorpedoAction(PlayerId, ETorpedoMount.Aft));
		var torpedo = Assert.Single(battle.Engine.World.Units.Values, unit => unit.State.Type == EType.Torpedo);
		torpedoId = torpedo.State.Id;
		return battle;
	}
}
