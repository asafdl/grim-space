---
parentPlan: ../full.plan.md
targetFile: src/battle/presentation/ui/BattlePresenter.cs
status: superseded
supersedes:
  - subplans/src-battle-presentation-BattleFrameBuilder.cs.plan.md
  - subplans/src-battle-presentation-Interaction-InteractionState.cs.plan.md
---

# Subplan: `BattlePresenter.cs` *(retired — parent plan name)*

This worktree has **no** `BattlePresenter.cs`. UI refactor split responsibilities:

| Old plan role | Actual files |
|---------------|----------------|
| **`BuildFrame`** + ring table on snapshot | [`BattleFrameBuilder.cs`](../../../../../../src/battle/presentation/BattleFrameBuilder.cs), [`PresentationFrame`](../../../../../../src/battle/presentation/Ui/PresentationFrame.cs) — [subplan](src-battle-presentation-BattleFrameBuilder.cs.plan.md) |
| Hover + **`TryQueueMove`** | [`BattleUi.cs`](../../../../../../src/battle/presentation/BattleUi.cs), [`InteractionState.cs`](../../../../../../src/battle/presentation/Interaction/InteractionState.cs) — [subplan](src-battle-presentation-Interaction-InteractionState.cs.plan.md) |

Use those subplans for implementation; do not create **`BattlePresenter.cs`** for this feature.
