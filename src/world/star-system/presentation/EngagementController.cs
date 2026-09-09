using GrimSpace.Run;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Agents;
using GrimSpace.World.StarSystem.Contact;

namespace GrimSpace.World.StarSystem.Presentation;

public sealed class EngagementController : IDisposable
{
	private readonly StarSystemOrchestrator _orchestrator;
	private readonly StarMapPlayerExecutionAgent _playerAgent;
	private readonly EngagementHudOverlay _hud;
	private bool _committedEngagementSeen;

	public EngagementController(
		StarSystemOrchestrator orchestrator,
		EngagementHudOverlay hud)
	{
		_orchestrator = orchestrator;
		_playerAgent = orchestrator.PlayerAgent
			?? throw new InvalidOperationException("Engagement requires a player execution agent.");
		_hud = hud;
		_hud.EngageRequested += OnEngageRequested;
		_hud.FleeRequested += OnFleeRequested;
		_orchestrator.WorldUpdated += Sync;
	}

	public event Action? BattleRequested;

	public bool TryHandleInput(Godot.InputEvent @event) => _hud.TryHandleInput(@event);

	public void Sync()
	{
		var playerId = _orchestrator.PlayerId
			?? throw new InvalidOperationException("Engagement requires a player id.");
		var world = _orchestrator.Map;

		if (EngagementQueries.TryGetPendingPlayerEngagement(world, playerId, out var pending))
		{
			if (!_hud.IsOpen)
				_hud.Sync(pending);
		}
		else if (_hud.IsOpen)
		{
			_hud.Close();
		}

		if (EngagementQueries.TryGetCommittedPlayerEngagement(world, playerId, out _))
		{
			if (!_committedEngagementSeen)
			{
				_committedEngagementSeen = true;
				BattleRequested?.Invoke();
			}
		}
		else
		{
			_committedEngagementSeen = false;
		}
	}

	private void OnEngageRequested()
	{
		var playerId = State.PlayerFleetUnitId;
		_hud.SetBusy(true);
		if (_playerAgent.TryEnqueue([new EngageAction(playerId)]))
			return;

		_hud.ShowError("Unable to engage.");
	}

	private void OnFleeRequested()
	{
		var playerId = State.PlayerFleetUnitId;
		_hud.SetBusy(true);
		if (_playerAgent.TryEnqueue([new FleeAction(playerId)]))
			return;

		_hud.ShowError("Unable to flee.");
	}

	public void Dispose()
	{
		_orchestrator.WorldUpdated -= Sync;
		_hud.EngageRequested -= OnEngageRequested;
		_hud.FleeRequested -= OnFleeRequested;
	}
}
