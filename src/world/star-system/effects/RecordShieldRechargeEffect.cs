using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Dockyard;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class RecordShieldRechargeEffect(ShieldRechargePurchased purchase)
	: IEffect<StarMap, ActorRuntime>
{
	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId) =>
		[new Record<ShieldRechargePurchased>(purchase)];

	public void Undo(StarMap world, ActorRuntime runtime, string actorId)
	{
	}
}
