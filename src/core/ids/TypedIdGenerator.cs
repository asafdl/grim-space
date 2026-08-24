using UniqueNameGenerator;

namespace GrimSpace.Core.Ids;

public static class TypedIdGenerator
{
	private static readonly UniqueName InstanceNames = new UniqueName(Adjectives.WordList, Animals.WordList)
		.Separator("-")
		.Format(Style.LowerCase);

	public static string NextInstanceSlug() => InstanceNames.Generate();

	public static string NextId(string typeSlug) => Format(typeSlug, NextInstanceSlug());

	public static string Format(string typeSlug, string instanceSlug) =>
		$"{typeSlug}-{instanceSlug}";
}
