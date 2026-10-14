using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Pilots;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record RefreshStrategicPilotSkillsAction(
	string ActorId,
	IReadOnlyList<PilotSkillGrant> StrategicGrants) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		RefreshStrategicPilotSkillsDef.Instance;
}

public sealed class RefreshStrategicPilotSkillsDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static RefreshStrategicPilotSkillsDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is RefreshStrategicPilotSkillsAction refresh
		&& world.FleetRegistry.Contains(refresh.ActorId);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var refresh = (RefreshStrategicPilotSkillsAction)action;
		return [new ReplaceStrategicActionEffectPipelineEffect(refresh.StrategicGrants)];
	}
}
