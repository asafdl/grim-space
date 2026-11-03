using Godot;
using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Specs;

namespace GrimSpace.Dev;

public partial class HeatVentsPreview : Node3D
{
	private Camera3D _camera = null!;
	private float _angle;
	private float _elevation = 0.5f;
	private UnitView _ship = null!;
	private State _state = null!;
	private TurnReplayPlayer _replay = null!;
	private double _elapsed;
	private bool _autoPlay = true;

	public override void _Ready()
	{
		AddChild(new WorldEnvironment
		{
			Environment = new Godot.Environment
			{
				BackgroundMode = Godot.Environment.BGMode.Color,
				BackgroundColor = new Color(0.018f, 0.025f, 0.04f),
				AmbientLightSource = Godot.Environment.AmbientSource.Color,
				AmbientLightColor = new Color(0.65f, 0.72f, 0.85f),
				AmbientLightEnergy = 0.8f,
			},
		});
		AddChild(new DirectionalLight3D
		{
			RotationDegrees = new Vector3(-48, -28, 0),
			LightEnergy = 1.8f,
		});
		_camera = new Camera3D { Current = true, Fov = 40 };
		AddChild(_camera);
		UpdateCamera();
		var state = State.FromShipInstance(
			ShipInstance.FromSpec("vent-preview", FighterSpec.Instance, ShipCatalog.FullFighterLoadout()),
			Coord.Zero);
		state.CurrentShieldPoints.Fill(0);
		_state = state;
		_ship = new UnitView();
		AddChild(_ship);
		_ship.Bind(state, Colors.White);
		_replay = new TurnReplayPlayer();
		AddChild(_replay);
		_replay.Configure(new Dictionary<string, UnitView> { [state.Id] = _ship },
			_ => Colors.White,
			(_, _) => throw new InvalidOperationException("Heat Vents must not spawn units."),
			_ => throw new InvalidOperationException("Heat Vents must not remove units."),
			states => _ship.Sync(states[state.Id]),
			_ => { }, _ => { }, _ => { });
		PlayBurst();
		var layer = new CanvasLayer();
		AddChild(layer);
		var label = new Label
		{
			Position = new Vector2(24, 24),
			Text = "Heat Vents: 0.8s billboard burst + air-release SFX\n"
				+ "Port + starboard | original artwork + mirrored copy\n"
				+ "Left/Right: orbit | Up/Down: elevation | Space: replay | P: pause loop",
		};
		label.AddThemeFontSizeOverride("font_size", 22);
		layer.AddChild(label);
	}

	public override void _Process(double delta)
	{
		if (!_autoPlay)
			return;
		_elapsed += delta;
		if (_elapsed >= 1.8)
		{
			_elapsed = 0;
			PlayBurst();
		}
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true } key)
			return;
		switch (key.Keycode)
		{
			case Key.Space:
				_elapsed = 0;
				PlayBurst();
				break;
			case Key.P: _autoPlay = !_autoPlay; break;
			case Key.Left: _angle -= 0.3f; break;
			case Key.Right: _angle += 0.3f; break;
			case Key.Up: _elevation = Mathf.Min(1.3f, _elevation + 0.15f); break;
			case Key.Down: _elevation = Mathf.Max(0.1f, _elevation - 0.15f); break;
			default: return;
		}
		UpdateCamera();
		GetViewport().SetInputAsHandled();
	}

	private void PlayBurst()
	{
		var before = _state.Clone();
		before.Heat = 60;
		var after = before.Clone();
		after.Heat = System.Math.Max(0, before.Heat - PilotSkillPercents.HeatVents.HeatReleased);
		var start = new Dictionary<string, State> { [before.Id] = before };
		var end = new Dictionary<string, State> { [after.Id] = after };
		_replay.ResetToLive(start, end);
		_replay.Play(new TurnReplay(start,
			[new HeatVentsAction(before.Id),
				new Record<HeatChangedFacts>(new(before.Id, after.Heat, after.Overdrive, after.Overheat))],
			end), 1, new Dictionary<string, ETeam> { [before.Id] = ETeam.Player });
	}

	private void UpdateCamera()
	{
		_camera.Position = new Vector3(
			Mathf.Sin(_angle) * Mathf.Cos(_elevation),
			Mathf.Sin(_elevation),
			Mathf.Cos(_angle) * Mathf.Cos(_elevation)) * 8f;
		_camera.LookAt(Vector3.Zero);
	}
}
