using System.Diagnostics;
using Godot;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Log;

namespace GrimSpace.Battle.Presentation.Replay;

public partial class TurnReplayPlayer : Node3D
{
	[Signal]
	public delegate void PlaybackCompleteEventHandler();

	private static readonly ReplayClipRegistry Clips = ReplayClipRegistry.Default;

	private IReadOnlyDictionary<string, UnitView> _unitViews = new Dictionary<string, UnitView>();
	private Func<string, Color> _colorFor = _ => Colors.White;
	private Action<State, Color> _ensureView = (_, _) => { };

	private TurnHistoryView _turnHistory = null!;
	private HazardBurstView _hazardBursts = null!;
	private ReplayClipContext _clipContext = null!;
	private IReadOnlyList<IAction> _actions = [];
	private int _actionIndex;

	private int _turnNumber;
	private string _playerId = string.Empty;
	private string _opponentId = string.Empty;
	private readonly Stopwatch _playbackTimer = new();
	private readonly Stopwatch _phaseTimer = new();
	private PlaybackPhase _phase;
	private double _playerAnimMs;
	private double _enemyAnimMs;
	private double _upkeepAnimMs;
	private double _timeToEnemyAnimMs;
	private bool _loggedEnemyAnimStart;

	public bool IsPlaying { get; private set; }

	public void Configure(
		IReadOnlyDictionary<string, UnitView> unitViews,
		Func<string, Color> colorFor,
		Action<State, Color> ensureView)
	{
		_unitViews = unitViews;
		_colorFor = colorFor;
		_ensureView = ensureView;

		_turnHistory = new TurnHistoryView { Name = "TurnHistory" };
		AddChild(_turnHistory);

		_hazardBursts = new HazardBurstView { Name = "HazardBursts" };
		AddChild(_hazardBursts);
	}

	public void ResetToLive(
		IReadOnlyDictionary<string, State> turnStart,
		IReadOnlyDictionary<string, State> endStates)
	{
		var replayState = new ReplayState(turnStart);
		_clipContext = new ReplayClipContext(
			replayState,
			_unitViews,
			_turnHistory,
			_hazardBursts,
			_colorFor,
			endStates,
			_ensureView);
		_turnHistory.BeginTurn(turnStart.ToDictionary(pair => pair.Key, pair => pair.Value.Position));
		_hazardBursts.Clear();

		foreach (var (unitId, state) in turnStart)
			_unitViews[unitId].Sync(state);
	}

	public void Play(IReadOnlyList<IAction> actions, int turnNumber, string playerId, string opponentId)
	{
		_actions = actions;
		_actionIndex = 0;
		_turnNumber = turnNumber;
		_playerId = playerId;
		_opponentId = opponentId;
		_playerAnimMs = 0;
		_enemyAnimMs = 0;
		_upkeepAnimMs = 0;
		_timeToEnemyAnimMs = 0;
		_loggedEnemyAnimStart = false;
		_phase = PlaybackPhase.Player;
		_playbackTimer.Restart();
		_phaseTimer.Restart();
		IsPlaying = true;
		PlayNext();
	}

	private void PlayNext()
	{
		while (_actionIndex < _actions.Count)
		{
			var action = _actions[_actionIndex++];
			BeginPhase(Classify(action.ActorId));

			Clips.TryPlay(action, _clipContext, out var playback);

			if (playback.Pauses)
			{
				GetTree().CreateTimer(playback.PauseSeconds).Timeout += PlayNext;
				return;
			}
		}

		Finish();
	}

	private void BeginPhase(PlaybackPhase phase)
	{
		if (phase == _phase)
			return;

		FlushPhase();
		_phase = phase;
		_phaseTimer.Restart();

		if (phase == PlaybackPhase.Enemy && !_loggedEnemyAnimStart)
		{
			_timeToEnemyAnimMs = _playbackTimer.Elapsed.TotalMilliseconds;
			_loggedEnemyAnimStart = true;
		}
	}

	private void FlushPhase()
	{
		var elapsed = _phaseTimer.Elapsed.TotalMilliseconds;
		switch (_phase)
		{
			case PlaybackPhase.Player:
				_playerAnimMs += elapsed;
				break;
			case PlaybackPhase.Enemy:
				_enemyAnimMs += elapsed;
				break;
			case PlaybackPhase.Upkeep:
				_upkeepAnimMs += elapsed;
				break;
		}
	}

	private PlaybackPhase Classify(string actorId) =>
		actorId == _playerId
			? PlaybackPhase.Player
			: actorId == _opponentId
				? PlaybackPhase.Enemy
				: PlaybackPhase.Upkeep;

	private void Finish()
	{
		FlushPhase();
		IsPlaying = false;

		var totalMs = _playbackTimer.Elapsed.TotalMilliseconds;
		GameLog.Log(
			$"Turn {_turnNumber} replay: total={totalMs:F1}ms "
			+ $"playerAnim={_playerAnimMs:F1}ms "
			+ $"enemyAnim={_enemyAnimMs:F1}ms "
			+ $"upkeepAnim={_upkeepAnimMs:F1}ms "
			+ $"toEnemyAnim={_timeToEnemyAnimMs:F1}ms "
			+ $"actions={_actions.Count}");

		EmitSignal(SignalName.PlaybackComplete);
	}

	private enum PlaybackPhase
	{
		Player,
		Enemy,
		Upkeep,
	}
}
