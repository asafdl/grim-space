using Godot;
using GrimSpace.Battle;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Education;

namespace GrimSpace.Tutorials;

public sealed class BattleTutorialAdapter : IDisposable
{
	private readonly TutorialController _controller;
	private readonly BattleOrchestrator _battle;
	private readonly UserExecutionAgent _battleAgent;
	private readonly PosedUnitGhostView _ghostView;
	private readonly Action _refocusCamera;
	private TutorialPresentationBinding? _presentation;
	private BattleTutorialObjective? _objective;
	private PosedUnitGhostSpec? _ghostSpec;
	private bool _ghostSuppressed;

	public BattleTutorialAdapter(
		TutorialController controller,
		BattleOrchestrator battle,
		PosedUnitGhostView battleGhost,
		Action refocusCamera)
	{
		_controller = controller;
		_battle = battle;
		_battleAgent = battle.PlayerAgent;
		_ghostView = battleGhost;
		_refocusCamera = refocusCamera;
		_battleAgent.PlanningChanged += OnBattlePlanningChanged;
		_controller.AssistanceRequested += OnAssistanceRequested;
		_controller.StepPresented += OnStepPresented;
	}

	private bool IsActive => _controller.ActiveFlow?.Id == FirstBattleTutorial.Id;

	private bool TargetDestroyed => _objective is { } objective
		&& (!UnitRegistry.For(_battle.Engine.World).TryGet(objective.TargetId, out var target)
			|| !target.State.IsAlive);

	public bool AllowsEndTurn =>
		!IsActive
		|| _controller.ActiveStep?.TargetId == FirstBattleTutorial.GoodLuckPilot
		|| (_controller.ActiveStep?.TargetId == FirstBattleTutorial.ExecuteTargetId
			&& _objective is { } objective
			&& BattleTutorialObjectives.HasQueuedKill(_battleAgent.Sim, _battle.PlayerId, objective.TargetId));

	public bool ResetsPlanOnUndo => IsActive
		&& _controller.ActiveStep?.TargetId == FirstBattleTutorial.UndoTargetId;

	public CommandHighlights Highlights => !IsActive ? default : _controller.ActiveStep?.TargetId switch
	{
		FirstBattleTutorial.ShieldShotTargetId => new(Ability: EPlayerMode.LightningCannon),
		FirstBattleTutorial.UndoTargetId => new(Undo: true),
		FirstBattleTutorial.ExecuteTargetId => new(Execute: AllowsEndTurn),
		_ => default,
	};

	public void Attach(ITutorialDialog dialog, IWorldFocus worldFocus, IWorldIndicator worldIndicator)
	{
		_presentation = new TutorialPresentationBinding(
			_controller,
			dialog,
			new WorldLinkNavigator(worldFocus, worldIndicator));
		_controller.ReconcileFromWorldState(cancelBattleFlowWhenOffBattlefield: false);

		if (_controller.State.IsFlowCompleted(FirstBattleTutorial.Id))
			return;

		if (IsActive)
		{
			if (_controller.ActiveStep?.TargetId == FirstBattleTutorial.GoodLuckPilot)
				_presentation.Attach();
			else if (ResolveObjective())
			{
				_presentation.Attach();
				AdvanceBattleStepIfReady();
				if (TargetDestroyed)
					NotifyBattlePhaseChanged(_battle.Phase);
			}
		}
		else if (!_controller.IsActive && ResolveObjective()
			&& _controller.TryStartFlow(FirstBattleTutorial.Create()))
			_presentation.Attach();
	}

	private bool ResolveObjective()
	{
		var result = BattleTutorialObjectives.ResolveShieldApproach(_battle);
		if (result is BattleTutorialObjectiveResult.Unreachable unreachable)
		{
			GD.PushWarning($"First battle tutorial setup failed: {unreachable.Reason}");
			if (IsActive)
				_controller.CancelActive();
			return false;
		}

		_objective = ((BattleTutorialObjectiveResult.Resolved)result).Objective;
		return true;
	}

	public void Detach()
	{
		_controller.ReconcileFromWorldState(cancelBattleFlowWhenOffBattlefield: true);
		_presentation?.Dispose();
		_presentation = null;
		SetGhostSpec(null);
	}

	private void OnStepPresented(TutorialFlow flow, TutorialStep step, bool _)
	{
		if (flow.Id != FirstBattleTutorial.Id)
			return;

		if (step.TargetId == FirstBattleTutorial.ShieldApproachTargetId)
			_refocusCamera();

		var showGhost = step.TargetId is FirstBattleTutorial.ShieldApproachTargetId
			or FirstBattleTutorial.ShieldShotTargetId;
		if (!showGhost || _objective is not { } objective)
		{
			SetGhostSpec(null);
			return;
		}

		var player = _battle.Engine.World.StateOf(_battle.PlayerId);
		SetGhostSpec(new PosedUnitGhostSpec(
			player.Type,
			objective.Destination,
			objective.RequiredBasis.Forward,
			objective.RequiredBasis.Up,
			Colors.Cyan));
	}

	private void OnBattlePlanningChanged()
	{
		if (!IsActive || !_battleAgent.IsPlanning)
			return;

		if (_battleAgent.Sim.Actions.Count == 0)
			RestoreGhost();

		AdvanceBattleStepIfReady();
	}

	private void AdvanceBattleStepIfReady()
	{
		if (!IsActive || _objective is not { } objective)
			return;

		var sim = _battleAgent.Sim;
		var ready = _controller.ActiveStep?.TargetId switch
		{
			FirstBattleTutorial.ShieldApproachTargetId =>
				BattleTutorialObjectives.MatchesQueuedMovement(sim, _battle.PlayerId, sim.Actions, objective),
			FirstBattleTutorial.ShieldShotTargetId =>
				BattleTutorialObjectives.HasQueuedShieldShot(sim, _battle.PlayerId, objective),
			FirstBattleTutorial.UndoTargetId => sim.Actions.Count == 0,
			FirstBattleTutorial.FindKillTargetId =>
				BattleTutorialObjectives.HasQueuedKill(sim, _battle.PlayerId, objective.TargetId),
			_ => false,
		};
		if (ready)
		{
			_controller.ClearAssistance();
			_controller.AdvanceActive();
			AdvanceBattleStepIfReady();
			return;
		}

		var assistance = _controller.ActiveStep?.TargetId switch
		{
			FirstBattleTutorial.ShieldApproachTargetId when sim.Actions.Count > 0 =>
				TutorialCopy.MoveToMarkedGhostAssistance,
			FirstBattleTutorial.ShieldShotTargetId when sim.Actions.Count > 0
				&& !BattleTutorialObjectives.MatchesQueuedMovement(sim, _battle.PlayerId, sim.Actions, objective) =>
				TutorialCopy.QueueShieldShotAssistance,
			FirstBattleTutorial.ExecuteTargetId
				when !BattleTutorialObjectives.HasQueuedKill(sim, _battle.PlayerId, objective.TargetId) =>
				TutorialCopy.QueueKillShotAssistance,
			_ => null,
		};
		if (assistance is null)
			_controller.ClearAssistance();
		else
			_controller.ShowAssistance(new TutorialAssistanceContent(assistance, TutorialCopy.UndoAndRetryAssistance));
	}

	private void OnAssistanceRequested()
	{
		if (IsActive && !_battleAgent.Undo())
			GD.PushWarning("Tutorial could not undo the invalid battle plan.");
	}

	public void NotifyMoveSelectionStarted()
	{
		_ghostSuppressed = true;
		ApplyGhost();
	}

	public void NotifyMoveSelectionCanceled() => RestoreGhost();

	public void NotifyBattlePhaseChanged(EBattlePhase phase)
	{
		if (!IsActive || _controller.ActiveStep?.TargetId != FirstBattleTutorial.ExecuteTargetId)
			return;

		if (phase == EBattlePhase.Resolving)
			_controller.RepresentActiveStep(openDialog: false);
		else if (phase is EBattlePhase.PlayerTurn or EBattlePhase.BattleOver)
		{
			if (TargetDestroyed)
				_controller.AdvanceActive();
			else
			{
				GD.PushWarning("The tutorial target survived the predicted killing shot; ending the scripted guidance.");
				_controller.CancelActive();
				_presentation?.Detach();
				SetGhostSpec(null);
			}
		}
	}

	private void SetGhostSpec(PosedUnitGhostSpec? spec)
	{
		_ghostSpec = spec;
		_ghostSuppressed = false;
		ApplyGhost();
	}

	private void RestoreGhost()
	{
		_ghostSuppressed = false;
		ApplyGhost();
	}

	private void ApplyGhost() => _ghostView.Apply(_ghostSuppressed ? null : _ghostSpec);

	public void Dispose()
	{
		_battleAgent.PlanningChanged -= OnBattlePlanningChanged;
		_controller.AssistanceRequested -= OnAssistanceRequested;
		_controller.StepPresented -= OnStepPresented;
		Detach();
		if (GodotObject.IsInstanceValid(_ghostView))
			_ghostView.QueueFree();
	}
}
