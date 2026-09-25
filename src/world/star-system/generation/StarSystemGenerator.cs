using GrimSpace.Units;
using GrimSpace.World.StarSystem.Generation;

namespace GrimSpace.World.StarSystem;

public static class StarSystemGenerator
{
	public static StarMap Generate(int seed, EStarSystemClass systemClass, IShipRegistryReader? shipRegistryReader = null) =>
		systemClass switch
		{
			EStarSystemClass.Supply => StarSystemBuilder.Build(
				SupplySystemGenerator.CreateBlueprint(seed),
				shipRegistryReader),
			_ => throw new ArgumentOutOfRangeException(nameof(systemClass), systemClass, null),
		};
}
