using Godot;
using GrimSpace.Battle;
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

public partial class ShieldGlowPreview : Node3D
{
	private static readonly string[] Samples =
	[
		"Shields Up: gain shields from zero",
		"Damage: forward shield face depleted",
		"Heal: forward shield face restored",
		"Expiry: temporary shields fade to zero",
		"Brace: consolidate onto forward face",
		"Brace release: redistribute shields",
	];
	private readonly Dictionary<string, UnitView> _views = new();
	private TurnReplayPlayer _replay = null!;
	private Label _status = null!;
	private int _sample;
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
				GlowEnabled = true,
				GlowIntensity = 0.5f,
				GlowBloom = 0f,
				GlowHdrThreshold = 1.5f,
			},
		});
		AddChild(new DirectionalLight3D
		{
			RotationDegrees = new Vector3(-48f, -28f, 0f),
			LightEnergy = 1.8f,
		});
		var camera = new Camera3D
		{
			Position = new Vector3(4.5f, 5f, 10f),
			Current = true,
			Fov = 40f,
		};
		AddChild(camera);
		camera.LookAt(new Vector3(4.5f, 0f, 0f));
		foreach (var state in StartStates().Values)
		{
			var view = new UnitView();
			AddChild(view);
			view.Bind(state, Colors.White);
			_views.Add(state.Id, view);
		}
		var layer = new CanvasLayer();
		AddChild(layer);
		_status = new Label
		{
			Position = new Vector2(24, 24),
		};
		_status.AddThemeFontSizeOverride("font_size", 22);
		layer.AddChild(_status);
		_replay = new TurnReplayPlayer();
		AddChild(_replay);
		_replay.Configure(
			_views,
			_ => Colors.White,
			(_, _) => throw new InvalidOperationException("Shield preview must not spawn units."),
			_ => throw new InvalidOperationException("Shield preview must not remove units."),
			states =>
			{
				foreach (var state in states.Values)
					_views[state.Id].Sync(state);
			},
			_ => { },
			_ => { },
			_ => { });
		PlaySample(0);
	}

	public override void _Process(double delta)
	{
		if (!_autoPlay)
			return;
		_elapsed += delta;
		if (_elapsed >= 1.6 && !_replay.IsPlaying)
			PlaySample((_sample + 1) % Samples.Length);
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } key)
			return;
		if (key.Keycode == Key.Space)
		{
			_autoPlay = !_autoPlay;
			_elapsed = 0;
		}
		else if (key.Keycode >= Key.Key1 && key.Keycode <= Key.Key6 && !_replay.IsPlaying)
			PlaySample((int)key.Keycode - (int)Key.Key1);
		else
			return;
		GetViewport().SetInputAsHandled();
	}

	private void PlaySample(int sample)
	{
		_sample = sample;
		_elapsed = 0;
		var start = StartStates();
		var before = start["shield-changing"];
		if (sample is 0 or 2 or 3)
			before.CurrentShieldPoints.Fill(0);
		if (sample == 3)
			TemporaryShields.GrantPerFace(before, "preview", 50);
		if (sample == 5)
		{
			ShieldBrace.Activate(before, ESpatialOrientation.Forward);
			Defense.AbsorbWithShields(before, ESpatialOrientation.Forward, 40);
		}
		var after = before.Clone();
		IRecord record;
		switch (sample)
		{
			case 0:
				TemporaryShields.GrantPerFacePercentOfMax(after, "preview", 25);
				record = new Record<ShieldsChangedFacts>(ShieldsChangedFacts.Capture(after));
				break;
			case 1:
				var damage = after.CurrentShieldPoints[ESpatialOrientation.Forward];
				Defense.AbsorbWithShields(after, ESpatialOrientation.Forward, damage);
				record = new Record<ImpactFacts>(new(
					"shield-control", after.Id, EImpactCause.RamImpact, ESpatialOrientation.Forward, damage, 0));
				break;
			case 2:
				var restored = Defense.RestoreShieldsOnFace(after, ESpatialOrientation.Forward, 100);
				record = new Record<HealFacts>(new(
					"shield-control", after.Id, EImpactCause.ScrapDroneSwarmBurst, ESpatialOrientation.Forward, restored, 0));
				break;
			case 3:
				TemporaryShields.TryRevokeGrant(after, "preview", out _);
				record = new Record<ShieldsChangedFacts>(ShieldsChangedFacts.Capture(after));
				break;
			case 4:
				ShieldBrace.Activate(after, ESpatialOrientation.Forward);
				record = new Record<ShieldsChangedFacts>(ShieldsChangedFacts.Capture(after));
				break;
			case 5:
				ShieldBrace.Release(after);
				record = new Record<ShieldsChangedFacts>(ShieldsChangedFacts.Capture(after));
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(sample));
		}
		var end = start.ToDictionary(pair => pair.Key, pair => pair.Value.Clone());
		end[after.Id] = after;
		var replay = new TurnReplay(start, [record], end);
		_replay.ResetToLive(start, end);
		_status.Text = $"{sample + 1}. {Samples[sample]}\n"
			+ "Left: changing ship | Right: unchanged control\n"
			+ "1-6: choose sample | Space: toggle auto-cycle";
		_replay.Play(replay, 1, new Dictionary<string, ETeam>
		{
			["shield-changing"] = ETeam.Player,
			["shield-control"] = ETeam.Player,
		});
	}

	private static Dictionary<string, State> StartStates() =>
		new()
		{
			["shield-changing"] = CreateState("shield-changing", Coord.Zero),
			["shield-control"] = CreateState("shield-control", new Coord(3, 0, 0)),
		};

	private static State CreateState(string id, Coord position) =>
		State.FromShipInstance(
			ShipInstance.FromSpec(id, FighterSpec.Instance, ShipCatalog.FullFighterLoadout()),
			position);
}
