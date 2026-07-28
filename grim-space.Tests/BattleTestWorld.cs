using GrimSpace.Battle.World;

namespace GrimSpace.Tests;

internal static class BattleTestWorld
{
	public static void InjectHazard(BattleWorld world, Hazard hazard) =>
		world.MutableNonUnits[hazard.Id] = hazard;
}
