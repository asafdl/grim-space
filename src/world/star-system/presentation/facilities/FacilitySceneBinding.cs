using GrimSpace.Application;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Presentation.Scene;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed class FacilitySceneBinding : IDisposable
{
	private readonly StarSystemOrchestrator _orchestrator;
	private readonly IDisposable _catalogQuoteSubscription;
	private readonly Action _presentInsufficientFunds;
	private readonly Action<string, string> _presentSystemError;
	private bool _disposed;

	private FacilitySceneBinding(
		State run,
		StarSystemOrchestrator orchestrator,
		string poiId,
		string facilityId,
		PointOfInterest poi,
		Facility facility,
		UserIntentTranslator intents,
		Action presentInsufficientFunds,
		Action<string, string> presentSystemError)
	{
		ArgumentNullException.ThrowIfNull(presentInsufficientFunds);
		ArgumentNullException.ThrowIfNull(presentSystemError);
		Run = run;
		_orchestrator = orchestrator;
		_presentInsufficientFunds = presentInsufficientFunds;
		_presentSystemError = presentSystemError;
		PoiId = poiId;
		FacilityId = facilityId;
		Poi = poi;
		Facility = facility;
		Intents = intents;
		Map = orchestrator.Map;
		_orchestrator.WorldUpdated += OnWorldUpdated;
		_catalogQuoteSubscription = orchestrator.Subscribe<Record<MerchantCatalogQuote>>(
			record => MerchantCatalogQuoted?.Invoke(record.Value));
	}

	public event Action? WorldUpdated;
	public event Action<MerchantCatalogQuote>? MerchantCatalogQuoted;

	public State Run { get; }

	public StarMap Map { get; }

	public string PoiId { get; }

	public string FacilityId { get; }

	public PointOfInterest Poi { get; }

	public Facility Facility { get; }

	public UserIntentTranslator Intents { get; }

	public void PresentInsufficientFunds() => _presentInsufficientFunds();

	public void PresentSystemError(string summary, string technicalDetails) =>
		_presentSystemError(summary, technicalDetails);

	public bool TryVisitContractOperator(string operatorName) =>
		_orchestrator.TryVisitContractOperator(PoiId, FacilityId, operatorName);

	public static FacilitySceneBinding Create(
		string sceneName,
		Action presentInsufficientFunds,
		Action<string, string> presentSystemError)
	{
		ArgumentException.ThrowIfNullOrEmpty(sceneName);
		ArgumentNullException.ThrowIfNull(presentInsufficientFunds);
		ArgumentNullException.ThrowIfNull(presentSystemError);

		var run = Session.Instance.Run;
		var orchestrator = run.StarSystem;
		orchestrator.RefreshPlayerAgent();
		var immediateActions = orchestrator.ImmediatePlayerActions
			?? throw new InvalidOperationException(
				$"{sceneName} requires a player execution agent.");
		var poiId = MapNavigationContext.ActivePoiId
			?? throw new InvalidOperationException($"{sceneName} requires an active POI.");
		var facilityId = MapNavigationContext.ActiveFacilityId
			?? throw new InvalidOperationException($"{sceneName} requires an active facility.");
		var poi = orchestrator.Map.GetPointOfInterest(poiId);

		return new FacilitySceneBinding(
			run,
			orchestrator,
			poiId,
			facilityId,
			poi,
			poi.GetFacility(facilityId),
			new UserIntentTranslator(State.PlayerFleetUnitId, immediateActions),
			presentInsufficientFunds,
			presentSystemError);
	}

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		_orchestrator.WorldUpdated -= OnWorldUpdated;
		_catalogQuoteSubscription.Dispose();
		_orchestrator.RefreshPlayerAgent();
		WorldUpdated = null;
		MerchantCatalogQuoted = null;
	}

	private void OnWorldUpdated() => WorldUpdated?.Invoke();
}
