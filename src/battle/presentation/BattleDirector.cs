using System.Diagnostics;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Presentation.Domains.Flak;
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
	private int _resolveJobVersion;
	private Task? _resolveJob;

	public BattleDirector(BattleUi ui) => _ui = ui;

	private readonly BattleUi _ui;

	public PresentationPhase Phase { get; private set; }

	public bool AcceptsInput => Phase == PresentationPhase.Planning;

	public event Action<PresentationFrame>? FrameChanged;
	public event Action<TurnReplay, int>? ReplayRequested;

	public void Start() => EnterPlanning();

	public void EndTurn()
	{
		if (Phase != PresentationPhase.Planning)
			return;

		if (!TryCommit(out var playerActions))
			return;

		var completedTurn = _ui.Battle.TurnNumber;
		var version = ++_resolveJobVersion;

		Phase = PresentationPhase.Resolving;
		EmitFrame();

		_resolveJob = ResolveAndContinue(playerActions, completedTurn, version);
	}

	public void NotifyReplayComplete()
	{
		if (Phase != PresentationPhase.Replaying)
			return;

		if (_ui.Battle.IsBattleOver)
		{
			Phase = PresentationPhase.BattleOver;
			EmitFrame();
			return;
		}

		EnterPlanning();
	}

	public bool SetMode(EPlayerMode mode)
	{
		if (!AcceptsInput)
			return false;

		_ui.State.SetMode(mode);
		EmitFrame();
		return true;
	}

	public bool QueueMove(int optionIndex)
	{
		if (!AcceptsInput)
			return false;

		var battle = _ui.Battle;
		var activeUnit = _ui.GetPlanningActor();
		var moveOptions = activeUnit is not null && battle.CanAct(activeUnit)
			? _ui.MoveUi.GetMoveOptions(battle.Sim.Actions)
			: [];

		if (!_ui.TryQueueMove(optionIndex, moveOptions))
			return false;

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

	private void EnterPlanning()
	{
		var turnNumber = _ui.Battle.TurnNumber;
		var totalTimer = Stopwatch.StartNew();

		Phase = PresentationPhase.Planning;
		_ui.ResetMoveUi();

		var moveUiTimer = Stopwatch.StartNew();
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
			totalTimer.Elapsed.TotalMilliseconds);
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

			if (version != _resolveJobVersion || Phase != PresentationPhase.Resolving)
				return;

			_ui.State.ResetAfterTurn();
			TurnPresentationTiming.LogResolveWait(completedTurn, resolveTimer.Elapsed.TotalMilliseconds);

			Phase = PresentationPhase.Replaying;
			ReplayRequested?.Invoke(replay, completedTurn);
		}
		catch (Exception ex) when (version == _resolveJobVersion && Phase == PresentationPhase.Resolving)
		{
			throw new InvalidOperationException("Turn resolve failed after commit.", ex);
		}
	}
}
