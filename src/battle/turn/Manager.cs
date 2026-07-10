namespace GrimSpace.Battle.Turn;

public sealed class Manager
{
	public string? ActiveUnitId { get; private set; }
	public int TurnNumber { get; private set; } = 1;

	public void SetActiveUnit(string unitId) => ActiveUnitId = unitId;

	public bool IsActive(string unitId) => ActiveUnitId == unitId;

	public void AdvanceTurn() => TurnNumber++;
}
