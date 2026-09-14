using Godot;
using GrimSpace.Components;

namespace GrimSpace.Presentation.Dev;

public sealed partial class DevMenuOverlay : Node
{
	private readonly ModalShell _shell;
	private Func<bool>? _canForceBattleOutcome;
	private Action? _winBattle;
	private Action? _loseBattle;

	public event Action? StartBattleRequested;

	public DevMenuOverlay()
	{
		_shell = new ModalShell(HudThemeFamily.Informative);
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
		var actions = new List<HudAction>
		{
			new("Start Battle", HudActionKind.Secondary, OnStartBattle),
		};
		if (_canForceBattleOutcome is not null)
		{
			var enabled = _canForceBattleOutcome();
			actions.Add(new HudAction("Win Battle", HudActionKind.Primary, OnWinBattle, enabled));
			actions.Add(new HudAction("Lose Battle", HudActionKind.Destructive, OnLoseBattle, enabled));
		}

		_shell.SetFooter(actions);
	}

	public void Close() => _shell.Close();

	public void SetBattleActions(Func<bool> canForceOutcome, Action winBattle, Action loseBattle)
	{
		_canForceBattleOutcome = canForceOutcome;
		_winBattle = winBattle;
		_loseBattle = loseBattle;
	}

	public void ClearBattleActions()
	{
		_canForceBattleOutcome = null;
		_winBattle = null;
		_loseBattle = null;
	}

	private void OnStartBattle() => StartBattleRequested?.Invoke();

	private void OnWinBattle()
	{
		Close();
		_winBattle?.Invoke();
	}

	private void OnLoseBattle()
	{
		Close();
		_loseBattle?.Invoke();
	}
}
