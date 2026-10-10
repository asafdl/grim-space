using GrimSpace.Application;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Presentation.Scene;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

public sealed class FacilitySceneBinding : IDisposable
{
	private readonly StarSystemOrchestrator _orchestrator;
	private bool _disposed;

	private FacilitySceneBinding(
		State run,
		StarSystemOrchestrator orchestrator,
		string poiId,
		string facilityId,
		PointOfInterest poi,
		Facility facility,
		UserIntentTranslator intents)
	{
		Run = run;
		_orchestrator = orchestrator;
		PoiId = poiId;
		FacilityId = facilityId;
		Poi = poi;
		Facility = facility;
		Intents = intents;
		Map = orchestrator.Map;
		_orchestrator.WorldUpdated += OnWorldUpdated;
	}

	public event Action? WorldUpdated;

	public State Run { get; }

	public StarMap Map { get; }

	public string PoiId { get; }

	public string FacilityId { get; }

	public PointOfInterest Poi { get; }

	public Facility Facility { get; }

	public UserIntentTranslator Intents { get; }

	public static FacilitySceneBinding Create(string sceneName)
	{
		ArgumentException.ThrowIfNullOrEmpty(sceneName);

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
			new UserIntentTranslator(State.PlayerFleetUnitId, immediateActions));
	}

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		_orchestrator.WorldUpdated -= OnWorldUpdated;
		_orchestrator.RefreshPlayerAgent();
		WorldUpdated = null;
	}

	private void OnWorldUpdated() => WorldUpdated?.Invoke();
}
