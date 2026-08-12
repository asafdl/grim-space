using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Player;

public sealed record MovePathOption(
	IReadOnlyList<Coord> Cells,
	Coord EndPosition,
	int ExtensionApCost,
	IReadOnlyList<ESpatialOrientation> Directions);
