using System.Text.Json;
using GrimSpace.Core.Log;

namespace GrimSpace.Run.Persistence;

public enum SaveStorageResult
{
	Success,
	NotFound,
	Corrupt,
	Failed,
}

public interface ISaveGameStorage
{
	bool Exists();
	SaveStorageResult TryRead(out SaveGameDocument? document);
	SaveStorageResult TryWrite(SaveGameDocument document);
}

public sealed class FileSystemSaveGameStorage : ISaveGameStorage
{
	private static readonly JsonSerializerOptions JsonOptions =
		ReflectionJson.CreateOptions(options => options.WriteIndented = true);

	private readonly string _path;
	private readonly string _temporaryPath;

	public FileSystemSaveGameStorage(string path)
	{
		ArgumentException.ThrowIfNullOrEmpty(path);
		_path = Path.GetFullPath(path);
		_temporaryPath = $"{_path}.tmp";
	}

	public bool Exists() => File.Exists(_path);

	public SaveStorageResult TryRead(out SaveGameDocument? document)
	{
		document = null;
		if (!File.Exists(_path))
			return SaveStorageResult.NotFound;

		try
		{
			using var json = JsonDocument.Parse(File.ReadAllText(_path));
			document = ReflectionJson.Read<SaveGameDocument>(
				json.RootElement,
				JsonOptions);
			document?.Validate();
			return document is null ? SaveStorageResult.Corrupt : SaveStorageResult.Success;
		}
		catch (JsonException)
		{
			return SaveStorageResult.Corrupt;
		}
		catch (InvalidDataException)
		{
			return SaveStorageResult.Corrupt;
		}
		catch (IOException ex)
		{
			GameLog.LogException(ex, $"Failed to read save game '{_path}'.");
			return SaveStorageResult.Failed;
		}
		catch (UnauthorizedAccessException ex)
		{
			GameLog.LogException(ex, $"Failed to read save game '{_path}'.");
			return SaveStorageResult.Failed;
		}
	}

	public SaveStorageResult TryWrite(SaveGameDocument document)
	{
		ArgumentNullException.ThrowIfNull(document);
		document.Validate();

		try
		{
			var directory = Path.GetDirectoryName(_path);
			if (!string.IsNullOrEmpty(directory))
				Directory.CreateDirectory(directory);

			var json = JsonSerializer.Serialize(
				ReflectionJson.Write(document, JsonOptions),
				JsonOptions);
			using (var stream = new FileStream(
				_temporaryPath,
				FileMode.Create,
				FileAccess.Write,
				FileShare.None,
				bufferSize: 4096,
				FileOptions.WriteThrough))
			using (var writer = new StreamWriter(stream))
			{
				writer.Write(json);
				writer.Flush();
				stream.Flush(flushToDisk: true);
			}

			File.Move(_temporaryPath, _path, overwrite: true);
			return SaveStorageResult.Success;
		}
		catch (IOException)
		{
			return SaveStorageResult.Failed;
		}
		catch (UnauthorizedAccessException)
		{
			return SaveStorageResult.Failed;
		}
		finally
		{
			try
			{
				if (File.Exists(_temporaryPath))
					File.Delete(_temporaryPath);
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}
	}
}
