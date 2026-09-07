using Godot;
using GrimSpace.Presentation.Ui.Hud;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed partial class BattlePauseMenuOverlay : Node
{
	public event Action? ContinueRequested;
	public event Action? RetireRequested;
	public event Action? RestartRequested;
	public event Action? MainMenuRequested;

	private readonly ModalHudShell _shell;

	public bool Visible
	{
		get => _shell.Visible;
		set
		{
			if (value)
				Open();
			else
				_shell.Visible = false;
		}
	}

	public BattlePauseMenuOverlay()
	{
		_shell = new ModalHudShell();
		AddChild(_shell);
		_shell.SetCloseHandler(() => ContinueRequested?.Invoke());
		Build();
	}

	private void Build()
	{
		var body = new VBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		body.AddThemeConstantOverride("separation", 12);

		body.AddChild(HudWidgets.CreateMenuButton(
			BattleHudCopy.Continue,
			BattleHudCopy.ContinueTooltip,
			() => ContinueRequested?.Invoke()));
		body.AddChild(HudWidgets.CreateMenuButton(
			BattleHudCopy.Retire,
			BattleHudCopy.RetireTooltip,
			() => RetireRequested?.Invoke()));
		body.AddChild(HudWidgets.CreateMenuButton(
			BattleHudCopy.Restart,
			BattleHudCopy.RestartTooltip,
			() => RestartRequested?.Invoke()));
		body.AddChild(HudWidgets.CreateMenuButton(
			BattleHudCopy.MainMenu,
			BattleHudCopy.MainMenuTooltip,
			() => MainMenuRequested?.Invoke()));

		_shell.SetBody(body);
		_shell.SetFooter([]);
	}

	public void Open()
	{
		_shell.Open(BattleHudCopy.PauseMenuTitle);
		_shell.SetSubtitle("");
		_shell.SetHeader(HudHeaderMode.Close);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible)
			return;

		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
		{
			ContinueRequested?.Invoke();
			_shell.Close();
			GetViewport().SetInputAsHandled();
		}
	}
}
