using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Presentation;

[BattleTestSuite]
public sealed class ReplayIdentityTests
{
	[Fact]
	public void Classify_PlayerActor_ReturnsPlayerPhase()
	{
		var participants = new Dictionary<string, ETeam> { ["fighter-a"] = ETeam.Player };

		Assert.Equal(EReplayPlaybackPhase.Player, ReplayActorPhase.Classify("fighter-a", participants));
	}

	[Fact]
	public void Classify_EnemyActors_ReturnsEnemyPhase()
	{
		var participants = new Dictionary<string, ETeam>
		{
			["carrier-a"] = ETeam.Enemy,
			["repurposed-miner-b"] = ETeam.Enemy,
		};

		Assert.Equal(EReplayPlaybackPhase.Enemy, ReplayActorPhase.Classify("carrier-a", participants));
		Assert.Equal(EReplayPlaybackPhase.Enemy, ReplayActorPhase.Classify("repurposed-miner-b", participants));
	}

	[Fact]
	public void Classify_RulesActor_ReturnsSystemPhase()
	{
		Assert.Equal(
			EReplayPlaybackPhase.System,
			ReplayActorPhase.Classify(BattleActorIds.Rules, new Dictionary<string, ETeam>()));
	}

	[Fact]
	public void Classify_TerrainActor_ReturnsSystemPhase()
	{
		Assert.Equal(
			EReplayPlaybackPhase.System,
			ReplayActorPhase.Classify(BattleActorIds.Terrain, new Dictionary<string, ETeam>()));
	}

	[Fact]
	public void Classify_UnknownActor_Throws()
	{
		Assert.Throws<KeyNotFoundException>(
			() => ReplayActorPhase.Classify("missing", new Dictionary<string, ETeam>()));
	}

	[Fact]
	public void ImpactInterestUsesTargetWhenSourceIsNotAUnit()
	{
		var target = State.FromShipInstance(
			ShipInstance.FromCatalog("fighter-a", EType.Fighter),
			new Coord(2, 3, 4));
		var replayState = new ReplayState(new Dictionary<string, State>
		{
			[target.Id] = target,
		});
		var impact = new ImpactFacts(
			SourceId: "terrain",
			TargetId: target.Id,
			Cause: EImpactCause.ScrapDroneSwarmBurst,
			Face: ESpatialOrientation.Forward,
			ShieldDamage: 1,
			HullDamage: 0);

		var points = TurnReplayPlayer.UnitInterestPoints(replayState, impact.SourceId, impact.TargetId);

		Assert.Equal([WorldMapping.ToWorld(target.Position)], points);
	}

	[Fact]
	public void HealingInterestIncludesHealerAndAlly()
	{
		var healer = State.FromShipInstance(
			ShipInstance.FromCatalog("healer", EType.Fighter),
			new Coord(2, 3, 4));
		var ally = State.FromShipInstance(
			ShipInstance.FromCatalog("ally", EType.Fighter),
			new Coord(3, 3, 4));
		var replayState = new ReplayState(new Dictionary<string, State>
		{
			[healer.Id] = healer,
			[ally.Id] = ally,
		});
		var heal = new HealFacts(
			healer.Id, ally.Id, EImpactCause.ScrapDroneSwarmBurst,
			ESpatialOrientation.Forward, 100, 0);

		var points = TurnReplayPlayer.UnitInterestPoints(replayState, heal.SourceId, heal.TargetId);

		Assert.Equal(
			[WorldMapping.ToWorld(healer.Position), WorldMapping.ToWorld(ally.Position)],
			points);
	}

	[Fact]
	public void ApplyHealRestoresRecordedShieldAndHullWithoutChangingSourceState()
	{
		var ally = State.FromShipInstance(
			ShipInstance.FromCatalog("ally", EType.Fighter),
			Coord.Zero);
		ally.ShieldPoints[ESpatialOrientation.Forward] -= 100;
		ally.HullPoints -= 100;
		var replayState = new ReplayState(new Dictionary<string, State>
		{
			[ally.Id] = ally,
		});
		var heal = new HealFacts(
			"healer", ally.Id, EImpactCause.ScrapDroneSwarmBurst,
			ESpatialOrientation.Forward, 100, 100);

		replayState.ApplyHeal(heal);

		var restored = replayState.StateOf(ally.Id);
		Assert.Equal(ally.ShieldPoints[ESpatialOrientation.Forward] + 100,
			restored.ShieldPoints[ESpatialOrientation.Forward]);
		Assert.Equal(ally.HullPoints + 100, restored.HullPoints);
	}

	[Fact]
	public void ImpactTotalDamageIncludesShieldAndHull()
	{
		var impact = new ImpactFacts(
			SourceId: "enemy",
			TargetId: "fighter-a",
			Cause: EImpactCause.LightningCannonBurst,
			Face: ESpatialOrientation.Forward,
			ShieldDamage: 2,
			HullDamage: 1);

		Assert.Equal(3, impact.TotalDamage);
	}

	[Fact]
	public void ApplyMoveUsesQueuedBodyRelativeDirection()
	{
		var state = State.FromShipInstance(
			ShipInstance.FromCatalog("fighter-a", EType.Fighter),
			new Coord(2, 3, 4));
		var replayState = new ReplayState(new Dictionary<string, State>
		{
			[state.Id] = state,
		});

		replayState.ApplyMove(new MoveStepAction(state.Id, ESpatialOrientation.Port));

		Assert.Equal(new Coord(1, 3, 4), replayState.StateOf(state.Id).Position);
	}
}
