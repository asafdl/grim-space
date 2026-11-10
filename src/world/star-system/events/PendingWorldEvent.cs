using GrimSpace.Pilots;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Events;

public sealed record WorldEventPresentation(string Title, string Body);

/// <summary>
/// The single world event currently waiting on the player. Owned by the map so it forks and saves with it;
/// the continuation action that consumes it decides what consequences apply.
/// Reward and recruit are not both set.
/// </summary>
public sealed record PendingWorldEvent(
	string Id,
	EWorldEventId EventId,
	string PilotId,
	/// <summary>The pilot's ship, or null for a pilot without one.</summary>
	string? ShipId,
	int Damage,
	WorldEventPresentation Presentation,
	ResourceBundle? Reward = null,
	PilotRecruitProfile? Recruit = null);
