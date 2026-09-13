// Placeholder until roguelike sector map exists.

using GrimSpace.Units;

namespace GrimSpace.Run;

public sealed class Party
{
	private readonly List<FleetMember> _members = [];

	public IReadOnlyList<FleetMember> Members => _members;

	public void Add(FleetMember member) => _members.Add(member);
}
