namespace GrimSpace.World.Factions;

public static class FactionRelations
{
	public static bool IsHostile(EFaction left, EFaction right)
	{
		ValidateFaction(left, nameof(left));
		ValidateFaction(right, nameof(right));

		return (left == EFaction.Player && right == EFaction.Pirates)
			|| (left == EFaction.Pirates && right == EFaction.Player);
	}

	private static void ValidateFaction(EFaction faction, string parameterName)
	{
		if (!Enum.IsDefined(faction))
			throw new ArgumentOutOfRangeException(parameterName, faction, null);
	}
}
