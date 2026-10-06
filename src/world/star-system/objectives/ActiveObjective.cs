using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Objectives;

public enum EObjectiveSource
{
	Contract,
	Story,
}

public readonly record struct ObjectiveDeadline(int Tick, int TicksRemaining);

public readonly record struct ActiveObjective(
	string Id,
	string Title,
	ObjectiveSummaryContent Summary,
	ResourceBundle Reward,
	EObjectiveSource Source,
	ObjectiveDeadline? Deadline = null);
