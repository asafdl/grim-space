using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Dfs;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Log;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Tests.Actions;

public sealed class SimulationSearchTests
{
	private const string PlayerId = "player";

	[Fact]
	public void RailgunBudgetEnforcedBySimulationTryEnqueue()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var session = battle.PlayerAgent.Sim;
		var railgun = new RailgunAction(PlayerId);

		Assert.Equal(CatalogExpectations.UsesPerTurn(EType.Fighter, EAbilityKind.Railgun), StateMountTestKit.UsesRemaining(session.StateOf<ActorState>(PlayerId), EAbilityKind.Railgun));
		Assert.True(session.TryEnqueue(railgun));
		Assert.Equal(CatalogExpectations.UsesPerTurn(EType.Fighter, EAbilityKind.Railgun) - 1, StateMountTestKit.UsesRemaining(session.StateOf<ActorState>(PlayerId), EAbilityKind.Railgun));
		Assert.False(session.TryEnqueue(new RailgunAction(PlayerId)));
	}

	[Fact]
	public void FlakBudgetEnforcedBySimulationTryEnqueue()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var session = battle.PlayerAgent.Sim;

		Assert.True(session.TryEnqueue(new FlakAction(PlayerId, ESpatialOrientation.Port)));
		Assert.False(session.TryEnqueue(new FlakAction(PlayerId, ESpatialOrientation.Port)));
		Assert.True(session.TryEnqueue(new FlakAction(PlayerId, ESpatialOrientation.Starboard)));
	}

	[Fact]
	public void PeekReturnsNullForIllegalAction()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var session = battle.PlayerAgent.Sim;

		Assert.True(session.TryEnqueue(new RailgunAction(PlayerId)));
		Assert.Null(session.Peek(new RailgunAction(PlayerId)));
	}

	[Fact]
	public void PeekReturnsFrameForLegalActionWithoutMutatingQueue()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var session = battle.PlayerAgent.Sim;
		var railgun = new RailgunAction(PlayerId);

		var peek = session.Peek(railgun);
		Assert.NotNull(peek);
		Assert.Empty(session.Actions);
		Assert.Equal(CatalogExpectations.UsesPerTurn(EType.Fighter, EAbilityKind.Railgun), StateMountTestKit.UsesRemaining(session.StateOf<ActorState>(PlayerId), EAbilityKind.Railgun));
	}

	[Fact]
	public void LegalCapabilitiesIncludeTorpedoesWithoutApplyingSpawnEffects()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var session = battle.PlayerAgent.Sim;

		var actions = Capabilities.LegalCapabilities(session, PlayerId);

		Assert.Equal(3, actions.Count(action => action is TorpedoAction));
		Assert.Empty(session.Actions);
		Assert.Equal(0, StateMountTestKit.CooldownRemaining(session.StateOf<ActorState>(PlayerId), EAbilityKind.TorpedoLauncher));
		Assert.DoesNotContain(
			UnitRegistry.For(session.World).All,
			unit => unit.State.Type == GrimSpace.Units.Enums.EType.Torpedo);
	}

	[Fact]
	public void MoveOnlySearch_StaysWithinDepthLimit()
	{
		const int expectedMaxDepth = 12;
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var maxDepth = 0;

		foreach (var frame in ActionSearch.Run(battle.PlayerAgent.Sim, PlayerId, [MoveDef.Instance], BattleSearchVisit.ForCapabilities))
			maxDepth = System.Math.Max(maxDepth, frame.Depth);

		Assert.True(maxDepth <= expectedMaxDepth, maxDepth.ToString());
	}

	[Fact]
	public void SearchWithQueuedActionsDoesNotMutateSimulation()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var session = battle.PlayerAgent.Sim;
		var heading = new HeadingTurnAction(
			PlayerId,
			GrimSpace.Battle.Movement.Enums.EHeadingTurn.YawRight);

		Assert.True(session.TryEnqueue(heading));
		var actionsBefore = session.Actions.ToList();
		var apBefore = session.StateOf<ActorState>(PlayerId).ActionPoints;

		var foundExtension = false;
		foreach (var frame in ActionSearch.Run(
			session,
			PlayerId,
			Capabilities.Movement,
			BattleSearchVisit.ForCapabilities))
		{
			if (frame.Actions.Count > actionsBefore.Count)
			{
				foundExtension = true;
				break;
			}
		}

		Assert.Equal(actionsBefore, session.Actions);
		Assert.Equal(apBefore, session.StateOf<ActorState>(PlayerId).ActionPoints);
		Assert.True(foundExtension);
	}
}
