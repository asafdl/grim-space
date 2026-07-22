using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Battle.Effects;

public sealed class BeginMovePathEffect : IEffect<BattleSlices>
{
	public void Apply(State actor, TurnPhaseContext phaseContext)
	{
		phaseContext.MinPathApCost = TurnPhaseContext.InitialMinPathApCost;
		phaseContext.PathApSpent = 0;
		phaseContext.PathForwardSteps = 0;
		phaseContext.UsedDirectionsMask = 0;
		phaseContext.MoveStartMomentumLevel = actor.MomentumLevel;
		phaseContext.MovementBuildupLevel = actor.MomentumLevel;
		phaseContext.MovementBuildupForwardSteps = 0;
	}

	void IEffect<BattleSlices>.Apply(BattleSlices slices) =>
		Apply(slices.Ap.Player, slices.PhaseContext);
}
