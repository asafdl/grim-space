using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class BeginWorkEffect : IEffect<StarMap, ActorRuntime>
{
	private readonly string _unitId;
	private readonly int _startTick;

	private BeginWorkEffect(string unitId, int startTick)
	{
		_unitId = unitId;
		_startTick = startTick;
	}

	public static BeginWorkEffect Start(string unitId, int startTick) =>
		new(unitId, startTick);

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		world.StateOf(_unitId).BeginWork(_startTick);
		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId) { }
}
