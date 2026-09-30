using System.Net.Http;
using System.Text;
using System.Text.Json;
using Godot;
using GrimSpace.Application;

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

		var payload = JsonSerializer.Serialize(new
		{
			content = TrimToDiscordLimit(FormatMessage(context, title, description)),
		});

		using var client = new System.Net.Http.HttpClient();
		client.DefaultRequestHeaders.UserAgent.ParseAdd("grim-space/1.0");
		using var response = await client.PostAsync(
			webhookUrl,
			new StringContent(payload, Encoding.UTF8, "application/json"),
			cancellationToken);
		return response.IsSuccessStatusCode;
	}

	private static string TrimToDiscordLimit(string message) =>
		message.Length <= 2000 ? message : message[..1997] + "...";
}
