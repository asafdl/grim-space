using System.Diagnostics;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Domains.Flak;
using GrimSpace.Battle.Presentation.Domains.Railgun;
using GrimSpace.Battle.Presentation.Domains.Torpedo;
using GrimSpace.Battle.Presentation.Domains.Turn;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
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

	public bool AcceptsCommands => AcceptsInput && !_ui.IsInspecting;

	public event Action<PresentationFrame>? FrameChanged;
	public event Action<TurnReplay, int>? ReplayRequested;

	public void Start() => EnterPlanningSync();

	public void EndTurn()
	{
		if (Phase != PresentationPhase.Planning || !AcceptsCommands)
		{
			PresentationDiagnostics.LogEndTurnIgnored(Phase);
			return;
		}

		if (!TryCommit())
		{
			var sim = _ui.Battle.Sim;
			PresentationDiagnostics.LogCommitFailed(
				_ui.Battle.IsBattleOver,
				sim.InvariantStatus,
				_ui.Battle.TurnNumber,
				sim.Actions.Count);
			return;
		}

		var completedTurn = _ui.Battle.TurnNumber;

		SetPhase(PresentationPhase.Resolving, $"turn {completedTurn} committed");
		EmitFrame();

		_jobs.Start(DirectorJobs.Resolve, version => ResolveAndContinue(completedTurn, version));
	}

	public void Retire()
	{
		if (Phase is PresentationPhase.BattleOver)
			return;

		_jobs.Cancel(DirectorJobs.Resolve);
		_jobs.Cancel(DirectorJobs.MovePrep);
		_ui.Battle.Retire();
		SetPhase(PresentationPhase.BattleOver, "retired");
		EmitFrame();
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
			SetPhase(PresentationPhase.BattleOver, "battle over after replay");
			EmitFrame();
			return;
		}

		EnterPlanningCore("replay complete");
	}

	public bool FocusUnit(string unitId)
	{
		if (!AcceptsInput)
			return false;

		var previewWorld = TurnUi.GetPreviewWorld(_ui.Battle);
		if (!UnitRegistry.For(previewWorld).TryGet(unitId, out var unit) || !unit.State.IsAlive)
			return false;

		_ui.State.FocusUnit(unitId);
		EmitFrame();
		return true;
	}

	public bool ClearFocus()
	{
		if (!AcceptsInput)
			return false;

		_ui.State.ClearFocus();
		EmitFrame();
		return true;
	}

	public bool SetMode(EPlayerMode mode)
	{
		if (!AcceptsCommands)
			return false;

		_ui.State.SetMode(mode);
		EmitFrame();
		return true;
	}

	public bool QueueMove(Coord endPosition)
	{
		if (!AcceptsCommands)
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
			var pathCount = _ui.MoveUi.GetMovePaths(battle.Sim, battle.PlayerId, battle.Sim.Actions).Count;
			PresentationDiagnostics.LogMoveRejected(
				"queue_failed",
				Phase,
				optionIndex: -1,
				optionCount: pathCount);
			return false;
		}

		EmitFrame();
		return true;
	}

	public bool Enqueue(IAction action)
	{
		if (!AcceptsCommands)
			return false;

		var actor = _ui.GetPlanningActor();
		if (actor is null || !_ui.Battle.CanAct(actor) || !_ui.Battle.Sim.TryEnqueue(action))
			return false;

		OrientationStreamline.CompactQueue(_ui.Battle.Sim);
		EmitFrame();
		return true;
	}

	public bool Undo()
	{
		if (!AcceptsCommands)
			return false;

		if (!_ui.Undo())
			return false;

		EmitFrame();
		return true;
	}

	public bool ApplyFlak(Coord cell)
	{
		if (!AcceptsCommands)
			return false;

		if (!FlakUi.TryApply(_ui.Battle, _ui.State, cell))
			return false;

		EmitFrame();
		return true;
	}

	public bool ApplyRailgun(Coord cell)
	{
		if (!AcceptsCommands)
			return false;

		if (!RailgunUi.TryApply(_ui.Battle, _ui.State, cell))
			return false;

		EmitFrame();
		return true;
	}

	public bool ApplyTorpedo(Coord cell)
	{
		if (!AcceptsCommands)
			return false;

		if (!TorpedoUi.TryApply(_ui.Battle, _ui.State, cell))
			return false;

		EmitFrame();
		return true;
	}

	public void SetFlakHover(Coord? cell)
	{
		if (!AcceptsCommands)
			return;

		_ui.State.FlakHover = cell;
		EmitFrame();
	}

	public void SetRailgunHover(Coord? cell)
	{
		if (!AcceptsCommands)
			return;

		_ui.State.RailgunHover = cell;
		EmitFrame();
	}

	public void SetTorpedoHover(Coord? cell)
	{
		if (!AcceptsCommands)
			return;

		if (_ui.State.TorpedoHover == cell)
			return;

		_ui.State.TorpedoHover = cell;
		EmitFrame();
	}

	private void EnterPlanningSync() => EnterPlanningCore("initial start");

	private void EnterPlanningCore(string reason)
	{
		var turnNumber = _ui.Battle.TurnNumber;
		var totalTimer = Stopwatch.StartNew();

		SetPhase(PresentationPhase.Planning, reason);
		_ui.ResetMoveUi();

		var previewTimer = Stopwatch.StartNew();
		EmitFrame();
		previewTimer.Stop();
		totalTimer.Stop();

		TurnPresentationTiming.LogPlanningReady(
			turnNumber,
			0,
			previewTimer.Elapsed.TotalMilliseconds,
			totalTimer.Elapsed.TotalMilliseconds,
			overlappedPrep: false);
	}

	private void SetPhase(PresentationPhase phase, string reason)
	{
		if (Phase == phase)
			return;

		var from = Phase;
		Phase = phase;
		PresentationDiagnostics.LogPhaseTransition(from, phase, reason);
	}

	private void EmitFrame() =>
		FrameChanged?.Invoke(_ui.BuildFrame(AcceptsCommands));

	private bool TryCommit()
	{
		if (_ui.Battle.IsBattleOver)
			return false;

		return _ui.Battle.Sim.TryCommit(out _, out _);
	}

	private async Task ResolveAndContinue(int completedTurn, int version)
	{
		var resolveTimer = Stopwatch.StartNew();
		try
		{
			var replay = await _ui.Battle.ResolveTurnAsync();
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
			_ui.AppendTurn(completedTurn, replay.History);
			TurnPresentationTiming.LogResolveWait(completedTurn, resolveTimer.Elapsed.TotalMilliseconds);

			SetPhase(PresentationPhase.Replaying, $"turn {completedTurn} resolved");
			EmitFrame();
			ReplayRequested?.Invoke(replay, completedTurn);
		}
		catch (Exception ex) when (_jobs.IsCurrent(DirectorJobs.Resolve, version) && Phase == PresentationPhase.Resolving)
		{
			PresentationDiagnostics.LogJobFailed(DirectorJobs.Resolve, ex);
			throw new InvalidOperationException("Turn resolve failed after commit.", ex);
		}
	}
}
