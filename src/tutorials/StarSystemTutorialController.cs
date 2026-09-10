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
	private bool _resumeOnCompletion;
	private bool _startFailureReported;

	public StarSystemTutorialController(
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
		_orchestrator.WorldUpdated += Sync;
	}

	public bool IsActive => _runner.ActiveFlow is not null;

	public void Sync()
	{
		if (IsActive || _progress.IsCompleted(FirstContractTutorial.Id))
			return;

		if (_orchestrator.Map.StoryObjectives.Active.All(
			objective => objective.Id != StoryObjective.FirstContract.Id))
			return;

		var result = _runner.Start(FirstContractTutorial.Create(_orchestrator.Map));
		if (result is TutorialStartResult.Started)
			return;

		if (!_startFailureReported)
		{
			_startFailureReported = true;
			GD.PushWarning(
				$"Tutorial '{FirstContractTutorial.Id}' could not start: {result.GetType().Name}.");
		}
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
