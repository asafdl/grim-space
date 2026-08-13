using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Core;
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
	public void Classify_SystemActor_ReturnsUpkeepPhase()
	{
		Assert.Equal(
			EReplayPlaybackPhase.Upkeep,
			ReplayActorPhase.Classify(EntityIds.System, new Dictionary<string, ETeam>()));
	}

	[Fact]
	public void Classify_UnknownActor_ReturnsUpkeepPhase()
	{
		Assert.Equal(
			EReplayPlaybackPhase.Upkeep,
			ReplayActorPhase.Classify("missing", new Dictionary<string, ETeam>()));
	}
}
