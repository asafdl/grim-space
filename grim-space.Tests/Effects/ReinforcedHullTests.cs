using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Run.Persistence;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Effects;

[BattleTestSuite]
public sealed class ReinforcedHullTests
{
	[Fact]
	public void Apply_ReducesHullDamageOnly()
	{
		var target = BattleTestFixture.Player(new Coord(5, 5, 5));
		var shooter = BattleTestFixture.RepurposedMiner(new Coord(6, 5, 5), "shooter");
		var battle = BeginBattle(target, [shooter], [target.State.Id]);
		var hitFace = BodyFrame.From(target.State).HitFaceFrom(shooter.State.Position);
		target.State.CurrentShieldPoints[hitFace] = 0;
		var hullBefore = target.State.CurrentHullPoints;

		ApplyDamage(shooter, target, battle, damage: 9);

		Assert.Equal(hullBefore - 6, target.State.CurrentHullPoints);
	}

	[Fact]
	public void Apply_DoesNotReduceShieldDamage()
	{
		var target = BattleTestFixture.Player(new Coord(5, 5, 5));
		var shooter = BattleTestFixture.RepurposedMiner(new Coord(6, 5, 5), "shooter");
		var battle = BeginBattle(target, [shooter], [target.State.Id]);
		var hitFace = BodyFrame.From(target.State).HitFaceFrom(shooter.State.Position);
		target.State.CurrentShieldPoints[hitFace] = 10;

		ApplyDamage(shooter, target, battle, damage: 6);

		Assert.Equal(4, target.State.CurrentShieldPoints[hitFace]);
	}

	[Fact]
	public void Apply_WithoutSkillUsesFullHullDamage()
	{
		var target = BattleTestFixture.Player(new Coord(5, 5, 5));
		var shooter = BattleTestFixture.RepurposedMiner(new Coord(6, 5, 5), "shooter");
		var battle = BattleTestFixture.BeginSimulation(
			target,
			[shooter],
			pilotsByActorId: new Dictionary<string, PilotBattleSnapshot?>(StringComparer.Ordinal));
		var hitFace = BodyFrame.From(target.State).HitFaceFrom(shooter.State.Position);
		target.State.CurrentShieldPoints[hitFace] = 0;
		var hullBefore = target.State.CurrentHullPoints;

		ApplyDamage(shooter, target, battle, damage: 9);

		Assert.Equal(hullBefore - 9, target.State.CurrentHullPoints);
	}

	[Fact]
	public void BattleUnitStateDto_PreservesReinforcedHullStatus()
	{
		var state = BattleTestFixture.Player(new Coord(0, 0, 0)).State;
		state.Statuses.Add(BattleStatusMaterializer.Create(EPilotCapabilityId.ReinforcedHull));

		var dto = SaveDtoMapper.CaptureBattleUnitState(state);
		var restored = SaveDtoMapper.RestoreBattleUnitState(dto);

		Assert.Contains(restored.Statuses, status => status is ReinforcedHull);
		Assert.Contains(state.Clone().Statuses, status => status is ReinforcedHull);
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
				[EPilotSkillId.ReinforcedHull]),
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
