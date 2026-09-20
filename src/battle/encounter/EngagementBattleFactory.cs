using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter.Generation;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Player;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.Units.Enums;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.Battle.Encounter;

public static class EngagementBattleFactory
{

	record Team {
		public ETeam on { get; init; }

		public required List<Fleet> Fleets { get; init; }

		public int center { get; set; }
		public int maxX { get; set; }
		public int minX { get; set; }
	}

	//TODO: duplicated grid size, horrible.
	private const int GridSize = 64;
	private const int FieldMargin = 2;

	public static BattleEncounter Create(
		Fleet[] participantFleets,
		RunShipRegistry registry,
		int seed,
		string id)
	{
		ArgumentNullException.ThrowIfNull(registry);

		var rng = new Random(seed);
		var center = GridSize / 2;
		var fieldCenter = new Coord(center, center, center);

		var teams = Teams(participantFleets);
		
		var spawns = new List<BattleSpawn>();
		var occupiedPositions = new HashSet<Coord>();
		foreach (var team in teams) {
			foreach (var fleet in team.Fleets) {
				AddFleet(
					fleet,
					team,
					fieldCenter,
					rng,
					occupiedPositions,
					registry,
					spawns
				);
			}
		}

		
		return new BattleEncounter
		{
			Id = id,
			Seed = seed,
			Spawns = spawns,
			Objective = EObjective.EliminateOpponents,
			WorldHazards = AsteroidFieldGenerator.Generate(new AsteroidFieldConfig
			{
				Seed = seed,
				GridSize = GridSize,
				UnitPositions = spawns.Select(spawn => spawn.Position).ToArray(),
				RegionCenter = fieldCenter,
				RegionHalfExtent = GridSize / 2 - FieldMargin,
				RegionMargin = FieldMargin,
			}),
		};
	}

	private static void AddFleet(
    Fleet fleet,
    Team team,
    Coord fieldCenter,
    Random rng,
    HashSet<Coord> occupiedPositions,
    RunShipRegistry registry,
    List<BattleSpawn> spawns)
	{

		foreach (var member in fleet.Members)
		{
			Coord position;
			do
			{
				var x = rng.Next(team.minX, team.maxX + 1);
				position = new Coord(
					x,
					GridSize / 2 + rng.Next(-8, 9),
					GridSize / 2 + rng.Next(-8, 9));
			}
			while (!occupiedPositions.Add(position));

			spawns.Add(new BattleSpawn
			{
				Ship = registry.Get(member.Id).Clone(),
				Team = team.on,
				Position = position,
				Fore = AxisToward(position, fieldCenter),
				Dorsal = Coord.Up,
				ExecutionAgent = fleet.State.Faction == EFaction.Player
					? new UserExecutionAgent()
					: new AiController(),
			});
		}
	}

	private static Coord AxisToward(Coord from, Coord to)
	{
		var delta = to - from;
		var ax = System.Math.Abs(delta.X);
		var ay = System.Math.Abs(delta.Y);
		var az = System.Math.Abs(delta.Z);

		if (ax >= ay && ax >= az)
			return new Coord(System.Math.Sign(delta.X), 0, 0);

		if (ay >= ax && ay >= az)
			return new Coord(0, System.Math.Sign(delta.Y), 0);

		return new Coord(0, 0, System.Math.Sign(delta.Z));
	}

	private static List<Team> Teams(IReadOnlyList<Fleet> fleets)
	{
		// TODO: this should be replaced by actual team calculation, based on faction status and aggroed state in current encounter
		var playerFleets = fleets.Where(f => f.State.Faction == EFaction.Player).ToList();
		var enemyFleets = fleets.Where(f => f.State.Faction != EFaction.Player).ToList();
		List<Team> ToReturn =
			[
				new Team { on = ETeam.Player, Fleets = playerFleets },
				new Team { on = ETeam.Enemy,  Fleets = enemyFleets },
			];

		for(var i=0; i<ToReturn.Count; i++) {
			var usableWidth = GridSize - 2 * FieldMargin;
			var segmentWidth = usableWidth / ToReturn.Count;
			var minX = FieldMargin + segmentWidth * i;
			var maxX = minX + segmentWidth - 1;

			ToReturn[i].minX = minX;
			ToReturn[i].maxX = maxX;
			ToReturn[i].center = minX + segmentWidth / 2;
		}

		return ToReturn;
	}
}
