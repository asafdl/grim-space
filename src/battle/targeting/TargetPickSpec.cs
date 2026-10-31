using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;

namespace GrimSpace.Battle.Targeting;

/// <summary>Declares how many units an ability picks and which units may be picked.</summary>
public sealed class TargetPickSpec
{
	public int MinPicks { get; }
	public int MaxPicks { get; }
	public IReadOnlyList<ITargetFilter> Filters { get; }

	public TargetPickSpec(int minPicks, int maxPicks, IReadOnlyList<ITargetFilter> filters)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(minPicks, 1);
		ArgumentOutOfRangeException.ThrowIfLessThan(maxPicks, minPicks);
		ArgumentNullException.ThrowIfNull(filters);
		MinPicks = minPicks;
		MaxPicks = maxPicks;
		Filters = filters;
	}

	public bool CanPickMore(int pickedCount) => pickedCount < MaxPicks;

	public bool CanLockIn(int pickedCount) => pickedCount >= MinPicks && pickedCount <= MaxPicks;

	public static bool IsLiving(BattleWorld world, string unitId) =>
		UnitRegistry.For(world).TryGet(unitId, out var unit) && unit.State.IsAlive;

	/// <summary>Living units passing every filter, nearest to the actor first, ties by id.</summary>
	public IReadOnlyList<string> LegalTargets(
		BattleWorld world,
		string actorId,
		IReadOnlyList<string> alreadyPicked)
	{
		var units = UnitRegistry.For(world);
		if (!units.TryGet(actorId, out var actor))
			return [];

		var origin = actor.State.Position;
		return units.All
			.Where(unit => unit.State.IsAlive
				&& Filters.All(filter => filter.Matches(world, actorId, unit.State.Id, alreadyPicked)))
			.OrderBy(unit =>
			{
				var offset = unit.State.Position - origin;
				return offset.X * offset.X + offset.Y * offset.Y + offset.Z * offset.Z;
			})
			.ThenBy(unit => unit.State.Id, StringComparer.Ordinal)
			.Select(unit => unit.State.Id)
			.ToArray();
	}
}
