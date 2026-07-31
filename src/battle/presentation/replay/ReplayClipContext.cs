using Godot;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Presentation.Replay;

public sealed class ReplayClipContext(
	ReplayState replayState,
	IReadOnlyDictionary<string, UnitView> unitViews,
	TurnHistoryView turnHistory,
	HazardBurstView hazardBursts,
	Func<string, Color> colorFor)
{
	public ReplayState ReplayState { get; } = replayState;
	public IReadOnlyDictionary<string, UnitView> UnitViews { get; } = unitViews;
	public TurnHistoryView TurnHistory { get; } = turnHistory;
	public HazardBurstView HazardBursts { get; } = hazardBursts;
	public Func<string, Color> ColorFor { get; } = colorFor;
}
