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
