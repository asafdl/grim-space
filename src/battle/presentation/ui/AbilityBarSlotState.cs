namespace GrimSpace.Battle.Presentation.Ui;

public sealed record AbilityBarSlotState(
	BattleHudTooltipContent Tooltip,
	string Charges,
	bool Enabled,
	int CooldownRemaining = 0);
