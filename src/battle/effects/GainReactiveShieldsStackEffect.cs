using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public readonly record struct ReactiveShieldsChangedFacts(string ActorId, int Stacks);

public sealed class GainReactiveShieldsStackEffect(string unitId) : IEffect<BattleWorld, ActorRuntime>
{
	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var status = world.StateOf(unitId).StatusOf<ReactiveShields>()
			?? throw new InvalidOperationException($"Actor '{unitId}' has no ReactiveShields status.");
		status.Stacks++;
		return [new Record<ReactiveShieldsChangedFacts>(new(unitId, status.Stacks))];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId) =>
		world.StateOf(unitId).StatusOf<ReactiveShields>()!.Stacks--;
}
