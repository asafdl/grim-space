using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class PublishMerchantCatalogEffect(MerchantCatalogQuote quote)
	: IEffect<StarMap, ActorRuntime>
{
	public MerchantCatalogQuote Quote { get; } = quote;

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId) =>
		[new Record<MerchantCatalogQuote>(Quote)];

	public void Undo(StarMap world, ActorRuntime runtime, string actorId)
	{
	}
}
