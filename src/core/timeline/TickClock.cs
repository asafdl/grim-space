namespace GrimSpace.Core.Engine;

public sealed class TickClock
{
	private readonly object _sync;
	private int _current;

	internal TickClock(object sync) => _sync = sync;

	public int Current
	{
		get
		{
			lock (_sync)
				return _current;
		}
	}

	public void Set(int tick)
	{
		lock (_sync)
			_current = tick;
	}

	public void Next()
	{
		lock (_sync)
			_current++;
	}
}
