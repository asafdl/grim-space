using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Ids;

public static class UnitTypeSlug
{
	public static string For(EType type) => type switch
	{
		EType.Fighter => "fighter",
		EType.Patrol => "patrol",
		EType.Torpedo => "torpedo",
		_ => throw new ArgumentOutOfRangeException(nameof(type)),
	};
}
