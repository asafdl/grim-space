namespace GrimSpace.Core.Actions;

public sealed class TickClock
{
	public int Current { get; private set; }

	public void Set(int tick) => Current = tick;

	public void Next() => Current++;

	public TickClock Clone() => new() { Current = Current };
}
