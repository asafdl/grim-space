namespace GrimSpace.Battle.NonUnits;

public static class NonUnitTypeSlug
{
	public const string Asteroid = "asteroid";
	public const string Goop = "goop";

	public static string ParseTypeSlug(string id)
	{
		ArgumentException.ThrowIfNullOrEmpty(id);
		var separator = id.IndexOf('-');
		if (separator <= 0)
			throw new InvalidDataException($"Non-unit id '{id}' is missing a type slug prefix.");

		return id[..separator];
	}
}
