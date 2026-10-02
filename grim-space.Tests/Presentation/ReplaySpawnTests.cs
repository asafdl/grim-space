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
	public void ActionLog_FormatsRepurposedMinerDeploy()
	{
		ITimelineEntry[] history =
		[
			new SpawnRepurposedMinerAction("carrier-a", ESpatialOrientation.Ventral, "repurposed-miner-b"),
			new Record<SpawnFacts>(new SpawnFacts(
				"carrier-a",
				"repurposed-miner-b",
				EType.RepurposedMiner,
				State.FromShipInstance(ShipInstance.FromCatalog("repurposed-miner-b", EType.RepurposedMiner), Coord.Zero))),
		];

		var lines = ActionLog.Format(history, id => id switch
		{
			"carrier-a" => "enemy carrier-a",
			"repurposed-miner-b" => "enemy repurposed-miner-b",
			_ => id,
		});

		AssertEntry(lines, "Deploy Repurposed Miner", "enemy carrier-a → enemy repurposed-miner-b");
	}

	[Fact]
	public void ActionLog_UsesSpawnedUnitIdWhenPresent()
	{
		ITimelineEntry[] history =
			[new SpawnRepurposedMinerAction("carrier-a", ESpatialOrientation.Ventral, "repurposed-miner-b")];

		var lines = ActionLog.Format(history, id => $"enemy {id}");

		AssertEntry(lines, "Deploy Repurposed Miner", "enemy carrier-a → enemy repurposed-miner-b");
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
