using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
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

	private static readonly MissileDef ForeMissile =
		MissileDef.For(EMissileMount.Fore, CombatConfig.ForeMissileMinRange);

	private static readonly FlakDef PortFlak = FlakDef.For(EFlakMount.Port);
	private static readonly FlakDef StarboardFlak = FlakDef.For(EFlakMount.Starboard);

	[Fact]
	public void AfterMaxMissiles_NoLegalMissileActions()
	{
		var session = BeginSessionAt(new Coord(5, 5, 1));
		var target = session.StateOf<ActorState>(PlayerId).Position + Coord.Forward * CombatConfig.ForeMissileMinRange;
		var missiles = Enumerable
			.Range(0, CombatConfig.MissilesPerTurn)
			.Select(_ => ForeMissile.Bind(PlayerId, target))
			.Cast<IAction>()
			.ToList();

		LegalActionProbe.EnqueueAll(session, missiles);

		Assert.Equal(0, session.StateOf<ActorState>(PlayerId).MissilesRemaining);
		LegalActionProbe.AssertExhausted(session, PlayerId, ForeMissile);
		Assert.False(session.TryEnqueue(ForeMissile.Bind(PlayerId, target)));
	}

	[Fact]
	public void AfterMaxFlak_NoLegalFlakActionsFromEitherMount()
	{
		var session = BeginSessionAt(new Coord(5, 5, 5));
		LegalActionProbe.EnqueueAll(session, [PortFlak.Bind(PlayerId)]);

		Assert.Equal(0, session.StateOf<ActorState>(PlayerId).FlakRemaining);
		LegalActionProbe.AssertExhausted(session, PlayerId, PortFlak);
		LegalActionProbe.AssertExhausted(session, PlayerId, StarboardFlak);
		Assert.False(session.TryEnqueue(StarboardFlak.Bind(PlayerId)));
	}

	[Fact]
	public void AfterMaxRailgun_NoLegalRailgunActions()
	{
		var session = BeginSessionAt(new Coord(5, 5, 5));
		var enemyId = session.World.Units.Keys.First(id => id != PlayerId);
		LegalActionProbe.EnqueueAll(session, [RailgunDef.Instance.Bind(PlayerId, enemyId)]);

		Assert.Equal(0, session.StateOf<ActorState>(PlayerId).RailgunRemaining);
		LegalActionProbe.AssertExhausted(session, PlayerId, RailgunDef.Instance);
		Assert.False(session.TryEnqueue(RailgunDef.Instance.Bind(PlayerId, enemyId)));
	}

	[Fact]
	public void AfterMaxRolls_NoLegalRollActions()
	{
		var session = BeginSessionAt(new Coord(5, 5, 5));
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
		var session = BeginSessionAt(new Coord(5, 5, 5));
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
		var session = BeginSessionAt(new Coord(5, 5, 1));
		var target = session.StateOf<ActorState>(PlayerId).Position + Coord.Forward * CombatConfig.ForeMissileMinRange;
		var enemyId = session.World.Units.Keys.First(id => id != PlayerId);
		var actions = new List<IAction>();

		for (var i = 0; i < CombatConfig.MissilesPerTurn; i++)
			actions.Add(ForeMissile.Bind(PlayerId, target));

		actions.Add(PortFlak.Bind(PlayerId));
		actions.Add(RailgunDef.Instance.Bind(PlayerId, enemyId));

		LegalActionProbe.EnqueueAll(session, actions);

		var actor = session.StateOf<ActorState>(PlayerId);
		Assert.Equal(0, actor.MissilesRemaining);
		Assert.Equal(0, actor.FlakRemaining);
		Assert.Equal(0, actor.RailgunRemaining);

		LegalActionProbe.AssertExhausted(session, PlayerId, ForeMissile);
		LegalActionProbe.AssertExhausted(session, PlayerId, PortFlak);
		LegalActionProbe.AssertExhausted(session, PlayerId, StarboardFlak);
		LegalActionProbe.AssertExhausted(session, PlayerId, RailgunDef.Instance);
	}

	[Fact]
	public void AfterApSpentOnRolls_WeaponsRemainLegal()
	{
		var session = BeginSessionAt(new Coord(5, 5, 1));
		var rolls = Enumerable
			.Range(0, MovementExpectations.FighterApPerTurn)
			.Select(_ => RollDef.Instance.Bind(PlayerId, ERollDirection.Clockwise))
			.Cast<IAction>()
			.ToList();

		LegalActionProbe.EnqueueAll(session, rolls);

		Assert.Equal(0, session.StateOf<ActorState>(PlayerId).ActionPoints);
		LegalActionProbe.AssertExhausted(session, PlayerId, RollDef.Instance);
		Assert.True(LegalActionProbe.HasAnyLegal(session, PlayerId, ForeMissile));
		Assert.True(LegalActionProbe.HasAnyLegal(session, PlayerId, PortFlak));
		Assert.True(LegalActionProbe.HasAnyLegal(session, PlayerId, RailgunDef.Instance));
	}

	[Fact]
	public void AfterWeaponBudgetsExhausted_RollsRemainLegal()
	{
		var session = BeginSessionAt(new Coord(5, 5, 1));
		var target = session.StateOf<ActorState>(PlayerId).Position + Coord.Forward * CombatConfig.ForeMissileMinRange;
		var enemyId = session.World.Units.Keys.First(id => id != PlayerId);

		LegalActionProbe.EnqueueAll(
			session,
			[
				ForeMissile.Bind(PlayerId, target),
				ForeMissile.Bind(PlayerId, target),
				PortFlak.Bind(PlayerId),
				RailgunDef.Instance.Bind(PlayerId, enemyId),
			]);

		Assert.True(LegalActionProbe.HasAnyLegal(session, PlayerId, RollDef.Instance));
		Assert.True(LegalActionProbe.HasAnyLegal(session, PlayerId, HeadingDef.Instance));
	}

	private static Simulation<BattleBoard, ActorSession> BeginSessionAt(Coord origin)
	{
		var battle = BattleTestFixture.BeginPlanning(origin);
		return battle.Sim;
	}
}
