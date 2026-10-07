using System.Diagnostics;
using Godot;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.World;
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
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Presentation.Replay;

public partial class TurnReplayPlayer : Node3D
{
	[Signal]
	public delegate void PlaybackCompleteEventHandler();
	public event Action<int>? TurnFlowIndexChanged;

	private static readonly ReplayClipRegistry Clips = ReplayClipRegistry.Default;

	private IReadOnlyDictionary<string, UnitView> _unitViews = new Dictionary<string, UnitView>();
	private Func<string, Color> _colorFor = _ => Colors.White;
	private Action<State, Color> _ensureView = (_, _) => { };
	private Action<string> _removeView = _ => { };
	private Action<IReadOnlyDictionary<string, State>> _synchronizeViews = _ => { };
	private Action<State> _stateChanged = _ => { };

	private HazardBurstView _hazardBursts = null!;
	private ReplayClipContext _clipContext = null!;
	private IReadOnlyList<ITimelineEntry> _history = [];
	private IReadOnlyList<string> _activationOrder = [];
	private int _flowCompletedCount;
	private int _entryIndex;

	private int _turnNumber;
	private Dictionary<string, ETeam> _participants = new(StringComparer.Ordinal);
	private readonly Stopwatch _playbackTimer = new();
	private readonly Stopwatch _phaseTimer = new();
	private EReplayPlaybackPhase _phase;
	private double _playerAnimMs;
	private double _enemyAnimMs;
	private double _upkeepAnimMs;
	private double _timeToEnemyAnimMs;
	private bool _loggedEnemyAnimStart;
	private double _actionWorkMs;
	private double _spawnWorkMs;
	private double _impactWorkMs;
	private double _scheduledWaitMs;
	private double _actualWaitMs;
	private int _actionCount;
	private int _spawnCount;
	private int _impactCount;

	public bool IsPlaying { get; private set; }

	public void Configure(
		IReadOnlyDictionary<string, UnitView> unitViews,
		Func<string, Color> colorFor,
		Action<State, Color> ensureView,
		Action<string> removeView,
		Action<IReadOnlyDictionary<string, State>> synchronizeViews,
		Action<State> stateChanged)
	{
		_unitViews = unitViews;
		_colorFor = colorFor;
		_ensureView = ensureView;
		_removeView = removeView;
		_synchronizeViews = synchronizeViews;
		_stateChanged = stateChanged;

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
			_hazardBursts,
			_colorFor,
			endStates,
			_ensureView,
			DismissUnitPresentation,
			reportInterest);
		foreach (var view in _unitViews.Values)
			view.ClearMovementTrail();
		_hazardBursts.Clear();
		_synchronizeViews(turnStart);
	}

	public void Play(
		TurnReplay replay,
		int turnNumber,
		IReadOnlyDictionary<string, ETeam> participants)
	{
		_history = replay.History;
		_activationOrder = replay.ActivationOrder;
		_flowCompletedCount = 0;
		_entryIndex = 0;
		_turnNumber = turnNumber;
		_participants = participants.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
		foreach (var (actorId, view) in _unitViews)
			view.ConfigureMovementTrail(TrailColorFor(actorId));
		_playerAnimMs = 0;
		_enemyAnimMs = 0;
		_upkeepAnimMs = 0;
		_timeToEnemyAnimMs = 0;
		_loggedEnemyAnimStart = false;
		_actionWorkMs = 0;
		_spawnWorkMs = 0;
		_impactWorkMs = 0;
		_scheduledWaitMs = 0;
		_actualWaitMs = 0;
		_actionCount = 0;
		_spawnCount = 0;
		_impactCount = 0;
		_phase = EReplayPlaybackPhase.Player;
		_playbackTimer.Restart();
		_phaseTimer.Restart();
		IsPlaying = true;
		EmitTurnFlowIndexChanged();
		PlayNext();
	}

	private void PlayNext()
	{
		while (_entryIndex < _history.Count)
		{
			var entry = _history[_entryIndex++];
			var entryStart = Stopwatch.GetTimestamp();
			switch (entry)
			{
				case EndOfPhaseAction endOfPhase:
					_flowCompletedCount++;
					EmitTurnFlowIndexChanged();
					break;
				case IAction action:
				{
					BeginPhase(ReplayActorPhase.Classify(action.ActorId, _participants));
					ReportActionInterest(action);
					_clipContext.FollowingActions = _history
						.Skip(_entryIndex)
						.TakeWhile(entry => entry is IAction)
						.Cast<IAction>()
						.ToArray();
					Clips.TryPlay(action, _clipContext, out var playback);
					_actionWorkMs += Stopwatch.GetElapsedTime(entryStart).TotalMilliseconds;
					_actionCount++;
					if (playback.Pauses)
					{
						var waitStart = Stopwatch.GetTimestamp();
						_scheduledWaitMs += playback.PauseSeconds * 1000;
						GetTree().CreateTimer(playback.PauseSeconds).Timeout += () =>
						{
							_actualWaitMs += Stopwatch.GetElapsedTime(waitStart).TotalMilliseconds;
							PlayNext();
						};
						return;
					}

					break;
				}
				case Record<SpawnFacts> { Value: var spawn }:
					BeginPhase(ReplayActorPhase.Classify(spawn.SourceId, _participants));
					ApplySpawn(spawn);
					_spawnWorkMs += Stopwatch.GetElapsedTime(entryStart).TotalMilliseconds;
					_spawnCount++;
					break;
				case Record<ImpactFacts> { Value: var impact }:
					BeginPhase(ReplayActorPhase.Classify(impact.SourceId, _participants));
					_impactCount++;
					if (PlayImpact(impact))
					{
						_impactWorkMs += Stopwatch.GetElapsedTime(entryStart).TotalMilliseconds;
						return;
					}
					_impactWorkMs += Stopwatch.GetElapsedTime(entryStart).TotalMilliseconds;
					break;
			}
		}

		Finish();
	}

	private void EmitTurnFlowIndexChanged()
	{
		var current = _activationOrder.Count == 0
			? 0
			: System.Math.Min(_flowCompletedCount, _activationOrder.Count - 1);
		TurnFlowIndexChanged?.Invoke(current);
	}

	private void ApplySpawn(SpawnFacts spawn)
	{
		if (_participants.TryGetValue(spawn.SourceId, out var team))
			_participants[spawn.TargetId] = team;

		switch (spawn.EntityType)
		{
			case EType.VoidBomb:
				ApplyVoidBombSpawn(spawn);
				break;
			case EType.RepurposedMiner:
				ApplyRepurposedMinerSpawn(spawn);
				break;
		}
	}

	private void ApplyVoidBombSpawn(SpawnFacts spawn)
	{
		var spawned = spawn.SpawnedState.Clone();
		_clipContext.ReplayState.Add(spawned);
		_clipContext.EnsureView(spawned, _clipContext.ColorFor(spawned.Id));
		var view = _clipContext.UnitViews[spawned.Id];
		view.ConfigureMovementTrail(TrailColorFor(spawned.Id));
		view.Sync(spawned);
		_clipContext.PendingVoidBombMountedOn = null;
	}

	private void ApplyRepurposedMinerSpawn(SpawnFacts spawn)
	{
		var spawned = spawn.SpawnedState.Clone();
		_clipContext.ReplayState.Add(spawned);
		_clipContext.EnsureView(spawned, _clipContext.ColorFor(spawned.Id));
		var view = _clipContext.UnitViews[spawned.Id];
		view.ConfigureMovementTrail(TrailColorFor(spawned.Id));
		view.Sync(spawned);
	}

	private bool PlayImpact(ImpactFacts impact)
	{
		ReportImpactInterest(impact);
		_clipContext.ReplayState.ApplyImpact(impact);
		if (!_clipContext.ReplayState.Contains(impact.TargetId))
			return false;

		var state = _clipContext.ReplayState.StateOf(impact.TargetId);
		_stateChanged(state);
		if (!_clipContext.UnitViews.TryGetValue(impact.TargetId, out var view))
			return false;

		if (impact.Cause == EImpactCause.VoidBombBlast && impact.TargetId == impact.SourceId)
		{
			DismissUnitPresentation(impact.TargetId);
			_removeView(impact.TargetId);
			return false;
		}

		if (!state.IsAlive)
			ClearReplayMovementTrail(impact.TargetId);

		if (state.IsAlive)
			view.Sync(state);
		else
			view.ShowPendingDeath(state);
		if (impact.Cause == EImpactCause.LightningCannonBurst)
			view.PlayLightningHitSparks();
		else
			view.PlayHitSparks();
		view.PlayDamagePopup(impact.TotalDamage);

		var died = !state.IsAlive;
		if (died)
			view.PlayDeathExplosion();

		var pause = died
			? ReplayTiming.ImpactPauseSeconds + ReplayTiming.DeathExplosionSeconds
			: ReplayTiming.ImpactPauseSeconds;

		var waitStart = Stopwatch.GetTimestamp();
		_scheduledWaitMs += pause * 1000;
		GetTree().CreateTimer(pause).Timeout += () =>
		{
			_actualWaitMs += Stopwatch.GetElapsedTime(waitStart).TotalMilliseconds;
			var cleanupStart = Stopwatch.GetTimestamp();
			if (died)
			{
				_removeView(impact.TargetId);
			}
			else if (_clipContext.UnitViews.TryGetValue(impact.TargetId, out var lingering)
				&& _clipContext.ReplayState.Contains(impact.TargetId))
			{
				lingering.Sync(_clipContext.ReplayState.StateOf(impact.TargetId));
			}

			_impactWorkMs += Stopwatch.GetElapsedTime(cleanupStart).TotalMilliseconds;
			PlayNext();
		};
		return true;
	}

	private void DismissUnitPresentation(string unitId)
	{
		ClearReplayMovementTrail(unitId);
		if (_unitViews.TryGetValue(unitId, out var view))
			view.HideVisual();
	}

	private void ClearReplayMovementTrail(string unitId)
	{
		if (_unitViews.TryGetValue(unitId, out var view))
			view.ClearMovementTrail();
	}

	private Color TrailColorFor(string actorId) =>
		_participants.GetValueOrDefault(actorId) switch
		{
			ETeam.Player => new Color(0.18f, 0.62f, 1f),
			ETeam.Enemy => new Color(1f, 0.2f, 0.16f),
			_ => new Color(0.72f, 0.78f, 0.86f),
		};

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

		if (action is VoidBombAction torpedo)
		{
			var firer = _clipContext.ReplayState.StateOf(torpedo.ActorId);
			var (launchCell, _, _) = VoidBombMount.LaunchPose(firer, torpedo.MountedOn);
			_clipContext.ReportInterest(new CameraInterest(
				[
					WorldMapping.ToWorld(firer.Position),
					WorldMapping.ToWorld(launchCell),
				],
				CameraImportance.Combat));
			return;
		}

		if (action is SpawnRepurposedMinerAction deploy)
		{
			var carrier = _clipContext.ReplayState.StateOf(deploy.ActorId);
			var (launchCell, _, _) = MinerBayMount.LaunchPose(carrier, deploy.MountedOn);
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

		var points = ImpactInterestPoints(_clipContext.ReplayState, impact);
		_clipContext.ReportInterest(new CameraInterest(points, CameraImportance.Combat));
	}

	internal static IReadOnlyList<Vector3> ImpactInterestPoints(
		ReplayState replayState,
		ImpactFacts impact)
	{
		var target = replayState.StateOf(impact.TargetId).Position;
		var points = new List<Vector3>(2);
		if (replayState.Contains(impact.SourceId))
		{
			var source = replayState.StateOf(impact.SourceId).Position;
			points.Add(WorldMapping.ToWorld(source));
		}

		points.Add(WorldMapping.ToWorld(target));
		return points;
	}

	private void Finish()
	{
		FlushPhase();
		IsPlaying = false;
		EmitTurnFlowIndexChanged();

		var totalMs = _playbackTimer.Elapsed.TotalMilliseconds;
		GameLog.Log(
			$"Turn {_turnNumber} replay: total={totalMs:F1}ms "
			+ $"playerAnim={_playerAnimMs:F1}ms "
			+ $"enemyAnim={_enemyAnimMs:F1}ms "
			+ $"upkeepAnim={_upkeepAnimMs:F1}ms "
			+ $"toEnemyAnim={_timeToEnemyAnimMs:F1}ms "
			+ $"history={_history.Count}");
		GameLog.Log(
			$"Turn {_turnNumber} replay breakdown: "
			+ $"actions={_actionCount}/{_actionWorkMs:F1}ms "
			+ $"spawns={_spawnCount}/{_spawnWorkMs:F1}ms "
			+ $"impacts={_impactCount}/{_impactWorkMs:F1}ms "
			+ $"waitScheduled={_scheduledWaitMs:F1}ms "
			+ $"waitActual={_actualWaitMs:F1}ms "
			+ $"other={totalMs - _actionWorkMs - _spawnWorkMs - _impactWorkMs - _actualWaitMs:F1}ms");

		EmitSignal(SignalName.PlaybackComplete);
	}
}
