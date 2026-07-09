namespace GrimSpace.Battle;

public sealed class UnitState
{
	public int Id { get; }
	public Team Team { get; }
	public GridCoord Position { get; private set; }
	public bool IsMobile { get; }

	public UnitState(int id, Team team, GridCoord position, bool isMobile)
	{
		Id = id;
		Team = team;
		Position = position;
		IsMobile = isMobile;
	}

	public void SetPosition(GridCoord position) => Position = position;
}
