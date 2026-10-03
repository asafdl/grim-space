using GrimSpace.Battle;
using GrimSpace.Battle.Objectives;
using GrimSpace.World.StarSystem;

namespace GrimSpace.Run.Persistence;

public sealed record RunStateSnapshotDto(
	IReadOnlyList<string> PartyShipIds,
	IReadOnlyList<ShipPersistenceDto> Ships,
	IReadOnlyDictionary<string, string> Portraits,
	TutorialStateDto? Tutorial,
	StarMapSaveDto StarMap,
	bool ContractGenerationEnabled,
	ESimMode SimMode,
	BattleEncounterSaveDto? ActiveBattle,
	BattleOutcome? PendingBattleOutcome,
	IReadOnlyList<string> ResolvedBattleIds,
	IReadOnlyList<string> LaunchedEngagementIds,
	BattleWorldSaveDto? BattleWorld,
	IReadOnlyList<StarSystemRuntimeDto> StarSystemRuntimes);
