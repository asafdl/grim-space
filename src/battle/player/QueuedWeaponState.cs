using GrimSpace.Battle.Abilities;

namespace GrimSpace.Battle.Player;

public sealed class QueuedWeaponState
{
	public static QueuedWeaponState Empty { get; } = new();

	public EFlakMount? FlakMount { get; init; }
	public bool Railgun { get; init; }
	public ETorpedoMount? TorpedoMount { get; init; }
}
