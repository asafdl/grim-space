namespace GrimSpace.Battle.Presentation.Replay;

public readonly record struct ClipPlayback(double PauseSeconds = 0)
{
	public bool Pauses => PauseSeconds > 0;

	public static ClipPlayback Instant => new();

	public static ClipPlayback Pause(double seconds) => new(seconds);
}
