using GrimSpace.Battle;
using GrimSpace.Battle.Objectives;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Run.Persistence;

internal sealed record ResolveEngagementDto(
	string InitiatorId,
	BattleOutcome Outcome,
	IReadOnlyList<LootRoll> LootRolls,
	ResourceBundle LootTotal);
