using System.Text;

namespace GrimSpace.Core.Log;

public static class IssueReportLogTail
{
	/// <summary>Maximum UTF-8 bytes read from the end of the on-disk Godot log.</summary>
	public const int MaxTailBytes = 96 * 1024;

	/// <summary>Long log lines are truncated after minify (battle state dumps, stack traces).</summary>
	public const int MaxLineLength = 512;

	public static bool TryReadMinifiedTail(string? logFilePath, out string minified)
	{
		minified = string.Empty;
		if (string.IsNullOrWhiteSpace(logFilePath) || !File.Exists(logFilePath))
			return false;

		var raw = ReadUtf8Tail(logFilePath, MaxTailBytes);
		if (raw.Length == 0)
			return false;

		minified = Minify(raw);
		return minified.Length > 0;
	}

	internal static string ReadUtf8Tail(string path, int maxBytes)
	{
		if (maxBytes <= 0)
			return string.Empty;

		using var stream = new FileStream(
			path,
			FileMode.Open,
			FileAccess.Read,
			FileShare.ReadWrite);
		if (stream.Length == 0)
			return string.Empty;

		var readLength = (int)System.Math.Min(stream.Length, maxBytes);
		stream.Seek(-readLength, SeekOrigin.End);
		var buffer = new byte[readLength];
		var offset = 0;
		while (offset < readLength)
		{
			var read = stream.Read(buffer, offset, readLength - offset);
			if (read == 0)
				break;
			offset += read;
		}

		if (stream.Length > readLength)
			buffer = TrimLeadingPartialLine(buffer);

		return Encoding.UTF8.GetString(buffer);
	}

	internal static string Minify(string text)
	{
		if (text.Length == 0)
			return string.Empty;

		text = text.Replace("\r\n", "\n").Replace('\r', '\n');
		var lines = text.Split('\n');
		var output = new StringBuilder(text.Length);
		var previousWasBlank = false;

		foreach (var line in lines)
		{
			var trimmed = line.TrimEnd();
			if (trimmed.Length > MaxLineLength)
				trimmed = trimmed[..(MaxLineLength - 1)] + "…";

			var isBlank = trimmed.Length == 0;
			if (isBlank)
			{
				if (previousWasBlank)
					continue;
				previousWasBlank = true;
			}
			else
				previousWasBlank = false;

			if (output.Length > 0)
				output.Append('\n');
			output.Append(trimmed);
		}

		return TrimToUtf8ByteBudget(output.ToString(), MaxTailBytes);
	}

	private static byte[] TrimLeadingPartialLine(byte[] buffer)
	{
		for (var i = 0; i < buffer.Length; i++)
		{
			if (buffer[i] == (byte)'\n')
				return buffer.AsSpan(i + 1).ToArray();
		}

		return buffer;
	}

	private static string TrimToUtf8ByteBudget(string text, int maxBytes)
	{
		if (Encoding.UTF8.GetByteCount(text) <= maxBytes)
			return text;

		var lines = text.Split('\n');
		for (var start = 1; start < lines.Length; start++)
		{
			var candidate = string.Join('\n', lines, start, lines.Length - start);
			if (Encoding.UTF8.GetByteCount(candidate) <= maxBytes)
				return candidate;
		}

		return lines[^1];
	}
}
