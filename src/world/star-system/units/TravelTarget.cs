namespace GrimSpace.World.StarSystem.Units;

public enum ETravelTargetKind
{
	None,
	Fleet,
	Wreck,
}

public readonly record struct TravelTarget(ETravelTargetKind Kind, string TargetId)
{
	public static TravelTarget None => new(ETravelTargetKind.None, "");

	public bool IsActive => Kind != ETravelTargetKind.None;

	public static TravelTarget Fleet(string unitId) => new(ETravelTargetKind.Fleet, unitId);

	public static TravelTarget Wreck(string contractId) => new(ETravelTargetKind.Wreck, contractId);

	public bool MatchesFleet(string unitId) =>
		Kind == ETravelTargetKind.Fleet
		&& string.Equals(TargetId, unitId, StringComparison.Ordinal);

	public bool MatchesWreck(string contractId) =>
		Kind == ETravelTargetKind.Wreck
		&& string.Equals(TargetId, contractId, StringComparison.Ordinal);
}
