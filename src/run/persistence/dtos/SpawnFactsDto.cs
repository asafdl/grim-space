using GrimSpace.Units.Enums;

namespace GrimSpace.Run.Persistence;

internal sealed record SpawnFactsDto(
	string SourceId,
	string TargetId,
	EType EntityType,
	BattleUnitStateDto SpawnedState);
