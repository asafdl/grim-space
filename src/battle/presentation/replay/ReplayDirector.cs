using Godot;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Units;
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
	private Func<Vector3> _playerPosition = () => Vector3.Zero;
	private Func<string, Color> _colorForActor = _ => Colors.White;

	private TurnReplay? _pendingReplay;
	private int _pendingCompletedTurn;
	private IReadOnlyDictionary<string, State>? _playbackEndStates;
	private EBattlePhase? _lastPhase;

	public void Configure(
		BattleOrchestrator battle,
		TurnReplayPlayer replayPlayer,
		BattleView battleView,
		BattleCameraDirector cameraDirector,
		Func<Vector3> playerPosition,
		Func<string, Color> colorForActor)
	{
		_battle = battle;
		_replayPlayer = replayPlayer;
		_battleView = battleView;
		_cameraDirector = cameraDirector;
		_playerPosition = playerPosition;
		_colorForActor = colorForActor;

		_battle.PhaseChanged += OnPhaseChanged;
		_battle.TurnResolved += OnTurnResolved;
		_replayPlayer.PlaybackComplete += OnPlaybackComplete;
	}

	public override void _Process(double delta)
	{
		if (_pendingReplay is TurnReplay replay)
		{
			_pendingReplay = null;
			StartPlayback(replay, _pendingCompletedTurn);
		}
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
			case EBattlePhase.PlayerTurn when previous == EBattlePhase.Replaying:
				_cameraDirector.ReturnControl(_playerPosition());
				break;

			case EBattlePhase.Replaying:
				_cameraDirector.BeginPlayback();
				break;
		}
	}

	private void StartPlayback(TurnReplay replay, int completedTurn)
	{
		_playbackEndStates = replay.EndStates;
		_replayPlayer.ResetToLive(
			replay.StartStates,
			replay.EndStates,
			interest => _cameraDirector.ReportInterest(interest));
		_replayPlayer.Play(replay.History, completedTurn, ParticipantTeams());
	}

	private IReadOnlyDictionary<string, ETeam> ParticipantTeams() =>
		UnitRegistry.For(_battle.Engine.World).All
			.ToDictionary(unit => unit.State.Id, unit => unit.Alliance.Team);

	private void OnPlaybackComplete()
	{
		if (_playbackEndStates is not null)
			_battleView.ApplyUnitStates(_playbackEndStates, _colorForActor);

		_playbackEndStates = null;
		_battle.NotifyReplayComplete();
	}
}
