using Godot;
using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Education;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tutorials;

public sealed class BattleTutorialAdapter : IDisposable
{
	private const int TutorialTurn1 = 1;
	private const int TutorialTurn2 = 2;

	private readonly TutorialController _controller;
	private readonly BattleOrchestrator _battle;
	private readonly UserExecutionAgent _battleAgent;
	private readonly TutorialGhostPresenter _ghostPresenter;
	private TutorialPresentationBinding? _presentation;
	private BattleTutorialObjective? _turn1Objective;
	private BattleTutorialObjective? _turn2Objective;
	private bool _turn2AwaitingPlayerTurn;

	public BattleTutorialAdapter(
		TutorialController controller,
		BattleOrchestrator battle,
		PosedUnitGhostView battleGhost)
	{
		_controller = controller ?? throw new ArgumentNullException(nameof(controller));
		_battle = battle ?? throw new ArgumentNullException(nameof(battle));
		_battleAgent = battle.PlayerAgent;
		_ghostPresenter = new TutorialGhostPresenter(battleGhost);
		_battleAgent.PlanningChanged += OnBattlePlanningChanged;
		_controller.AssistanceRequested += OnAssistanceRequested;
		_controller.StepPresented += OnStepPresented;
	}

	private void OnStepPresented(TutorialFlow flow, TutorialStep step, bool openDialog)
	{
		_ = openDialog;
		_ = flow;
		_ = step;
		UpdateGhostForActiveStep();
	}

	public bool AllowsEndTurn =>
		_controller.ActiveStep?.TargetId is FirstBattleTutorial.Turn1EndTargetId
			or FirstBattleTutorial.Turn2EndTargetId;

	public void Attach(ITutorialDialog dialog, IWorldFocus worldFocus, IWorldIndicator worldIndicator)
	{
		_presentation = new TutorialPresentationBinding(
			_controller,
			dialog,
			new WorldLinkNavigator(worldFocus, worldIndicator));
		_controller.ReconcileFromWorldState(cancelBattleFlowWhenOffBattlefield: false);

		if (_controller.Progress.IsCompleted(FirstBattleTutorial.Id))
			return;

		if (_controller.IsActive && _controller.ActiveFlow?.Id == FirstBattleTutorial.Id)
			_presentation.Attach();
		else
			TryStartBattleTutorial();
	}

	public void Detach()
	{
		_controller.ReconcileFromWorldState(cancelBattleFlowWhenOffBattlefield: true);
		_presentation?.Detach();
		_presentation?.Dispose();
		_presentation = null;
		_ghostPresenter.SetSpec(null);
	}

	private void TryStartBattleTutorial()
	{
		if (_controller.IsActive || _controller.Progress.IsCompleted(FirstBattleTutorial.Id))
			return;

		var player = _battleAgent.Sim.StateOf<ActorState>(_battle.PlayerId);
		var initialBasis = GridBasis.From(player.Fore, player.Dorsal, player.Starboard);

		var turn1Result = BattleTutorialObjectives.ResolveTurn1(_battle, initialBasis);
		if (turn1Result is BattleTutorialObjectiveResult.Unreachable unreachable)
		{
			GD.PushWarning($"First battle tutorial setup failed: {unreachable.Reason}");
			return;
		}

		_turn1Objective = ((BattleTutorialObjectiveResult.Resolved)turn1Result).Objective;
		var startResult = _controller.TryStartFlow(FirstBattleTutorial.Create());
		if (startResult is TutorialStartResult.Started)
			_presentation?.Attach();
	}

	private void OnBattlePlanningChanged()
	{
		if (_battleAgent.Sim.Actions.Count == 0
			&& _controller.ActiveStep?.TargetId is FirstBattleTutorial.Turn1MoveTargetId
				or FirstBattleTutorial.Turn2MoveTargetId)
			_ghostPresenter.Restore();

		AdvanceBattleStepIfReady();
	}

	private void AdvanceBattleStepIfReady()
	{
		if (_controller.ActiveFlow is not { Id: FirstBattleTutorial.Id })
			return;

		switch (_controller.ActiveStep?.TargetId)
		{
			case FirstBattleTutorial.Turn1MoveTargetId:
				AdvanceBattleMoveIfReady(_turn1Objective, TutorialCopy.MoveToMarkedGhostAssistance);
				break;
			case FirstBattleTutorial.Turn2MoveTargetId:
				AdvanceBattleMoveIfReady(_turn2Objective, TutorialCopy.MatchGhostPoseAssistance);
				break;
			case FirstBattleTutorial.Turn2TorpedoTargetId:
				AdvanceBattleTorpedoIfReady();
				break;
		}
	}

	private void AdvanceBattleMoveIfReady(BattleTutorialObjective? objective, string mismatchMessage)
	{
		if (objective is null)
			return;

		if (_battleAgent.Sim.Actions.Count == 0)
		{
			_controller.ClearAssistance();
			return;
		}

		if (QueuedMovementMatchesObjective(objective))
		{
			_controller.ClearAssistance();
			_controller.AdvanceActive();
			return;
		}

		_controller.ShowAssistance(new TutorialAssistanceContent(
			mismatchMessage,
			TutorialCopy.UndoAndRetryAssistance));
	}

	private void AdvanceBattleTorpedoIfReady()
	{
		if (_turn2Objective is null || !QueuedMovementMatchesObjective(_turn2Objective))
		{
			_controller.ShowAssistance(new TutorialAssistanceContent(
				TutorialCopy.MatchGhostPoseAssistance,
				TutorialCopy.UndoAndRetryAssistance));
			return;
		}

		if (!_battleAgent.Sim.Actions.Any(action =>
				action is TorpedoAction { MountedOn: ESpatialOrientation.Ventral }))
		{
			if (_battleAgent.Sim.Actions.Any(action => action is TorpedoAction))
			{
				_controller.ShowAssistance(new TutorialAssistanceContent(
					TutorialCopy.QueueVentralTorpedoAssistance,
					TutorialCopy.UndoAndRetryAssistance));
			}
			else
				_controller.ClearAssistance();

			return;
		}

		_controller.ClearAssistance();
		_controller.AdvanceActive();
	}

	private bool QueuedMovementMatchesObjective(BattleTutorialObjective objective)
	{
		var movementActions = MovementPrefix(_battleAgent.Sim.Actions);
		if (movementActions.Count == 0)
			return false;

		var checkpoints = MovePathIndex.ProjectCheckpoints(
			_battleAgent.Sim.ForkFromAnchor(),
			_battle.PlayerId,
			movementActions,
			includeStart: false);
		if (checkpoints.Count == 0)
			return false;

		var end = checkpoints[^1];
		return end.Position == objective.Destination && end.Basis == objective.RequiredBasis;
	}

	private void OnAssistanceRequested()
	{
		if (_controller.ActiveFlow is not { Id: FirstBattleTutorial.Id })
			return;

		switch (_controller.ActiveStep?.TargetId)
		{
			case FirstBattleTutorial.Turn1MoveTargetId:
			case FirstBattleTutorial.Turn2MoveTargetId:
			case FirstBattleTutorial.Turn2TorpedoTargetId:
				if (!_battleAgent.Undo())
					GD.PushWarning("Tutorial could not undo the invalid battle plan.");
				break;
		}
	}

	private static List<IAction> MovementPrefix(IReadOnlyList<IAction> actions)
	{
		var movement = new List<IAction>();
		foreach (var action in actions)
		{
			if (action is MoveStepAction or HeadingTurnAction or RollAction)
			{
				movement.Add(action);
				continue;
			}

			break;
		}

		return movement;
	}

	public void NotifyMoveSelectionStarted() => _ghostPresenter.Suppress();

	public void NotifyMoveSelectionCanceled() => _ghostPresenter.Restore();

	public void NotifyBattlePhaseChanged(EBattlePhase phase)
	{
		if (_controller.ActiveFlow is not { Id: FirstBattleTutorial.Id })
			return;

		switch (phase)
		{
			case EBattlePhase.Resolving
				when _controller.ActiveStep?.TargetId == FirstBattleTutorial.Turn1EndTargetId:
				_controller.AdvanceActiveSilently();
				break;
			case EBattlePhase.PlayerTurn:
				TryPresentTurn2();
				break;
		}
	}

	public void NotifyBattleTurnResolved(int completedTurn)
	{
		if (_controller.ActiveFlow is not { Id: FirstBattleTutorial.Id })
			return;

		switch (completedTurn)
		{
			case TutorialTurn1
				when _controller.ActiveStep?.TargetId == FirstBattleTutorial.Turn2MoveTargetId:
				_turn2AwaitingPlayerTurn = PrepareTurn2Objective();
				break;
			case TutorialTurn2
				when _controller.ActiveStep?.TargetId == FirstBattleTutorial.Turn2EndTargetId:
				_controller.AdvanceActive();
				break;
		}
	}

	private void TryPresentTurn2()
	{
		if (!_turn2AwaitingPlayerTurn
			|| _controller.ActiveStep?.TargetId != FirstBattleTutorial.Turn2MoveTargetId
			|| _turn2Objective is not { } objective)
			return;

		if (!BattleTutorialObjectives.IsReachable(_battle, objective))
		{
			AbortBattleTutorial("First battle tutorial turn 2 setup failed: Turn 2 objective is unreachable.");
			return;
		}

		_turn2AwaitingPlayerTurn = false;
		_controller.RepresentActiveStep();
	}

	private bool PrepareTurn2Objective()
	{
		var turn2Result = BattleTutorialObjectives.ResolveTurn2(_battle);
		if (turn2Result is BattleTutorialObjectiveResult.Unreachable unreachable)
		{
			AbortBattleTutorial($"First battle tutorial turn 2 setup failed: {unreachable.Reason}");
			return false;
		}

		_turn2Objective = ((BattleTutorialObjectiveResult.Resolved)turn2Result).Objective;
		UpdateGhostForActiveStep();
		return true;
	}

	private void AbortBattleTutorial(string reason)
	{
		GD.PushWarning(reason);
		_turn2AwaitingPlayerTurn = false;
		_ghostPresenter.SetSpec(null);
		_controller.CancelActive();
	}

	private void UpdateGhostForActiveStep()
	{
		var objective = _controller.ActiveStep?.TargetId switch
		{
			FirstBattleTutorial.Turn1MoveTargetId => _turn1Objective,
			FirstBattleTutorial.Turn2MoveTargetId => _turn2Objective,
			_ => null,
		};
		if (objective is null)
		{
			_ghostPresenter.SetSpec(null);
			return;
		}

		var player = _battle.Engine.World.StateOf(_battle.PlayerId);
		_ghostPresenter.SetSpec(new PosedUnitGhostSpec(
			player.Type,
			objective.Destination,
			objective.RequiredBasis.Forward,
			objective.RequiredBasis.Up,
			Colors.Cyan));
	}

	public void Dispose()
	{
		_battleAgent.PlanningChanged -= OnBattlePlanningChanged;
		_controller.AssistanceRequested -= OnAssistanceRequested;
		_controller.StepPresented -= OnStepPresented;
		_ghostPresenter.Dispose();
		Detach();
	}
}
