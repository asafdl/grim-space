using System.Text;
using GrimSpace.Core.Log;

namespace grim_space.Tests.Core;

public sealed class IssueReportLogTailTests
{
	[Fact]
	public void ReadUtf8Tail_returns_end_of_file_within_byte_budget()
	{
		var path = WriteTempFile("alpha\nbeta\ngamma\ndelta\n");
		var tail = IssueReportLogTail.ReadUtf8Tail(path, 20);
		Assert.Contains("gamma", tail);
		Assert.Contains("delta", tail);
		Assert.DoesNotContain("alpha", tail);
	}

	[Fact]
	public void Minify_collapses_blank_runs_and_truncates_long_lines()
	{
		var longLine = new string('x', IssueReportLogTail.MaxLineLength + 40);
		var input = $"ok\n\n\n{longLine}\n\nlast";
		var minified = IssueReportLogTail.Minify(input);

		Assert.Equal("ok\n\n" + longLine[..(IssueReportLogTail.MaxLineLength - 1)] + "…\n\nlast", minified);
	}

	[Fact]
	public void TryReadMinifiedTail_reads_from_disk()
	{
		var path = WriteTempFile("session\nerror: boom\n");
		Assert.True(IssueReportLogTail.TryReadMinifiedTail(path, out var tail));
		Assert.Contains("error: boom", tail);
	}

	private static string WriteTempFile(string content)
	{
		var path = Path.Combine(Path.GetTempPath(), $"grim-space-log-tail-{Guid.NewGuid():N}.log");
		File.WriteAllText(path, content, Encoding.UTF8);
		return path;
	}
}
