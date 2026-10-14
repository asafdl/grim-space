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
	public static class TagNames
	{
		public const string Fetch = "fetch";
		public const string Merchant = "merchant";
		public const string Catalog = "catalog";
	}

	private static readonly string[] ActionTags =
		[TagNames.Fetch, TagNames.Merchant, TagNames.Catalog];

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
		if (!world.FleetRegistry.TryGet(fetch.ActorId, out var fleet))
			return false;
		if (!fleet.Members.Any(member =>
				string.Equals(member.Id, fetch.ShipSnapshot.Id, StringComparison.Ordinal)))
			return false;
		if (!EndpointServesCatalog(fetch.Endpoint, fetch.Catalog, world))
			return false;
		if (world.ShipRegistryReader?.Matches(
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

		return [PriceCatalog(fetch, world, runtime)];
	}

	internal static PublishMerchantCatalogEffect PriceCatalog(
		FetchMerchantCatalogAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		ArgumentNullException.ThrowIfNull(action);
		ArgumentNullException.ThrowIfNull(world);
		ArgumentNullException.ThrowIfNull(runtime);

		var quote = new MerchantCatalogQuote(
			action.Endpoint,
			action.Catalog,
			action.ShipSnapshot.Id,
			MerchantCatalog.ListFor(action.Catalog, action.ShipSnapshot, world));
		var priced = runtime.ActionEffects.Apply(
			action,
			world,
			runtime,
			[new PublishMerchantCatalogEffect(quote)]).Single();
		return priced as PublishMerchantCatalogEffect
			?? throw new InvalidOperationException(
				$"Merchant pricing replaced {nameof(PublishMerchantCatalogEffect)} with "
				+ $"'{priced.GetType().Name}'.");
	}

	internal static FetchMerchantCatalogAction CreatePricingProbe(
		string actorId,
		MerchantEndpoint endpoint,
		EMerchantCatalog catalog,
		ShipInstance ship) =>
		new(actorId, endpoint, catalog, ship);

	internal static bool EndpointServesCatalog(
		MerchantEndpoint endpoint,
		EMerchantCatalog catalog,
		StarMap world) =>
		catalog switch
		{
			EMerchantCatalog.Weapons => MerchantPurchaseValidation.EndpointServesMerchantSubtype(
				world,
				endpoint,
				EMerchantSubtype.Weapons),
			EMerchantCatalog.ShipSupport => MerchantPurchaseValidation.EndpointServesMerchantSubtype(
				world,
				endpoint,
				EMerchantSubtype.ShipSupport),
			EMerchantCatalog.Healing => MerchantPurchaseValidation.EndpointServesHealingStation(
				world,
				endpoint),
			_ => false,
		};
}
