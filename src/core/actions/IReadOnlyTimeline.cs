namespace GrimSpace.Core.Actions;

public interface IReadOnlyTimeline
{
	IReadOnlyList<IAction> At(int tick);

	IEnumerable<(int Tick, IAction Action)> From(int startTick);
}
