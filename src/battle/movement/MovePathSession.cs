using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Movement;

public readonly record struct MoveCheckpoint(Coord Position, GridBasis Basis);

public sealed record MovePathSession(
	string ActorId,
	IReadOnlyList<MoveStepAction> Steps,
	IReadOnlyList<MoveCheckpoint> Checkpoints,
	int RemainingAp,
	State ResultState)
{
	public IReadOnlyList<Coord> Cells => Checkpoints.Skip(1).Select(checkpoint => checkpoint.Position).ToList();
	public Coord EndPosition => Checkpoints[^1].Position;
	public GridBasis EndBasis => Checkpoints[^1].Basis;
	public int ExtensionApCost => Steps.Count;
	public int PathApSpent => ExtensionApCost;
	public bool CanEndPath => true;
}
