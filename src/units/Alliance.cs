using GrimSpace.Units.Enums;

namespace GrimSpace.Units;

public sealed class Alliance
{
	private static readonly IReadOnlySet<ETeam> None = new HashSet<ETeam>();

	public static Alliance Player { get; } = new() { Team = ETeam.Player, AlliedTo = None };
	public static Alliance Enemy { get; } = new() { Team = ETeam.Enemy, AlliedTo = None };

	public required ETeam Team { get; init; }
	public required IReadOnlySet<ETeam> AlliedTo { get; init; }

	public bool IsAlliedWith(ETeam team) =>
		Team == team || AlliedTo.Contains(team);

	public bool IsAlliedWith(Alliance other) => IsAlliedWith(other.Team);
}
