using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
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
public sealed class InTheFrayTests
{
	[Fact]
	public void Apply_ReducesDamageWhenTwoEnemiesAreAtLeastFourKmAway()
	{
		var target = BattleTestFixture.Player(new Coord(5, 5, 5));
		var shooter = BattleTestFixture.RepurposedMiner(new Coord(9, 5, 5), "shooter");
		var distantEnemyA = BattleTestFixture.RepurposedMiner(new Coord(0, 5, 5), "distant-a");
		var distantEnemyB = BattleTestFixture.RepurposedMiner(new Coord(10, 5, 5), "distant-b");
		var battle = BeginBattle(
			target,
			[shooter, distantEnemyA, distantEnemyB],
			[target.State.Id]);
		var hitFace = BodyFrame.From(target.State).HitFaceFrom(shooter.State.Position);
		target.State.CurrentShieldPoints[hitFace] = 6;

		ApplyDamage(shooter, target, battle, damage: 3);

		Assert.Equal(4, target.State.CurrentShieldPoints[hitFace]);
	}

	[Fact]
	public void Apply_DoesNotReduceDamageWhenOnlyOneEnemyIsFarEnoughAway()
	{
		var target = BattleTestFixture.Player(new Coord(5, 5, 5));
		var shooter = BattleTestFixture.RepurposedMiner(new Coord(8, 5, 5), "shooter");
		var closeEnemy = BattleTestFixture.RepurposedMiner(new Coord(6, 5, 5), "close");
		var distantEnemy = BattleTestFixture.RepurposedMiner(new Coord(0, 5, 5), "distant");
		var battle = BeginBattle(
			target,
			[shooter, closeEnemy, distantEnemy],
			[target.State.Id]);
		var hitFace = BodyFrame.From(target.State).HitFaceFrom(shooter.State.Position);
		target.State.CurrentShieldPoints[hitFace] = 6;

		ApplyDamage(shooter, target, battle, damage: 3);

		Assert.Equal(3, target.State.CurrentShieldPoints[hitFace]);
	}

	[Fact]
	public void Apply_ReducesSelfDamageWhenConditionIsMet()
	{
		var source = BattleTestFixture.Player(new Coord(5, 5, 5));
		var distantEnemyA = BattleTestFixture.RepurposedMiner(new Coord(0, 5, 5), "distant-a");
		var distantEnemyB = BattleTestFixture.RepurposedMiner(new Coord(10, 5, 5), "distant-b");
		var battle = BeginBattle(
			source,
			[distantEnemyA, distantEnemyB],
			[source.State.Id]);
		var hitFace = BodyFrame.From(source.State).HitFaceFrom(source.State.Position);
		source.State.CurrentShieldPoints[hitFace] = 6;

		ApplyDamage(source, source, battle, damage: 3);

		Assert.Equal(4, source.State.CurrentShieldPoints[hitFace]);
	}

	[Fact]
	public void Apply_ShieldDamageBonusIsIncludedInReduction()
	{
		var target = BattleTestFixture.Player(new Coord(5, 5, 5));
		var shooter = BattleTestFixture.RepurposedMiner(new Coord(9, 5, 5), "shooter");
		var distantEnemyA = BattleTestFixture.RepurposedMiner(new Coord(0, 5, 5), "distant-a");
		var distantEnemyB = BattleTestFixture.RepurposedMiner(new Coord(10, 5, 5), "distant-b");
		var battle = BeginBattle(
			target,
			[shooter, distantEnemyA, distantEnemyB],
			[target.State.Id]);
		var hitFace = BodyFrame.From(target.State).HitFaceFrom(shooter.State.Position);
		target.State.CurrentShieldPoints[hitFace] = 6;

		var action = new HighVelocityRodAction(shooter.State.Id, ESpatialOrientation.Forward);
		BattleEffectTestKit.ApplyEffects(
			action,
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(shooter.State.Id),
			[
				new DamageUnitEffect(
					EImpactCause.HighVelocityRod,
					target.State.Id,
					shooter.State.Position,
					3) { ShieldDamageBonusPercent = 33 },
			]);

		Assert.Equal(3, target.State.CurrentShieldPoints[hitFace]);
	}

	[Fact]
	public void Apply_DodgeThisBonusIsAddedBeforeReduction()
	{
		var source = BattleTestFixture.Player(new Coord(5, 5, 5));
		var target = Factory.Create(
			ShipInstance.FromCatalog("ally", EType.Fighter),
			ETeam.Player,
			new Coord(7, 5, 5),
			BattleAgentFactory.Create(EBattleAgentKind.Player));
		var distantEnemyA = BattleTestFixture.RepurposedMiner(new Coord(0, 5, 5), "distant-a");
		var distantEnemyB = BattleTestFixture.RepurposedMiner(new Coord(0, 1, 5), "distant-b");
		var pilots = new Dictionary<string, PilotBattleSnapshot?>(StringComparer.Ordinal)
		{
			[source.State.Id] = PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.DodgeThis]),
			[target.State.Id] = PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.InTheFray]),
		};
		var battle = BeginBattle(
			source,
			[target, distantEnemyA, distantEnemyB],
			[target.State.Id],
			pilots);
		var hitFace = BodyFrame.From(target.State).HitFaceFrom(source.State.Position);
		target.State.CurrentShieldPoints[hitFace] = 6;

		ApplyDamage(source, target, battle, damage: 2);

		Assert.Equal(4, target.State.CurrentShieldPoints[hitFace]);
	}

	[Fact]
	public void Apply_DefensiveManeuversStacksWithInTheFray()
	{
		var target = BattleTestFixture.Player(new Coord(5, 5, 5));
		var shooter = BattleTestFixture.RepurposedMiner(new Coord(9, 5, 5), "shooter");
		var distantEnemyA = BattleTestFixture.RepurposedMiner(new Coord(0, 5, 5), "distant-a");
		var distantEnemyB = BattleTestFixture.RepurposedMiner(new Coord(10, 5, 5), "distant-b");
		var pilots = new Dictionary<string, PilotBattleSnapshot?>(StringComparer.Ordinal)
		{
			[target.State.Id] = PilotBattleSnapshot.FromSkills(
				"pilot",
				[EPilotSkillId.InTheFray, EPilotSkillId.DefensiveManeuvers]),
		};
		var battle = BeginBattle(
			target,
			[shooter, distantEnemyA, distantEnemyB],
			[target.State.Id],
			pilots);
		var hitFace = BodyFrame.From(target.State).HitFaceFrom(shooter.State.Position);
		target.State.CurrentShieldPoints[hitFace] = 300;
		target.State.DefensiveManeuversMpUsed = 0;

		ApplyDamage(shooter, target, battle, damage: 100);

		Assert.Equal(240, target.State.CurrentShieldPoints[hitFace]);
	}

	[Fact]
	public void StateClone_PreservesPublishedCondition()
	{
		var state = BattleTestFixture.Player(new Coord(5, 5, 5)).State;
		state.Statuses.Add(BattleStatusMaterializer.Create(EPilotCapabilityId.InTheFray));

		Assert.Contains(
			state.Clone().Statuses,
			status => status is InTheFray);
	}

	private static BattleOrchestrator BeginBattle(
		Unit player,
		IReadOnlyList<Unit> enemies,
		IReadOnlyList<string> skilledUnitIds,
		IReadOnlyDictionary<string, PilotBattleSnapshot?>? pilotsByActorId = null)
	{
		pilotsByActorId ??= skilledUnitIds.ToDictionary(
			id => id,
			_ => (PilotBattleSnapshot?)PilotBattleSnapshot.FromSkills(
				"pilot",
				[EPilotSkillId.InTheFray]),
			StringComparer.Ordinal);
		return BattleTestFixture.BeginSimulation(player, enemies, pilotsByActorId: pilotsByActorId);
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
