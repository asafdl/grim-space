using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Actions;

public sealed class ActionLogTests
{
	[Fact]
	public void AggregatesConsecutiveMoveSteps()
	{
		ITimelineEntry[] history =
		[
			new MoveStepAction("patrol-a"),
			new MoveStepAction("patrol-a"),
			new MoveStepAction("patrol-a"),
		];

		var lines = ActionLog.Format(history, id => $"enemy {id}");

		AssertEntries(lines, "Move · 3 steps|enemy patrol-a");
	}

	[Fact]
	public void AggregatesCombinedManeuverSteps()
	{
		ITimelineEntry[] history =
		[
			new MoveStepAction("patrol-a", GrimSpace.Battle.Movement.Enums.EHeadingTurn.YawRight),
			new MoveStepAction("patrol-a", Roll: GrimSpace.Battle.Movement.Enums.ERollDirection.Clockwise),
		];

		var lines = ActionLog.Format(history, id => $"enemy {id}");

		AssertEntries(lines, "Move · 2 steps|enemy patrol-a");
	}

	[Fact]
	public void FormatsRailgunHit()
	{
		ITimelineEntry[] history =
		[
			new RailgunAction("patrol-a"),
			new Record<ImpactFacts>(new ImpactFacts(
				SourceId: "patrol-a",
				TargetId: "fighter-b",
				Cause: EHazardKind.RailgunBurst,
				Face: ESpatialOrientation.Dorsal,
				ShieldDamage: 2,
				HullDamage: 1,
				MomentumLoss: 0)),
		];

		var lines = ActionLog.Format(history, id => id switch
		{
			"patrol-a" => "enemy patrol-a",
			"fighter-b" => "player fighter-b",
			_ => id,
		});

		AssertEntries(
			lines,
			"Railgun · Hit|enemy patrol-a → player fighter-b|dorsal · 2 shield · 1 hull");
	}

	[Fact]
	public void FormatsFlakMiss()
	{
		ITimelineEntry[] history = [new FlakAction("fighter-a", ESpatialOrientation.Port)];

		var lines = ActionLog.Format(history, id => $"player {id}");

		AssertEntries(lines, "Flak · Miss|player fighter-a|port mount");
	}

	[Fact]
	public void SkipsSystemNoise()
	{
		ITimelineEntry[] history =
		[
			new EndOfPhaseAction("patrol-a"),
			new RoundUpkeepAction("patrol-a"),
			new FuelBurnAction("patrol-a"),
			new Record<SpawnFacts>(new SpawnFacts("patrol-a", "torpedo-x", GrimSpace.Units.Enums.EType.Torpedo)),
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
			new MoveStepAction("patrol-b"),
			new MoveStepAction("patrol-b"),
		];

		var lines = ActionLog.Format(history, id => id);

		AssertEntries(
			lines,
			"Move · 1 step|fighter-a",
			"Move · 2 steps|patrol-b");
	}

	[Fact]
	public void CombinedOrientationChangesRemainInMoveSummary()
	{
		ITimelineEntry[] history =
		[
			new MoveStepAction("fighter-a"),
			new MoveStepAction("fighter-a", GrimSpace.Battle.Movement.Enums.EHeadingTurn.YawRight),
			new MoveStepAction("fighter-a", Roll: GrimSpace.Battle.Movement.Enums.ERollDirection.Clockwise),
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
				Cause: EHazardKind.MissileZone,
				Face: ESpatialOrientation.Forward,
				ShieldDamage: 1,
				HullDamage: 0,
				MomentumLoss: 0)),
			new MoveStepAction("fighter-a"),
		];

		var lines = ActionLog.Format(history, id => id);

		AssertEntries(
			lines,
			"Move · 2 steps|fighter-a",
			"Impact · missile zone|hazard → fighter-a|forward · 1 shield",
			"Move · 1 step|fighter-a");
	}

	private static void AssertEntries(
		IReadOnlyList<ActionLog.Entry> entries,
		params string[] expected) =>
		Assert.Equal(
			expected,
			entries.Select(entry => string.Join('|', entry.Metadata.Prepend(entry.Title))));
}
