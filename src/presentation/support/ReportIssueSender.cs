using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Godot;
using GrimSpace.Application;
using GrimSpace.Core.Log;

namespace GrimSpace.Presentation.Support;

internal static class ReportIssueSender
{
	internal static string FormatMessage(string context, string title, string description)
	{
		var godotVersion = Engine.GetVersionInfo();
		var diagnostics = new StringBuilder();
		diagnostics.Append(ReportIssueSystemContext.Format(context));
		diagnostics.AppendLine();
		if (GameVersion.TryGetReleaseVersion(out var releaseVersion))
			diagnostics.AppendLine($"Game: v{releaseVersion}");
		diagnostics.AppendLine($"OS: {OS.GetName()} {OS.GetVersion()}");
		diagnostics.AppendLine(
			$"Godot: {godotVersion["major"]}.{godotVersion["minor"]}.{godotVersion["patch"]}");

		return $"""
			**{title.Trim()}**

			{description.Trim()}

			---
			{diagnostics}
			""";
	}

	internal static async Task<bool> TrySendAsync(
		string context,
		string title,
		string description,
		CancellationToken cancellationToken = default)
	{
		var webhookUrl = IssueReportChannels.DiscordWebhookUrl;
		if (string.IsNullOrWhiteSpace(webhookUrl))
			return false;

		var payloadJson = JsonSerializer.Serialize(new
		{
			content = TrimToDiscordLimit(FormatMessage(context, title, description)),
			thread_name = TrimToDiscordThreadName(title),
		});

		using var client = new System.Net.Http.HttpClient();
		client.DefaultRequestHeaders.UserAgent.ParseAdd("grim-space/1.0");
		using var requestContent = BuildRequestContent(payloadJson);
		using var response = await client.PostAsync(
			webhookUrl,
			requestContent,
			cancellationToken);
		if (response.IsSuccessStatusCode)
			return true;

		var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
		GD.PushError(
			$"Issue report failed with HTTP {(int)response.StatusCode} {response.StatusCode}: {responseBody}");
		return false;
	}

	private static HttpContent BuildRequestContent(string payloadJson)
	{
		if (!IssueReportLogTail.TryReadMinifiedTail(
				GameLogPaths.GodotLogFilePath(),
				out var logTail))
			return new StringContent(payloadJson, Encoding.UTF8, "application/json");

		var form = new MultipartFormDataContent();
		form.Add(
			new StringContent(payloadJson, Encoding.UTF8, "application/json"),
			"payload_json");

		var fileBytes = Encoding.UTF8.GetBytes(logTail);
		var fileContent = new ByteArrayContent(fileBytes);
		fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
		form.Add(
			fileContent,
			"file",
			$"godot-log-{DateTime.UtcNow:yyyyMMdd-HHmmss}Z.txt");
		return form;
	}

	private static string TrimToDiscordLimit(string message) =>
		message.Length <= 2000 ? message : message[..1997] + "...";

	private static string TrimToDiscordThreadName(string title)
	{
		var trimmed = title.Trim();
		return trimmed.Length <= 100 ? trimmed : trimmed[..100];
	}
}
