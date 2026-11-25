using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class AnnounceAreaDamageEffect(AreaDamageFacts facts) : IEffect<BattleWorld, ActorRuntime>
{
	public AreaDamageFacts Facts => facts;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId) =>
		[new Record<AreaDamageFacts>(facts)];

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
	}
}
