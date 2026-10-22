using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Player;
using GrimSpace.Math.Grid;
using GrimSpace.Run.Items;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Units;
using BattleUnitType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.Battle.Encounter.Generation;

public static class DeploymentPlacement
{
	private const int Margin = 4;
	private const int DeploySpreadDivisor = 5;
	private const int EnemySpreadBand = 4;
	private const int LaneHalfBand = 8;
	private const int EngagementSeparation = 16;
	private const int MinEngagementSeparation = EngagementSeparation;
	private const int MaxEngagementSeparation = 20;
	private const int MinEngagementSeparationSquared = MinEngagementSeparation * MinEngagementSeparation;
	private const int MaxEngagementSeparationSquared = MaxEngagementSeparation * MaxEngagementSeparation;
	private static readonly Coord[] EngagementSeparationOffsets = BuildEngagementSeparationOffsets();
	private static readonly Coord[] CardinalDirections =
	[
		new(1, 0, 0),
		new(-1, 0, 0),
		new(0, 1, 0),
		new(0, -1, 0),
		new(0, 0, 1),
		new(0, 0, -1),
	];

	public static (BattleSpawn Player, BattleSpawn Enemy) DevDuel(
		BattleUnitType playerChassis,
		BattleUnitType enemyChassis,
		int seed,
		int gridSize,
		string? playerId = null,
		string? enemyId = null)
	{
		var playerShip = ShipInstance.FromCatalog(
			playerId ?? "dev-player",
			playerChassis);
		playerShip = UpgradeDevShip(playerShip);
		var enemyShip = ShipInstance.FromCatalog(
			enemyId ?? "dev-enemy",
			enemyChassis);
		enemyShip = UpgradeDevShip(enemyShip);
		return DevDuel(playerShip, enemyShip, seed, gridSize);
	}

	internal static ShipInstance UpgradeDevShip(ShipInstance ship)
	{
		foreach (var slot in ship.Spec.Slots)
		{
			if (ship.Loadout.InstalledAbilities.Any(installed => installed.Mount == slot.Mount))
				continue;
			if (!ship.TryWithInstalledAbility(
				new InstalledAbility(slot.Mount.Kind, slot.Mount.Facet),
				out var expanded))
			{
				throw new InvalidOperationException($"Dev ship '{ship.Spec.Chassis}' could not install '{slot.Mount}'.");
			}

			ship = expanded;
		}

		foreach (var installed in ship.Loadout.InstalledAbilities.ToArray())
		{
			if (ship.TryWithDamageUpgraded(installed.Mount, out var damageUpgraded))
				ship = damageUpgraded;
			if (ship.TryWithRangeUpgraded(installed.Mount, out var rangeUpgraded))
				ship = rangeUpgraded;
		}

		return ship;
	}

	public static List<BattleSpawn> ForEngagement(
		IReadOnlyList<Fleet> participantFleets,
		Inventory registry,
		int seed,
		int gridSize,
		string initiatorFleetId,
		IReadOnlyDictionary<string, PilotBattleSnapshot>? shipIdToPilotSnapshot = null,
		bool canonicalLayout = false,
		bool tutorialScriptedSpawn = false)
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
			.ToList();
		var interceptedMembers = participantFleets
			.Where(fleet => fleet.State.Id != initiatorFleetId)
			.SelectMany(fleet => fleet.Members.Select(member => (fleet, member)))
			.ToList();

		var rng = new Random(seed);
		Coord attackerAnchor;
		Coord interceptedAnchor;
		Coord attackerFore;
		Coord attackerDorsal;
		Coord interceptedFore;
		Coord interceptedDorsal;
		if (tutorialScriptedSpawn)
		{
			var center = gridSize / 2;
			var halfSeparation = System.Math.Min(EngagementSeparation / 2, System.Math.Max(0, center - Margin));
			var fieldCenter = new Coord(center, center, center);
			var approach = new Coord(1, 0, 0);
			attackerAnchor = fieldCenter - approach * halfSeparation;
			interceptedAnchor = fieldCenter + approach * halfSeparation;
			attackerFore = approach;
			attackerDorsal = Coord.Up;
			interceptedFore = new Coord(-1, 0, 0);
			interceptedDorsal = Coord.Up;
		}
		else if (canonicalLayout || !TryPickVariedEngagementAnchors(rng, gridSize, out attackerAnchor, out interceptedAnchor))
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

	public static (BattleSpawn Player, BattleSpawn Enemy) DevDuel(
		ShipInstance playerShip,
		ShipInstance enemyShip,
		int seed,
		int gridSize)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(gridSize, 1);
		var center = gridSize / 2;
		var deploySpread = gridSize / DeploySpreadDivisor;
		var playerPosition = new Coord(center - deploySpread, center, center);
		var enemyPosition = PickEnemyPosition(seed, gridSize, playerPosition);
		var dorsal = Coord.Up;
		var playerFore = AxisToward(playerPosition, enemyPosition);
		var enemyFore = AxisToward(enemyPosition, playerPosition);

		return (
			new BattleSpawn
			{
				Ship = playerShip,
				Team = ETeam.Player,
				Position = playerPosition,
				Fore = playerFore,
				Dorsal = dorsal,
				ExecutionAgent = new UserExecutionAgent(),
			},
			new BattleSpawn
			{
				Ship = enemyShip,
				Team = ETeam.Enemy,
				Position = enemyPosition,
				Fore = enemyFore,
				Dorsal = dorsal,
				ExecutionAgent = new AiController(),
			});
	}

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
			var team = fleet.State.Faction == EFaction.Player ? ETeam.Player : ETeam.Enemy;
			var position = index == 0 && occupied.Add(anchor)
				? anchor
				: PickFormationCell(rng, anchor, basis, occupied, gridSize);
			PilotBattleSnapshot? pilot = null;
			if (team == ETeam.Player && shipIdToPilotSnapshot is not null)
			{
				if (!shipIdToPilotSnapshot.TryGetValue(member.Id, out pilot))
					throw new InvalidOperationException(
						$"Player ship '{member.Id}' has no pilot snapshot for deployment.");
			}

			spawns.Add(new BattleSpawn
			{
				Ship = registry.Get(member.Id).Clone(),
				Pilot = pilot,
				Team = team,
				Position = position,
				Fore = fore,
				Dorsal = dorsal,
				ExecutionAgent = fleet.State.Faction == EFaction.Player
					? new UserExecutionAgent()
					: new AiController(),
			});
		}
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

	private static Coord[] BuildEngagementSeparationOffsets()
	{
		var offsets = new List<Coord>();
		for (var x = -MaxEngagementSeparation; x <= MaxEngagementSeparation; x++)
		{
			for (var y = -MaxEngagementSeparation; y <= MaxEngagementSeparation; y++)
			{
				for (var z = -MaxEngagementSeparation; z <= MaxEngagementSeparation; z++)
				{
					var lengthSquared = x * x + y * y + z * z;
					if (lengthSquared < MinEngagementSeparationSquared
						|| lengthSquared > MaxEngagementSeparationSquared)
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
		if (EngagementSeparationOffsets.Length == 0)
			return false;

		var anchorMin = Margin + MaxEngagementSeparation;
		var anchorMax = gridSize - Margin - MaxEngagementSeparation - 1;
		if (anchorMin > anchorMax)
			return false;

		for (var attempt = 0; attempt < 128; attempt++)
		{
			attackerAnchor = new Coord(
				rng.Next(anchorMin, anchorMax + 1),
				rng.Next(anchorMin, anchorMax + 1),
				rng.Next(anchorMin, anchorMax + 1));
			var offset = EngagementSeparationOffsets[rng.Next(EngagementSeparationOffsets.Length)];
			interceptedAnchor = attackerAnchor + offset;
			if (IsInBounds(interceptedAnchor, gridSize))
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

	private static Coord PickEnemyPosition(int seed, int gridSize, Coord playerPosition)
	{
		var rng = new Random(seed);
		var half = gridSize / 2;
		var center = gridSize / 2;

		var deploySpread = gridSize / DeploySpreadDivisor;
		var enemyCenter = center + deploySpread;
		var minX = playerPosition.X < half
			? enemyCenter - EnemySpreadBand
			: Margin;
		var maxX = playerPosition.X < half
			? enemyCenter + EnemySpreadBand
			: half - Margin - 1;

		return new Coord(
			NextInBounds(rng, minX, maxX, gridSize),
			NextInBounds(rng, center - LaneHalfBand, center + LaneHalfBand, gridSize),
			NextInBounds(rng, center - LaneHalfBand, center + LaneHalfBand, gridSize));
	}

	private static int NextInBounds(Random rng, int min, int max, int gridSize)
	{
		var last = gridSize - 1;
		var boundedMin = System.Math.Clamp(min, 0, last);
		var boundedMax = System.Math.Clamp(max, boundedMin, last);
		return rng.Next(boundedMin, boundedMax + 1);
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
}
