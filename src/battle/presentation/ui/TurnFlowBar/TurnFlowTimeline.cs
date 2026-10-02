namespace GrimSpace.Battle.Presentation.Ui;

public sealed record TurnFlowEntry(string? UnitId, bool EndTurnAfter = false);

public static class TurnFlowTimeline
{
	public static IReadOnlyList<TurnFlowEntry> Build(
		IReadOnlyList<IReadOnlyList<string>> completedTurns,
		IReadOnlyList<string> currentTurn)
	{
		var timeline = new List<TurnFlowEntry>(
			completedTurns.Sum(turn => turn.Count) + currentTurn.Count);

		foreach (var turn in completedTurns)
		{
			for (var index = 0; index < turn.Count; index++)
				timeline.Add(new TurnFlowEntry(turn[index], index == turn.Count - 1));
		}

		timeline.AddRange(currentTurn.Select(unitId => new TurnFlowEntry(unitId)));
		return timeline;
	}

	public static int ClampIndex(int index, int count) =>
		count == 0 ? 0 : System.Math.Clamp(index, 0, count - 1);

	public static int ActivationIndexForSlot(int centerIndex, int slot, int centerSlot = 2) =>
		centerIndex + (slot - centerSlot);

	public static string? UnitAt(IReadOnlyList<TurnFlowEntry> timeline, int activationIndex) =>
		activationIndex < 0 || activationIndex >= timeline.Count
			? null
			: timeline[activationIndex].UnitId;

	public static bool HasDividerAfter(
		IReadOnlyList<TurnFlowEntry> timeline,
		int activationIndex) =>
		activationIndex >= 0
		&& activationIndex < timeline.Count
		&& timeline[activationIndex].EndTurnAfter;

	public static int CompletedTurnsToLeft(
		IReadOnlyList<TurnFlowEntry> timeline,
		int activationIndex)
	{
		if (activationIndex < 0)
			return 0;

		var end = System.Math.Min(activationIndex, timeline.Count - 1);
		var count = 0;
		for (var index = 0; index <= end; index++)
		{
			if (timeline[index].EndTurnAfter)
				count++;
		}

		return count;
	}

	public static int StartOfCurrentSegment(IReadOnlyList<TurnFlowEntry> timeline)
	{
		for (var index = timeline.Count - 1; index >= 0; index--)
		{
			if (timeline[index].EndTurnAfter)
				return index + 1;
		}

		return 0;
	}
}
