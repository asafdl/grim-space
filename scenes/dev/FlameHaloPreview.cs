using Godot;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Dev;

public partial class FlameHaloPreview : Node3D
{
	private static readonly Color[] UnitTints =
	[
		new(0.3f, 0.72f, 0.94f),
		new(0.89f, 0.52f, 0.2f),
		new(0.7f, 0.76f, 0.82f),
	];
	private static readonly string[] SampleNames =
	[
		"WARMUP",
		"OVERDRIVE",
		"OVERHEAT",
	];
	private readonly State[] _states = new State[3];
	private readonly UnitView[] _views = new UnitView[3];
	private int _selected;
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
				AmbientLightColor = new Color(0.55f, 0.64f, 0.78f),
				AmbientLightEnergy = 0.75f,
				GlowEnabled = true,
				GlowIntensity = 0.8f,
				GlowBloom = 0.08f,
			},
		});
		AddChild(new DirectionalLight3D
		{
			RotationDegrees = new Vector3(-48f, -28f, 0f),
			LightColor = new Color(0.72f, 0.84f, 1f),
			LightEnergy = 1.8f,
			ShadowEnabled = false,
		});

		var camera = new Camera3D
		{
			Position = new Vector3(9f, 7.5f, 22f),
			Current = true,
			Fov = 40f,
		};
		AddChild(camera);
		camera.LookAt(new Vector3(9f, 2.1f, 1f), Vector3.Up);

		AddFloor();
		BuildSamples();
		BuildOverlay();
		UpdateStatus();
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } key)
			return;

		switch (key.Keycode)
		{
			case Key.Left:
				_selected = (_selected + _states.Length - 1) % _states.Length;
				break;
			case Key.Right:
				_selected = (_selected + 1) % _states.Length;
				break;
			case Key.Up:
				_states[_selected].Heat = System.Math.Min(
					_states[_selected].Heat + 10,
					_states[_selected].HeatProfile.EffectiveCapacity);
				break;
			case Key.Down:
				_states[_selected].Heat = System.Math.Max(_states[_selected].Heat - 10, 0);
				break;
			case Key.O:
				_states[_selected].Overdrive = !_states[_selected].Overdrive;
				if (_states[_selected].Overdrive)
					_states[_selected].Overheat = false;
				break;
			case Key.H:
				_states[_selected].Overheat = !_states[_selected].Overheat;
				if (_states[_selected].Overheat)
				{
					_states[_selected].Overdrive = false;
					_states[_selected].Heat = System.Math.Max(
						_states[_selected].Heat,
						_states[_selected].HeatProfile.EffectiveCapacity);
				}
				break;
			case Key.R:
				BuildSamples();
				break;
			default:
				return;
		}

		_views[_selected].Sync(_states[_selected]);
		UpdateStatus();
		GetViewport().SetInputAsHandled();
	}

	private void BuildSamples()
	{
		var types = new[] { EType.Fighter, EType.Gunship, EType.Carrier };
		var positions = new[] { new Coord(1, 0, 0), new Coord(4, 0, 0), new Coord(7, 0, 0) };
		for (var index = 0; index < _states.Length; index++)
		{
			var state = CreateState(types[index], positions[index], index);
			_states[index] = state;
			if (_views[index] is null)
			{
				var view = new UnitView { Name = $"PreviewUnit{index + 1}" };
				AddChild(view);
				view.Bind(state, UnitTints[index]);
				_views[index] = view;
				AddDisplayLabel(state, index);
			}
			else
			{
				_views[index].Sync(state);
			}
		}
	}

	private static State CreateState(EType type, Coord position, int sample)
	{
		var chassis = ShipCatalog.SpecFor(type);
		var loadout = ShipCatalog.NewRunLoadoutFor(type);
		var heat = chassis.Heat.EffectiveCapacity;
		return new State
		{
			Id = $"flame-preview-{sample}",
			Type = type,
			Initiative = chassis.Initiative,
			Loadout = loadout.DeepCopy(),
			Position = position,
			Fore = Coord.Forward,
			Dorsal = Coord.Up,
			Starboard = Coord.Cross(Coord.Up, Coord.Forward),
			Maneuverability = chassis.Maneuverability,
			HeatProfile = chassis.Heat,
			Heat = sample == 0 ? heat / 4 : heat,
			Overdrive = sample == 1,
			Overheat = sample == 2,
			HullPoints = loadout.MaxHullPoints,
		};
	}

	private void AddFloor()
	{
		var floor = new MeshInstance3D
		{
			Position = new Vector3(9f, -0.09f, 0f),
			Mesh = new BoxMesh { Size = new Vector3(22f, 0.12f, 9f) },
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = new Color(0.025f, 0.035f, 0.052f),
				Metallic = 0.35f,
				Roughness = 0.7f,
			},
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		AddChild(floor);

		for (var index = 0; index < _states.Length; index++)
		{
			var cell = new Coord(1 + index * 3, 0, 0);
			var padPosition = WorldMapping.ToWorld(cell);
			padPosition.Y = 0.005f;
			var pad = new MeshInstance3D
			{
				Position = padPosition,
				Mesh = new CylinderMesh { TopRadius = 1.15f, BottomRadius = 1.15f, Height = 0.06f },
				MaterialOverride = new StandardMaterial3D
				{
					AlbedoColor = new Color(0.05f, 0.09f, 0.13f),
					Metallic = 0.45f,
					Roughness = 0.48f,
					EmissionEnabled = true,
					Emission = UnitTints[index],
					EmissionEnergyMultiplier = 0.16f,
				},
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			};
			AddChild(pad);
		}
	}

	private void AddDisplayLabel(State state, int index)
	{
		var label = new Label3D
		{
			Name = $"SampleLabel{index + 1}",
			Text = SampleNames[index],
			Position = WorldMapping.ToWorld(state.Position) + new Vector3(0f, 3.8f, 0f),
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			FixedSize = true,
			PixelSize = 0.0015f,
			FontSize = 28,
			OutlineSize = 6,
			Modulate = UnitTints[index],
			OutlineModulate = new Color(0.008f, 0.012f, 0.022f, 0.95f),
		};
		AddChild(label);
	}

	private void BuildOverlay()
	{
		var layer = new CanvasLayer { Name = "PreviewOverlay" };
		AddChild(layer);
		_status = new Label
		{
			Name = "Instructions",
			Position = new Vector2(24f, 20f),
			Size = new Vector2(1200f, 92f),
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		_status.AddThemeFontSizeOverride("font_size", 20);
		_status.AddThemeColorOverride("font_color", new Color(0.78f, 0.9f, 1f));
		_status.AddThemeColorOverride("font_outline_color", new Color(0.01f, 0.02f, 0.04f, 0.95f));
		_status.AddThemeConstantOverride("outline_size", 5);
		layer.AddChild(_status);
	}

	private void UpdateStatus()
	{
		var state = _states[_selected];
		var capacity = state.HeatProfile.EffectiveCapacity;
		var mode = state.Overheat ? "OVERHEAT / BLUE"
			: state.Overdrive ? "OVERDRIVE / ORANGE-RED"
			: "WARMUP / CYAN TO ORANGE";
		_status.Text =
			$"FLAME HALO PREVIEW  |  {SampleNames[_selected]} — {mode}  |  Heat {state.Heat}/{capacity}\n" +
			"←/→ select unit   ↑/↓ heat   O toggle overdrive   H toggle overheat   R reset";
	}
}
