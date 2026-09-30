using System.Reflection;

namespace GrimSpace.Presentation.Support;

/// <summary>
/// Outbound issue-report destinations. Webhook URL is injected at build time via
/// <c>DISCORD_ISSUE_WEBHOOK_URL</c> → <see cref="AssemblyMetadataAttribute"/> in the project file.
/// </summary>
internal static class IssueReportChannels
{
	private const string DiscordWebhookMetadataKey = "DiscordIssueWebhookUrl";

	internal static string DiscordWebhookUrl =>
		typeof(IssueReportChannels).Assembly
			.GetCustomAttributes<AssemblyMetadataAttribute>()
			.FirstOrDefault(a => a.Key == DiscordWebhookMetadataKey)
			?.Value
		?? "";
}
