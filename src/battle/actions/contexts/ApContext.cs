using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Actions.Contexts;

public readonly struct ApContext(State player)
{
	public State Player => player;

	public int Current => player.ActionPoints;

	public void Change(int delta) => player.ActionPoints += delta;
}
