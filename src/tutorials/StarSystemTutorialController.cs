using Godot;
using GrimSpace.Education;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Objectives;

namespace GrimSpace.Tutorials;

public sealed class StarSystemTutorialController : IDisposable
{
	private readonly StarSystemOrchestrator _orchestrator;
	private readonly TutorialProgress _progress;
	private readonly TutorialRunner _runner;
	private readonly Func<bool> _canStart;
	private bool _resumeOnCompletion;
	private readonly HashSet<string> _reportedStartFailures = new(StringComparer.Ordinal);

	public StarSystemTutorialController(
		StarSystemOrchestrator orchestrator,
		TutorialProgress progress,
		ITutorialDialog dialog,
		IWorldFocus worldFocus,
		IWorldIndicator worldIndicator,
		Func<bool> canStart)
	{
		_orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
		_progress = progress ?? throw new ArgumentNullException(nameof(progress));
		_runner = new TutorialRunner(progress, dialog, worldFocus, worldIndicator);
		_canStart = canStart ?? throw new ArgumentNullException(nameof(canStart));
		_runner.Started += OnStarted;
		_runner.Completed += OnCompleted;
		_orchestrator.WorldUpdated += Sync;
	}

	public bool IsActive => _runner.ActiveFlow is not null;

	public void Sync()
	{
		if (IsActive || !_canStart())
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

	private void OnStarted(TutorialFlow _)
	{
		_resumeOnCompletion = _orchestrator.IsRunning;
		_orchestrator.SetStepped();
	}

	private void OnCompleted(TutorialFlow _)
	{
		if (_resumeOnCompletion)
			_orchestrator.SetRunning();
		_resumeOnCompletion = false;
	}

	public void Dispose()
	{
		_orchestrator.WorldUpdated -= Sync;
		_runner.Started -= OnStarted;
		_runner.Completed -= OnCompleted;
		_runner.Dispose();

		if (_resumeOnCompletion)
			_orchestrator.SetRunning();
		_resumeOnCompletion = false;
	}
}
