using GrimSpace.Battle.Abilities;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Player;

public sealed class QueuedWeaponState
{
	public static QueuedWeaponState Empty { get; } = new();

	public ESpatialOrientation? FlakMountedOn { get; init; }
	public bool Railgun { get; init; }
	public ESpatialOrientation? TorpedoMountedOn { get; init; }
}
