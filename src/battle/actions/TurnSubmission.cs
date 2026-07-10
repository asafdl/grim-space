using System.Collections.Generic;

namespace GrimSpace.Battle.Actions;

public sealed class TurnSubmission
{
	public required string ShipId { get; init; }
	public List<ActionRequest> QueuedActions { get; init; } = [];
}
