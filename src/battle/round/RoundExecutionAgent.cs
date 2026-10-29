using GrimSpace.Battle;
using GrimSpace.Battle.ActivationRound;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Round;

public sealed class RoundExecutionAgent(Func<BattleWorld> world) : ExecutionAgent<BattleWorld, ActorRuntime>
{
	protected override bool PublishOnActivate => false;

	public void PlanAndPublish(string unitId, EUnitActivationPhase unitPhase)
	{
		ArgumentException.ThrowIfNullOrEmpty(unitId);
		if (!_canWork || _actorId is null)
			return;

		ClearBatchInFlight();
		var listActions = world().ActivationRoundLists.ProcessPass(unitId, unitPhase);
		if (listActions.Count == 0)
		{
			Publish([new ProcessActivationRoundAction(unitId, unitPhase)]);
			return;
		}

		var batch = new List<IAction>(listActions.Count + 1)
		{
			new ProcessActivationRoundAction(unitId, unitPhase),
		};
		batch.AddRange(listActions);
		Publish(batch);
	}
}
