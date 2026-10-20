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
	public void Apply_AlliedDamageIsHalvedAndRoundedUp()
	{
		var (source, target, battle, hitFace) = CreateBattle(ETeam.Player, ETeam.Player);
		target.State.ShieldPoints[hitFace] = 6;

		ApplyDamage(source, target, battle, damage: 3);

		Assert.True(target.State.InTheFrayActive);
		Assert.Equal(4, target.State.ShieldPoints[hitFace]);
	}

	[Fact]
	public void Apply_EnemyDamageIsNotReduced()
	{
		var (source, target, battle, hitFace) = CreateBattle(ETeam.Enemy, ETeam.Player);
		target.State.ShieldPoints[hitFace] = 6;

		ApplyDamage(source, target, battle, damage: 3);

		Assert.Equal(3, target.State.ShieldPoints[hitFace]);
	}

	[Fact]
	public void Apply_SelfDamageIsNotReduced()
	{
		var source = BattleTestFixture.Player(new Coord(5, 5, 5));
		var bystander = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var pilots = new Dictionary<string, PilotBattleSnapshot?>(StringComparer.Ordinal)
		{
			[source.State.Id] = PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.InTheFray]),
		};
		var battle = BattleTestFixture.BeginSimulation(
			source,
			bystander,
			pilotsByActorId: pilots);
		var hitFace = BodyFrame.From(source.State).HitFaceFrom(source.State.Position);
		source.State.ShieldPoints[hitFace] = 6;

		ApplyDamage(source, source, battle, damage: 3);

		Assert.Equal(3, source.State.ShieldPoints[hitFace]);
	}

	[Fact]
	public void Apply_ShieldDamageBonusIsIncludedInReduction()
	{
		var (source, target, battle, hitFace) = CreateBattle(ETeam.Player, ETeam.Player);
		target.State.ShieldPoints[hitFace] = 6;

		ApplyDamage(source, target, battle, damage: 3, shieldDamageBonus: 1);

		Assert.Equal(4, target.State.ShieldPoints[hitFace]);
	}

	[Fact]
	public void Apply_DodgeThisBonusIsAddedBeforeReduction()
	{
		var (source, target, battle, hitFace) = CreateBattle(ETeam.Player, ETeam.Player);
		target.State.ShieldPoints[hitFace] = 6;

		ApplyDamage(source, target, battle, damage: 2, adjacentTargetDamageBonus: 2);

		Assert.Equal(4, target.State.ShieldPoints[hitFace]);
	}

	[Fact]
	public void Apply_DeflectionRunsAfterFriendlyFireReduction()
	{
		var (source, target, battle, hitFace) = CreateBattle(
			ETeam.Enemy,
			ETeam.Enemy,
			targetId: "enemy");
		target.State.ShieldPoints[hitFace] = 6;
		target.State.DefensiveManeuversMpSpentLastTurn = 0;
		target.State.DefensiveManeuversRollIndex = 6;

		ApplyDamage(source, target, battle, damage: 4);

		Assert.Equal(5, target.State.ShieldPoints[hitFace]);
	}

	[Fact]
	public void StateClone_PreservesPublishedCondition()
	{
		var state = BattleTestFixture.Player(new Coord(5, 5, 5)).State;
		state.InTheFrayActive = true;

		Assert.True(state.Clone().InTheFrayActive);
	}

	private static void ApplyDamage(
		Unit source,
		Unit target,
		BattleOrchestrator battle,
		int damage,
		int shieldDamageBonus = 0,
		int adjacentTargetDamageBonus = 0)
	{
		var effect = new ApplyAreaDamageEffect(
			EImpactCause.LightningCannonBurst,
			new HashSet<Coord> { target.State.Position },
			damage,
			source.State.Position,
			shieldDamageBonus,
			adjacentTargetDamageBonus);
		effect.Apply(
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(source.State.Id),
			source.State.Id);
	}

	private static (
		Unit Source,
		Unit Target,
		BattleOrchestrator Battle,
		ESpatialOrientation HitFace) CreateBattle(
			ETeam sourceTeam,
			ETeam targetTeam,
			string targetId = "target")
	{
		var source = Factory.Create(
			ShipInstance.FromCatalog("source", EType.Fighter),
			sourceTeam,
			new Coord(5, 5, 5),
			BattleAgentFactory.Create(EBattleAgentKind.Player));
		var target = Factory.Create(
			ShipInstance.FromCatalog(targetId, EType.Fighter),
			targetTeam,
			new Coord(6, 5, 5),
			BattleAgentFactory.Create(EBattleAgentKind.Ai));
		var pilots = new Dictionary<string, PilotBattleSnapshot?>(StringComparer.Ordinal)
		{
			[target.State.Id] = PilotBattleSnapshot.FromSkills("pilot", [EPilotSkillId.InTheFray]),
		};
		var battle = BattleTestFixture.BeginSimulation(
			source,
			target,
			pilotsByActorId: pilots);
		var hitFace = BodyFrame.From(target.State).HitFaceFrom(source.State.Position);
		return (source, target, battle, hitFace);
	}
}
