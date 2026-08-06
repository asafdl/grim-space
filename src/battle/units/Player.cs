using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Engine;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Units;

public sealed class Player : Unit
{
	public Player(State state, IExecutionAgent<BattleWorld, ActorRuntime, Unit> executionAgent)
		: base(EController.Player, state, executionAgent)
	{
	}
}
