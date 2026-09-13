using GrimSpace.Core.Ids;
using GrimSpace.Units.Enums;

namespace GrimSpace.Units;

public sealed record FleetMember(string Id, EType Type)
{
	public static FleetMember Create(EType type) =>
		Create(type, TypedIdGenerator.NextInstanceSlug());

	public static FleetMember Create(EType type, string instanceSlug) =>
		new(TypedIdGenerator.Format(UnitTypeSlug.For(type), instanceSlug), type);
}
