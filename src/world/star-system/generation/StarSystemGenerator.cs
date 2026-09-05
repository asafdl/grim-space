using GrimSpace.World.StarSystem.Generation;

namespace GrimSpace.World.StarSystem;

public static class StarSystemGenerator
{
	public static StarMap Generate(int seed, EStarSystemClass systemClass) =>
		systemClass switch
		{
			EStarSystemClass.Supply => StarSystemBuilder.Build(
				SupplySystemGenerator.CreateBlueprint(seed)),
			_ => throw new ArgumentOutOfRangeException(nameof(systemClass), systemClass, null),
		};
}
