using Godot;
using GrimSpace.Presentation.Ui.Hud;

namespace GrimSpace.Presentation.Dev;

public sealed partial class DevMenuOverlay : Node
{
	private readonly ModalHudShell _shell;

	public event Action? StartBattleRequested;

	public DevMenuOverlay()
	{
		_shell = new ModalHudShell(HudThemeFamily.Informative);
		AddChild(_shell);
	}

	public bool IsOpen => _shell.IsOpen;

	public void Open()
	{
		_shell.Open("Dev Menu", "Debug shortcuts");
		_shell.SetHeader(HudHeaderMode.Close);
		_shell.SetHeaderVisible(true);
		_shell.SetBackHandler(null);
		_shell.SetCloseHandler(null);

		var battleSection = HudWidgets.CreateInformativePanel("Battle");
		battleSection.Body.AddChild(new Label
		{
			Text = "Jump into the default dev duel without a star-map engagement.",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			ThemeTypeVariation = HudStyles.InformativeItemDescriptionLabelType,
		});

		_shell.SetBody(battleSection.Root);
		_shell.SetFooter(
		[
			new HudAction("Start Battle", HudActionKind.Primary, OnStartBattle),
		]);
	}

	public void Close() => _shell.Close();

	private void OnStartBattle() => StartBattleRequested?.Invoke();
}
