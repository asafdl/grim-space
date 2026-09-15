using GrimSpace.Battle.Movement;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Player;

public sealed record MovePathOption(
	IReadOnlyList<IAction> Steps,
	IReadOnlyList<MoveCheckpoint> Checkpoints,
	Coord EndPosition,
	GridBasis EndBasis,
	int ExtensionApCost,
	int RemainingAp,
	UnitDisplayState ResultState)
{
	public IReadOnlyList<Coord> Cells =>
		Checkpoints.Skip(1).Select(checkpoint => checkpoint.Position).ToList();
}
