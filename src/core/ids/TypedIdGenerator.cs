using UniqueNameGenerator;

namespace GrimSpace.Core.Ids;

public sealed class TypedIdGenerator
{
	private readonly UniqueName _instanceNames;

	public TypedIdGenerator()
	{
		_instanceNames = new UniqueName(Adjectives.WordList, Animals.WordList)
			.Separator("-")
			.Format(Style.LowerCase);
	}

	public string NextInstanceSlug() => _instanceNames.Generate();

	public string NextId(string typeSlug) => Format(typeSlug, NextInstanceSlug());

	public static string Format(string typeSlug, string instanceSlug) =>
		$"{typeSlug}-{instanceSlug}";
}
