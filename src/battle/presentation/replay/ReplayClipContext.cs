using Godot;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;

namespace GrimSpace.Battle.Presentation.Replay;

public sealed class ReplayClipContext(
	ReplayState replayState,
	IReadOnlyDictionary<string, UnitView> unitViews,
	TurnHistoryView turnHistory,
	HazardBurstView hazardBursts,
	Func<string, Color> colorFor,
	IReadOnlyDictionary<string, State> endStates,
	Action<State, Color> ensureView)
{
	public ReplayState ReplayState { get; } = replayState;
	public IReadOnlyDictionary<string, UnitView> UnitViews { get; } = unitViews;
	public TurnHistoryView TurnHistory { get; } = turnHistory;
	public HazardBurstView HazardBursts { get; } = hazardBursts;
	public Func<string, Color> ColorFor { get; } = colorFor;
	public IReadOnlyDictionary<string, State> EndStates { get; } = endStates;
	public Action<State, Color> EnsureView { get; } = ensureView;
	public ETorpedoMount? PendingTorpedoMount { get; set; }
}
