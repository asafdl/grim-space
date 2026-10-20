using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Pilots;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record RefreshStrategicPilotCapabilitiesAction(
	string ActorId,
	IReadOnlyList<PilotCapabilityGrant> StrategicGrants) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		RefreshStrategicPilotCapabilitiesDef.Instance;
}

public sealed class RefreshStrategicPilotCapabilitiesDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static RefreshStrategicPilotCapabilitiesDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is RefreshStrategicPilotCapabilitiesAction refresh
		&& world.FleetRegistry.Contains(refresh.ActorId);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var refresh = (RefreshStrategicPilotCapabilitiesAction)action;
		return [new ReplaceStrategicActionEffectPipelineEffect(refresh.StrategicGrants)];
	}
}
