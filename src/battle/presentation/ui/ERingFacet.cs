namespace GrimSpace.Battle.Presentation.Ui;

/// <summary>Asymmetric classifiers for ring membership (not interchangeable grid scalars).</summary>
[Flags]
public enum ERingFacet
{
	None = 0,
	ThrustClass = 1 << 0,
	BodyWedge = 1 << 1,
	ApTier = 1 << 2,
	MomentumOutcome = 1 << 3,
	ManhattanReach = 1 << 4,
	SortByOptionCount = 1 << 5,
}

public static class RingFacetLabels
{
	public static ERingFacet Normalize(ERingFacet facets)
	{
		if (facets == ERingFacet.None)
			return ERingFacet.ApTier | ERingFacet.ThrustClass;

		return facets & ~ERingFacet.SortByOptionCount;
	}
}
