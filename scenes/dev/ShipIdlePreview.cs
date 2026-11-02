using Godot;
using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Dev;

public partial class ShipIdlePreview : Node3D
{
	private readonly Dictionary<string, UnitView> _views = new();
	private readonly Dictionary<string, State> _states = new();
	private TurnReplayPlayer _replay = null!;
	private Label _status = null!;

	public override void _Ready()
	{
		AddChild(new WorldEnvironment
		{
			Environment = new Godot.Environment
			{
				BackgroundMode = Godot.Environment.BGMode.Color,
				BackgroundColor = new Color(0.008f, 0.012f, 0.022f),
				AmbientLightSource = Godot.Environment.AmbientSource.Color,
				AmbientLightColor = new Color(0.65f, 0.72f, 0.85f),
				AmbientLightEnergy = 0.8f,
				GlowEnabled = true,
				GlowIntensity = 0.5f,
				GlowHdrThreshold = 1.5f,
			},
		});
		AddChild(new DirectionalLight3D
		{
			RotationDegrees = new Vector3(-48, -28, 0),
			LightEnergy = 1.8f,
		});
		var camera = new Camera3D
		{
			Position = new Vector3(5, 4, 9),
			Current = true,
			Fov = 45,
		};
		AddChild(camera);
		var state = State.FromShipInstance(ShipCatalog.CreateInstance("idle-Fighter", EType.Fighter),
			Coord.Zero);
		var view = new UnitView();
		AddChild(view);
		view.Bind(state, Colors.White);
		_views.Add(state.Id, view);
		_states.Add(state.Id, state);
		camera.LookAt(view.Position);
		var layer = new CanvasLayer();
		AddChild(layer);
		_status = new Label { Position = new Vector2(24, 24) };
		_status.AddThemeFontSizeOverride("font_size", 22);
		layer.AddChild(_status);
		_replay = new TurnReplayPlayer();
		AddChild(_replay);
		_replay.Configure(_views, _ => Colors.White,
			(_, _) => throw new InvalidOperationException("Idle preview must not spawn units."),
			_ => throw new InvalidOperationException("Idle preview must not remove units."),
			states =>
			{
				foreach (var state in states.Values)
					_views[state.Id].Sync(state);
			},
			_ => { }, _ => { }, _ => { });
		_replay.PlaybackComplete += () => SetStatus("Idle resumed on Fighter.");
		SetStatus("Fighter idle; rare rolls occur naturally.");
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } key)
			return;
		if (key.Keycode == Key.B)
			TriggerBarrelRolls();
		else if (key.Keycode == Key.Space)
			PlayReplay();
		else
			return;
		GetViewport().SetInputAsHandled();
	}

	public void TriggerBarrelRolls()
	{
		var count = _views.Values.Count(view => view.TryPlayIdleBarrelRoll());
		SetStatus(count > 0 ? "Triggered a barrel roll." : "Fighter is busy or already rolling.");
	}

	public void PlayReplay()
	{
		if (_replay.IsPlaying)
		{
			SetStatus("Fighter replay is already running.");
			return;
		}
		const string actorId = "idle-Fighter";
		var history = new List<ITimelineEntry> { new OverchargeEnginesAction(actorId) };
		for (var i = 0; i < 12; i++)
			history.Add(new MoveStepAction(actorId,
				i % 2 == 0 ? ESpatialOrientation.Forward : ESpatialOrientation.Retro));
		_replay.ResetToLive(_states, _states);
		_replay.Play(new TurnReplay(_states, history, _states), 1,
			_states.Keys.ToDictionary(id => id, _ => ETeam.Player));
		SetStatus("Fighter replay: idle settles until playback finishes.");
	}

	private void SetStatus(string message) =>
		_status.Text = "Ship idle: +/-0.06 sideways, two 0.04-high arcs, gentle banking, 4s cycle\n"
			+ "B: force idle barrel rolls | Space: Fighter replay (interrupts its roll)\n"
			+ "Natural barrel roll: 1% per idle cycle, 3s duration\n" + message;
}
