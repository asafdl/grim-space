using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record FetchMerchantCatalogAction(
	string ActorId,
	MerchantEndpoint Endpoint,
	EMerchantCatalog Catalog,
	ShipInstance ShipSnapshot) : IAction<StarMap, ActorRuntime>
{
	private static readonly string[] ActionTags =
		[StarMapActionTags.Fetch, StarMapActionTags.Merchant, StarMapActionTags.Catalog];

	IReadOnlyList<string> IAction.Tags => ActionTags;

	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		FetchMerchantCatalogActionDef.Instance;
}

public sealed class FetchMerchantCatalogActionDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static FetchMerchantCatalogActionDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime)
	{
		if (action is not FetchMerchantCatalogAction fetch)
			return false;
		if (world.ItemInventory?.IsOwnedBy(fetch.ShipSnapshot.Id, fetch.ActorId) != true)
			return false;
		if (!EndpointServesCatalog(fetch.Endpoint, fetch.Catalog, world))
			return false;
		if (world.ItemInventory?.Matches(
			    fetch.ShipSnapshot.Id,
			    fetch.ShipSnapshot) != true)
			return false;

		return true;
	}

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var fetch = (FetchMerchantCatalogAction)action;
		if (!IsLegal(fetch, world, runtime))
			return [];

		return
		[
			new PublishMerchantCatalogEffect(new MerchantCatalogQuote(
				fetch.Endpoint,
				fetch.Catalog,
				fetch.ShipSnapshot.Id,
				MerchantCatalog.ListFor(fetch.Catalog, fetch.ShipSnapshot, world))),
		];
	}

	internal static bool EndpointServesCatalog(
		MerchantEndpoint endpoint,
		EMerchantCatalog catalog,
		StarMap world) =>
		catalog switch
		{
			EMerchantCatalog.Healing => MerchantPurchaseValidation.EndpointServesHealingStation(
				world,
				endpoint),
			_ => false,
		};
}
