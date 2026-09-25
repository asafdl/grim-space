using System.Collections.Concurrent;
using GrimSpace.Tutorials;
using GrimSpace.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem;

/// <summary>
/// Assembly-scoped cache of <see cref="StarMap.Create"/> templates.
/// Call <see cref="Fresh"/> for a mutable fork; <see cref="Template"/> when the test only reads layout.
/// </summary>
public sealed class StarMapFixture
{
	private readonly ConcurrentDictionary<int, StarMap> _templates = new();

	public StarMap Template(int seed) =>
		_templates.GetOrAdd(seed, static s => StarMap.Create(s));

	public StarMap Fresh(int seed = 42) => Template(seed).Fork();

	public StarMap FreshWithBeatAHunt(int seed = 42)
	{
		var map = Fresh(seed);
		TutorialBeatContracts.OfferBeatA(map);
		return map;
	}
}
