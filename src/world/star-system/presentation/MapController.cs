using Godot;
using GrimSpace.Core;
using GrimSpace.World.StarSystem;

namespace GrimSpace.World.StarSystem.Presentation;

public partial class MapController : Node3D
{
	private const float SecondsPerTick = 0.2f;
	private static readonly float[] SpeedOptions = [0.5f, 1f, 2f, 4f, 8f];

	private MapView _view = null!;
	private UnitsView _units = null!;
	private MapCamera _camera = null!;
	private PanelContainer _tooltip = null!;
	private Label _typeLabel = null!;
	private Label _nameLabel = null!;
	private Label _tickLabel = null!;
	private Label _systemLabel = null!;
	private Button _pauseButton = null!;
	private Button _stepButton = null!;
	private Button _speedButton = null!;
	private CanvasLayer _uiLayer = null!;

	private StarSystemOrchestrator _orchestrator = null!;
	private float _tickAccumulator;
	private bool _paused;
	private int _speedIndex = 1;

	public override void _Ready()
	{
		_view = GetNode<MapView>("MapView");
		_units = GetNode<UnitsView>("UnitsView");
		_camera = GetNode<MapCamera>("Camera3D");

		if (GetNodeOrNull<DirectionalLight3D>("DirectionalLight3D") is { } light)
		{
			light.LightColor = new Color(0.70f, 0.78f, 0.92f);
			light.LightEnergy = 0.22f;
		}

		_orchestrator = RunSession.Instance.Run.Traffic;
		BuildTooltip();
		BuildDebugUi();

		var world = RunSession.Instance.Run.Map;
		var halfX = world.Width * MapMapping.WorldUnitsPerPoint * 0.5f;
		var halfZ = world.Height * MapMapping.WorldUnitsPerPoint * 0.5f;
		var mapRadius = Mathf.Max(halfX, halfZ);

		var backdrop = new MapBackdrop();
		backdrop.Build(mapRadius);
		AddChild(backdrop);
		MoveChild(backdrop, 0);

		_view.Build(world);
		_units.Build(world);
		_camera.Configure(Vector3.Zero, halfX, halfZ);
		UpdateSystemLabel(world);
		UpdateDebugUi();
	}

	public override void _Process(double delta)
	{
		_view.SetCameraDistance(_camera.Distance);

		if (!_paused)
			AdvanceSimulation(delta);

		var world = _orchestrator.Map;
		var tickFraction = _tickAccumulator / SecondsPerTick;
		_units.Sync(world, tickFraction);
		UpdateDebugUi();

		var screen = GetViewport().GetMousePosition();
		var point = MapPick.PickPoint(_camera, screen, world.Width, world.Height);
		var unitHover = point is { } unitPoint ? _units.UnitAt(world, unitPoint, tickFraction) : null;
		var dockHover = unitHover is null && point is { } dockPoint ? _view.DockAt(dockPoint) : null;
		var poiId = dockHover is null && unitHover is null && point is { } pick ? _view.PoiAt(pick) : null;
		_view.SetHovered(poiId);
		UpdateTooltip(world, poiId, dockHover, unitHover, screen);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } key)
			return;

		switch (key.Keycode)
		{
			case Key.Escape:
				GetTree().ChangeSceneToFile("res://scenes/main.tscn");
				GetViewport().SetInputAsHandled();
				break;
			case Key.Space:
				_paused = !_paused;
				GetViewport().SetInputAsHandled();
				break;
			case Key.Period when _paused:
				_orchestrator.AdvanceTick();
				_tickAccumulator = 0f;
				GetViewport().SetInputAsHandled();
				break;
			case Key.Bracketright:
				CycleSpeed(1);
				GetViewport().SetInputAsHandled();
				break;
			case Key.Bracketleft:
				CycleSpeed(-1);
				GetViewport().SetInputAsHandled();
				break;
		}
	}

	private void AdvanceSimulation(double delta)
	{
		_tickAccumulator += (float)delta * SpeedOptions[_speedIndex];
		while (_tickAccumulator >= SecondsPerTick)
		{
			_tickAccumulator -= SecondsPerTick;
			_orchestrator.AdvanceTick();
		}
	}

	private void BuildTooltip()
	{
		_typeLabel = new Label();
		_typeLabel.AddThemeFontSizeOverride("font_size", 11);
		_typeLabel.AddThemeColorOverride("font_color", new Color(0.44f, 0.51f, 0.58f)); // #718295

		_nameLabel = new Label();
		_nameLabel.AddThemeFontSizeOverride("font_size", 15);
		_nameLabel.AddThemeColorOverride("font_color", new Color(0.79f, 0.83f, 0.87f)); // #C9D4DF

		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 2);
		column.AddChild(_typeLabel);
		column.AddChild(_nameLabel);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 10);
		margin.AddThemeConstantOverride("margin_right", 10);
		margin.AddThemeConstantOverride("margin_top", 6);
		margin.AddThemeConstantOverride("margin_bottom", 6);
		margin.AddChild(column);

		_tooltip = new PanelContainer
		{
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		_tooltip.AddThemeStyleboxOverride("panel", new StyleBoxFlat
		{
			BgColor = new Color(0.02f, 0.04f, 0.07f, 0.82f),
			ContentMarginLeft = 0,
			ContentMarginRight = 0,
			ContentMarginTop = 0,
			ContentMarginBottom = 0,
			CornerRadiusTopLeft = 2,
			CornerRadiusTopRight = 2,
			CornerRadiusBottomLeft = 2,
			CornerRadiusBottomRight = 2,
		});
		_tooltip.AddChild(margin);

		var hint = new Label
		{
			Position = new Vector2(16, 16),
			Text = "F10 star map (dev) — drag/WASD pan, right orbit, wheel zoom, Space pause, Rebuild rerolls seed, Esc menu",
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		hint.AddThemeFontSizeOverride("font_size", 13);
		hint.AddThemeColorOverride("font_color", new Color(0.44f, 0.51f, 0.58f));

		var layer = new CanvasLayer();
		layer.AddChild(hint);
		layer.AddChild(_tooltip);
		AddChild(layer);
		_uiLayer = layer;
	}

	private void BuildDebugUi()
	{
		_tickLabel = new Label();
		_tickLabel.AddThemeFontSizeOverride("font_size", 13);
		_tickLabel.AddThemeColorOverride("font_color", new Color(0.79f, 0.83f, 0.87f));

		_pauseButton = new Button { Text = "Pause" };
		_pauseButton.Pressed += () => _paused = !_paused;

		_stepButton = new Button { Text = "Step" };
		_stepButton.Pressed += () =>
		{
			_orchestrator.AdvanceTick();
			_tickAccumulator = 0f;
		};

		_speedButton = new Button();
		_speedButton.Pressed += () => CycleSpeed(1);

		var rebuildButton = new Button { Text = "Rebuild" };
		rebuildButton.Pressed += RebuildScene;

		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 8);
		row.AddChild(_tickLabel);
		row.AddChild(_pauseButton);
		row.AddChild(_stepButton);
		row.AddChild(_speedButton);
		row.AddChild(rebuildButton);

		var panel = new PanelContainer
		{
			Position = new Vector2(16, 44),
			MouseFilter = Control.MouseFilterEnum.Stop,
		};
		panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
		{
			BgColor = new Color(0.02f, 0.04f, 0.07f, 0.82f),
			ContentMarginLeft = 10,
			ContentMarginRight = 10,
			ContentMarginTop = 8,
			ContentMarginBottom = 8,
			CornerRadiusTopLeft = 2,
			CornerRadiusTopRight = 2,
			CornerRadiusBottomLeft = 2,
			CornerRadiusBottomRight = 2,
		});
		panel.AddChild(row);
		_uiLayer.AddChild(panel);

		_systemLabel = new Label
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		_systemLabel.AddThemeFontSizeOverride("font_size", 13);
		_systemLabel.AddThemeColorOverride("font_color", new Color(0.44f, 0.51f, 0.58f));
		_systemLabel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
		_systemLabel.OffsetLeft = -220;
		_systemLabel.OffsetTop = 16;
		_systemLabel.OffsetRight = -16;
		_systemLabel.OffsetBottom = 36;
		_systemLabel.HorizontalAlignment = HorizontalAlignment.Right;
		_uiLayer.AddChild(_systemLabel);
	}

	private void UpdateDebugUi()
	{
		_tickLabel.Text = $"Tick {_orchestrator.Tick}";
		_pauseButton.Text = _paused ? "Resume" : "Pause";
		_stepButton.Disabled = !_paused;
		_speedButton.Text = $"Speed {SpeedOptions[_speedIndex]:0.#}x";
	}

	private void CycleSpeed(int delta)
	{
		_speedIndex = Mathf.PosMod(_speedIndex + delta, SpeedOptions.Length);
	}

	private void RebuildScene()
	{
		RunSession.Instance.RegenerateMap();
		GetTree().ReloadCurrentScene();
	}

	private void UpdateSystemLabel(StarMap world)
	{
		var blueprint = world.Blueprint;
		_systemLabel.Text = $"{blueprint.SystemClass} · seed {blueprint.Seed} · {blueprint.SupplyPlan.ResourceId}";
	}

	private void UpdateTooltip(
		StarMap world,
		string? poiId,
		MapView.DockHoverInfo? dockHover,
		UnitsView.UnitHoverInfo? unitHover,
		Vector2 screen)
	{
		if (unitHover is not null)
		{
			_typeLabel.Text = unitHover.Phase.ToString().ToUpperInvariant();
			_nameLabel.Text = $"{unitHover.Type} ({unitHover.UnitId})";
			_tooltip.Visible = true;
			_tooltip.Position = screen + new Vector2(14, 18);
			return;
		}

		if (dockHover is not null)
		{
			_typeLabel.Text = "DOCK";
			_nameLabel.Text = dockHover.DisplayName;
			_tooltip.Visible = true;
			_tooltip.Position = screen + new Vector2(14, 18);
			return;
		}

		if (poiId is null)
		{
			_tooltip.Visible = false;
			return;
		}

		var poi = world.PointsOfInterest.First(p => p.Id == poiId);
		var blueprint = world.Blueprint;
		var spec = blueprint.Pois.First(p => p.Id == poiId);
		_typeLabel.Text =
			$"{blueprint.SystemClass} · seed {blueprint.Seed} · {spec.LogicalRole} · {poi.Kind} · {blueprint.SupplyPlan.ResourceId}";
		_nameLabel.Text = poi.DisplayName;
		_tooltip.Visible = true;
		_tooltip.Position = screen + new Vector2(14, 18);
	}
}
