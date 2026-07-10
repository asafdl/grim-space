using GrimSpace.Battle.Actions.Enums;
using GrimSpace.Battle.Grid;

namespace GrimSpace.Battle.Actions;

public sealed class ActionRequest
{
	public EActionType Type { get; init; }
	public int ApCost { get; init; }
	public Coord NewForwardDirection { get; init; }
	public Coord NewUpDirection { get; init; }
	public ELateralDirection LatDirection { get; init; }
}
