namespace GrimSpace.World.StarSystem.Presentation;

/// <summary>
/// Tracks focus-lease generations for the map camera. Presentation transitions,
/// follow updates, and manual input supersede older leases so disposing a stale
/// handle cannot restore its captured pose.
/// </summary>
public sealed class MapCameraFocusLeases
{
	private int _generation;

	public int Begin() => ++_generation;

	public void Supersede() => _generation++;

	public bool IsCurrent(int generation) => generation == _generation;
}
