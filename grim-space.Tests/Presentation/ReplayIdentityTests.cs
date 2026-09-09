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
			["patrol-b"] = ETeam.Enemy,
		};

		Assert.Equal(EReplayPlaybackPhase.Enemy, ReplayActorPhase.Classify("carrier-a", participants));
		Assert.Equal(EReplayPlaybackPhase.Enemy, ReplayActorPhase.Classify("patrol-b", participants));
	}

	[Fact]
	public void Classify_RulesActor_ReturnsUpkeepPhase()
	{
		Assert.Equal(
			EReplayPlaybackPhase.Upkeep,
			ReplayActorPhase.Classify(BattleActorIds.Rules, new Dictionary<string, ETeam>()));
	}

	[Fact]
	public void Classify_TerrainActor_ReturnsUpkeepPhase()
	{
		Assert.Equal(
			EReplayPlaybackPhase.Upkeep,
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
		var target = State.FromSpawn(
			new Instance
			{
				Id = "fighter-a",
				Type = EType.Fighter,
				Alliance = Alliance.Player,
			},
			new Coord(2, 3, 4));
		var replayState = new ReplayState(new Dictionary<string, State>
		{
			[target.Id] = target,
		});
		var impact = new ImpactFacts(
			SourceId: "terrain",
			TargetId: target.Id,
			Cause: EHazardKind.MissileZone,
			Face: ESpatialOrientation.Forward,
			ShieldDamage: 1,
			HullDamage: 0,
			MomentumLoss: 0);

		var points = TurnReplayPlayer.ImpactInterestPoints(replayState, impact);

		Assert.Equal([WorldMapping.ToWorld(target.Position)], points);
	}

	[Fact]
	public void ApplyMomentumUsesAuthoritativeResult()
	{
		var state = State.FromSpawn(
			new Instance
			{
				Id = "fighter-a",
				Type = EType.Fighter,
				Alliance = Alliance.Player,
			},
			Coord.Zero);
		var replayState = new ReplayState(new Dictionary<string, State>
		{
			[state.Id] = state,
		});

		replayState.ApplyMomentum(new MomentumChangedFacts(state.Id, 2));

		Assert.Equal(2, replayState.StateOf(state.Id).MomentumLevel);
	}

	[Fact]
	public void ImpactTotalDamageIncludesShieldAndHull()
	{
		var impact = new ImpactFacts(
			SourceId: "enemy",
			TargetId: "fighter-a",
			Cause: EHazardKind.RailgunBurst,
			Face: ESpatialOrientation.Forward,
			ShieldDamage: 2,
			HullDamage: 1,
			MomentumLoss: 0);

		Assert.Equal(3, impact.TotalDamage);
	}
}
