using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Domains.Move;

internal sealed class MoveReopenHoldTracker
{
	public const double HoldThresholdSeconds = 0.18;

	private Coord? _cell;
	private double _elapsed;

	public bool IsPending => _cell is not null;

	public void Arm(Coord cell)
	{
		_cell = cell;
		_elapsed = 0;
	}

	public void Cancel() => _cell = null;

	public bool TryAdvance(double deltaSeconds, bool buttonHeld, out Coord activatedCell)
	{
		activatedCell = default;
		if (_cell is not Coord cell)
			return false;

		if (!buttonHeld)
		{
			Cancel();
			return false;
		}

		_elapsed += deltaSeconds;
		if (_elapsed < HoldThresholdSeconds)
			return false;

		activatedCell = cell;
		Cancel();
		return true;
	}
}
