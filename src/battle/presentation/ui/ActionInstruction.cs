namespace GrimSpace.Battle.Presentation.Ui;

public readonly record struct ActionInstruction(
	bool Visible,
	string Label,
	bool CanConfirm);
