using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Effects;

public readonly record struct SpawnFacts(
	string SourceId,
	string TargetId,
	EType EntityType);
