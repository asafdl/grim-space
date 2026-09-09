using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.World;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Actions;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Objectives;

public sealed class ObjectiveTests
{
	private const string PlayerId = "player";

	[Fact]
	public void TorpedoDeathDoesNotEndBattleWhilePlayerAliveAndEnemyAlive()
	{
		var battle = BattleWithTorpedo(out var torpedoId);
		battle.Engine.World.StateOf(torpedoId).HullPoints = 0;
		BattleTestFixture.ResetPlayerPlanning(battle);

		_ = BattleTestActions.CommitAndResolve(battle);

		Assert.Equal(BattleOutcome.Ongoing, battle.Outcome);
	}

	private static BattleOrchestrator BattleWithTorpedo(out string torpedoId)
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		battle.Engine.Commit(TorpedoDef.Instance.Bind(PlayerId, ESpatialOrientation.Retro));
		var torpedo = Assert.Single(UnitRegistry.For(battle.Engine.World).All, unit => unit.State.Type == EType.Torpedo);
		torpedoId = torpedo.State.Id;
		ExecutionAgent<BattleWorld, ActorRuntime>.Initialize(
			torpedo.ExecutionAgent,
			torpedoId,
			battle.Engine.CreateSimulation,
			battle.WriterFor(torpedoId));
		BattleTestFixture.ResetPlayerPlanning(battle);
		return battle;
	}
}
