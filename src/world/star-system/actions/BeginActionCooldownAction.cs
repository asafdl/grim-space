using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record BeginActionCooldownAction(
	string ActorId,
	int DurationTicks) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		BeginActionCooldownDef.Instance;
}

public sealed class BeginActionCooldownDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static BeginActionCooldownDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is BeginActionCooldownAction cooldown
		&& cooldown.DurationTicks > 0
		&& world.FleetRegistry.Contains(cooldown.ActorId);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var cooldown = (BeginActionCooldownAction)action;
		return [new SetActionCooldownEffect(cooldown.DurationTicks)];
	}
}
