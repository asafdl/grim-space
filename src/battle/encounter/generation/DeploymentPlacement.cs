using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Player;
using GrimSpace.Math.Grid;
using GrimSpace.Run.Items;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Units;
using BattleUnitType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.Battle.Encounter.Generation;

public static class DeploymentPlacement
{
	private const int Margin = 4;
	private const int EngagementSeparation = 12;
	private static readonly Coord[] EngagementSeparationOffsets = BuildEngagementSeparationOffsets(10, 12);
	private static readonly Coord[] CardinalDirections =
	[
		new(1, 0, 0),
		new(-1, 0, 0),
		new(0, 1, 0),
		new(0, -1, 0),
		new(0, 0, 1),
		new(0, 0, -1),
	];

	public static List<BattleSpawn> ForEngagement(
		IReadOnlyList<Fleet> participantFleets,
		Inventory registry,
		int seed,
		int gridSize,
		string initiatorFleetId,
		IReadOnlyDictionary<string, PilotBattleSnapshot>? shipIdToPilotSnapshot = null,
		bool canonicalLayout = false,
		bool tutorialScriptedSpawn = false,
		IReadOnlySet<string>? playerDeployShipIds = null)
	{
		ArgumentNullException.ThrowIfNull(registry);
		ArgumentNullException.ThrowIfNull(participantFleets);
		ArgumentException.ThrowIfNullOrWhiteSpace(initiatorFleetId);
		ArgumentOutOfRangeException.ThrowIfLessThan(gridSize, 1);

		if (!participantFleets.Any(fleet => fleet.State.Id == initiatorFleetId))
			throw new ArgumentException(
				$"Initiator fleet '{initiatorFleetId}' is not an encounter participant.",
				nameof(initiatorFleetId));

		var attackerMembers = participantFleets
			.Where(fleet => fleet.State.Id == initiatorFleetId)
			.SelectMany(fleet => fleet.Members.Select(member => (fleet, member)))
			.Where(entry => IncludesMember(entry.fleet, entry.member, playerDeployShipIds))
			.ToList();
		var interceptedMembers = participantFleets
			.Where(fleet => fleet.State.Id != initiatorFleetId)
			.SelectMany(fleet => fleet.Members.Select(member => (fleet, member)))
			.Where(entry => IncludesMember(entry.fleet, entry.member, playerDeployShipIds))
			.ToList();

		var rng = new Random(seed);
		if (tutorialScriptedSpawn)
			return PlaceFirstBattleTutorial(
				[.. attackerMembers, .. interceptedMembers],
				rng,
				gridSize,
				registry,
				shipIdToPilotSnapshot);

		Coord attackerAnchor;
		Coord interceptedAnchor;
		Coord attackerFore;
		Coord attackerDorsal;
		Coord interceptedFore;
		Coord interceptedDorsal;
		if (canonicalLayout || !TryPickVariedEngagementAnchors(rng, gridSize, out attackerAnchor, out interceptedAnchor))
		{
			var center = gridSize / 2;
			var approach = CardinalDirections[rng.Next(CardinalDirections.Length)];
			var halfSeparation = System.Math.Min(EngagementSeparation / 2, System.Math.Max(0, center - Margin));
			var fieldCenter = new Coord(center, center, center);
			attackerAnchor = fieldCenter - approach * halfSeparation;
			interceptedAnchor = fieldCenter + approach * halfSeparation;
			attackerFore = approach;
			attackerDorsal = PickPerpendicular(approach, rng);
			interceptedFore = CardinalDirections[rng.Next(CardinalDirections.Length)];
			interceptedDorsal = PickPerpendicular(interceptedFore, rng);
		}
		else
		{
			attackerFore = AxisToward(attackerAnchor, interceptedAnchor);
			interceptedFore = AxisToward(interceptedAnchor, attackerAnchor);
			attackerDorsal = PickPerpendicular(attackerFore, rng);
			interceptedDorsal = PickPerpendicular(interceptedFore, rng);
		}
		var occupied = new HashSet<Coord>();
		var spawns = new List<BattleSpawn>();
		PlaceFormation(
			attackerMembers,
			attackerAnchor,
			attackerFore,
			attackerDorsal,
			rng,
			gridSize,
			occupied,
			registry,
			shipIdToPilotSnapshot,
			spawns);
		PlaceFormation(
			interceptedMembers,
			interceptedAnchor,
			interceptedFore,
			interceptedDorsal,
			rng,
			gridSize,
			occupied,
			registry,
			shipIdToPilotSnapshot,
			spawns);
		return spawns;
	}

	private static bool IncludesMember(
		Fleet fleet,
		FleetMember member,
		IReadOnlySet<string>? playerDeployShipIds) =>
		playerDeployShipIds is null
		|| fleet.State.Type != GrimSpace.World.StarSystem.Units.EType.PlayerFleet
		|| playerDeployShipIds.Contains(member.Id);

	private static void PlaceFormation(
		IReadOnlyList<(Fleet Fleet, FleetMember Member)> members,
		Coord anchor,
		Coord fore,
		Coord dorsal,
		Random rng,
		int gridSize,
		HashSet<Coord> occupied,
		Inventory registry,
		IReadOnlyDictionary<string, PilotBattleSnapshot>? shipIdToPilotSnapshot,
		List<BattleSpawn> spawns)
	{
		if (members.Count == 0)
			return;

		var basis = GridBasis.From(fore, dorsal, Coord.Cross(dorsal, fore));

		for (var index = 0; index < members.Count; index++)
		{
			var (fleet, member) = members[index];
			var position = index == 0 && occupied.Add(anchor)
				? anchor
				: PickFormationCell(rng, anchor, basis, occupied, gridSize);
			spawns.Add(CreateSpawn(fleet, member, position, fore, dorsal, registry, shipIdToPilotSnapshot));
		}
	}

	private static List<BattleSpawn> PlaceFirstBattleTutorial(
		IReadOnlyList<(Fleet Fleet, FleetMember Member)> members,
		Random rng,
		int gridSize,
		Inventory registry,
		IReadOnlyDictionary<string, PilotBattleSnapshot>? shipIdToPilotSnapshot)
	{
		var center = gridSize / 2;
		var target = new Coord(System.Math.Min(center, gridSize - 12), center, center);
		var towardEnemy = new Coord(1, 0, 0);
		var towardPlayer = new Coord(-1, 0, 0);
		Coord[] playerSlots = [target + new Coord(-3, -3, 0), target + new Coord(-7, -3, -3), target + new Coord(-7, -3, 3)];
		Coord[] enemySlots = [target, target + new Coord(11, 1, -6), target + new Coord(11, 1, 6)];

		var players = members
			.Where(entry => entry.Fleet.State.Faction == EFaction.Player)
			.OrderBy(entry => registry.Get(entry.Member.Id).Spec.Chassis == BattleUnitType.Fighter ? 0 : 1)
			.ToArray();
		var enemies = members
			.Where(entry => entry.Fleet.State.Faction != EFaction.Player)
			.ToArray();
		var occupied = new HashSet<Coord>(playerSlots.Concat(enemySlots));
		var spawns = new List<BattleSpawn>(members.Count);
		Place(players, playerSlots, towardEnemy);
		Place(enemies, enemySlots, towardPlayer);
		return spawns;

		void Place(IReadOnlyList<(Fleet Fleet, FleetMember Member)> side, Coord[] slots, Coord fore)
		{
			var basis = GridBasis.From(fore, Coord.Up, Coord.Cross(Coord.Up, fore));
			for (var index = 0; index < side.Count; index++)
			{
				var position = index < slots.Length
					? slots[index]
					: PickFormationCell(rng, slots[^1], basis, occupied, gridSize);
				spawns.Add(CreateSpawn(
					side[index].Fleet,
					side[index].Member,
					position,
					fore,
					Coord.Up,
					registry,
					shipIdToPilotSnapshot));
			}
		}
	}

	private static BattleSpawn CreateSpawn(
		Fleet fleet,
		FleetMember member,
		Coord position,
		Coord fore,
		Coord dorsal,
		Inventory registry,
		IReadOnlyDictionary<string, PilotBattleSnapshot>? shipIdToPilotSnapshot)
	{
		var team = fleet.State.Faction == EFaction.Player ? ETeam.Player : ETeam.Enemy;
		PilotBattleSnapshot? pilot = null;
		if (team == ETeam.Player && shipIdToPilotSnapshot is not null
			&& !shipIdToPilotSnapshot.TryGetValue(member.Id, out pilot))
			throw new InvalidOperationException(
				$"Player ship '{member.Id}' has no pilot snapshot for deployment.");

		return new BattleSpawn
		{
			Ship = registry.Get(member.Id).Clone(),
			Pilot = pilot,
			Team = team,
			Faction = fleet.State.Faction,
			Position = position,
			Fore = fore,
			Dorsal = dorsal,
			ExecutionAgent = team == ETeam.Player
				? new UserExecutionAgent()
				: new AiController(),
		};
	}

	private static Coord PickFormationCell(
		Random rng,
		Coord anchor,
		GridBasis basis,
		HashSet<Coord> occupied,
		int gridSize)
	{
		for (var attempt = 0; attempt < 64; attempt++)
		{
			var candidate = basis.ToWorldCell(
				anchor,
				rng.Next(-2, 3),
				rng.Next(-3, 4),
				rng.Next(-2, 3));
			if (!IsInBounds(candidate, gridSize) || !occupied.Add(candidate))
				continue;

			return candidate;
		}

		throw new InvalidOperationException("Could not find an open deployment cell.");
	}

	private static Coord[] BuildEngagementSeparationOffsets(int minSeparation, int maxSeparation)
	{
		var offsets = new List<Coord>();
		for (var x = -maxSeparation; x <= maxSeparation; x++)
		{
			for (var y = -maxSeparation; y <= maxSeparation; y++)
			{
				for (var z = -maxSeparation; z <= maxSeparation; z++)
				{
					var lengthSquared = x * x + y * y + z * z;
					if (lengthSquared < minSeparation * minSeparation
						|| lengthSquared > maxSeparation * maxSeparation)
						continue;

					offsets.Add(new Coord(x, y, z));
				}
			}
		}

		return offsets.ToArray();
	}

	private static bool TryPickVariedEngagementAnchors(
		Random rng,
		int gridSize,
		out Coord attackerAnchor,
		out Coord interceptedAnchor)
	{
		attackerAnchor = default;
		interceptedAnchor = default;
		if (gridSize <= Margin * 2)
			return false;

		for (var attempt = 0; attempt < 128; attempt++)
		{
			attackerAnchor = new Coord(
				rng.Next(Margin, gridSize - Margin),
				rng.Next(Margin, gridSize - Margin),
				rng.Next(Margin, gridSize - Margin));
			interceptedAnchor = attackerAnchor + EngagementSeparationOffsets[
				rng.Next(EngagementSeparationOffsets.Length)];
			if (interceptedAnchor.X >= Margin && interceptedAnchor.X < gridSize - Margin
				&& interceptedAnchor.Y >= Margin && interceptedAnchor.Y < gridSize - Margin
				&& interceptedAnchor.Z >= Margin && interceptedAnchor.Z < gridSize - Margin)
				return true;
		}

		return false;
	}

	private static Coord PickPerpendicular(Coord direction, Random rng)
	{
		var candidates = CardinalDirections
			.Where(candidate => Coord.Dot(candidate, direction) == 0)
			.ToArray();
		return candidates[rng.Next(candidates.Length)];
	}

	private static bool IsInBounds(Coord cell, int gridSize) =>
		cell.X >= 0 && cell.X < gridSize
		&& cell.Y >= 0 && cell.Y < gridSize
		&& cell.Z >= 0 && cell.Z < gridSize;

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
}
