using Godot;
using GrimSpace.Components;
using GrimSpace.Ui.Components;

namespace GrimSpace.Presentation.Dev;

public sealed partial class DevMenuOverlay : Control
{
	private readonly ModalShell _shell;
	private Func<bool>? _canForceBattleOutcome;
	private Action? _winBattle;
	private Action? _loseBattle;
	private Func<bool>? _canGrantResources;
	private Action? _grantResources;
	private Func<bool>? _canBumpDanger;
	private Action? _bumpDanger;
	private Func<bool>? _canRevealAllUnits;
	private Action? _revealAllUnits;
	private Func<bool>? _canSpawnPilotRecruitmentLead;
	private Action? _spawnPilotRecruitmentLead;

	public event Action? StartBattleRequested;

	public DevMenuOverlay()
	{
		AnchorsPreset = (int)LayoutPreset.FullRect;
		AnchorRight = 1f;
		AnchorBottom = 1f;
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;
		MouseFilter = MouseFilterEnum.Ignore;

		_shell = new ModalShell(UiContainerVisual.Modal, HudThemeFamily.Informative);
		AddChild(_shell);
		_shell.SetDismissVisible(true);
	}

	public bool IsOpen => _shell.IsOpen;

	public void Open()
	{
		_shell.Open("Dev Menu", "Debug shortcuts");
		_shell.SetHeader(HudHeaderMode.Close);
		_shell.SetHeaderVisible(true);
		_shell.SetDismissVisible(true);
		_shell.SetBackHandler(null);

		var items = HudWidgets.CreateCardList();
		items.AddThemeConstantOverride("separation", 10);
		items.AddChild(CreateMenuItem("Start Battle", true, OnStartBattle));

		var canForceBattleOutcome = _canForceBattleOutcome?.Invoke() == true;
		items.AddChild(CreateMenuItem("Win Battle", canForceBattleOutcome, OnWinBattle));
		items.AddChild(CreateMenuItem("Lose Battle", canForceBattleOutcome, OnLoseBattle));

		if (_canGrantResources is not null)
			items.AddChild(CreateMenuItem(
				"Add Resource Bundle",
				_canGrantResources(),
				OnGrantResources));

		if (_canBumpDanger is not null)
			items.AddChild(CreateMenuItem(
				"Advance Danger Progression",
				_canBumpDanger(),
				OnBumpDanger));

		if (_canRevealAllUnits is not null)
			items.AddChild(CreateMenuItem(
				"Reveal All Units",
				_canRevealAllUnits(),
				OnRevealAllUnits));

		if (_spawnPilotRecruitmentLead is not null)
			items.AddChild(CreateMenuItem(
				"Spawn Pilot Recruitment Lead",
				_canSpawnPilotRecruitmentLead?.Invoke() == true,
				OnSpawnPilotRecruitmentLead));

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

	public void SetDangerActions(Func<bool> canBumpDanger, Action bumpDanger)
	{
		_canBumpDanger = canBumpDanger;
		_bumpDanger = bumpDanger;
	}

	public void ClearResourceActions()
	{
		_canGrantResources = null;
		_grantResources = null;
	}

	public void SetMapActions(
		Func<bool> canRevealAllUnits,
		Action revealAllUnits,
		Func<bool>? canSpawnPilotRecruitmentLead = null,
		Action? spawnPilotRecruitmentLead = null)
	{
		_canRevealAllUnits = canRevealAllUnits;
		_revealAllUnits = revealAllUnits;
		_canSpawnPilotRecruitmentLead = canSpawnPilotRecruitmentLead;
		_spawnPilotRecruitmentLead = spawnPilotRecruitmentLead;
	}

	public void ClearMapActions()
	{
		_canRevealAllUnits = null;
		_revealAllUnits = null;
		_canSpawnPilotRecruitmentLead = null;
		_spawnPilotRecruitmentLead = null;
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

	private void OnBumpDanger()
	{
		Close();
		_bumpDanger?.Invoke();
	}

	private void OnRevealAllUnits()
	{
		Close();
		_revealAllUnits?.Invoke();
	}

	private void OnSpawnPilotRecruitmentLead()
	{
		Close();
		_spawnPilotRecruitmentLead?.Invoke();
	}

	private static UiListItem CreateMenuItem(string text, bool enabled, Action onPressed)
	{
		var item = new UiListItem(text, "", "")
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		item.SetVisual(UiListItemVisual.Bracket);
		item.SetAffordable(enabled);
		item.SetEnabled(enabled);
		item.Pressed += onPressed;
		return item;
	}
}
