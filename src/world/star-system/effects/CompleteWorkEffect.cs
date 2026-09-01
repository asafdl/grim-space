using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class CompleteWorkEffect : IEffect<StarMap, ActorRuntime>
{
	private readonly string _unitId;

	private CompleteWorkEffect(string unitId)
	{
		_unitId = unitId;
	}

	public static CompleteWorkEffect Instance(string unitId) => new(unitId);

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		world.StateOf(_unitId).CompleteWork();
		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId) { }
}
