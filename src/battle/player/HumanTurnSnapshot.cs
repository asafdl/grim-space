using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Player;

public sealed class HumanTurnSnapshot
{
	public required string HumanActorId { get; init; }
	public int TurnNumber { get; init; }
	public bool CanAct { get; init; }
	public required IReadOnlyDictionary<string, UnitDisplayState> Units { get; init; }
	public required IReadOnlyDictionary<string, UnitDisplayState> PreviewUnits { get; init; }
	public IReadOnlySet<Coord> HazardCells { get; init; } = new HashSet<Coord>();
	public IReadOnlyList<MovePathOption> MoveOptions { get; init; } = [];
	public IReadOnlyList<Coord> CommittedMovePath { get; init; } = [];
	public QueuedWeaponState QueuedWeapon { get; init; } = QueuedWeaponState.Empty;
	public WeaponPeek Weapons { get; init; } = WeaponPeek.Empty;
	public IReadOnlySet<string> ThreatenedUnitIds { get; init; } = new HashSet<string>();
	public IReadOnlyList<IReadOnlySet<Coord>> TorpedoEnvelopeLayers { get; init; } = [];
	public bool CanUndo { get; init; }
	public bool CanCommit { get; init; }
	public int MovePathApBaseline { get; init; }

	public static HumanTurnSnapshot Empty(string humanActorId) =>
		new()
		{
			HumanActorId = humanActorId,
			Units = new Dictionary<string, UnitDisplayState>(),
			PreviewUnits = new Dictionary<string, UnitDisplayState>(),
		};
}
