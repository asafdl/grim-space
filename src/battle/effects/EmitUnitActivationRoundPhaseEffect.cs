using GrimSpace.Battle;
using GrimSpace.Battle.ActivationRound;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class EmitUnitActivationRoundPhaseEffect(
	string unitId,
	EUnitActivationPhase phase) : IEffect<BattleWorld, ActorRuntime>
{
	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId) =>
		[new Record<UnitActivationRoundPhase>(new UnitActivationRoundPhase(unitId, phase))];

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
	}
}
