using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Engine;
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
		Assert.Equal(1, target.State.StatusOf<ReactiveShields>()!.Stacks);

		ApplyDamage(shooter, target, battle, damage: 10);
		Assert.Equal(82, target.State.CurrentShieldPoints[hitFace]);
		Assert.Equal(2, target.State.StatusOf<ReactiveShields>()!.Stacks);

		ApplyDamage(shooter, target, battle, damage: 10);
		Assert.Equal(76, target.State.CurrentShieldPoints[hitFace]);
		Assert.Equal(3, target.State.StatusOf<ReactiveShields>()!.Stacks);
	}

	[Fact]
	public void Apply_UndoRestoresStacks()
	{
		var (target, shooter, battle, _) = ReadyBattle(currentShieldPoints: 50);
		var action = new HighVelocityRodAction(shooter.State.Id, ESpatialOrientation.Forward);
		var runtime = battle.Engine.ActorRuntimes.For(shooter.State.Id);
		var applied = BattleEffectTestKit.ApplyEffects(
			action,
			battle.Engine.World,
			runtime,
			[
				new DamageUnitEffect(
					EImpactCause.HighVelocityRod,
					target.State.Id,
					shooter.State.Position,
					10),
			]);
		Assert.Equal(1, target.State.StatusOf<ReactiveShields>()!.Stacks);

		ExecutionHelper.UndoEffects(applied, action, battle.Engine.World, runtime);
		Assert.Equal(0, target.State.StatusOf<ReactiveShields>()!.Stacks);
	}

	[Fact]
	public void TurnUpkeep_ClearsStacks()
	{
		var (target, _, battle, _) = ReadyBattle(currentShieldPoints: 50);
		target.State.StatusOf<ReactiveShields>()!.Stacks = 3;

		new TurnUpkeepEffect().Apply(
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(target.State.Id),
			target.State.Id);

		Assert.Equal(0, target.State.StatusOf<ReactiveShields>()!.Stacks);
	}

	[Fact]
	public void StateClone_PreservesStacksAndPassive()
	{
		var state = BattleTestFixture.Player(new Coord(5, 5, 5)).State;
		state.Statuses.Add(BattleStatusMaterializer.Create(EPilotCapabilityId.ReactiveShields));
		state.StatusOf<ReactiveShields>()!.Stacks = 2;

		var clone = state.Clone();

		Assert.Equal(2, clone.StatusOf<ReactiveShields>()!.Stacks);
		Assert.Contains(
			clone.Statuses,
			status => status is ReactiveShields);
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
		var action = new HighVelocityRodAction(source.State.Id, ESpatialOrientation.Forward);
		BattleEffectTestKit.ApplyEffects(
			action,
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(source.State.Id),
			[
				new DamageUnitEffect(
					EImpactCause.HighVelocityRod,
					target.State.Id,
					source.State.Position,
					damage),
			]);
	}
}
