using GrimSpace.Battle.Actions;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Movement;

namespace GrimSpace.Tests.Actions;

public sealed class ActionBudgetExhaustionTests
{
	private const string PlayerId = "player";

	private static readonly FlakDef Flak = FlakDef.Instance;

	[Fact]
	public void AfterMaxFlak_NoLegalFlakActionsFromEitherSide()
	{
		var session = BeginSimulationAt(new Coord(5, 5, 5));
		LegalActionProbe.EnqueueAll(session, [Flak.Bind(PlayerId, ESpatialOrientation.Port)]);

		Assert.Equal(0, session.StateOf<ActorState>(PlayerId).FlakRemaining);
		LegalActionProbe.AssertExhausted(session, PlayerId, Flak);
		Assert.False(session.TryEnqueue(Flak.Bind(PlayerId, ESpatialOrientation.Starboard)));
	}

	[Fact]
	public void AfterMaxRailgun_NoLegalRailgunActions()
	{
		var session = BeginSimulationAt(new Coord(5, 5, 5));
		LegalActionProbe.EnqueueAll(session, [RailgunDef.Instance.Bind(PlayerId)]);

		Assert.Equal(0, session.StateOf<ActorState>(PlayerId).RailgunRemaining);
		LegalActionProbe.AssertExhausted(session, PlayerId, RailgunDef.Instance);
		Assert.False(session.TryEnqueue(RailgunDef.Instance.Bind(PlayerId)));
	}

	[Fact]
	public void AfterMaxRolls_NoLegalRollActions()
	{
		var session = BeginSimulationAt(new Coord(5, 5, 5));
		var rolls = Enumerable
			.Range(0, MovementExpectations.FighterApPerTurn)
			.Select(_ => RollDef.Instance.Bind(PlayerId, ERollDirection.Clockwise))
			.Cast<IAction>()
			.ToList();

		LegalActionProbe.EnqueueAll(session, rolls);

		Assert.Equal(0, session.StateOf<ActorState>(PlayerId).ActionPoints);
		LegalActionProbe.AssertExhausted(session, PlayerId, RollDef.Instance);
	}

	[Fact]
	public void AfterMaxPitchUps_NoLegalPitchActions()
	{
		var session = BeginSimulationAt(new Coord(5, 5, 5));
		var turns = Enumerable
			.Range(0, MovementExpectations.FighterApPerTurn)
			.Select(_ => HeadingDef.Instance.Bind(PlayerId, EHeadingTurn.PitchUp))
			.Cast<IAction>()
			.ToList();

		LegalActionProbe.EnqueueAll(session, turns);

		Assert.Equal(0, session.StateOf<ActorState>(PlayerId).ActionPoints);
		LegalActionProbe.AssertExhausted(session, PlayerId, HeadingDef.Instance);
	}

	[Fact]
	public void AfterAllWeaponBudgetsExhausted_NoLegalWeaponActions()
	{
		var session = BeginSimulationAt(new Coord(5, 5, 1));

		LegalActionProbe.EnqueueAll(
			session,
			[
				Flak.Bind(PlayerId, ESpatialOrientation.Port),
				RailgunDef.Instance.Bind(PlayerId),
			]);

		var actor = session.StateOf<ActorState>(PlayerId);
		Assert.Equal(0, actor.FlakRemaining);
		Assert.Equal(0, actor.RailgunRemaining);

		LegalActionProbe.AssertExhausted(session, PlayerId, Flak);
		LegalActionProbe.AssertExhausted(session, PlayerId, RailgunDef.Instance);
	}

	[Fact]
	public void AfterApSpentOnRolls_WeaponsRemainLegal()
	{
		var session = BeginSimulationAt(new Coord(5, 5, 1));
		var rolls = Enumerable
			.Range(0, MovementExpectations.FighterApPerTurn)
			.Select(_ => RollDef.Instance.Bind(PlayerId, ERollDirection.Clockwise))
			.Cast<IAction>()
			.ToList();

		LegalActionProbe.EnqueueAll(session, rolls);

		Assert.Equal(0, session.StateOf<ActorState>(PlayerId).ActionPoints);
		LegalActionProbe.AssertExhausted(session, PlayerId, RollDef.Instance);
		Assert.True(LegalActionProbe.HasAnyLegal(session, PlayerId, Flak));
		Assert.True(LegalActionProbe.HasAnyLegal(session, PlayerId, RailgunDef.Instance));
	}

	[Fact]
	public void AfterWeaponBudgetsExhausted_RollsRemainLegal()
	{
		var session = BeginSimulationAt(new Coord(5, 5, 1));

		LegalActionProbe.EnqueueAll(
			session,
			[
				Flak.Bind(PlayerId, ESpatialOrientation.Port),
				RailgunDef.Instance.Bind(PlayerId),
			]);

		Assert.True(LegalActionProbe.HasAnyLegal(session, PlayerId, RollDef.Instance));
		Assert.True(LegalActionProbe.HasAnyLegal(session, PlayerId, HeadingDef.Instance));
	}

	private static Simulation<BattleWorld, ActorRuntime> BeginSimulationAt(Coord origin)
	{
		var battle = BattleTestFixture.BeginSimulation(origin);
		return battle.PlayerAgent.Sim;
	}
}
