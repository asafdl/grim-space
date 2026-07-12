// Placeholder until roguelike sector map exists.

using GrimSpace.Units;

namespace GrimSpace.Run;

public sealed class Party
{
	private readonly List<Instance> _members = [];

	public IReadOnlyList<Instance> Members => _members;

	public void Add(Instance instance) => _members.Add(instance);
}
