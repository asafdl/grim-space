using Godot;
using GrimSpace.Application;
using GrimSpace.Presentation.Menu;

namespace GrimSpace.Presentation.Support;

public sealed class PauseMenuSettingsHost
{
	private const string ScenePath = "res://scenes/ui/game_settings_menu.tscn";

	private GameSettingsMenu _menu = null!;

	public bool IsOpen => _menu.Visible;

	public void Attach(Control parent)
	{
		_menu = GD.Load<PackedScene>(ScenePath).Instantiate<GameSettingsMenu>();
		_menu.Host = GameSettingsApplicator.Host.InRun;
		_menu.Visible = false;
		_menu.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_menu.AnchorRight = 1f;
		_menu.AnchorBottom = 1f;
		_menu.GrowHorizontal = Control.GrowDirection.Both;
		_menu.GrowVertical = Control.GrowDirection.Both;
		_menu.CloseRequested += () => _menu.Hide();
		parent.AddChild(_menu);
	}

	public void Open() => _menu.Open();

	public bool TryCloseOnEscape(InputEvent @event)
	{
		if (!_menu.Visible)
			return false;

		if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
			return false;

		_menu.Hide();
		return true;
	}
}
