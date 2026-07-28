using Godot;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation.Replay;

public partial class TurnReplayPlayer : Node3D
{
	[Signal]
	public delegate void PlaybackCompleteEventHandler();

	private static readonly ReplayClipRegistry Clips = ReplayClipRegistry.Default;

	private readonly Dictionary<string, UnitView> _unitViews = new();
	private Func<string, Color> _colorFor = _ => Colors.White;

	private TurnHistoryView _turnHistory = null!;
	private HazardBurstView _hazardBursts = null!;
	private ReplayClipContext _clipContext = null!;
	private IReadOnlyList<IAction> _actions = [];
	private int _actionIndex;

	public bool IsPlaying { get; private set; }

	public void Configure(IReadOnlyDictionary<string, UnitView> unitViews, Func<string, Color> colorFor)
	{
		_unitViews.Clear();
		foreach (var (unitId, view) in unitViews)
			_unitViews[unitId] = view;

		_colorFor = colorFor;

		_turnHistory = new TurnHistoryView { Name = "TurnHistory" };
		AddChild(_turnHistory);

		_hazardBursts = new HazardBurstView { Name = "HazardBursts" };
		AddChild(_hazardBursts);
	}

	public void PrepareTurnStart(IReadOnlyDictionary<string, State> turnStart)
	{
		var replayState = new ReplayState(turnStart);
		_clipContext = new ReplayClipContext(replayState, _unitViews, _turnHistory, _hazardBursts, _colorFor);
		_turnHistory.BeginTurn(turnStart.ToDictionary(pair => pair.Key, pair => pair.Value.Position));
		_hazardBursts.Clear();

		foreach (var (unitId, state) in turnStart)
			_unitViews[unitId].SyncFromState(state);
	}

	public void Play(IReadOnlyList<IAction> actions)
	{
		_actions = actions;
		_actionIndex = 0;
		IsPlaying = true;
		PlayNext();
	}

	private void PlayNext()
	{
		while (_actionIndex < _actions.Count)
		{
			var action = _actions[_actionIndex++];
			Clips.TryPlay(action, _clipContext, out var playback);

			if (playback.Pauses)
			{
				GetTree().CreateTimer(playback.PauseSeconds).Timeout += PlayNext;
				return;
			}
		}

		Finish();
	}

	private void Finish()
	{
		IsPlaying = false;
		EmitSignal(SignalName.PlaybackComplete);
	}
}
