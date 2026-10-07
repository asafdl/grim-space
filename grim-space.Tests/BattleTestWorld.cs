using GrimSpace.Battle.World;

namespace GrimSpace.Tests;

internal static class BattleTestWorld
{
	public static void InjectNonUnit(BattleWorld world, NonUnit nonUnit) =>
		world.AddNonUnit(nonUnit);
}
