using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Actions;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Movement;

[BattleTestSuite]
public sealed class TorpedoMovementTests
{
	private const string PlayerId = "player";

	[Fact]
	public void ForwardAndLateralStepsUseTorpedoCosts()
	{
		var body = CatalogExpectations.DefaultTorpedoBody();
		var (sim, torpedoId) = CreateSimulation();
		var state = sim.StateOf<ActorState>(torpedoId);

		Assert.True(sim.TryEnqueue(new TorpedoMoveStepAction(torpedoId, ESpatialOrientation.Port)));
		Assert.Equal(
			body.MovementActionPoints - body.LateralMoveApCost,
			state.ActionPoints);

		Assert.True(sim.TryEnqueue(new TorpedoMoveStepAction(torpedoId, ESpatialOrientation.Forward)));
		Assert.Equal(
			body.MovementActionPoints - body.LateralMoveApCost - body.ForwardMoveApCost,
			state.ActionPoints);
	}

	[Fact]
	public void LateralMovementDoesNotRotate()
	{
		var (sim, torpedoId) = CreateSimulation();
		var state = sim.StateOf<ActorState>(torpedoId);
		var basis = (state.Fore, state.Dorsal, state.Starboard);

		Assert.True(sim.TryEnqueue(new TorpedoMoveStepAction(torpedoId, ESpatialOrientation.Dorsal)));

		Assert.Equal(basis, (state.Fore, state.Dorsal, state.Starboard));
	}

	[Fact]
	public void RetroMovementIsIllegal()
	{
		var (sim, torpedoId) = CreateSimulation();

		Assert.False(sim.TryEnqueue(new TorpedoMoveStepAction(torpedoId, ESpatialOrientation.Retro)));
	}

	[Fact]
	public void FullActivationAllowsFourForwardOrTwoLateralSteps()
	{
		var (forwardSim, forwardId) = CreateSimulation();
		var (lateralSim, lateralId) = CreateSimulation();

		for (var step = 0; step < 4; step++)
			Assert.True(forwardSim.TryEnqueue(new TorpedoMoveStepAction(forwardId, ESpatialOrientation.Forward)));

		Assert.False(forwardSim.TryEnqueue(new TorpedoMoveStepAction(forwardId, ESpatialOrientation.Forward)));
		Assert.True(lateralSim.TryEnqueue(new TorpedoMoveStepAction(lateralId, ESpatialOrientation.Port)));
		Assert.True(lateralSim.TryEnqueue(new TorpedoMoveStepAction(lateralId, ESpatialOrientation.Port)));
		Assert.False(lateralSim.TryEnqueue(new TorpedoMoveStepAction(lateralId, ESpatialOrientation.Port)));
	}

	private static (BattleSimulation Sim, string TorpedoId) CreateSimulation()
	{
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			new Coord(5, 5, 5),
			new Coord(0, 0, 0));
		battle.Engine.Commit(TorpedoDef.Instance.Bind(PlayerId, ESpatialOrientation.Retro));
		var torpedo = Assert.Single(
			UnitRegistry.For(battle.Engine.World).All,
			unit => unit.State.Type == EType.Torpedo);
		torpedo.State.Position = new Coord(5, 5, 5);
		torpedo.State.Fore = Coord.Forward;
		torpedo.State.Dorsal = Coord.Up;
		torpedo.State.Starboard = Coord.Cross(Coord.Up, Coord.Forward);
		return (battle.Engine.CreateSimulation(), torpedo.State.Id);
	}
}
