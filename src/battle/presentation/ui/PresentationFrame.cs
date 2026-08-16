using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Abilities;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Ui;

public sealed class PresentationFrame
{
	public required EPlayerMode Mode { get; init; }
	public required string FocusId { get; init; }
	public required UnitDisplayState FocusState { get; init; }
	public bool IsInspecting { get; init; }
	public bool ShowMovePreview { get; init; }
	public required IReadOnlyList<MovePathOption> MovePaths { get; init; }
	public int MovePathApBaseline { get; init; }
	public required IReadOnlyDictionary<string, UnitDisplayState> PreviewUnits { get; init; }
	public required QueuedWeaponState QueuedWeapon { get; init; }
	public required WeaponPeek Weapons { get; init; }
	public required AbilityLegality Abilities { get; init; }
	public required IReadOnlySet<string> ThreatenedUnitIds { get; init; }
	public IReadOnlyList<IReadOnlySet<Coord>> TorpedoEnvelopeLayers { get; init; } = [];
	public ESpatialOrientation? FlakHoverMountedOn { get; init; }
	public bool RailgunHovered { get; init; }
	public ESpatialOrientation? TorpedoHoverMountedOn { get; init; }
	public required IReadOnlyList<Coord> MovePath { get; init; }
	public required IReadOnlyList<Coord> CommittedMovePath { get; init; }
	public Coord? MoveTarget { get; init; }
	public int TurnNumber { get; init; }
	public bool CanAct { get; init; }
	public bool CanFocusCamera { get; init; }
	public bool CanUndo { get; init; }
	public bool ShowOutcomeOverlay { get; init; }
	public bool ShowIntroOverlay { get; init; }
	public bool ShowWeaponPreviews { get; init; }
	public EBattleResult Outcome { get; init; }
	public required IReadOnlyList<string> ActionLogLines { get; init; }
}
