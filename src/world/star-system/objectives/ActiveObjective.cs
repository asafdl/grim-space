using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Objectives;

public enum EObjectiveSource
{
	Contract,
	Lead,
	Story,
}

public enum EObjectiveKind
{
	Hunt,
	Wreckage,
	Delivery,
	Lead,
	Story,
}

public readonly record struct ObjectiveDeadline(int Tick, int TicksRemaining);

public readonly record struct ActiveObjective(
	string Id,
	string Title,
	ObjectiveSummaryContent Summary,
	ResourceBundle Reward,
	EObjectiveSource Source,
	EObjectiveKind Kind,
	ObjectiveDeadline? Deadline = null);
