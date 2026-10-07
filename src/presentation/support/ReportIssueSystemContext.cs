using System.Text;
using Godot;
using GrimSpace.Application;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Presentation.Scene;

namespace GrimSpace.Presentation.Support;

internal static class ReportIssueSystemContext
{
	private static string? _battleSnapshot;

	internal static void SetBattleSnapshot(string? snapshot) => _battleSnapshot = snapshot;

	internal static string Format(string screen)
	{
		var report = new StringBuilder();
		report.AppendLine($"Screen: {screen}");

		var scenePath = (Engine.GetMainLoop() as SceneTree)?.CurrentScene?.SceneFilePath;
		report.AppendLine($"Scene: {scenePath ?? "(none)"}");

		if (!TryAppendRunContext(report, screen))
			report.AppendLine("Run: (no active run)");

		return report.ToString().TrimEnd();
	}

	private static bool TryAppendRunContext(StringBuilder report, string screen)
	{
		State run;
		try
		{
			run = Session.Instance.Run;
		}
		catch (InvalidOperationException)
		{
			return false;
		}

		if (run.StarSystem is not { } starSystem)
			return false;

		report.AppendLine($"Run seed: {starSystem.Map.Seed}");
		report.AppendLine($"Star map tick: {starSystem.Tick}");

		if (screen == "star-map")
			AppendStarMapContext(report, starSystem);
		else if (screen == "battle")
			AppendBattleContext(report, run);

		return true;
	}

	private static void AppendStarMapContext(StringBuilder report, StarSystemOrchestrator starSystem)
	{
		if (MapNavigationContext.ActivePoiId is { Length: > 0 } poiId)
			report.AppendLine($"POI: {poiId}");
		if (MapNavigationContext.ActiveFacilityId is { Length: > 0 } facilityId)
			report.AppendLine($"Facility: {facilityId}");
		if (MapNavigationContext.ActiveOperatorName is { Length: > 0 } operatorName)
			report.AppendLine($"Operator: {operatorName}");

		var playerId = starSystem.PlayerId;
		if (playerId is not null
			&& starSystem.Map.FleetRegistry.TryGet(playerId, out var fleet))
		{
			report.AppendLine($"Fleet travel: {fleet.State.Travel}");
			report.AppendLine($"Fleet position: {starSystem.CommittedPositionOf(playerId)}");
			if (starSystem.Map.DockAt(fleet.State) is { } dock)
				report.AppendLine($"Docked at: {dock.Id}");
		}
	}

	private static void AppendBattleContext(StringBuilder report, State run)
	{
		if (run.ActiveBattle is { } engagement)
		{
			report.AppendLine($"Engagement: {engagement.Id}");
			report.AppendLine($"Battle seed: {engagement.Seed}");
			report.AppendLine($"Objective: {engagement.Objective}");
		}
		else
			report.AppendLine("Engagement: (skirmish / dev)");

		if (_battleSnapshot is { Length: > 0 } snapshot)
			report.AppendLine(snapshot);
	}
}
