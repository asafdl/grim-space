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
			new MoveStepAction("patrol-a", ESpatialOrientation.Forward),
			new MoveStepAction("patrol-a", ESpatialOrientation.Forward),
			new MoveStepAction("patrol-a", ESpatialOrientation.Forward),
		];

		var lines = ActionLog.Format(history, id => $"enemy {id}");

		Assert.Equal(["enemy patrol-a moved 3 steps"], lines);
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

		Assert.Equal(
			[
				"enemy patrol-a shot railgun → Hit player fighter-b at dorsal for 2 shield damage and 1 hull damage",
			],
			lines);
	}

	[Fact]
	public void FormatsFlakMiss()
	{
		ITimelineEntry[] history = [new FlakAction("fighter-a", GrimSpace.Battle.Abilities.EFlakMount.Port)];

		var lines = ActionLog.Format(history, id => $"player {id}");

		Assert.Equal(["player fighter-a fired flak from port → Miss!"], lines);
	}

	[Fact]
	public void SkipsSystemNoise()
	{
		ITimelineEntry[] history =
		[
			new EndOfPhaseAction("patrol-a"),
			new RoundUpkeepAction("patrol-a"),
			new ClearTurnHazardsAction(),
			new FuelBurnAction("patrol-a"),
			new Record<SpawnFacts>(new SpawnFacts("patrol-a", "torpedo-x", GrimSpace.Units.Enums.EType.Torpedo)),
		];

		Assert.Empty(ActionLog.Format(history, id => id));
	}

	[Fact]
	public void InsertsBlankLineBetweenActors()
	{
		ITimelineEntry[] history =
		[
			new MoveStepAction("fighter-a", ESpatialOrientation.Forward),
			new EndOfPhaseAction("fighter-a"),
			new MoveStepAction("patrol-b", ESpatialOrientation.Forward),
			new MoveStepAction("patrol-b", ESpatialOrientation.Forward),
		];

		var lines = ActionLog.Format(history, id => id);

		Assert.Equal(
			[
				"fighter-a moved 1 step",
				"",
				"patrol-b moved 2 steps",
			],
			lines);
	}

	[Fact]
	public void SummarizesMoveTurnInterleaveAsMovedSteps()
	{
		ITimelineEntry[] history =
		[
			new MoveStepAction("fighter-a", ESpatialOrientation.Forward),
			new HeadingTurnAction("fighter-a", GrimSpace.Battle.Movement.Enums.EHeadingTurn.YawRight),
			new MoveStepAction("fighter-a", ESpatialOrientation.Forward),
			new HeadingTurnAction("fighter-a", GrimSpace.Battle.Movement.Enums.EHeadingTurn.YawLeft),
			new MoveStepAction("fighter-a", ESpatialOrientation.Forward),
			new MoveStepAction("fighter-a", ESpatialOrientation.Forward),
		];

		var lines = ActionLog.Format(history, id => id);

		Assert.Equal(["fighter-a moved 4 steps"], lines);
	}

	[Fact]
	public void ImpactBreaksMoveSummary()
	{
		ITimelineEntry[] history =
		[
			new MoveStepAction("fighter-a", ESpatialOrientation.Forward),
			new MoveStepAction("fighter-a", ESpatialOrientation.Forward),
			new Record<ImpactFacts>(new ImpactFacts(
				SourceId: "hazard",
				TargetId: "fighter-a",
				Cause: EHazardKind.MissileZone,
				Face: ESpatialOrientation.Forward,
				ShieldDamage: 1,
				HullDamage: 0,
				MomentumLoss: 0)),
			new MoveStepAction("fighter-a", ESpatialOrientation.Forward),
		];

		var lines = ActionLog.Format(history, id => id);

		Assert.Equal(
			[
				"fighter-a moved 2 steps",
				"hit fighter-a at forward for 1 shield damage",
				"fighter-a moved 1 step",
			],
			lines);
	}
}
