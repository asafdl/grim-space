using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;

namespace GrimSpace.Core.Actions.Battle.Contexts;

public readonly struct MoveContext(State actor, Unit actorUnit)
{
	public void Apply(Option option)
	{
		actorUnit.Movement.ApplyMomentum(actor, option.Path);
		actorUnit.Movement.ApplyMove(actor, option);
	}
}
