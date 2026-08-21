using Godot;
using GrimSpace.Core;

namespace GrimSpace.World.StarSystem.Presentation;

public partial class MapController : Node3D
{
	private MapView _view = null!;
	private MapCamera _camera = null!;
	private PanelContainer _tooltip = null!;
	private Label _typeLabel = null!;
	private Label _nameLabel = null!;

	public override void _Ready()
	{
		_view = GetNode<MapView>("MapView");
		_camera = GetNode<MapCamera>("Camera3D");

		if (GetNodeOrNull<DirectionalLight3D>("DirectionalLight3D") is { } light)
		{
			light.LightColor = new Color(0.70f, 0.78f, 0.92f);
			light.LightEnergy = 0.22f;
		}

		BuildTooltip();

		var world = RunSession.Instance.Run.Map;
		var halfX = world.Width * MapMapping.CellSize * 0.5f;
		var halfZ = world.Height * MapMapping.CellSize * 0.5f;
		var mapRadius = Mathf.Max(halfX, halfZ);

		var backdrop = new MapBackdrop();
		backdrop.Build(mapRadius);
		AddChild(backdrop);
		MoveChild(backdrop, 0);

		_view.Build(world);
		_camera.Configure(Vector3.Zero, halfX, halfZ);
	}

	public override void _Process(double delta)
	{
		_view.SetCameraDistance(_camera.Distance);

		var screen = GetViewport().GetMousePosition();
		var cell = MapPick.PickCell(_camera, screen, RunSession.Instance.Run.Map.Width, RunSession.Instance.Run.Map.Height);
		var poiId = cell is { } c ? _view.OwnerOf(c) : null;
		_view.SetHovered(poiId);
		UpdateTooltip(poiId, screen);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
			return;

		GetTree().ChangeSceneToFile("res://scenes/main.tscn");
		GetViewport().SetInputAsHandled();
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
			Text = "F10 star map (dev) — WASD/middle pan, right orbit, wheel zoom, Esc menu",
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		hint.AddThemeFontSizeOverride("font_size", 13);
		hint.AddThemeColorOverride("font_color", new Color(0.44f, 0.51f, 0.58f));

		var layer = new CanvasLayer();
		layer.AddChild(hint);
		layer.AddChild(_tooltip);
		AddChild(layer);
	}

	private void UpdateTooltip(string? poiId, Vector2 screen)
	{
		if (poiId is null)
		{
			_tooltip.Visible = false;
			return;
		}

		var dash = poiId.IndexOf('-');
		var type = dash > 0 ? poiId[..dash] : poiId;
		var name = dash > 0 && dash < poiId.Length - 1 ? poiId[(dash + 1)..] : poiId;

		_typeLabel.Text = type.ToUpperInvariant();
		_nameLabel.Text = name;
		_tooltip.Visible = true;
		_tooltip.Position = screen + new Vector2(14, 18);
	}
}
