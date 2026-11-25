using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public readonly record struct ThirstForScrapChangedFacts(string ActorId, int Stacks);

public sealed class GainThirstForScrapStackEffect(string unitId) : IEffect<BattleWorld, ActorRuntime>
{
	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var status = world.StateOf(unitId).StatusOf<ThirstForScrap>()
			?? throw new InvalidOperationException($"Actor '{unitId}' has no ThirstForScrap status.");
		status.Stacks++;
		return [new Record<ThirstForScrapChangedFacts>(new(unitId, status.Stacks))];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId) =>
		world.StateOf(unitId).StatusOf<ThirstForScrap>()!.Stacks--;
}
