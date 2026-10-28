namespace GrimSpace.Run.Persistence;

public static class SaveGameMigrator
{
	public static SaveGameDocument MigrateToCurrent(SaveGameDocument document)
	{
		ArgumentNullException.ThrowIfNull(document);
		return document;
	}
}
