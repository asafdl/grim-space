using System.Collections.Concurrent;
using GrimSpace.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem;

/// <summary>
/// Assembly-scoped cache of <see cref="StarMap.CreateDevDefault"/> templates.
/// Call <see cref="Fresh"/> for a mutable fork; <see cref="Template"/> when the test only reads layout.
/// </summary>
public sealed class DevStarMapFixture
{
	private readonly ConcurrentDictionary<int, StarMap> _templates = new();

	public StarMap Template(int seed) =>
		_templates.GetOrAdd(seed, StarMap.CreateDevDefault);

	public StarMap Fresh(int seed = 42) => Template(seed).Fork();
}
