using GrimSpace.Battle.World;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Objectives;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed class PresentationFrame
{
	public required EPlayerMode Mode { get; init; }
	public Unit? ActiveUnit { get; init; }
	public required IReadOnlyList<MovePathSession> MovePaths { get; init; }
	public int MovePathApBaseline { get; init; }
	public required BattleWorld PreviewWorld { get; init; }
	public required State ActorState { get; init; }
	public required IReadOnlySet<Coord> ValidFlakPortCells { get; init; }
	public required IReadOnlySet<Coord> ValidFlakStarboardCells { get; init; }
	public required IReadOnlySet<Coord> FlakPreviewCells { get; init; }
	public required IReadOnlySet<Coord> ValidFlakPickCells { get; init; }
	public required IReadOnlySet<Coord> RailgunCells { get; init; }
	public required IReadOnlySet<Coord> RailgunPreviewCells { get; init; }
	public required IReadOnlySet<Coord> TorpedoMountCells { get; init; }
	public required IReadOnlyList<IReadOnlySet<Coord>> TorpedoEnvelopeLayers { get; init; }
	public required IReadOnlyList<Coord> MovePath { get; init; }
	public Coord? MoveTarget { get; init; }
	public int TurnNumber { get; init; }
	public bool CanAct { get; init; }
	public bool CanFocusCamera { get; init; }
	public bool CanUndo { get; init; }
	public bool FlakAvailable { get; init; }
	public bool RailgunAvailable { get; init; }
	public bool TorpedoAvailable { get; init; }
	public bool ShowOutcomeOverlay { get; init; }
	public EBattleResult Outcome { get; init; }
	public required IReadOnlyList<string> ActionLogLines { get; init; }
}
