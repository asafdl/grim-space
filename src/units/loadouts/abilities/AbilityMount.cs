using GrimSpace.Math.Grid;

namespace GrimSpace.Units.Loadouts.Abilities;

public sealed record AbilityMount(EAbilityKind Ability, ESpatialOrientation Mount)
{
	public AbilityDefinition Definition => AbilityDefinition.For(Ability);

	public static void EnsureUniqueOnShip(IReadOnlyList<AbilityMount> mounts)
	{
		ArgumentNullException.ThrowIfNull(mounts);
		if (mounts.Count == 0)
			return;

		if (mounts.ToHashSet().Count != mounts.Count)
			throw new ArgumentException("Each ability mount must have a unique ability and facet pair.", nameof(mounts));
	}
}
