using Godot;
using GrimSpace.Education;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Objectives;

namespace GrimSpace.Tutorials;

public sealed class TutorialController : IDisposable
{
	private readonly StarSystemOrchestrator _orchestrator;
	private readonly TutorialProgress _progress;
	private readonly TutorialRunner _runner;
	private readonly HashSet<string> _reportedStartFailures = new(StringComparer.Ordinal);
	private readonly IDisposable _narrativeSubscription;
	private IDisposable? _activeActionSubscription;

	public TutorialController(
		StarSystemOrchestrator orchestrator,
		TutorialProgress progress,
		ITutorialDialog dialog,
		IWorldFocus worldFocus,
		IWorldIndicator worldIndicator)
	{
		_orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
		_progress = progress ?? throw new ArgumentNullException(nameof(progress));
		_runner = new TutorialRunner(progress, dialog, worldFocus, worldIndicator);
		_runner.Started += OnStarted;
		_runner.Completed += OnCompleted;
		_narrativeSubscription = _orchestrator.Subscribe<CompleteNarrativeAction>(_ => Sync());
	}

	public bool IsActive => _runner.ActiveFlow is not null;

	public void Sync()
	{
		if (IsActive)
			return;

		var flow = NextFlow();
		if (flow is null)
			return;

		var result = _runner.Start(flow);
		if (result is TutorialStartResult.Started)
			return;

		if (_reportedStartFailures.Add(flow.Id))
		{
			GD.PushWarning(
				$"Tutorial '{flow.Id}' could not start: {result.GetType().Name}.");
		}
	}

	private TutorialFlow? NextFlow()
	{
		if (!_progress.IsCompleted(FirstContractTutorial.Id)
			&& _orchestrator.Map.StoryObjectives.Active.Any(
				objective => objective.Id == StoryObjective.FirstContract.Id))
		{
			return FirstContractTutorial.Create(_orchestrator.Map);
		}

		if (_progress.IsCompleted(FirstContractTutorial.Id)
			&& !_progress.IsCompleted(FirstPirateTutorial.Id))
		{
			return FirstPirateTutorial.CreateForActiveContract(_orchestrator.Map);
		}

		return null;
	}

	private void OnStarted(TutorialFlow flow)
	{
		_activeActionSubscription?.Dispose();
		_activeActionSubscription = flow.Id switch
		{
			FirstContractTutorial.Id => _orchestrator.Subscribe<MoveAction>(OnMove),
			FirstPirateTutorial.Id => _orchestrator.Subscribe<HuntUnitAction>(OnHunt),
			_ => null,
		};
	}

	private void OnCompleted(TutorialFlow _)
	{
		_activeActionSubscription?.Dispose();
		_activeActionSubscription = null;
	}

	private void OnMove(MoveAction move)
	{
		if (_runner.ActiveFlow is not { Id: FirstContractTutorial.Id } flow
			|| move.ActorId != _orchestrator.PlayerId
			|| move.UnitId != _orchestrator.PlayerId
			|| !_orchestrator.Map.DocksByPoiId.TryGetValue(flow.WorldObjectId, out var dock)
			|| move.Destination != dock.Position)
			return;

		_runner.CompleteActive();
	}

	private void OnHunt(HuntUnitAction hunt)
	{
		if (_runner.ActiveFlow is not { Id: FirstPirateTutorial.Id } flow
			|| hunt.ActorId != _orchestrator.PlayerId
			|| hunt.TargetUnitId != flow.WorldObjectId)
			return;

		_runner.CompleteActive();
	}

	public void Dispose()
	{
		_narrativeSubscription.Dispose();
		_runner.Started -= OnStarted;
		_runner.Completed -= OnCompleted;
		_activeActionSubscription?.Dispose();
		_activeActionSubscription = null;
		_runner.Dispose();
	}
}
