using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;

namespace GrimSpace.Core.Actions.Battle.Contexts;

public readonly struct MoveContext(State player, Unit playerUnit, bool commitMomentum)
{
	public void Apply(Option option)
	{
		if (commitMomentum)
			playerUnit.Movement.ApplyMomentum(player, option.Path);

		playerUnit.Movement.ApplyMove(player, option);
	}
}
