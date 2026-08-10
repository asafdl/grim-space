using System.Diagnostics;
using Godot;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Log;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Replay;

public partial class TurnReplayPlayer : Node3D
{
	[Signal]
	public delegate void PlaybackCompleteEventHandler();

	private const double ImpactPauseSeconds = 0.32;

	private static readonly ReplayClipRegistry Clips = ReplayClipRegistry.Default;

	private IReadOnlyDictionary<string, UnitView> _unitViews = new Dictionary<string, UnitView>();
	private Func<string, Color> _colorFor = _ => Colors.White;
	private Action<State, Color> _ensureView = (_, _) => { };

	private TurnHistoryView _turnHistory = null!;
	private HazardBurstView _hazardBursts = null!;
	private ReplayClipContext _clipContext = null!;
	private IReadOnlyList<ITimelineEntry> _history = [];
	private int _entryIndex;

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

	public void Play(IReadOnlyList<ITimelineEntry> history, int turnNumber, string playerId, string opponentId)
	{
		_history = history;
		_entryIndex = 0;
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
		while (_entryIndex < _history.Count)
		{
			var entry = _history[_entryIndex++];
			switch (entry)
			{
				case IAction action:
				{
					BeginPhase(Classify(action.ActorId));
					Clips.TryPlay(action, _clipContext, out var playback);
					if (playback.Pauses)
					{
						GetTree().CreateTimer(playback.PauseSeconds).Timeout += PlayNext;
						return;
					}

					break;
				}
				case Record<SpawnFacts> { Value: var spawn }:
					BeginPhase(Classify(spawn.SourceId));
					ApplySpawn(spawn);
					break;
				case Record<ImpactFacts> { Value: var impact }:
					BeginPhase(Classify(impact.SourceId));
					if (PlayImpact(impact))
						return;
					break;
			}
		}

		Finish();
	}

	private void ApplySpawn(SpawnFacts spawn)
	{
		if (spawn.EntityType != EType.Torpedo)
			return;

		if (_clipContext.PendingTorpedoMount is not { } mount)
			throw new InvalidOperationException($"SpawnFacts for {spawn.TargetId} missing preceding TorpedoAction mount.");

		if (!_clipContext.EndStates.TryGetValue(spawn.TargetId, out var template))
			throw new InvalidOperationException($"SpawnFacts target {spawn.TargetId} missing from EndStates.");

		var firer = _clipContext.ReplayState.StateOf(spawn.SourceId);
		var (position, fore, dorsal) = TorpedoMount.LaunchPose(firer, mount);

		var spawned = template.Clone();
		spawned.Position = position;
		spawned.Fore = fore;
		spawned.Dorsal = dorsal;
		spawned.Starboard = Coord.Cross(dorsal, fore);
		spawned.FuelRemaining = TorpedoConfig.Fuel;
		spawned.MomentumLevel = TorpedoConfig.SpawnMomentum;
		spawned.HullPoints = spawned.Stats.MaxHullPoints;
		spawned.ActionPoints = spawned.Stats.MaxAp;

		_clipContext.ReplayState.Add(spawned);
		_clipContext.EnsureView(spawned, _clipContext.ColorFor(spawned.Id));
		_clipContext.UnitViews[spawned.Id].Sync(spawned);
		_clipContext.PendingTorpedoMount = null;
	}

	/// <summary>Returns true when playback yields for the hit flash.</summary>
	private bool PlayImpact(ImpactFacts impact)
	{
		_clipContext.ReplayState.ApplyImpact(impact);
		if (!_clipContext.ReplayState.Contains(impact.TargetId))
			return false;

		if (!_clipContext.UnitViews.TryGetValue(impact.TargetId, out var view))
			return false;

		var state = _clipContext.ReplayState.StateOf(impact.TargetId);
		view.ShowImpactState(state);
		view.PlayHitFlash();

		GetTree().CreateTimer(ImpactPauseSeconds).Timeout += () =>
		{
			if (_clipContext.UnitViews.TryGetValue(impact.TargetId, out var lingering)
				&& _clipContext.ReplayState.Contains(impact.TargetId))
			{
				lingering.Sync(_clipContext.ReplayState.StateOf(impact.TargetId));
			}

			PlayNext();
		};
		return true;
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
			+ $"history={_history.Count}");

		EmitSignal(SignalName.PlaybackComplete);
	}

	private enum PlaybackPhase
	{
		Player,
		Enemy,
		Upkeep,
	}
}
