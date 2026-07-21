using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Slices;

public readonly struct MissileContext(State actor)
{
	public int Remaining => actor.MissilesRemaining;

	public void Change(int delta) => actor.MissilesRemaining += delta;
}
