using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;

namespace GrimSpace.Battle.Targeting;

public interface ITargetFilter
{
	bool Matches(
		BattleWorld world,
		string actorId,
		string candidateId,
		IReadOnlyList<string> alreadyPicked);
}

/// <summary>Accepts candidates whose relation to the actor is one of <paramref name="Allowed"/>.</summary>
public sealed record RelationFilter(params EUnitRelation[] Allowed) : ITargetFilter
{
	public bool Matches(
		BattleWorld world,
		string actorId,
		string candidateId,
		IReadOnlyList<string> alreadyPicked)
	{
		var units = UnitRegistry.For(world);
		return Allowed.Contains(units.UnitOf(actorId).RelationTo(units.UnitOf(candidateId)));
	}
}

public sealed record MaxRangeFilter(int Cells) : ITargetFilter
{
	public bool Matches(
		BattleWorld world,
		string actorId,
		string candidateId,
		IReadOnlyList<string> alreadyPicked) =>
		world.StateOf(actorId).Position.ManhattanDistanceTo(world.StateOf(candidateId).Position) <= Cells;
}

public sealed record ExcludeAlreadyPickedFilter : ITargetFilter
{
	public bool Matches(
		BattleWorld world,
		string actorId,
		string candidateId,
		IReadOnlyList<string> alreadyPicked) =>
		!alreadyPicked.Contains(candidateId, StringComparer.Ordinal);
}
