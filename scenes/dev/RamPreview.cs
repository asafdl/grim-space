using Godot;
using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Specs;

namespace GrimSpace.Dev;

public partial class RamPreview : Node3D
{
	private readonly Dictionary<string, UnitView> _views = new();
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
				BackgroundColor = new Color(0.012f, 0.02f, 0.035f),
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
			Position = new Vector3(12, 3, 4.5f),
			Current = true,
			Fov = 40,
		};
		AddChild(camera);
		camera.LookAt(new Vector3(0, 0, 4.5f));
		var layer = new CanvasLayer();
		AddChild(layer);
		var label = new Label
		{
			Position = new Vector2(24, 24),
			Text = "Ram: side view | 0.6s reveal + 0.25s hold + 0.1s dash\n"
				+ "Space: replay | P: pause loop",
		};
		label.AddThemeFontSizeOverride("font_size", 22);
		layer.AddChild(label);
		_replay = new TurnReplayPlayer();
		AddChild(_replay);
		_replay.Configure(_views, _ => Colors.White,
			(_, _) => throw new InvalidOperationException("Ram must not spawn units."),
			_ => throw new InvalidOperationException("Preview hit must not kill units."),
			states =>
			{
				foreach (var state in states.Values)
				{
					if (!_views.TryGetValue(state.Id, out var view))
					{
						view = new UnitView();
						AddChild(view);
						view.Bind(state, Colors.White);
						_views.Add(state.Id, view);
					}
					view.Sync(state);
				}
			},
			_ => { }, _ => { }, _ => { });
		PlayRam();
	}

	public override void _Process(double delta)
	{
		if (!_autoPlay)
			return;
		_elapsed += delta;
		if (_elapsed >= 2.5 && !_replay.IsPlaying)
			PlayRam();
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } key)
			return;
		if (key.Keycode == Key.P)
			_autoPlay = !_autoPlay;
		else if (key.Keycode == Key.Space && !_replay.IsPlaying)
			PlayRam();
		else
			return;
		GetViewport().SetInputAsHandled();
	}

	private void PlayRam()
	{
		_elapsed = 0;
		var actor = CreateState("ram-actor", Coord.Zero);
		var target = CreateState("ram-target", actor.Fore * 3);
		var start = new Dictionary<string, State> { [actor.Id] = actor, [target.Id] = target };
		var end = start.ToDictionary(pair => pair.Key, pair => pair.Value.Clone());
		end[actor.Id].Position = target.Position - actor.Fore;
		var face = ESpatialOrientation.Retro;
		var damage = System.Math.Min(22, target.HullPoints - 1);
		end[target.Id].HullPoints -= damage;
		_replay.ResetToLive(start, end);
		_replay.Play(new TurnReplay(start,
			[new RamAction(actor.Id),
				new Record<AreaDamageFacts>(new(actor.Id, EImpactCause.RamImpact,
					actor.Position, new HashSet<Coord> { target.Position })),
				new Record<ImpactFacts>(new(actor.Id, target.Id, EImpactCause.RamImpact, face, 0, damage))],
			end), 1, new Dictionary<string, ETeam>
		{
			[actor.Id] = ETeam.Player,
			[target.Id] = ETeam.Enemy,
		});
	}

	private static State CreateState(string id, Coord position) =>
		State.FromShipInstance(
			ShipInstance.FromSpec(id, FighterSpec.Instance, ShipCatalog.FullFighterLoadout()), position);
}
