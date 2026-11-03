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

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class MarkWeakspotTests
{
	[Theory]
	[InlineData(5, 0, 0, true)]
	[InlineData(6, 0, 0, true)]
	[InlineData(7, 0, 0, false)]
	[InlineData(2, 2, 2, true)]
	[InlineData(3, 2, 2, false)]
	public void Range_UsesSixCellManhattanLimitForPicksDiscoveryAndLegality(
		int x, int y, int z, bool inRange)
	{
		var (player, enemy, battle) = Battle();
		using var cleanup = battle;
		enemy.State.Position = player.State.Position + new Coord(x, y, z);
		var world = battle.Engine.World;
		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);
		var action = MarkWeakspotDef.Instance.Bind(
			player.State.Id, enemy.State.Id, ESpatialOrientation.Forward);

		Assert.Equal(inRange, MarkWeakspotDef.TargetPick.LegalTargets(world, player.State.Id, [])
			.Contains(enemy.State.Id));
		Assert.Equal(inRange, MarkWeakspotDef.Instance.IsLegal(action, world, runtime));
		var discovered = MarkWeakspotDef.Instance.Discover(world, runtime, player.State.Id).ToArray();
		Assert.Equal(inRange ? 6 : 0, discovered.Length);
		Assert.All(discovered, candidate =>
			Assert.Equal(enemy.State.Id, Assert.IsType<MarkWeakspotAction>(candidate).TargetId));
	}

	[Fact]
	public void Apply_MarksChosenFaceAndExpiresAfterDuration()
	{
		var (player, enemy, battle) = Battle();
		var runtime = battle.Engine.ActorRuntimes.For(player.State.Id);

		Assert.True(MarkWeakspotDef.Instance.IsLegal(
			MarkWeakspotDef.Instance.Bind(player.State.Id, enemy.State.Id, ESpatialOrientation.Forward),
			battle.Engine.World,
			runtime));

		foreach (var effect in MarkWeakspotDef.Instance.Resolve(
			MarkWeakspotDef.Instance.Bind(player.State.Id, enemy.State.Id, ESpatialOrientation.Forward),
			battle.Engine.World,
			runtime))
		{
			effect.Apply(battle.Engine.World, runtime, player.State.Id);
		}

		Assert.True(MarkWeakspot.IsActive(enemy.State));
		Assert.Equal(ESpatialOrientation.Forward, MarkWeakspot.TryPayload(enemy.State)!.Face);
		Assert.Equal(
			PilotSkillPercents.MarkWeakspot.CooldownTurns,
			player.State.MarkWeakspotCooldownRemaining);

		BattleTestApply.CommitActivationRoundEndPasses(
			battle,
			player.State.Id,
			PilotSkillPercents.MarkWeakspot.DurationEndPasses);

		Assert.False(MarkWeakspot.IsActive(enemy.State));
	}

	[Fact]
	public void IncomingHit_AddsShieldBonusOnlyOnMarkedFace()
	{
		var (player, enemy, battle) = Battle();
		var hitFace = BodyFrame.From(enemy.State).HitFaceFrom(player.State.Position);
		enemy.State.Statuses.Add(BattleStatuses.CreateMarkWeakspot(enemy.State.Id, hitFace));
		enemy.State.CurrentShieldPoints[hitFace] = 200;

		var effect = new ApplyAreaDamageEffect(
			EImpactCause.LightningCannonBurst,
			new HashSet<Coord> { enemy.State.Position },
			100,
			player.State.Position);
		effect.Apply(
			battle.Engine.World,
			battle.Engine.ActorRuntimes.For(player.State.Id),
			player.State.Id);

		Assert.Equal(80, enemy.State.CurrentShieldPoints[hitFace]);
	}

	private static (Unit Player, Unit Enemy, BattleOrchestrator Battle) Battle()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));
		var enemy = BattleTestFixture.Enemy(new Coord(6, 5, 5));
		var pilots = new Dictionary<string, PilotBattleSnapshot?>(StringComparer.Ordinal)
		{
			[player.State.Id] = PilotBattleSnapshot.FromSkills(
				"pilot",
				[EPilotSkillId.MarkWeakspot]),
		};
		var battle = BattleTestFixture.BeginSimulation(player, enemy, pilotsByActorId: pilots);
		return (player, enemy, battle);
	}
}
