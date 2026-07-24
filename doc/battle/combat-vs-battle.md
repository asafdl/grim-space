# Combat vs battle

No separate gameplay systems — terms overlap.

**Battle** — code and encounter lifecycle: `src/battle/`, `GrimSpace.Battle`, `BattleOrchestrator`, `BattleBoard`, `battle.tscn`, `IsBattleOver`. Covers planning, timeline, win/loss.

**Combat** — prose and tuning: README “combat prototype”; `CombatConfig` (weapon/grid constants); `CombatHints` (UI strings for weapon modes).

Battle = module + encounter; combat = mechanics/constants language inside that layer.

See also: [overview/high-level.md](../overview/high-level.md)
