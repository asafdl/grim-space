using Godot;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Presentation.Replay;

public sealed class ReplayClipContext(
	ReplayState replayState,
	IReadOnlyDictionary<string, UnitView> unitViews,
	TurnHistoryView turnHistory,
	Func<string, Color> colorFor)
{
	public ReplayState ReplayState { get; } = replayState;
	public IReadOnlyDictionary<string, UnitView> UnitViews { get; } = unitViews;
	public TurnHistoryView TurnHistory { get; } = turnHistory;
	public Func<string, Color> ColorFor { get; } = colorFor;

	public void SyncUnit(string actorId) =>
		UnitViews[actorId].SyncFromState(ReplayState.StateOf(actorId));
}
