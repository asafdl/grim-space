using GrimSpace.Core.Ids;

namespace GrimSpace.Battle.Ids;

public sealed class UnitIdGenerator
{
	private readonly TypedIdGenerator _ids = new();

	public string NextInstanceSlug() => _ids.NextInstanceSlug();

	public static string Format(string typeSlug, string instanceSlug) =>
		TypedIdGenerator.Format(typeSlug, instanceSlug);
}
