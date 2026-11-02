using Godot;
using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.Units;
using GrimSpace.Core;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Specs;

namespace GrimSpace.Dev;

public partial class ProtectionDronePreview : Node3D
{
	private static readonly string[] Stages =
	[
		"Launch: caster sends one protection drone",
		"Movement: drone follows the protected ship",
		"Hover: drone remains while the buff is active",
		"Expiry: drone fades away",
		"Launch again: prepare target-death case",
		"Target death: drone fades early",
	];
	private readonly Dictionary<string, UnitView> _views = new();
	private Dictionary<string, State> _states = new();
	private TurnReplayPlayer _replay = null!;
	private Label _status = null!;
	private int _stage = -1;
	private double _elapsed;
	private bool _autoPlay = true;

	public override void _Ready()
	{
		AddChild(new WorldEnvironment
		{
			Environment = new Godot.Environment
			{
				BackgroundMode = Godot.Environment.BGMode.Color,
				BackgroundColor = new Color(0.008f, 0.012f, 0.022f),
				AmbientLightSource = Godot.Environment.AmbientSource.Color,
				AmbientLightColor = new Color(0.55f, 0.64f, 0.78f),
				AmbientLightEnergy = 0.75f,
				TonemapMode = Godot.Environment.ToneMapper.Filmic,
			},
		});
		AddChild(new DirectionalLight3D
		{
			RotationDegrees = new Vector3(-48, -28, 0),
			LightEnergy = 1.8f,
		});
		var camera = new Camera3D
		{
			Position = new Vector3(4.5f, 7, 13),
			Current = true,
			Fov = 40,
		};
		AddChild(camera);
		camera.LookAt(new Vector3(4.5f, 0, 0));
		var layer = new CanvasLayer();
		AddChild(layer);
		_status = new Label { Position = new Vector2(24, 24) };
		_status.AddThemeFontSizeOverride("font_size", 22);
		layer.AddChild(_status);
		_replay = new TurnReplayPlayer();
		AddChild(_replay);
		_replay.Configure(
			_views, _ => Colors.White, EnsureView,
			id =>
			{
				_views[id].QueueFree();
				_views.Remove(id);
			},
			states =>
			{
				foreach (var state in states.Values.Where(state => state.IsAlive))
				{
					EnsureView(state, Colors.White);
					_views[state.Id].Sync(state);
				}
			},
			_ => { }, _ => { }, _ => { });
		PlayNextStage();
	}

	public override void _Process(double delta)
	{
		if (!_autoPlay)
			return;
		_elapsed += delta;
		if (_elapsed >= 2.2 && !_replay.IsPlaying)
			PlayNextStage();
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } key)
			return;
		if (key.Keycode == Key.Space)
			_autoPlay = !_autoPlay;
		else if (key.Keycode == Key.N && !_replay.IsPlaying)
			PlayNextStage();
		else
			return;
		GetViewport().SetInputAsHandled();
	}

	private void EnsureView(State state, Color color)
	{
		if (_views.ContainsKey(state.Id))
			return;
		var view = new UnitView();
		AddChild(view);
		view.Bind(state, color);
		_views.Add(state.Id, view);
	}

	private void PlayNextStage()
	{
		_stage = (_stage + 1) % Stages.Length;
		_elapsed = 0;
		if (_stage is 0 or 4)
			_states = new Dictionary<string, State>
			{
				["drone-caster"] = CreateState("drone-caster", Coord.Zero),
				["drone-target"] = CreateState("drone-target", new Coord(3, 0, 0)),
			};
		var start = _states.ToDictionary(pair => pair.Key, pair => pair.Value.Clone());
		var end = start.ToDictionary(pair => pair.Key, pair => pair.Value.Clone());
		var target = end["drone-target"];
		IReadOnlyList<ITimelineEntry> history = [];
		switch (_stage)
		{
			case 0:
			case 4:
				var status = BattleStatuses.CreateProtectionDrone(target.Id);
				target.Statuses.Add(status);
				history = [new Record<ProtectionDroneChangedFacts>(
					new("drone-caster", target.Id, status.DeepCopy()))];
				break;
			case 1:
				var move = new MoveStepAction(target.Id, ESpatialOrientation.Starboard);
				var moved = new ReplayState(start);
				moved.ApplyMove(move);
				end[target.Id] = moved.StateOf(target.Id).Clone();
				history = [move];
				break;
			case 3:
				target.Statuses.Clear();
				history = [new Record<ProtectionDroneChangedFacts>(new(target.Id, target.Id, null))];
				break;
			case 5:
				var damage = target.HullPoints;
				target.HullPoints = 0;
				history = [new Record<ImpactFacts>(new(
					"drone-caster", target.Id, EImpactCause.RamImpact, ESpatialOrientation.Forward, 0, damage))];
				break;
		}
		_states = end;
		_status.Text = $"{_stage + 1}. {Stages[_stage]}\n"
			+ "Left: caster | Right: protected target\nSpace: pause/resume cycle | N: next stage";
		_replay.ResetToLive(start, end);
		_replay.Play(new TurnReplay(start, history, end), 1, new Dictionary<string, ETeam>
		{
			["drone-caster"] = ETeam.Player,
			["drone-target"] = ETeam.Player,
		});
	}

	private static State CreateState(string id, Coord position) =>
		State.FromShipInstance(
			ShipInstance.FromSpec(id, FighterSpec.Instance, ShipCatalog.FullFighterLoadout()), position);
}
