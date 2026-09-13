using GrimSpace.Units;

namespace GrimSpace.World.StarSystem.Units;

public sealed class Fleet
{
	public State State { get; }
	public IReadOnlyList<FleetMember> Members { get; }

	public Fleet(State state, IEnumerable<FleetMember>? members = null)
	{
		ArgumentNullException.ThrowIfNull(state);
		State = state;
		var materialized = members?.ToArray() ?? [];
		if (materialized.Any(member => string.IsNullOrWhiteSpace(member.Id)))
			throw new ArgumentException("Fleet member IDs cannot be empty.", nameof(members));
		if (materialized.Select(member => member.Id).Distinct(StringComparer.Ordinal).Count() != materialized.Length)
			throw new ArgumentException("Fleet member IDs must be unique.", nameof(members));

		Members = Array.AsReadOnly(materialized);
	}
}
