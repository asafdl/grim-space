using GrimSpace.Battle.Weapons;

namespace GrimSpace.Battle.Player;

public readonly record struct HumanTurnViewInput(
	string? PresentationFocusId = null,
	EFlakMount? FlakHoverMount = null,
	bool RailgunHovered = false,
	ETorpedoMount? TorpedoHoverMount = null);
