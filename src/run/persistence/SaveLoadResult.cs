namespace GrimSpace.Run.Persistence;

public enum LoadResult
{
	Success,
	NotFound,
	Corrupt,
	UnsupportedFormat,
	IncompatibleGameVersion,
	Failed,
}

public static class SaveLoadResult
{
	public static LoadResult Classify(
		SaveStorageResult storageResult,
		SaveGameDocument? document,
		int supportedFormatVersion = SaveGameDocument.CurrentFormatVersion,
		Func<string, bool>? isGameVersionCompatible = null)
	{
		if (storageResult == SaveStorageResult.NotFound)
			return LoadResult.NotFound;
		if (storageResult == SaveStorageResult.Corrupt)
			return LoadResult.Corrupt;
		if (storageResult != SaveStorageResult.Success || document is null)
			return LoadResult.Failed;
		if (document.FormatVersion != supportedFormatVersion)
			return LoadResult.UnsupportedFormat;
		if (isGameVersionCompatible is not null
			&& !isGameVersionCompatible(document.GameVersion))
			return LoadResult.IncompatibleGameVersion;

		return LoadResult.Success;
	}
}
