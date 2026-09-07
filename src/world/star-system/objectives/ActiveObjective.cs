namespace GrimSpace.World.StarSystem.Objectives;

public enum EObjectiveSource
{
	Contract,
	Story,
}

public readonly record struct ActiveObjective(
	string Id,
	string Title,
	string Summary,
	EObjectiveSource Source);
