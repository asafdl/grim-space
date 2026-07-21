using GrimSpace.Battle.Planning;
using GrimSpace.Battle.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed class PresentationFrame
{
	public required EPlayerMode Mode { get; init; }
	public EMissileMount? MissileMount { get; init; }
	public int MissileRange { get; init; }
	public Unit? ActiveUnit { get; init; }
	public required IReadOnlyList<Option> MoveOptions { get; init; }
	public required SimulatedTurn Simulation { get; init; }
	public required IReadOnlySet<Coord> PlannedHazardCells { get; init; }
	public required IReadOnlySet<Coord> ValidMissileCells { get; init; }
	public required IReadOnlySet<Coord> MissilePreviewCells { get; init; }
	public required IReadOnlySet<Coord> ValidFlakPortCells { get; init; }
	public required IReadOnlySet<Coord> ValidFlakStarboardCells { get; init; }
	public required IReadOnlySet<Coord> FlakPreviewCells { get; init; }
	public required IReadOnlySet<Coord> ValidFlakPickCells { get; init; }
	public required IReadOnlySet<Coord> RailgunTargetCells { get; init; }
	public Coord? RailgunHoveredCell { get; init; }
	public required IReadOnlyList<Coord> MovePath { get; init; }
	public Coord? MoveTarget { get; init; }
	public bool MissileAimActive { get; init; }
	public State? MissileAimShip { get; init; }
	public required string HintText { get; init; }
	public bool CanAct { get; init; }
	public int MissilesRemaining { get; init; }
	public bool ExitMissileMode { get; init; }
	public bool FlakAvailable { get; init; }
	public bool ShowOutcomeOverlay { get; init; }
	public bool PlayerWon { get; init; }
}
