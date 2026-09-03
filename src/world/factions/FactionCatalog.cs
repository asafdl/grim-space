namespace GrimSpace.World.Factions;

public static class FactionCatalog
{
	public const EFaction DevDefault = EFaction.TheOptimality;

	public static string DisplayName(EFaction faction) =>
		faction switch
		{
			EFaction.TheOptimality => "The Optimality",
			EFaction.Pirates => "Pirates",
			_ => throw new ArgumentOutOfRangeException(nameof(faction), faction, null),
		};
}
