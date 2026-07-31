using GrimSpace.Battle.Actions;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Movement;

namespace GrimSpace.Tests.Actions;

public sealed class ActionBudgetExhaustionTests
{
	private const string PlayerId = "player";

	private static readonly FlakDef PortFlak = FlakDef.For(EFlakMount.Port);
	private static readonly FlakDef StarboardFlak = FlakDef.For(EFlakMount.Starboard);

	[Fact]
	public void AfterMaxFlak_NoLegalFlakActionsFromEitherMount()
	{
		var session = BeginSimulationAt(new Coord(5, 5, 5));
		LegalActionProbe.EnqueueAll(session, [PortFlak.Bind(PlayerId)]);

		Assert.Equal(0, session.StateOf<ActorState>(PlayerId).FlakRemaining);
		LegalActionProbe.AssertExhausted(session, PlayerId, PortFlak);
		LegalActionProbe.AssertExhausted(session, PlayerId, StarboardFlak);
		Assert.False(session.TryEnqueue(StarboardFlak.Bind(PlayerId)));
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
				PortFlak.Bind(PlayerId),
				RailgunDef.Instance.Bind(PlayerId),
			]);

		var actor = session.StateOf<ActorState>(PlayerId);
		Assert.Equal(0, actor.FlakRemaining);
		Assert.Equal(0, actor.RailgunRemaining);

		LegalActionProbe.AssertExhausted(session, PlayerId, PortFlak);
		LegalActionProbe.AssertExhausted(session, PlayerId, StarboardFlak);
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
		Assert.True(LegalActionProbe.HasAnyLegal(session, PlayerId, PortFlak));
		Assert.True(LegalActionProbe.HasAnyLegal(session, PlayerId, RailgunDef.Instance));
	}

	[Fact]
	public void AfterWeaponBudgetsExhausted_RollsRemainLegal()
	{
		var session = BeginSimulationAt(new Coord(5, 5, 1));

		LegalActionProbe.EnqueueAll(
			session,
			[
				PortFlak.Bind(PlayerId),
				RailgunDef.Instance.Bind(PlayerId),
			]);

		Assert.True(LegalActionProbe.HasAnyLegal(session, PlayerId, RollDef.Instance));
		Assert.True(LegalActionProbe.HasAnyLegal(session, PlayerId, HeadingDef.Instance));
	}

	private static Simulation<BattleWorld, ActorRuntime> BeginSimulationAt(Coord origin)
	{
		var battle = BattleTestFixture.BeginSimulation(origin);
		return battle.Sim;
	}
}
