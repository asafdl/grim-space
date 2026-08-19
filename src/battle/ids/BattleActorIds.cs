namespace GrimSpace.Battle.Ids;

/// <summary>Reserved battle actor ids — not units. Used for terrain ownership and global rule timeline steps.</summary>
public static class BattleActorIds
{
	/// <summary>Owner of persistent terrain hazards (asteroids, etc.).</summary>
	public const string Terrain = "__terrain__";

	/// <summary>Timeline owner for global rule steps (hazard cleanup, etc.).</summary>
	public const string Rules = "__rules__";
}
