using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Weapons;

public enum EFlakMount
{
	Port,
	Starboard,
}

public sealed class FlakMountConfig
{
	public required int Range { get; init; }
	public required int SideSign { get; init; }

	public static FlakMountConfig For(EFlakMount mount) =>
		mount switch
		{
			EFlakMount.Port => new FlakMountConfig { Range = CombatConfig.FlakRange, SideSign = -1 },
			EFlakMount.Starboard => new FlakMountConfig { Range = CombatConfig.FlakRange, SideSign = 1 },
			_ => throw new ArgumentOutOfRangeException(nameof(mount)),
		};
}
