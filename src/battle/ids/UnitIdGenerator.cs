using UniqueNameGenerator;

namespace GrimSpace.Battle.Ids;

public sealed class UnitIdGenerator
{
	private readonly UniqueName _instanceNames;

	public UnitIdGenerator()
	{
		_instanceNames = new UniqueName(Adjectives.WordList, Animals.WordList)
			.Separator("-")
			.Format(Style.LowerCase);
	}

	public string NextInstanceSlug() => _instanceNames.Generate();

	public static string Format(string typeSlug, string instanceSlug) =>
		$"{typeSlug}-{instanceSlug}";
}
