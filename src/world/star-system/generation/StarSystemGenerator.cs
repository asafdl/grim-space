using GrimSpace.Run.Items;
using GrimSpace.World.StarSystem.Generation;
namespace GrimSpace.World.StarSystem;

public static class StarSystemGenerator
{
	public static StarMap Generate(
		int seed,
		EStarSystemClass systemClass,
		Inventory? itemInventory = null) =>
		systemClass switch
		{
			EStarSystemClass.Supply => StarSystemBuilder.Build(
				SupplySystemGenerator.CreateBlueprint(seed),
				itemInventory),
			_ => throw new ArgumentOutOfRangeException(nameof(systemClass), systemClass, null),
		};
}
