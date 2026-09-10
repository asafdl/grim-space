using GrimSpace.Education;
using GrimSpace.Run;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;

namespace GrimSpace.Tutorials;

public static class FirstPirateTutorial
{
	public const string Id = "first-pirate";

	public static TutorialFlow? CreateForActiveContract(StarMap map)
	{
		ArgumentNullException.ThrowIfNull(map);

		var pirateId = map.ContractRegistry
			.ActiveFor(State.PlayerFleetUnitId)
			.OrderBy(contract => contract.State.AcceptedAtTick)
			.ThenBy(contract => contract.Definition.Id, StringComparer.Ordinal)
			.SelectMany(contract => contract.State.SpawnBindings
				.OrderBy(binding => binding.Key, StringComparer.Ordinal)
				.SelectMany(binding => binding.Value))
			.FirstOrDefault(unitId =>
				map.UnitRegistry.TryGet(unitId, out var unit)
				&& unit.State.Faction == EFaction.Pirates);
		if (pirateId is null)
			return null;

		return new TutorialFlow(
			Id,
			pirateId,
			new TutorialDialogContent(
				"Your contract target",
				$"Right-click the [url={pirateId}]pirate ship[/url] spawned by your contract to pursue it."));
	}
}
