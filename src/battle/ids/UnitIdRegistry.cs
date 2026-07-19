using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Ids;

public sealed class UnitIdRegistry
{
	private readonly UnitIdGenerator _generator = new();
	private readonly HashSet<string> _used = new();

	public string NextUnitId(EType type) => NextId(UnitTypeSlug.For(type));

	public string NextNonUnitId(string kindSlug) => NextId(kindSlug);

	public void Register(string id) => _used.Add(id);

	private string NextId(string typeSlug)
	{
		for (var attempt = 0; attempt < 64; attempt++)
		{
			var id = UnitIdGenerator.Format(typeSlug, _generator.NextInstanceSlug());
			if (id is EntityIds.Board or EntityIds.System)
				continue;

			if (_used.Add(id))
				return id;
		}

		throw new InvalidOperationException($"Failed to generate unique id for type slug '{typeSlug}'.");
	}
}
