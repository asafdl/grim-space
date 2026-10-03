using Godot;
using GrimSpace.Run.Persistence;

namespace GrimSpace.Application;

public sealed class SaveGameStore : ISaveGameStorage
{
	public const string RelativeSavePath = "saves/save.json";

	private readonly FileSystemSaveGameStorage _storage;

	public SaveGameStore()
		: this(SaveGameDocument.DefaultSaveSlotId)
	{
	}

	public SaveGameStore(string saveSlotId)
		: this(
			saveSlotId,
			Path.Combine(OS.GetUserDataDir(), "saves"))
	{
	}

	internal SaveGameStore(string saveSlotId, string saveDirectory)
	{
		ValidateSlotId(saveSlotId);
		var fileName = saveSlotId == SaveGameDocument.DefaultSaveSlotId
			? "save.json"
			: $"save-{saveSlotId}.json";
		_storage = new FileSystemSaveGameStorage(Path.Combine(saveDirectory, fileName));
	}

	public bool Exists() => _storage.Exists();

	public SaveStorageResult TryRead(out SaveGameDocument? document) =>
		_storage.TryRead(out document);

	public SaveStorageResult TryWrite(SaveGameDocument document) =>
		_storage.TryWrite(document);

	private static void ValidateSlotId(string saveSlotId)
	{
		if (string.IsNullOrWhiteSpace(saveSlotId)
			|| saveSlotId.Contains(Path.DirectorySeparatorChar)
			|| saveSlotId.Contains(Path.AltDirectorySeparatorChar)
			|| saveSlotId is "." or "..")
			throw new ArgumentException("Save slot ID is invalid.", nameof(saveSlotId));
	}
}
