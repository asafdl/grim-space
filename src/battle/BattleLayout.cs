using GrimSpace.Battle.World;
using GrimSpace.Units.Enums;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle;

/// <summary>
/// Frozen encounter scaffolding for presentation setup: grid, terrain hazards, and participant identity.
/// No live state — use <see cref="BattleOrchestrator.Sim"/> for preview and engine world for resolution.
/// </summary>
public sealed class BattleLayout
{
	public BoundedGrid Grid { get; }
	public IReadOnlyList<Hazard> TerrainHazards { get; }
	public IReadOnlyDictionary<string, ETeam> Participants { get; }

	public BattleLayout(
		BoundedGrid grid,
		IReadOnlyList<Hazard> terrainHazards,
		IReadOnlyDictionary<string, ETeam> participants)
	{
		Grid = grid;
		TerrainHazards = terrainHazards;
		Participants = participants;
	}

	public static BattleLayout FromEncounter(
		BoundedGrid grid,
		IEnumerable<Hazard> terrainHazards,
		IEnumerable<Units.Unit> units) =>
		new(
			grid,
			terrainHazards.ToList(),
			units.ToDictionary(unit => unit.State.Id, unit => unit.Alliance.Team));
}
