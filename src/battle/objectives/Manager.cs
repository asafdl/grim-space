using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;

namespace GrimSpace.Battle.Objectives;

public sealed class Manager
{
	private readonly EObjective _objective;
	private readonly IReadOnlyList<BattleParticipant> _participants;

	public Manager(
		EObjective objective,
		IReadOnlyList<BattleParticipant> declaredParticipants,
		UnitRegistry units)
	{
		_objective = objective;
		_participants = ResolveParticipants(declaredParticipants, units);
	}

	public BattleOutcome Evaluate(BattleWorld world, string perspectiveUnitId) =>
		_objective switch
		{
			EObjective.EliminateOpponents => EliminateOpponents(world, perspectiveUnitId),
			_ => throw new ArgumentOutOfRangeException(nameof(_objective), _objective, null),
		};

	public BattleOutcome Retire(BattleWorld world, string perspectiveUnitId) =>
		CreateOutcome(world, perspectiveUnitId, EBattleResult.Lose);

	private BattleOutcome EliminateOpponents(BattleWorld world, string perspectiveUnitId)
	{
		var units = UnitRegistry.For(world);
		var perspective = _participants.Single(
			participant => participant.TacticalUnitIds.Contains(perspectiveUnitId, StringComparer.Ordinal));
		var perspectiveAlliance = units.UnitOf(perspective.TacticalUnitIds[0]).Alliance;
		var states = ParticipantStates(units);
		var livingParticipants = _participants
			.Where(participant => states[participant.ParticipantId] == EBattleParticipantState.Alive)
			.ToArray();

		if (livingParticipants.Length == 0)
			return CreateOutcome(EBattleResult.Tie, units, states);

		var anyFriendly = livingParticipants.Any(participant =>
			perspectiveAlliance.IsAlliedWith(units.UnitOf(participant.TacticalUnitIds[0]).Alliance));
		var anyOpponent = livingParticipants.Any(participant =>
			!perspectiveAlliance.IsAlliedWith(units.UnitOf(participant.TacticalUnitIds[0]).Alliance));

		if (anyFriendly && anyOpponent)
			return CreateOutcome(EBattleResult.Ongoing, units, states);

		return CreateOutcome(anyFriendly ? EBattleResult.Win : EBattleResult.Lose, units, states);
	}

	private BattleOutcome CreateOutcome(
		BattleWorld world,
		string perspectiveUnitId,
		EBattleResult result)
	{
		var units = UnitRegistry.For(world);
		_ = _participants.Single(
			participant => participant.TacticalUnitIds.Contains(perspectiveUnitId, StringComparer.Ordinal));
		return CreateOutcome(result, units, ParticipantStates(units));
	}

	private Dictionary<string, EBattleParticipantState> ParticipantStates(UnitRegistry units) =>
		_participants.ToDictionary(
			participant => participant.ParticipantId,
			participant => participant.TacticalUnitIds.Any(unitId => units.UnitOf(unitId).State.IsAlive)
				? EBattleParticipantState.Alive
				: EBattleParticipantState.Destroyed,
			StringComparer.Ordinal);

	private static IReadOnlyList<BattleParticipant> ResolveParticipants(
		IReadOnlyList<BattleParticipant> declared,
		UnitRegistry units)
	{
		var tacticalUnits = units.All.ToArray();
		if (declared.Count == 0)
			return tacticalUnits
				.Select(unit => new BattleParticipant(unit.State.Id, [unit.State.Id]))
				.ToArray();

		var unitIds = tacticalUnits
			.Select(unit => unit.State.Id)
			.ToHashSet(StringComparer.Ordinal);
		var unitsById = tacticalUnits.ToDictionary(unit => unit.State.Id, StringComparer.Ordinal);
		var participantIds = declared.Select(participant => participant.ParticipantId).ToArray();
		var memberIds = declared
			.SelectMany(participant => participant.TacticalUnitIds)
			.ToArray();
		if (participantIds.Any(string.IsNullOrWhiteSpace)
			|| participantIds.Distinct(StringComparer.Ordinal).Count() != participantIds.Length
			|| declared.Any(participant => participant.TacticalUnitIds.Count == 0)
			|| memberIds.Distinct(StringComparer.Ordinal).Count() != memberIds.Length
			|| !unitIds.SetEquals(memberIds)
			|| declared.Any(participant => participant.TacticalUnitIds
				.Select(unitId => unitsById[unitId].Alliance.Team)
				.Distinct()
				.Skip(1)
				.Any()))
		{
			throw new InvalidOperationException(
				"Battle participants must uniquely contain every initial tactical unit.");
		}

		return declared;
	}

	private BattleOutcome CreateOutcome(
		EBattleResult result,
		UnitRegistry units,
		IReadOnlyDictionary<string, EBattleParticipantState> states) =>
		BattleOutcome.Create(
			result,
			[.. states.Select(pair => (pair.Key, pair.Value))],
			[.. TacticalUnitOutcomes(units)]);

	private IReadOnlyList<TacticalUnitOutcome> TacticalUnitOutcomes(UnitRegistry units)
	{
		var memberToParticipant = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (var participant in _participants)
		{
			foreach (var unitId in participant.TacticalUnitIds)
				memberToParticipant[unitId] = participant.ParticipantId;
		}

		var unitsById = units.All.ToDictionary(unit => unit.State.Id, StringComparer.Ordinal);
		return units.All
			.Select(unit => new TacticalUnitOutcome(
				unit.State.Id,
				ResolveOwner(unit.State.Id, memberToParticipant, unitsById),
				unit.State.Type,
				unit.State.IsAlive ? EBattleParticipantState.Alive : EBattleParticipantState.Destroyed))
			.ToArray();
	}

	private static string ResolveOwner(
		string unitId,
		IReadOnlyDictionary<string, string> memberToParticipant,
		IReadOnlyDictionary<string, Unit> unitsById)
	{
		var current = unitId;
		while (true)
		{
			if (memberToParticipant.TryGetValue(current, out var participantId))
				return participantId;

			if (!unitsById.TryGetValue(current, out var unit))
				throw new InvalidOperationException($"Tactical unit '{unitId}' has no owning battle participant.");

			var parentId = unit.State.ParentId;
			if (parentId == BattleActorIds.Rules)
				throw new InvalidOperationException($"Tactical unit '{unitId}' has no owning battle participant.");

			current = parentId;
		}
	}
}
