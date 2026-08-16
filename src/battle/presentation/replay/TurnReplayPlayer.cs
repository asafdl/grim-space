using System.Diagnostics;
using Godot;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Log;
using GrimSpace.Math.Grid;
using GrimSpace.Core;
using GrimSpace.Units.Enums;

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
	private IReadOnlyList<ITimelineEntry> _history = [];
	private int _entryIndex;

	private int _turnNumber;
	private IReadOnlyDictionary<string, ETeam> _participants = new Dictionary<string, ETeam>();
	private readonly Stopwatch _playbackTimer = new();
	private readonly Stopwatch _phaseTimer = new();
	private EReplayPlaybackPhase _phase;
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
		IReadOnlyDictionary<string, State> endStates,
		Action<CameraInterest>? reportInterest = null)
	{
		var replayState = new ReplayState(turnStart);
		_clipContext = new ReplayClipContext(
			replayState,
			_unitViews,
			_turnHistory,
			_hazardBursts,
			_colorFor,
			endStates,
			_ensureView,
			reportInterest);
		_turnHistory.BeginTurn(turnStart.ToDictionary(pair => pair.Key, pair => pair.Value.Position));
		_hazardBursts.Clear();

		foreach (var (unitId, state) in turnStart)
		{
			_ensureView(state, _colorFor(state.Id));
			_unitViews[unitId].Sync(state);
		}
	}

	public void Play(
		IReadOnlyList<ITimelineEntry> history,
		int turnNumber,
		IReadOnlyDictionary<string, ETeam> participants)
	{
		_history = history;
		_entryIndex = 0;
		_turnNumber = turnNumber;
		_participants = participants;
		_playerAnimMs = 0;
		_enemyAnimMs = 0;
		_upkeepAnimMs = 0;
		_timeToEnemyAnimMs = 0;
		_loggedEnemyAnimStart = false;
		_phase = EReplayPlaybackPhase.Player;
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
					BeginPhase(ReplayActorPhase.Classify(action.ActorId, _participants));
					ReportActionInterest(action);
					Clips.TryPlay(action, _clipContext, out var playback);
					if (playback.Pauses)
					{
						GetTree().CreateTimer(playback.PauseSeconds).Timeout += PlayNext;
						return;
					}

					break;
				}
				case Record<SpawnFacts> { Value: var spawn }:
					BeginPhase(ReplayActorPhase.Classify(spawn.SourceId, _participants));
					ApplySpawn(spawn);
					break;
				case Record<ImpactFacts> { Value: var impact }:
					BeginPhase(ReplayActorPhase.Classify(impact.SourceId, _participants));
					if (PlayImpact(impact))
						return;
					break;
			}
		}

		Finish();
	}

	private void ApplySpawn(SpawnFacts spawn)
	{
		switch (spawn.EntityType)
		{
			case EType.Torpedo:
				ApplyTorpedoSpawn(spawn);
				break;
			case EType.Patrol:
				ApplyPatrolSpawn(spawn);
				break;
		}
	}

	private void ApplyTorpedoSpawn(SpawnFacts spawn)
	{
		if (_clipContext.PendingTorpedoMountedOn is not { } mountedOn)
			throw new InvalidOperationException($"SpawnFacts for {spawn.TargetId} missing preceding TorpedoAction mount.");

		if (!_clipContext.EndStates.TryGetValue(spawn.TargetId, out var template))
			throw new InvalidOperationException($"SpawnFacts target {spawn.TargetId} missing from EndStates.");

		var firer = _clipContext.ReplayState.StateOf(spawn.SourceId);
		var (position, fore, dorsal) = TorpedoMount.LaunchPose(firer, mountedOn);

		var spawned = template.Clone();
		spawned.Position = position;
		spawned.Fore = fore;
		spawned.Dorsal = dorsal;
		spawned.Starboard = Coord.Cross(dorsal, fore);
		spawned.ParentId = spawn.SourceId;
		spawned.FuelRemaining = TorpedoConfig.Fuel;
		spawned.MomentumLevel = TorpedoConfig.SpawnMomentum;
		spawned.HullPoints = spawned.Stats.MaxHullPoints;
		spawned.ActionPoints = spawned.Stats.MaxAp;

		_clipContext.ReplayState.Add(spawned);
		_clipContext.EnsureView(spawned, _clipContext.ColorFor(spawned.Id));
		_clipContext.UnitViews[spawned.Id].Sync(spawned);
		_clipContext.PendingTorpedoMountedOn = null;
	}

	private void ApplyPatrolSpawn(SpawnFacts spawn)
	{
		if (!_clipContext.EndStates.TryGetValue(spawn.TargetId, out var template))
			throw new InvalidOperationException($"SpawnFacts target {spawn.TargetId} missing from EndStates.");

		var carrier = _clipContext.ReplayState.StateOf(spawn.SourceId);
		var (position, fore, dorsal) = PatrolBayMount.LaunchPose(carrier);

		var spawned = template.Clone();
		spawned.Position = position;
		spawned.Fore = fore;
		spawned.Dorsal = dorsal;
		spawned.Starboard = Coord.Cross(dorsal, fore);
		spawned.ParentId = spawn.SourceId;
		spawned.MomentumLevel = PatrolBayMount.SpawnMomentum(carrier);
		spawned.HullPoints = spawned.Stats.MaxHullPoints;
		spawned.ActionPoints = spawned.Stats.MaxAp;
		spawned.ShieldPoints = spawned.Stats.MaxShieldPoints.Clone();
		spawned.FlakRemaining = spawned.Stats.FlaksPerTurn;
		spawned.RailgunRemaining = spawned.Stats.RailgunsPerTurn;
		spawned.PatrolSpawnCooldownRemaining = 0;

		_clipContext.ReplayState.Add(spawned);
		_clipContext.EnsureView(spawned, _clipContext.ColorFor(spawned.Id));
		_clipContext.UnitViews[spawned.Id].Sync(spawned);
	}

	private bool PlayImpact(ImpactFacts impact)
	{
		ReportImpactInterest(impact);
		_clipContext.ReplayState.ApplyImpact(impact);
		if (!_clipContext.ReplayState.Contains(impact.TargetId))
			return false;

		if (!_clipContext.UnitViews.TryGetValue(impact.TargetId, out var view))
			return false;

		var state = _clipContext.ReplayState.StateOf(impact.TargetId);
		view.ShowImpactState(state);
		view.PlayHitFlash();

		var damage = impact.HullDamage > 0 ? impact.HullDamage : impact.ShieldDamage;
		view.PlayDamagePopup(damage);

		var died = !state.IsAlive;
		if (died)
			view.PlayDeathExplosion();

		var pause = died
			? ReplayTiming.ImpactPauseSeconds + ReplayTiming.DeathExplosionSeconds
			: ReplayTiming.ImpactPauseSeconds;

		GetTree().CreateTimer(pause).Timeout += () =>
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

	private void BeginPhase(EReplayPlaybackPhase phase)
	{
		if (phase == _phase)
			return;

		FlushPhase();
		_phase = phase;
		_phaseTimer.Restart();

		if (phase == EReplayPlaybackPhase.Enemy && !_loggedEnemyAnimStart)
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
			case EReplayPlaybackPhase.Player:
				_playerAnimMs += elapsed;
				break;
			case EReplayPlaybackPhase.Enemy:
				_enemyAnimMs += elapsed;
				break;
			case EReplayPlaybackPhase.Upkeep:
				_upkeepAnimMs += elapsed;
				break;
		}
	}

	private void ReportActionInterest(IAction action)
	{
		if (_clipContext.ReportInterest is null)
			return;

		if (action is TorpedoAction torpedo)
		{
			var firer = _clipContext.ReplayState.StateOf(torpedo.ActorId);
			var (launchCell, _, _) = TorpedoMount.LaunchPose(firer, torpedo.MountedOn);
			_clipContext.ReportInterest(new CameraInterest(
				[
					WorldMapping.ToWorld(firer.Position),
					WorldMapping.ToWorld(launchCell),
				],
				CameraImportance.Combat));
			return;
		}

		if (action is SpawnPatrolAction deploy)
		{
			var carrier = _clipContext.ReplayState.StateOf(deploy.ActorId);
			var (launchCell, _, _) = PatrolBayMount.LaunchPose(carrier);
			_clipContext.ReportInterest(new CameraInterest(
				[
					WorldMapping.ToWorld(carrier.Position),
					WorldMapping.ToWorld(launchCell),
				],
				CameraImportance.Combat));
		}
	}

	private void ReportImpactInterest(ImpactFacts impact)
	{
		if (_clipContext.ReportInterest is null)
			return;

		var source = _clipContext.ReplayState.StateOf(impact.SourceId).Position;
		var target = _clipContext.ReplayState.StateOf(impact.TargetId).Position;
		_clipContext.ReportInterest(new CameraInterest(
			[
				WorldMapping.ToWorld(source),
				WorldMapping.ToWorld(target),
			],
			CameraImportance.Combat));
	}

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
}
