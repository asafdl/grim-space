using Godot;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed record PoseHitOpportunity(
	string TargetId,
	string? IconPath,
	Color IconTint);
