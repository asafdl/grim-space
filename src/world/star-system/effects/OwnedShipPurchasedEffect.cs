using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class OwnedShipPurchasedEffect(ShipSpawnDeclaration declaration) : IEffect<StarMap, ActorRuntime>
{
	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId) =>
		[new Record<OwnedShipPurchased>(new OwnedShipPurchased(declaration))];

	public void Undo(StarMap world, ActorRuntime runtime, string actorId)
	{
	}
}
