using GrimSpace.Battle;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Spatial;
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
		target.State.ShieldPoints[hitFace] = 0;
		var hullBefore = target.State.HullPoints;

		ApplyDamage(shooter, target, battle, damage: 9);

		Assert.Equal(hullBefore - 6, target.State.HullPoints);
	}

	[Fact]
	public void Apply_DoesNotReduceShieldDamage()
	{
		var target = BattleTestFixture.Player(new Coord(5, 5, 5));
		var shooter = BattleTestFixture.RepurposedMiner(new Coord(6, 5, 5), "shooter");
		var battle = BeginBattle(target, [shooter], [target.State.Id]);
		var hitFace = BodyFrame.From(target.State).HitFaceFrom(shooter.State.Position);
		target.State.ShieldPoints[hitFace] = 10;

		ApplyDamage(shooter, target, battle, damage: 6);

		Assert.Equal(4, target.State.ShieldPoints[hitFace]);
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
		target.State.ShieldPoints[hitFace] = 0;
		var hullBefore = target.State.HullPoints;

		ApplyDamage(shooter, target, battle, damage: 9);

		Assert.Equal(hullBefore - 9, target.State.HullPoints);
	}

	[Fact]
	public void BattleUnitStateDto_PreservesReinforcedHullActive()
	{
		var state = BattleTestFixture.Player(new Coord(0, 0, 0)).State;
		state.ReinforcedHullActive = true;

		var dto = SaveDtoMapper.CaptureBattleUnitState(state);
		var restored = SaveDtoMapper.RestoreBattleUnitState(dto);

		Assert.True(restored.ReinforcedHullActive);
		Assert.True(state.Clone().ReinforcedHullActive);
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
