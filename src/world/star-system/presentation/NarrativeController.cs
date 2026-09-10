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

	public NarrativeController(
		StarSystemOrchestrator orchestrator,
		NarrativeHudOverlay hud)
	{
		_orchestrator = orchestrator;
		_playerAgent = orchestrator.PlayerAgent
			?? throw new InvalidOperationException("Narrative requires a player execution agent.");
		_hud = hud;
		_hud.Completed += OnCompleted;
		_orchestrator.WorldUpdated += Sync;
	}

	public bool TryHandleInput(Godot.InputEvent @event) => _hud.TryHandleInput(@event);

	public void Sync()
	{
		var narrativeId = _orchestrator.Map.ActiveNarrativeId;
		if (narrativeId is not null && MapNarratives.TryGet(narrativeId, out var narrative))
		{
			if (!_hud.IsOpen)
				_hud.Open(narrative);
		}
		else if (_hud.IsOpen)
		{
			_hud.Close();
		}
	}

	private void OnCompleted()
	{
		var narrativeId = _orchestrator.Map.ActiveNarrativeId;
		if (narrativeId is null)
			return;

		_hud.SetBusy(true);
		if (_playerAgent.TryEnqueue([new CompleteNarrativeAction(State.PlayerFleetUnitId, narrativeId)]))
			return;

		_hud.ShowError("Unable to continue.");
	}

	public void Dispose()
	{
		_orchestrator.WorldUpdated -= Sync;
		_hud.Completed -= OnCompleted;
	}
}
