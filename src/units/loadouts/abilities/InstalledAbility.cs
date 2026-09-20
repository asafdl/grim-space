using GrimSpace.Math.Grid;

namespace GrimSpace.Units.Loadouts.Abilities;

/// <summary>
/// One ability installed on one ship facet.
/// </summary>
public sealed record InstalledAbility(
	AbilitySpec Spec,
	ESpatialOrientation MountedOn)
{
	public AbilityMount Mount => new(Spec.Kind, MountedOn);

	public static void EnsureValidOnShip(IReadOnlyList<InstalledAbility> installed)
	{
		ArgumentNullException.ThrowIfNull(installed);
		if (installed.Count == 0)
			return;

		var mounts = new HashSet<AbilityMount>();
		foreach (var ability in installed)
		{
			if (!mounts.Add(ability.Mount))
				throw new ArgumentException(
					$"Ability '{ability.Spec.Kind}' is already installed on facet '{ability.MountedOn}'.",
					nameof(installed));

			if (!ability.Spec.CompatibleFacets.Contains(ability.MountedOn))
				throw new ArgumentException(
					$"Facet '{ability.MountedOn}' on '{ability.Spec.Kind}' is not allowed by that spec.",
					nameof(installed));
		}
	}
}
