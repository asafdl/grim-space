using Godot;
using GrimSpace.Education;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Agents;
using GrimSpace.World.StarSystem.Narrative;

namespace GrimSpace.World.StarSystem.Presentation;

public sealed class NarrativeController : IDisposable
{
	private readonly StarSystemOrchestrator _orchestrator;
	private readonly StarMapPlayerExecutionAgent _playerAgent;
	private readonly NarrativeHudOverlay _hud;
	private readonly IWorldFocus _worldFocus;
	private readonly IWorldIndicator _worldIndicator;
	private IWorldIndicatorHandle? _indicatorHandle;

	public NarrativeController(
		StarSystemOrchestrator orchestrator,
		NarrativeHudOverlay hud,
		IWorldFocus worldFocus,
		IWorldIndicator worldIndicator)
	{
		_orchestrator = orchestrator;
		_playerAgent = orchestrator.PlayerAgent
			?? throw new InvalidOperationException("Narrative requires a player execution agent.");
		_hud = hud;
		_worldFocus = worldFocus;
		_worldIndicator = worldIndicator;
		_hud.Completed += OnCompleted;
		_hud.PageBegan += OnPageBegan;
		_hud.Body.MetaClicked += OnMetaClicked;
		_orchestrator.WorldUpdated += Sync;
	}

	public bool TryHandleInput(Godot.InputEvent @event) => _hud.TryHandleInput(@event);

	public void Sync()
	{
		var narrativeId = _orchestrator.Map.ActiveNarrativeId;
		if (narrativeId is not null
			&& MapNarratives.TryGet(narrativeId, _orchestrator.Map, out var narrative))
		{
			if (!_hud.IsOpen)
				_hud.Open(narrative);
		}
		else if (_hud.IsOpen)
		{
			ClearIndicator();
			_hud.Close();
		}
	}

	private void OnCompleted()
	{
		ClearIndicator();
		var narrativeId = _orchestrator.Map.ActiveNarrativeId;
		if (narrativeId is null)
			return;

		_hud.SetBusy(true);
		if (_playerAgent.TryEnqueue([new CompleteNarrativeAction(State.PlayerFleetUnitId, narrativeId)]))
			return;

		_hud.ShowError("Unable to continue.");
	}

	private void OnMetaClicked(Variant metadata)
	{
		ClearIndicator();
		if (metadata.VariantType != Variant.Type.String)
		{
			GD.PushError($"Narrative received unsupported link metadata type '{metadata.VariantType}'.");
			_hud.ShowWorldLinkError("Target is no longer available.");
			return;
		}

		var objectId = metadata.AsString();
		var focus = _worldFocus.Focus(objectId);
		if (focus is not WorldFocusResult.Accepted)
		{
			ShowWorldLinkFailure(objectId, "focus", focus);
			return;
		}

		var indicator = _worldIndicator.Show(objectId);
		if (indicator is not WorldIndicatorResult.Shown shown)
		{
			ShowWorldLinkFailure(objectId, "indicator", indicator);
			return;
		}

		_indicatorHandle = shown.Handle;
		_hud.ClearWorldLinkError();
	}

	private void ShowWorldLinkFailure(string objectId, string tool, object result)
	{
		GD.PushWarning(
			$"Narrative world link '{objectId}' failed during {tool}: {result.GetType().Name}.");
		_hud.ShowWorldLinkError("Target is no longer available.");
	}

	private void OnPageBegan(int _) => ClearIndicator();

	private void ClearIndicator()
	{
		_indicatorHandle?.Dispose();
		_indicatorHandle = null;
	}

	public void Dispose()
	{
		ClearIndicator();
		_orchestrator.WorldUpdated -= Sync;
		_hud.Completed -= OnCompleted;
		_hud.PageBegan -= OnPageBegan;
		_hud.Body.MetaClicked -= OnMetaClicked;
	}
}
