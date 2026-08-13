using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Presentation;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Presentation;

public sealed class ReplaySpawnTests
{
	[Fact]
	public void ActionLog_FormatsPatrolDeploy()
	{
		ITimelineEntry[] history =
		[
			new SpawnPatrolAction("carrier-a"),
			new Record<SpawnFacts>(new SpawnFacts("carrier-a", "patrol-b", EType.Patrol)),
		];

		var lines = ActionLog.Format(history, id => id switch
		{
			"carrier-a" => "enemy carrier-a",
			"patrol-b" => "enemy patrol-b",
			_ => id,
		});

		Assert.Equal(["enemy carrier-a deployed enemy patrol-b"], lines);
	}

	[Fact]
	public void ActionLog_UsesSpawnedUnitIdWhenPresent()
	{
		ITimelineEntry[] history = [new SpawnPatrolAction("carrier-a", "patrol-b")];

		var lines = ActionLog.Format(history, id => $"enemy {id}");

		Assert.Equal(["enemy carrier-a deployed enemy patrol-b"], lines);
	}
}
