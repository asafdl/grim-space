using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Traffic;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record AdvanceTrafficAction(string ActorId) : IAction<StarMap, EmptyRuntime>
{
	public IActionDef<IAction, StarMap, EmptyRuntime, IEffect<StarMap, EmptyRuntime>> Definition =>
		AdvanceTrafficDef.Instance;
}

public sealed class AdvanceTrafficDef
	: IActionDef<IAction, StarMap, EmptyRuntime, IEffect<StarMap, EmptyRuntime>>
{
	public static AdvanceTrafficDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, EmptyRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, EmptyRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, EmptyRuntime runtime) => true;

	public IReadOnlyList<IEffect<StarMap, EmptyRuntime>> Resolve(
		IAction action,
		StarMap world,
		EmptyRuntime runtime) =>
		[new AdvanceTrafficEffect()];
}
