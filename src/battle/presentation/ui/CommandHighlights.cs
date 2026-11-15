namespace GrimSpace.Battle.Presentation.Ui;

public readonly record struct CommandHighlights(
	bool Undo = false,
	bool Execute = false,
	EPlayerMode? Ability = null);
