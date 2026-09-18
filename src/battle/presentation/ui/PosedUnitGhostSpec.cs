using Godot;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed record PosedUnitGhostSpec(
	EType Type,
	Coord Position,
	Coord Fore,
	Coord Dorsal,
	Color Tint);
