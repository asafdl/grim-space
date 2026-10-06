using GrimSpace.World.StarSystem.Contact;

namespace GrimSpace.World.StarSystem.Units;

public enum ETravelTargetKind
{
	None,
	Fleet,
	Wreck,
}

public readonly record struct TravelTarget(
	ETravelTargetKind Kind,
	string TargetId,
	EContactIntent? ContactIntent)
{
	public static TravelTarget None => new(ETravelTargetKind.None, "", null);

	public bool IsActive => Kind != ETravelTargetKind.None;

	public static TravelTarget Fleet(
		string unitId,
		EContactIntent intent) =>
		new(ETravelTargetKind.Fleet, unitId, intent);

	public static TravelTarget Wreck(string contractId) =>
		new(ETravelTargetKind.Wreck, contractId, null);

	public bool MatchesFleet(string unitId) =>
		Kind == ETravelTargetKind.Fleet
		&& string.Equals(TargetId, unitId, StringComparison.Ordinal);

	public bool MatchesFleet(string unitId, EContactIntent intent) =>
		MatchesFleet(unitId)
		&& ContactIntent == intent;

	public bool MatchesWreck(string contractId) =>
		Kind == ETravelTargetKind.Wreck
		&& string.Equals(TargetId, contractId, StringComparison.Ordinal);
}
