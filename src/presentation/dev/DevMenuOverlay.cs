using Godot;
using GrimSpace.Components;

namespace GrimSpace.Presentation.Dev;

public sealed partial class DevMenuOverlay : Control
{
	private readonly ModalShell _shell;
	private Func<bool>? _canForceBattleOutcome;
	private Action? _winBattle;
	private Action? _loseBattle;
	private Func<bool>? _canGrantResources;
	private Action? _grantResources;
	private Func<bool>? _canRevealAllUnits;
	private Action? _revealAllUnits;

	public event Action? StartBattleRequested;

	public DevMenuOverlay()
	{
		AnchorsPreset = (int)LayoutPreset.FullRect;
		AnchorRight = 1f;
		AnchorBottom = 1f;
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;
		MouseFilter = MouseFilterEnum.Ignore;

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

		var items = HudWidgets.CreateCardList();
		items.MouseFilter = Control.MouseFilterEnum.Ignore;
		items.AddChild(CreateMenuItem("Start Battle", true, OnStartBattle));

		var canForceBattleOutcome = _canForceBattleOutcome?.Invoke() == true;
		items.AddChild(CreateMenuItem("Win Battle", canForceBattleOutcome, OnWinBattle));
		items.AddChild(CreateMenuItem("Lose Battle", canForceBattleOutcome, OnLoseBattle));

		if (_canGrantResources is not null)
			items.AddChild(CreateMenuItem(
				"Add Resource Bundle",
				_canGrantResources(),
				OnGrantResources));

		if (_canRevealAllUnits is not null)
			items.AddChild(CreateMenuItem(
				"Reveal All Units",
				_canRevealAllUnits(),
				OnRevealAllUnits));

		_shell.SetBody(items);
		_shell.SetFooter([]);
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

	public void SetResourceActions(Func<bool> canGrantResources, Action grantResources)
	{
		_canGrantResources = canGrantResources;
		_grantResources = grantResources;
	}

	public void ClearResourceActions()
	{
		_canGrantResources = null;
		_grantResources = null;
	}

	public void SetMapActions(Func<bool> canRevealAllUnits, Action revealAllUnits)
	{
		_canRevealAllUnits = canRevealAllUnits;
		_revealAllUnits = revealAllUnits;
	}

	public void ClearMapActions()
	{
		_canRevealAllUnits = null;
		_revealAllUnits = null;
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

	private void OnGrantResources()
	{
		Close();
		_grantResources?.Invoke();
	}

	private void OnRevealAllUnits()
	{
		Close();
		_revealAllUnits?.Invoke();
	}

	private static Button CreateMenuItem(string text, bool enabled, Action onPressed)
	{
		var button = HudWidgets.CreateCompactButton(text, onPressed);
		button.Disabled = !enabled;
		button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		button.Alignment = HorizontalAlignment.Left;
		return button;
	}
}
