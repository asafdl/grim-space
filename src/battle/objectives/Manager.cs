using GrimSpace.Battle.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Objectives;

public sealed class Manager
{
	private readonly EObjective _objective;
	private readonly UnitRegistry _units;

	public Manager(
		EObjective objective,
		UnitRegistry units)
	{
		_objective = objective;
		_units = units;
	}

	public EBattleResult EvaluateFor(ETeam team) =>
		_objective switch
		{
			EObjective.EliminateOpponents => EliminateOpponents(team),
			_ => throw new ArgumentOutOfRangeException(nameof(_objective), _objective, null),
		};

	public EBattleResult Retire() =>
		EBattleResult.Lose;

	private EBattleResult EliminateOpponents(ETeam team)
	{
		var teamAlive = _units.All
		.GroupBy(unit => unit.Team)
		.ToDictionary(
			group => group.Key,
			group => group.Any(unit => unit.State.IsAlive));

		
		var perspectiveAlive = teamAlive.GetValueOrDefault(team);
		var anyOtherAlive = teamAlive
			.Where(pair => pair.Key != team)
			.Any(pair => pair.Value);
		return (perspectiveAlive, anyOtherAlive) switch
		{
			(true, false) => EBattleResult.Win,
			(false, true) => EBattleResult.Lose,
			(false, false) => EBattleResult.Tie,
			_ => EBattleResult.Ongoing,
		};
	}
}
