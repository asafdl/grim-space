using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Presentation;

[BattleTestSuite]
public sealed class ReplaySpawnTests
{
	[Fact]
	public void ActionLog_FormatsPatrolDeploy()
	{
		ITimelineEntry[] history =
		[
			new SpawnPatrolAction("carrier-a", ESpatialOrientation.Ventral, "patrol-b"),
			new Record<SpawnFacts>(new SpawnFacts(
				"carrier-a",
				"patrol-b",
				EType.Patrol,
				State.FromShipInstance(ShipInstance.FromCatalog("patrol-b", EType.Patrol), Coord.Zero))),
		];

		var lines = ActionLog.Format(history, id => id switch
		{
			"carrier-a" => "enemy carrier-a",
			"patrol-b" => "enemy patrol-b",
			_ => id,
		});

		AssertEntry(lines, "Deploy patrol", "enemy carrier-a → enemy patrol-b");
	}

	[Fact]
	public void ActionLog_UsesSpawnedUnitIdWhenPresent()
	{
		ITimelineEntry[] history =
			[new SpawnPatrolAction("carrier-a", ESpatialOrientation.Ventral, "patrol-b")];

		var lines = ActionLog.Format(history, id => $"enemy {id}");

		AssertEntry(lines, "Deploy patrol", "enemy carrier-a → enemy patrol-b");
	}

	private static void AssertEntry(
		IReadOnlyList<ActionLog.Entry> entries,
		string title,
		params string[] metadata)
	{
		var entry = Assert.Single(entries);
		Assert.Equal(title, entry.Title);
		Assert.Equal(metadata, entry.Metadata);
	}
}
