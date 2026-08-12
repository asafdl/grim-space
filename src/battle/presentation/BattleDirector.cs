using System.Diagnostics;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Weapons;

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

	public BattleDirector(BattleUi ui, HumanExecutionAgent agent)
	{
		_ui = ui;
		_agent = agent;
		_agent.Changed += _ => EmitFrame();
	}

	private readonly BattleUi _ui;
	private readonly HumanExecutionAgent _agent;

	public PresentationPhase Phase { get; private set; }

	public bool AcceptsInput => Phase == PresentationPhase.Planning;

	public bool AcceptsCommands => AcceptsInput && !_ui.IsInspecting;

	public event Action<PresentationFrame>? FrameChanged;
	public event Action<TurnReplay, int>? ReplayRequested;

	public void Start() => EnterPlanningSync();

	/// <summary>Rebuild the current presentation frame from interaction state + agent snapshot.</summary>
	public void RefreshFrame() => EmitFrame();

	public void EndTurn()
	{
		if (Phase != PresentationPhase.Planning || !AcceptsCommands)
		{
			PresentationDiagnostics.LogEndTurnIgnored(Phase);
			return;
		}

		if (!_agent.Commit())
		{
			PresentationDiagnostics.LogCommitFailed(
				_ui.Battle.IsBattleOver,
				_agent.Sim.InvariantStatus,
				_ui.Battle.TurnNumber,
				_agent.Sim.Actions.Count);
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

		if (!_agent.Current.PreviewUnits.TryGetValue(unitId, out var unit) || !unit.IsAlive)
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

	public void ClearHovers()
	{
		_ui.State.ClearHovers();
		EmitFrame();
	}

	private void EnterPlanningSync() => EnterPlanningCore("initial start");

	public void SetFlakHoverMount(EFlakMount? mount)
	{
		if (!AcceptsCommands)
			return;

		if (_ui.State.FlakHoverMount == mount)
			return;

		_ui.State.FlakHoverMount = mount;
		EmitFrame();
	}

	public void SetRailgunHovered(bool hovered)
	{
		if (!AcceptsCommands)
			return;

		if (_ui.State.RailgunHovered == hovered)
			return;

		_ui.State.RailgunHovered = hovered;
		EmitFrame();
	}

	public void SetTorpedoHoverMount(ETorpedoMount? mount)
	{
		if (!AcceptsCommands)
			return;

		if (_ui.State.TorpedoHoverMount == mount)
			return;

		_ui.State.TorpedoHoverMount = mount;
		EmitFrame();
	}

	private void EnterPlanningCore(string reason)
	{
		var turnNumber = _ui.Battle.TurnNumber;
		var totalTimer = Stopwatch.StartNew();

		SetPhase(PresentationPhase.Planning, reason);

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
		FrameChanged?.Invoke(_ui.BuildFrame(CurrentSnapshot(), AcceptsCommands));

	private HumanTurnSnapshot CurrentSnapshot() =>
		_agent.BuildSnapshot(new HumanTurnViewInput(
			_ui.State.FocusId,
			_ui.State.FlakHoverMount,
			_ui.State.RailgunHovered,
			_ui.State.TorpedoHoverMount));

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
