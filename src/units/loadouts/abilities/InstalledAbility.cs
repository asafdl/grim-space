using GrimSpace.Math.Grid;

namespace GrimSpace.Units.Loadouts.Abilities;

/// <summary>
/// One ability hardpoint on a ship: installed spec and active facets.
/// </summary>
public sealed record InstalledAbility(
	AbilitySpec Spec,
	IReadOnlyList<ESpatialOrientation> Facets)
{
	public static void EnsureValidOnShip(IReadOnlyList<InstalledAbility> installed)
	{
		ArgumentNullException.ThrowIfNull(installed);
		if (installed.Count == 0)
			return;

		var kinds = installed.Select(ability => ability.Spec.Kind).ToList();
		if (kinds.ToHashSet().Count != kinds.Count)
			throw new ArgumentException("Each installed ability must have a unique kind.", nameof(installed));

		foreach (var ability in installed)
		{
			foreach (var facet in ability.Facets)
			{
				if (!ability.Spec.CompatibleFacets.Contains(facet))
					throw new ArgumentException(
						$"Facet '{facet}' on '{ability.Spec.Kind}' is not allowed by that spec.",
						nameof(installed));
			}
		}
	}

	public AbilityActionContext ForAction() => new(Spec, Facets);

	public MountRuntimeCounters ForState() => Spec.CreateInitialRuntime();
}
