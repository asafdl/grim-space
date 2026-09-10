using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Narrative;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record BeginNarrativeAction(string ActorId, string NarrativeId)
	: IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		BeginNarrativeDef.Instance;
}

public sealed class BeginNarrativeDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static BeginNarrativeDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is BeginNarrativeAction begin
		&& world.ActiveNarrativeId is null
		&& MapNarratives.TryGet(begin.NarrativeId, out _)
		&& world.UnitRegistry.TryGet(begin.ActorId, out _);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var begin = (BeginNarrativeAction)action;
		return
		[
			new SetActiveNarrativeEffect(begin.NarrativeId),
			new PlayerInputEffect(true),
		];
	}
}
