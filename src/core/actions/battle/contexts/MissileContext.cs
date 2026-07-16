using GrimSpace.Battle.Units;

namespace GrimSpace.Core.Actions.Battle.Contexts;

public readonly struct MissileContext(State actor)
{
	public int Remaining => actor.MissilesRemaining;

	public void Change(int delta) => actor.MissilesRemaining += delta;
}
