using System.Diagnostics;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Domains.Flak;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Domains.Railgun;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation;

public enum PresentationPhase
{
	Planning,
	Resolving,
	Replaying,
	BattleOver,
}

/// <summary>
/// Presentation lifecycle owner: phases, async resolve job, and sole source of <see cref="PresentationFrame"/> updates.
/// Pure C# — no Godot nodes.
/// </summary>
public sealed class BattleDirector
{
	private readonly DirectorJobMap _jobs = new();

	public BattleDirector(BattleUi ui) => _ui = ui;

	private readonly BattleUi _ui;

	public PresentationPhase Phase { get; private set; }

	public bool AcceptsInput => Phase == PresentationPhase.Planning;

	public event Action<PresentationFrame>? FrameChanged;
	public event Action<TurnReplay, int>? ReplayRequested;

	public void Start() => EnterPlanningSync();

	public void EndTurn()
	{
		if (Phase != PresentationPhase.Planning)
		{
			PresentationDiagnostics.LogEndTurnIgnored(Phase);
			return;
		}

		_jobs.Cancel(DirectorJobs.MovePrep);

		if (!TryCommit(out var playerActions))
		{
			PresentationDiagnostics.LogCommitFailed();
			return;
		}

		var completedTurn = _ui.Battle.TurnNumber;

		SetPhase(PresentationPhase.Resolving, $"turn {completedTurn} committed ({playerActions.Count} actions)");
		EmitFrame();

		_jobs.Start(DirectorJobs.Resolve, version => ResolveAndContinue(playerActions, completedTurn, version));
	}

	public void NotifyReplayComplete()
	{
		if (Phase != PresentationPhase.Replaying)
		{
			PresentationDiagnostics.LogReplayNotifyIgnored(Phase);
			return;
		}

		if (_ui.Battle.IsBattleOver)
		{
			_jobs.Cancel(DirectorJobs.MovePrep);
			SetPhase(PresentationPhase.BattleOver, "battle over after replay");
			EmitFrame();
			return;
		}

		_ = FinishReplayAndEnterPlanningAsync();
	}

	public bool SetMode(EPlayerMode mode)
	{
		if (!AcceptsInput)
			return false;

		_ui.State.SetMode(mode);
		EmitFrame();
		return true;
	}

	public bool QueueMove(Coord endPosition)
	{
		if (!AcceptsInput)
		{
			PresentationDiagnostics.LogMoveRejected("not_planning", Phase, optionIndex: -1);
			return false;
		}

		var battle = _ui.Battle;
		var activeUnit = _ui.GetPlanningActor();
		if (activeUnit is null)
		{
			PresentationDiagnostics.LogMoveRejected("no_planning_actor", Phase, optionIndex: -1);
			return false;
		}

		if (!battle.CanAct(activeUnit))
		{
			PresentationDiagnostics.LogMoveRejected("cannot_act", Phase, optionIndex: -1);
			return false;
		}

		if (!_ui.TryQueueMove(endPosition))
		{
			var optionCount = _ui.MoveUi.GetMoveOptions(battle.Sim.Actions).Count;
			PresentationDiagnostics.LogMoveRejected(
				"queue_failed",
				Phase,
				optionIndex: -1,
				optionCount);
			return false;
		}

		EmitFrame();
		return true;
	}

	public bool Enqueue(IAction action)
	{
		if (!AcceptsInput)
			return false;

		var actor = _ui.GetPlanningActor();
		if (actor is null || !_ui.Battle.CanAct(actor) || !_ui.Battle.Sim.TryEnqueue(action))
			return false;

		EmitFrame();
		return true;
	}

	public bool Undo()
	{
		if (!AcceptsInput)
			return false;

		if (!_ui.Undo())
			return false;

		EmitFrame();
		return true;
	}

	public bool ApplyFlak(Coord cell)
	{
		if (!AcceptsInput)
			return false;

		if (!FlakUi.TryApply(_ui.Battle, _ui.State, cell))
			return false;

		EmitFrame();
		return true;
	}

	public bool ApplyRailgun(Coord cell)
	{
		if (!AcceptsInput)
			return false;

		if (!RailgunUi.TryApply(_ui.Battle, _ui.State, cell))
			return false;

		EmitFrame();
		return true;
	}

	public void SetFlakHover(Coord? cell)
	{
		if (!AcceptsInput)
			return;

		_ui.State.FlakHover = cell;
		EmitFrame();
	}

	public void SetRailgunHover(Coord? cell)
	{
		if (!AcceptsInput)
			return;

		_ui.State.RailgunHover = cell;
		EmitFrame();
	}

	private void EnterPlanningSync() => EnterPlanningCore(null, overlappedPrep: false, "initial start");

	private async Task FinishReplayAndEnterPlanningAsync()
	{
		var (prepared, isCurrent, elapsed) = await _jobs.Await<MoveUi>(DirectorJobs.MovePrep);
		_jobs.Clear(DirectorJobs.MovePrep);

		if (!isCurrent)
		{
			PresentationDiagnostics.LogPlanningHandoffAborted(
				"move_prep_not_current",
				Phase,
				prepared is not null);
			return;
		}

		if (Phase != PresentationPhase.Replaying)
		{
			PresentationDiagnostics.LogPlanningHandoffAborted($"unexpected_phase", Phase, prepared is not null);
			return;
		}

		EnterPlanningCore(prepared, elapsed.TotalMilliseconds < 1.0, "replay complete");
	}

	private void EnterPlanningCore(MoveUi? preparedMoveUi, bool overlappedPrep, string reason)
	{
		var turnNumber = _ui.Battle.TurnNumber;
		var totalTimer = Stopwatch.StartNew();

		SetPhase(PresentationPhase.Planning, reason);
		_ui.ResetMoveUi();

		var moveUiTimer = Stopwatch.StartNew();
		if (preparedMoveUi is not null)
			_ui.InstallMoveUi(preparedMoveUi);
		else
			_ = _ui.MoveUi;
		moveUiTimer.Stop();

		var previewTimer = Stopwatch.StartNew();
		EmitFrame();
		previewTimer.Stop();
		totalTimer.Stop();

		TurnPresentationTiming.LogPlanningReady(
			turnNumber,
			moveUiTimer.Elapsed.TotalMilliseconds,
			previewTimer.Elapsed.TotalMilliseconds,
			totalTimer.Elapsed.TotalMilliseconds,
			overlappedPrep);
	}

	private void StartMovePreparation()
	{
		var battle = _ui.Battle;
		var playerId = battle.PlayerId;
		_jobs.Start(
			DirectorJobs.MovePrep,
			_ => Task.Run(() => MoveUi.Build(battle.Engine.CreateSimulation(), playerId)));
	}

	private bool ShouldPrepareMoveUi() =>
		!_ui.Battle.IsBattleOver
		&& _ui.Battle.GetActiveUnit() is { State.Id: var id, State.IsAlive: true }
		&& id == _ui.Battle.PlayerId;

	private void SetPhase(PresentationPhase phase, string reason)
	{
		if (Phase == phase)
			return;

		var from = Phase;
		Phase = phase;
		PresentationDiagnostics.LogPhaseTransition(from, phase, reason);
	}

	private void EmitFrame() =>
		FrameChanged?.Invoke(_ui.BuildFrame(AcceptsInput));

	private bool TryCommit(out IReadOnlyList<IAction> playerActions)
	{
		playerActions = [];

		if (_ui.Battle.IsBattleOver)
			return false;

		if (!_ui.Battle.Sim.TryCommit(out var actions, out _))
			return false;

		playerActions = HeadingDef.Instance.Streamline(actions, _ui.Battle.Sim.UndoGroups).ToList();
		return true;
	}

	private async Task ResolveAndContinue(
		IReadOnlyList<IAction> playerActions,
		int completedTurn,
		int version)
	{
		var resolveTimer = Stopwatch.StartNew();
		try
		{
			var replay = await _ui.Battle.ResolveTurnAsync(playerActions);
			resolveTimer.Stop();

			if (!_jobs.IsCurrent(DirectorJobs.Resolve, version))
			{
				PresentationDiagnostics.LogResolveAborted($"resolve_job_stale v{version}", Phase);
				return;
			}

			if (Phase != PresentationPhase.Resolving)
			{
				PresentationDiagnostics.LogResolveAborted($"unexpected_phase", Phase);
				return;
			}

			_ui.State.ResetAfterTurn();
			TurnPresentationTiming.LogResolveWait(completedTurn, resolveTimer.Elapsed.TotalMilliseconds);

			SetPhase(PresentationPhase.Replaying, $"turn {completedTurn} resolved");
			ReplayRequested?.Invoke(replay, completedTurn);

			if (ShouldPrepareMoveUi())
				StartMovePreparation();
			else
				PresentationDiagnostics.LogMovePrepSkipped(
					_ui.Battle.TurnNumber,
					DescribeMovePrepSkipReason());
		}
		catch (Exception ex) when (_jobs.IsCurrent(DirectorJobs.Resolve, version) && Phase == PresentationPhase.Resolving)
		{
			PresentationDiagnostics.LogJobFailed(DirectorJobs.Resolve, ex);
			throw new InvalidOperationException("Turn resolve failed after commit.", ex);
		}
	}

	private string DescribeMovePrepSkipReason()
	{
		if (_ui.Battle.IsBattleOver)
			return "battle_over";

		var active = _ui.Battle.GetActiveUnit();
		if (active is null)
			return "no_active_unit";

		if (!active.State.IsAlive)
			return "active_unit_dead";

		if (active.State.Id != _ui.Battle.PlayerId)
			return $"active_unit_is_{active.State.Id}";

		return "unknown";
	}
}
