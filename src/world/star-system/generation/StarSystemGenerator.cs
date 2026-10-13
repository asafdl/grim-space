using GrimSpace.Units;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.Pilots;

namespace GrimSpace.World.StarSystem;

public static class StarSystemGenerator
{
	public static StarMap Generate(
		int seed,
		EStarSystemClass systemClass,
		IShipRegistryReader? shipRegistryReader = null,
		IPilotSkillRosterReader? pilotSkillRosterReader = null) =>
		systemClass switch
		{
			EStarSystemClass.Supply => StarSystemBuilder.Build(
				SupplySystemGenerator.CreateBlueprint(seed),
				shipRegistryReader,
				pilotSkillRosterReader),
			_ => throw new ArgumentOutOfRangeException(nameof(systemClass), systemClass, null),
		};
}
