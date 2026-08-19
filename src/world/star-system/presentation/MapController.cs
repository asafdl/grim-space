using Godot;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Core;

namespace GrimSpace.World.StarSystem.Presentation;

public partial class MapController : Node3D
{
	private MapView _view = null!;
	private MapCamera _camera = null!;
	private Label _hoverHud = null!;

	public override void _Ready()
	{
		_view = GetNode<MapView>("MapView");
		_camera = GetNode<MapCamera>("Camera3D");

		_hoverHud = new Label
		{
			Position = new Vector2(16, 16),
			Text = "",
			Visible = false,
		};
		_hoverHud.AddThemeFontSizeOverride("font_size", 18);
		var layer = new CanvasLayer();
		layer.AddChild(_hoverHud);
		var hint = new Label
		{
			Position = new Vector2(16, 48),
			Text = "F10 star map (dev) — drag/WASD pan, wheel zoom, Esc menu",
		};
		hint.AddThemeFontSizeOverride("font_size", 14);
		hint.AddThemeColorOverride("font_color", new Color(0.7f, 0.75f, 0.85f));
		layer.AddChild(hint);
		AddChild(layer);

		var world = RunSession.Instance.Run.Map;
		var halfX = world.Width * MapMapping.CellSize * 0.5f;
		var halfZ = world.Height * MapMapping.CellSize * 0.5f;
		var mapRadius = Mathf.Max(halfX, halfZ);

		// Large chamber around origin so the camera sits inside the void; no membrane box
		// (that read as a wall/floor in front of a flat map); keep stars off the playable plane.
		var backdrop = new SpaceBackdrop();
		backdrop.Build(
			Vector3.Zero,
			new Vector3(140f, 90f, 140f),
			includeSun: false,
			starKeepOutRadius: mapRadius + 28f,
			includeMembrane: false);
		AddChild(backdrop);
		MoveChild(backdrop, 0);

		_view.Build(world);
		_camera.Configure(Vector3.Zero, halfX, halfZ);
	}

	public override void _Process(double delta)
	{
		var screen = GetViewport().GetMousePosition();
		var cell = MapPick.PickCell(_camera, screen, RunSession.Instance.Run.Map.Width, RunSession.Instance.Run.Map.Height);
		var poiId = cell is { } c ? _view.OwnerOf(c) : null;
		_view.SetHovered(poiId, cell);

		if (poiId is null)
		{
			_hoverHud.Visible = false;
		}
		else
		{
			_hoverHud.Text = poiId;
			_hoverHud.Visible = true;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
			return;

		GetTree().ChangeSceneToFile("res://scenes/main.tscn");
		GetViewport().SetInputAsHandled();
	}
}
