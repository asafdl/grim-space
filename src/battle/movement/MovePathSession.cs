using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Movement;

public readonly record struct MoveCheckpoint(Coord Position, GridBasis Basis);

public sealed record MovePathSession(
	string ActorId,
	IReadOnlyList<IAction> Steps,
	IReadOnlyList<MoveCheckpoint> Checkpoints,
	int ExtensionApCost,
	int ExtensionMpCost,
	int RemainingAp,
	int RemainingMp,
	State ResultState)
{
	public IReadOnlyList<Coord> Cells => Checkpoints.Skip(1).Select(checkpoint => checkpoint.Position).ToList();
	public Coord EndPosition => ResultState.Position;
	public GridBasis EndBasis =>
		GridBasis.From(ResultState.Fore, ResultState.Dorsal, ResultState.Starboard);
	public int PathApSpent => ExtensionApCost;
	public bool CanEndPath => true;
}
