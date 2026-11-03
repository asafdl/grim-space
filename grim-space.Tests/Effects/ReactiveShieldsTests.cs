using GrimSpace.Battle;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Effects;

[BattleTestSuite]
public sealed class ReactiveShieldsTests
{
	[Fact]
	public void Apply_FirstHitFullThenStacksReduceMultiplicatively()
	{
		var (target, shooter, battle, hitFace) = ReadyBattle(currentShieldPoints: 100);

		ApplyDamage(shooter, target, battle, damage: 10);
		Assert.Equal(90, target.State.CurrentShieldPoints[hitFace]);
		Assert.Equal(1, target.State.ReactiveShieldsStacks);

		ApplyDamage(shooter, target, battle, damage: 10);
		Assert.Equal(82, target.State.CurrentShieldPoints[hitFace]);
		Assert.Equal(2, target.State.ReactiveShieldsStacks);

		ApplyDamage(shooter, target, battle, damage: 10);
		Assert.Equal(76, target.State.CurrentShieldPoints[hitFace]);
		Assert.Equal(3, target.State.ReactiveShieldsStacks);
	}

	[Fact]
	public void Apply_UndoRestoresStacks()
	{
		var (target, shooter, battle, _) = ReadyBattle(currentShieldPoints: 50);
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.LightningCannonBurst,
			new HashSet<Coord> { target.State.Position },
			10,
			shooter.State.Position);
		var runtime = battle.Engine.ActorRuntimes.For(shooter.State.Id);

		effect.Apply(battle.Engine.World, runtime, shooter.State.Id);
		Assert.Equal(1, target.State.ReactiveShieldsStacks);

		effect.Undo(battle.Engine.World, runtime, shooter.State.Id);
		Assert.Equal(0, target.State.ReactiveShieldsStacks);
	}

	[Fact]
	public void TurnUpkeep_ClearsStacks()
	{
		var (target, _, battle, _) = ReadyBattle(currentShieldPoints: 50);
		target.State.ReactiveShieldsStacks = 3;

		new TurnUpkeepEffect().Apply(
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(target.State.Id),
			target.State.Id);

		Assert.Equal(0, target.State.ReactiveShieldsStacks);
	}

	[Fact]
	public void StateClone_PreservesStacksAndPassive()
	{
		var state = BattleTestFixture.Player(new Coord(5, 5, 5)).State;
		state.Statuses.Add(BattlePassiveStatusInstaller.Create(EPilotCapabilityId.ReactiveShields, state.Id));
		state.ReactiveShieldsStacks = 2;

		var clone = state.Clone();

		Assert.Equal(2, clone.ReactiveShieldsStacks);
		Assert.Contains(
			clone.Statuses,
			status => status.InstanceId
				== BattleStatuses.PassiveInstanceId(nameof(EPilotCapabilityId.ReactiveShields), state.Id));
	}

	private static (Unit Target, Unit Shooter, BattleOrchestrator Battle, ESpatialOrientation HitFace) ReadyBattle(
		int currentShieldPoints)
	{
		var target = BattleTestFixture.Player(new Coord(5, 5, 5));
		var shooter = BattleTestFixture.RepurposedMiner(new Coord(8, 5, 5), "shooter");
		var pilots = new Dictionary<string, PilotBattleSnapshot?>(StringComparer.Ordinal)
		{
			[target.State.Id] = PilotBattleSnapshot.FromSkills(
				"pilot",
				[EPilotSkillId.ReactiveShields]),
		};
		var battle = BattleTestFixture.BeginSimulation(target, [shooter], pilotsByActorId: pilots);
		var hitFace = BodyFrame.From(target.State).HitFaceFrom(shooter.State.Position);
		target.State.CurrentShieldPoints[hitFace] = currentShieldPoints;
		return (target, shooter, battle, hitFace);
	}

	private static void ApplyDamage(
		Unit source,
		Unit target,
		BattleOrchestrator battle,
		int damage)
	{
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.LightningCannonBurst,
			new HashSet<Coord> { target.State.Position },
			damage,
			source.State.Position);
		effect.Apply(
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(source.State.Id),
			source.State.Id);
	}
}
