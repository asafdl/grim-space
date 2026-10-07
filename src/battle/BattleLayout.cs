using GrimSpace.Battle.World;
using GrimSpace.Units.Enums;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle;

/// <summary>
/// Frozen encounter scaffolding for presentation setup: grid, terrain asteroids, and participant identity.
/// No live state — use <see cref="BattleOrchestrator.Sim"/> for preview and engine world for resolution.
/// </summary>
public sealed class BattleLayout
{
	public BoundedGrid Grid { get; }
	public IReadOnlyList<Asteroid> Asteroids { get; }
	public IReadOnlyDictionary<string, ETeam> Participants { get; }

	public BattleLayout(
		BoundedGrid grid,
		IReadOnlyList<Asteroid> asteroids,
		IReadOnlyDictionary<string, ETeam> participants)
	{
		Grid = grid;
		Asteroids = asteroids;
		Participants = participants;
	}

	public static BattleLayout FromEncounter(
		BoundedGrid grid,
		IEnumerable<Asteroid> asteroids,
		IEnumerable<Units.Unit> units) =>
		new(
			grid,
			asteroids.ToList(),
			units.ToDictionary(unit => unit.State.Id, unit => unit.Team));
}
