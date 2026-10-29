using GrimSpace.Battle;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Spatial;
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
		target.State.ShieldPoints[hitFace] = 6;

		ApplyDamage(shooter, target, battle, damage: 3);

		Assert.Equal(4, target.State.ShieldPoints[hitFace]);
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
		target.State.ShieldPoints[hitFace] = 6;

		ApplyDamage(shooter, target, battle, damage: 3);

		Assert.Equal(3, target.State.ShieldPoints[hitFace]);
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
		source.State.ShieldPoints[hitFace] = 6;

		ApplyDamage(source, source, battle, damage: 3);

		Assert.Equal(4, source.State.ShieldPoints[hitFace]);
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
		target.State.ShieldPoints[hitFace] = 6;

		ApplyDamage(shooter, target, battle, damage: 3, shieldDamageBonus: 1);

		Assert.Equal(3, target.State.ShieldPoints[hitFace]);
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
		target.State.ShieldPoints[hitFace] = 6;

		ApplyDamage(
			source,
			target,
			battle,
			damage: 2,
			adjacentHitDamagePercent: 200,
			adjacentHitDamageRangeCells: 2);

		Assert.Equal(3, target.State.ShieldPoints[hitFace]);
	}

	[Fact]
	public void Apply_DeflectionRunsAfterInTheFrayReduction()
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
		target.State.ShieldPoints[hitFace] = 6;
		target.State.DefensiveManeuversMpUsed = 0;
		target.State.DefensiveManeuversRollIndex = 6;

		ApplyDamage(shooter, target, battle, damage: 3);

		Assert.Equal(5, target.State.ShieldPoints[hitFace]);
	}

	[Fact]
	public void StateClone_PreservesPublishedCondition()
	{
		var state = BattleTestFixture.Player(new Coord(5, 5, 5)).State;
		state.InTheFrayActive = true;

		Assert.True(state.Clone().InTheFrayActive);
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
		int damage,
		int shieldDamageBonus = 0,
		int shieldDamageBonusPercent = 0,
		int adjacentHitDamagePercent = 100,
		int adjacentHitDamageRangeCells = 1)
	{
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.LightningCannonBurst,
			new HashSet<Coord> { target.State.Position },
			damage,
			source.State.Position,
			shieldDamageBonus,
			shieldDamageBonusPercent,
			adjacentHitDamagePercent,
			adjacentHitDamageRangeCells);
		effect.Apply(
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(source.State.Id),
			source.State.Id);
	}
}
