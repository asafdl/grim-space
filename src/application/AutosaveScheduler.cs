namespace GrimSpace.Application;

public sealed class AutosaveScheduler
{
	private readonly double _intervalSeconds;
	private double _elapsedSeconds;

	public AutosaveScheduler(double intervalSeconds)
	{
		if (!double.IsFinite(intervalSeconds) || intervalSeconds <= 0)
			throw new ArgumentOutOfRangeException(nameof(intervalSeconds));

		_intervalSeconds = intervalSeconds;
	}

	public bool Advance(double deltaSeconds)
	{
		if (!double.IsFinite(deltaSeconds) || deltaSeconds < 0)
			throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

		_elapsedSeconds += deltaSeconds;
		if (_elapsedSeconds < _intervalSeconds)
			return false;

		_elapsedSeconds %= _intervalSeconds;
		return true;
	}

	public void Reset() => _elapsedSeconds = 0;
}
