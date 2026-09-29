using GrimSpace.Units.Enums;

namespace GrimSpace.Units;

public static class UnitTypeSlug
{
	public static string For(EType type) => type switch
	{
		EType.Fighter => "fighter",
		EType.Carrier => "carrier",
		EType.Patrol => "patrol",
		EType.VoidBomb => "void_bomb",
		_ => throw new ArgumentOutOfRangeException(nameof(type)),
	};
}
