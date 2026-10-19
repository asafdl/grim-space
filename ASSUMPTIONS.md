# Assumptions and Test Results

## Assumptions (about code design)

1. `SimulationExecutionAgent<BattleWorld, ActorRuntime>` is base class for both `UserExecutionAgent` and `AiExecutionAgent`
2. `UserExecutionAgent` has `PlanningChanged` event and `Undo()` method (NOT on base class)
3. `AiExecutionAgent` has `IsPlanning` property but no `Undo()` (returns false)
4. `BattleController._agent` field type → `SimulationExecutionAgent<BattleWorld, ActorRuntime>` (base type)
5. `PresentationFrameBuilder.BuildFrame` agent param → base type `SimulationExecutionAgent<BattleWorld, ActorRuntime>`
6. `_translator.Bind` accepts `IActionSink` (both agents implement it)
7. `PresentationFrameBuilder.BuildFrame` needs null guard for `agent.Sim` when agent is null (AI vs AI battles)

## Test results (what has been verified)

1. `fixed BattleOrchestrator.PlayerAgent return type to SimulationExecutionAgent<BattleWorld, ActorRuntime>`
2. `fixed BattleController._agent field type to SimulationExecutionAgent<BattleWorld, ActorRuntime>`
3. `fixed PresentationFrameBuilder.BuildFrame agent param to base type`
4. `fix BattleController.PlanningChanged/Undo calls via cast to UserExecutionAgent`
5. `before: _agent = activeAgent as UserExecutionAgent at line 129 references undefined activeAgent`
6. `after: _agent = _battle.PlayerAgent as UserExecutionAgent uses BattleOrchestrator.PlayerAgent (base type cast to UserExecutionAgent)`
7. `before: _agent = activeAgent as UserExecutionAgent where activeAgent was undefined`
8. `after: _agent = _battle.PlayerAgent as UserExecutionAgent uses base ExecutionAgent cast to UserExecutionAgent`
9. `Fixing BattleOrchestrator.PlayerAgent cast to as-UserExecutionAgent + null guards will prevent crash and render units`
10. `Fixing BattleTutorialAdapter null _battleAgent will prevent second crash`

## Assumptions from teaser recording session (2026-10-19)

1. Assumed the 63-second resolve wait was AI thinking time when it was actually 1-second think (DFS ~1s, rest was action sink wait)
2. Assumed ScriptedCombat scene would render without WorldEnvironment — showed only gray because no Environment/background was set
3. Assumed the overview camera + static mode would make units visible during replay — camera was still moving because BeginPlayback was called before EnableStaticMode
4. Assumed units would be visible after render layer fix — but camera was zoomed too close, only showing one ship
5. Assumed MaxSearchDepth=20 was needed for AI quality — reduced to 2 for teaser, but time budget approach is better
6. Assumed the SubViewport would render ScriptedCombat 3D content — it did, but only after adding WorldEnvironment
7. Assumed the camera at (0, 15, 30) would show all 4 ships — ships were at ±6,±3 positions, camera was too close
8. Assumed the tween animation would work in _Ready() — it didn't, had to switch to _Process-based animation
9. Assumed GetTree().CreateTimer() would fire in _Ready() — it didn't, had to use _Process-based timing
10. Assumed removing the 5x scale would make all 4 ships visible — only 1 ship visible, others too small
11. Assumed the camera at (0, 40, 0) top-down would show all ships — still only 1 visible
12. Assumed scaling ships by 3.0 would make them visible — they became too large and filled the screen
13. Assumed the laser beams (CylinderMesh 0.1 radius) would be visible — they may be too thin at camera distance
14. Assumed the explosion spheres (radius 0.5, scaled 5x) would be visible — they may not render correctly
15. Assumed the real battle system would show combat (lasers, explosions) during replay — replay only shows movement, no impact effects
16. Assumed the static camera mode would prevent camera movement — camera was still moving because _staticCameraMode flag wasn't set in BattleController
17. Assumed the overview camera position (dist=30) would show all 4 units — furthest unit is at distance 30, so it's at the edge
18. Assumed the RefreshPresentation() call in RunAiVsAiAsync would keep camera static — it calls _camera.SetFocus which overrides the overview position
19. Assumed the _Process method would be called on ScriptedCombat in the SubViewport — it was, but only after fixing the node structure
20. Assumed the battle scene's camera would not move during replay with static mode — it did move because FocusPlayer was not checked against _staticMode
