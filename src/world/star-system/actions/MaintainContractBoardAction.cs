using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record ContractAddition(Contract Contract, int ExpiresAtTick);

public sealed record MaintainContractBoardAction(
	string ActorId,
	int Tick,
	IReadOnlyList<ContractAddition> Additions) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		MaintainContractBoardDef.Instance;
}

public sealed class MaintainContractBoardDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static MaintainContractBoardDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is MaintainContractBoardAction maintain
		&& string.Equals(maintain.ActorId, StarSystemActorIds.Contracts, StringComparison.Ordinal)
		&& maintain.Tick == world.Timeline.Clock.Current
		&& AdditionsAreLegal(maintain);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var maintain = (MaintainContractBoardAction)action;
		var effects = new List<IEffect<StarMap, ActorRuntime>>
		{
			new ExpireContractsEffect(maintain.Tick),
		};

		foreach (var addition in maintain.Additions)
			effects.Add(new AddContractEffect(addition.Contract, addition.ExpiresAtTick));

		return effects;
	}

	private static bool AdditionsAreLegal(MaintainContractBoardAction maintain)
	{
		var seenIds = new HashSet<string>(StringComparer.Ordinal);
		foreach (var addition in maintain.Additions)
		{
			if (addition.Contract.IsStoryObjective)
				return false;

			if (addition.ExpiresAtTick <= maintain.Tick)
				return false;

			if (!seenIds.Add(addition.Contract.Id))
				return false;
		}

		return true;
	}
}
