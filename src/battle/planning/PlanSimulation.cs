using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Slices;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Planning;

public sealed class PlanSimulation : Simulation<BattleBoard, TurnState, BattleActionContext, BattleSlices, IBattleAction>
{
	public PlanSimulation(Func<IReadOnlyList<IBattleAction>, IReadOnlyList<IBattleAction>>? expandPlayback = null)
		: base(BattleActionContext.For, expandPlayback)
	{
	}
}
