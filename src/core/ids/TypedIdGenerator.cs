using UniqueNameGenerator;

namespace GrimSpace.Core.Ids;

public static class TypedIdGenerator
{
	private static readonly UniqueName InstanceNames = new UniqueName(Adjectives.WordList, [Animals.WordList, Colors.WordList])
		.Separator("-")
		.Format(Style.LowerCase); 

	private static readonly Lock InstanceNamesSync = new();

	public static string NextInstanceSlug()
	{
		lock (InstanceNamesSync)
			return InstanceNames.Generate();
	}

	public static string NextId(string typeSlug) => Format(typeSlug, NextInstanceSlug());

	public static string Format(string typeSlug, string instanceSlug) =>
		$"{typeSlug}-{instanceSlug}";
}
