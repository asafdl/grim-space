using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Poi;

public sealed class Facility
{
	public string Id { get; }
	public string DisplayName { get; }
	public EPresentationAnchor PresentationAnchor { get; }
	public string ScenePath { get; }
	public IReadOnlyList<FacilityOperator> Operators { get; }

	public Facility(
		string id,
		string displayName,
		EPresentationAnchor presentationAnchor,
		string scenePath,
		IEnumerable<FacilityOperator> operators)
	{
		if (string.IsNullOrWhiteSpace(id))
			throw new ArgumentException("Facility id is required.", nameof(id));
		if (string.IsNullOrWhiteSpace(displayName))
			throw new ArgumentException("Facility display name is required.", nameof(displayName));
		if (string.IsNullOrWhiteSpace(scenePath))
			throw new ArgumentException("Facility scene path is required.", nameof(scenePath));

		var operatorList = operators.ToArray();
		ValidateOperators(operatorList);

		Id = id;
		DisplayName = displayName;
		PresentationAnchor = presentationAnchor;
		ScenePath = scenePath;
		Operators = operatorList;
	}

	public static string ScopedId(string poiId, string slug) => $"{poiId}-{slug}";

	private static void ValidateOperators(FacilityOperator[] operators)
	{
		var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var slots = new HashSet<string>(StringComparer.Ordinal);

		foreach (var operatorEntry in operators)
		{
			if (string.IsNullOrWhiteSpace(operatorEntry.Name))
				throw new ArgumentException("Facility operator name is required.");
			if (string.IsNullOrWhiteSpace(operatorEntry.SceneSlotId))
				throw new ArgumentException("Facility operator scene slot id is required.");

			if (!names.Add(operatorEntry.Name))
				throw new ArgumentException(
					$"Duplicate facility operator name '{operatorEntry.Name}' (names are unique case-insensitively).");

			if (!slots.Add(operatorEntry.SceneSlotId))
				throw new ArgumentException(
					$"Duplicate facility operator scene slot id '{operatorEntry.SceneSlotId}'.");

			if (operatorEntry.Role == EFacilityOperatorRole.Merchant)
			{
				if (operatorEntry.MerchantCatalog is null)
					throw new ArgumentException(
						$"Merchant operator '{operatorEntry.Name}' requires a merchant catalog.");
			}
			else if (operatorEntry.MerchantCatalog is not null)
			{
				throw new ArgumentException(
					$"Operator '{operatorEntry.Name}' with role '{operatorEntry.Role}' cannot carry a merchant catalog.");
			}
		}
	}
}
