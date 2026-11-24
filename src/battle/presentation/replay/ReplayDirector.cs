using System.Diagnostics;
using Godot;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Log;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Replay;

/// <summary>
/// Runs turn replay playback when the battle enters <see cref="EBattlePhase.Replaying"/>.
/// </summary>
public sealed partial class ReplayDirector : Node
{
	private BattleOrchestrator _battle = null!;
	private TurnReplayPlayer _replayPlayer = null!;
	private BattleView _battleView = null!;
	private BattleCameraDirector _cameraDirector = null!;
	private Func<string, Color> _colorForActor = _ => Colors.White;
	private Func<bool> _playbackReady = () => true;
	private Action<IReadOnlyList<AsteroidBelt>> _syncAsteroidBelts = _ => { };

	private TurnReplay? _pendingReplay;
	private TurnReplay? _entryReplay;
	private Action? _entryFocus;
	private bool _entryApplied;
	private int _pendingCompletedTurn;
	private IReadOnlyDictionary<string, State>? _playbackEndStates;
	private EBattlePhase? _lastPhase;
	private string _lastPlayerId = "";

	public bool IsShowingReplay => _entryApplied;

	public void Configure(
		BattleOrchestrator battle,
		TurnReplayPlayer replayPlayer,
		BattleView battleView,
		BattleCameraDirector cameraDirector,
		Func<string, Color> colorForActor,
		Func<bool>? playbackReady = null,
		Action<IReadOnlyList<AsteroidBelt>>? syncAsteroidBelts = null)
	{
		_battle = battle;
		_replayPlayer = replayPlayer;
		_battleView = battleView;
		_cameraDirector = cameraDirector;
		_colorForActor = colorForActor;
		_playbackReady = playbackReady ?? (() => true);
		_syncAsteroidBelts = syncAsteroidBelts ?? (_ => { });
		_lastPlayerId = battle.ActivePlayerId;

		_battle.PhaseChanged += OnPhaseChanged;
		_battle.TurnResolved += OnTurnResolved;
		_replayPlayer.PlaybackComplete += OnPlaybackComplete;
	}

	public override void _Process(double delta)
	{
		if (_pendingReplay is not TurnReplay replay || !_playbackReady())
			return;

		ApplyReplayEntry();
		if (_cameraDirector.IsEnemyOverviewMoving)
			return;

		_pendingReplay = null;
		StartPlayback(replay, _pendingCompletedTurn);
	}

	private void OnTurnResolved(TurnReplay replay, int completedTurn)
	{
		_pendingReplay = replay;
		_pendingCompletedTurn = completedTurn;
	}

	private void OnPhaseChanged(EBattlePhase phase)
	{
		if (phase == _lastPhase)
			return;

		var previous = _lastPhase;
		_lastPhase = phase;

		switch (phase)
		{
			case EBattlePhase.PlayerTurn:
				if (previous == EBattlePhase.Replaying)
					_cameraDirector.PrepareReturnControl(
						!string.Equals(_lastPlayerId, _battle.ActivePlayerId, StringComparison.Ordinal));
				_lastPlayerId = _battle.ActivePlayerId;
				break;

			case EBattlePhase.Replaying:
				_cameraDirector.BeginPlayback();
				PrepareReplayEntry(_battle.PendingReplay
					?? throw new InvalidOperationException("Replay phase started without a pending replay."));
				break;
		}
	}

	private void PrepareReplayEntry(TurnReplay replay)
	{
		_entryReplay = replay;
		_entryApplied = false;
		var participants = ParticipantTeams();
		var actors = ReplayActorPhase.EnemyOverviewActorsForReplay(replay.ActivationOrder, participants);
		var points = new List<Vector3>();
		var priorityPoints = new List<Vector3>();
		var hasPriorityEnemy = false;
		foreach (var id in actors)
		{
			if (replay.StartStates.TryGetValue(id, out var state))
			{
				if (!state.IsAlive)
					continue;
			}
			else if (!replay.EndStates.TryGetValue(id, out state))
				continue;

			var position = WorldMapping.ToWorld(state.Position);
			points.Add(position);
			if (id == _battle.ActivePlayerId || (!hasPriorityEnemy && participants[id] == ETeam.Enemy))
			{
				priorityPoints.Add(position);
				hasPriorityEnemy |= participants[id] == ETeam.Enemy;
			}
		}
		_entryFocus = hasPriorityEnemy
			? _cameraDirector.PrepareEnemyOverview(points, priorityPoints, tween: true)
			: null;
	}

	public void ApplyReplayEntry()
	{
		if (_entryReplay is not { } replay || _entryApplied)
			return;

		_replayPlayer.ResetToLive(
			replay.StartStates,
			replay.EndStates,
			interest => _cameraDirector.ReportInterest(interest),
			scale => _cameraDirector.PlayImpactShake(scale));
		_syncAsteroidBelts(replay.StartAsteroidBelts);
		_entryFocus?.Invoke();
		_entryFocus = null;
		_entryApplied = true;
	}

	private void StartPlayback(TurnReplay replay, int completedTurn)
	{
		_playbackEndStates = replay.EndStates;
		var start = Stopwatch.GetTimestamp();
		ApplyReplayEntry();
		var resetMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
		var participants = ParticipantTeams();
		var participantsMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds - resetMs;
		GameLog.Log(
			$"Turn {completedTurn} replay setup: reset={resetMs:F1}ms "
			+ $"participants={participantsMs:F1}ms");
		_replayPlayer.Play(replay, completedTurn, participants);
	}

	private IReadOnlyDictionary<string, ETeam> ParticipantTeams() =>
		UnitRegistry.For(_battle.Engine.World).All
			.ToDictionary(unit => unit.State.Id, unit => unit.Team);

	private void OnPlaybackComplete()
	{
		var start = Stopwatch.GetTimestamp();
		if (_playbackEndStates is not null)
			_battleView.ApplyUnitStates(_playbackEndStates, _colorForActor);

		var syncMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
		_playbackEndStates = null;
		_entryReplay = null;
		_entryFocus = null;
		_entryApplied = false;
		_battle.NotifyReplayComplete();
		GameLog.Log(
			$"Turn {_pendingCompletedTurn} replay teardown: "
			+ $"sync={syncMs:F1}ms notify={Stopwatch.GetElapsedTime(start).TotalMilliseconds - syncMs:F1}ms");
	}
}
