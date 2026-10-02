using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class ActionLogTests
{
	[Fact]
	public void AggregatesConsecutiveMoveSteps()
	{
		ITimelineEntry[] history =
		[
			new MoveStepAction("repurposed-miner-a"),
			new MoveStepAction("repurposed-miner-a"),
			new MoveStepAction("repurposed-miner-a"),
		];

		var lines = ActionLog.Format(history, id => $"enemy {id}");

		AssertEntries(lines, "Move · 3 steps|enemy repurposed-miner-a");
	}

	[Fact]
	public void AggregatesCombinedManeuverSteps()
	{
		ITimelineEntry[] history =
		[
			new HeadingTurnAction("repurposed-miner-a", GrimSpace.Battle.Movement.Enums.EHeadingTurn.YawRight),
			new MoveStepAction("repurposed-miner-a"),
			new RollAction("repurposed-miner-a", GrimSpace.Battle.Movement.Enums.ERollDirection.Clockwise),
			new MoveStepAction("repurposed-miner-a"),
		];

		var lines = ActionLog.Format(history, id => $"enemy {id}");

		AssertEntries(lines, "Move · 2 steps|enemy repurposed-miner-a");
	}

	[Fact]
	public void FormatsLightningCannonHit()
	{
		ITimelineEntry[] history =
		[
			new LightningCannonAction("repurposed-miner-a"),
			new Record<ImpactFacts>(new ImpactFacts(
				SourceId: "repurposed-miner-a",
				TargetId: "fighter-b",
				Cause: EHazardKind.LightningCannonBurst,
				Face: ESpatialOrientation.Dorsal,
				ShieldDamage: 2,
				HullDamage: 1)),
		];

		var lines = ActionLog.Format(history, id => id switch
		{
			"repurposed-miner-a" => "enemy repurposed-miner-a",
			"fighter-b" => "player fighter-b",
			_ => id,
		});

		AssertEntries(
			lines,
			"Lightning Cannon · Hit|enemy repurposed-miner-a → player fighter-b|dorsal · 2 shield · 1 hull");
	}

	[Fact]
	public void FormatsScrapDroneSwarmMiss()
	{
		ITimelineEntry[] history = [new ScrapDroneSwarmAction("fighter-a", ESpatialOrientation.Port)];

		var lines = ActionLog.Format(history, id => $"player {id}");

		AssertEntries(
			lines,
			"Scrap Drone Swarm · Miss|player fighter-a|port mount");
	}

	[Fact]
	public void SkipsSystemNoise()
	{
		ITimelineEntry[] history =
		[
			new EndOfPhaseAction("repurposed-miner-a"),
			new RoundUpkeepAction("repurposed-miner-a"),
			new FuelBurnAction("repurposed-miner-a"),
			new Record<SpawnFacts>(new SpawnFacts(
				"repurposed-miner-a",
				"torpedo-x",
				EType.VoidBomb,
				State.FromShipInstance(ShipInstance.FromCatalog("torpedo-x", EType.VoidBomb), Coord.Zero))),
		];

		Assert.Empty(ActionLog.Format(history, id => id));
	}

	[Fact]
	public void PreservesActionOrderWithoutSpacerEntries()
	{
		ITimelineEntry[] history =
		[
			new MoveStepAction("fighter-a"),
			new EndOfPhaseAction("fighter-a"),
			new MoveStepAction("repurposed-miner-b"),
			new MoveStepAction("repurposed-miner-b"),
		];

		var lines = ActionLog.Format(history, id => id);

		AssertEntries(
			lines,
			"Move · 1 step|fighter-a",
			"Move · 2 steps|repurposed-miner-b");
	}

	[Fact]
	public void CombinedOrientationChangesRemainInMoveSummary()
	{
		ITimelineEntry[] history =
		[
			new MoveStepAction("fighter-a"),
			new HeadingTurnAction("fighter-a", GrimSpace.Battle.Movement.Enums.EHeadingTurn.YawRight),
			new MoveStepAction("fighter-a"),
			new RollAction("fighter-a", GrimSpace.Battle.Movement.Enums.ERollDirection.Clockwise),
			new MoveStepAction("fighter-a"),
			new MoveStepAction("fighter-a"),
		];

		var lines = ActionLog.Format(history, id => id);

		AssertEntries(lines, "Move · 4 steps|fighter-a");
	}

	[Fact]
	public void ImpactBreaksMoveSummary()
	{
		ITimelineEntry[] history =
		[
			new MoveStepAction("fighter-a"),
			new MoveStepAction("fighter-a"),
			new Record<ImpactFacts>(new ImpactFacts(
				SourceId: "hazard",
				TargetId: "fighter-a",
				Cause: EHazardKind.ScrapDroneSwarmBurst,
				Face: ESpatialOrientation.Forward,
				ShieldDamage: 1,
				HullDamage: 0)),
			new MoveStepAction("fighter-a"),
		];

		var lines = ActionLog.Format(history, id => id);

		AssertEntries(
			lines,
			"Move · 2 steps|fighter-a",
			"Impact · Scrap Drone Swarm|hazard → fighter-a|forward · 1 shield",
			"Move · 1 step|fighter-a");
	}

	private static void AssertEntries(
		IReadOnlyList<ActionLog.Entry> entries,
		params string[] expected) =>
		Assert.Equal(
			expected,
			entries.Select(entry => string.Join('|', entry.Metadata.Prepend(entry.Title))));
}
