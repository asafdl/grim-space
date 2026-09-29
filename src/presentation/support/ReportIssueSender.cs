using System.Net.Http;
using System.Text;
using System.Text.Json;
using Godot;

namespace GrimSpace.Presentation.Support;

internal static class ReportIssueSender
{
	internal static string FormatMessage(string context, string title, string description)
	{
		var godotVersion = Engine.GetVersionInfo();
		var diagnostics = $"""
			Context: {context}
			OS: {OS.GetName()} {OS.GetVersion()}
			Godot: {godotVersion["major"]}.{godotVersion["minor"]}.{godotVersion["patch"]}
			""";

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
		using var response = await client.PostAsync(
			webhookUrl,
			new StringContent(payload, Encoding.UTF8, "application/json"),
			cancellationToken);
		return response.IsSuccessStatusCode;
	}

	private static string TrimToDiscordLimit(string message) =>
		message.Length <= 2000 ? message : message[..1997] + "...";
}
