using Godot;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Log;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contact;

namespace GrimSpace.Core;

public partial class RunSession : Node
{
	private const string MapScenePath = "res://scenes/map.tscn";

	private static RunSession? _instance;

	public static RunSession Instance =>
		_instance ?? throw new InvalidOperationException("RunSession autoload is not ready.");

	public State Run { get; private set; } = null!;

	public override void _EnterTree()
	{
		_instance = this;
		ConfigureLogging();
		GameSettings.ApplySavedVideoConfig();
	}

	public override void _ExitTree()
	{
		if (_instance == this)
			_instance = null;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.F10 })
			return;

		EnterMapDevMode();
		GetViewport().SetInputAsHandled();
	}

	private void EnterMapDevMode()
	{
		if (!IsRunReady())
			StartNewRun();

		GetTree().ChangeSceneToFile(MapScenePath);
	}

	private bool IsRunReady() =>
		Run is not null && Run.StarSystem is not null;

	private static void ConfigureLogging()
	{
		var godotLog = Path.Combine(OS.GetUserDataDir(), "logs", "godot.log");

		GameLog.Configure(GD.Print, GD.PrintErr);

		GameLog.Log("=== grim-space session started ===");
		GameLog.Log($"godot log: {godotLog}");
		GameLog.Log($"OS: {OS.GetName()} {OS.GetVersion()}");

		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
	}

	private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
	{
		var ex = args.ExceptionObject as Exception
			?? new Exception(args.ExceptionObject?.ToString() ?? "unknown unhandled exception");
		GameLog.LogException(ex, $"Unhandled exception (terminating={args.IsTerminating})");
	}

	private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
	{
		foreach (var ex in args.Exception.InnerExceptions)
			GameLog.LogException(ex, "Unobserved task exception");
		args.SetObserved();
	}

	public void StartNewRun()
	{
		Run?.StarSystem?.Dispose();
		Run = State.CreateDevDefault(Random.Shared.Next());
		Run.ActiveBattle = null;
	}

	public bool BeginEngagement(string playerId)
	{
		var starSystem = Run.StarSystem;
		if (!EngagementQueries.TryGetCommittedPlayerEngagement(starSystem.Map, playerId, out var committed))
			return false;

		var targetId = committed.ParticipantUnitIds.First(id => id != playerId);
		var targetProfile = starSystem.Map.StateOf(targetId).CombatProfile;
		var seed = targetProfile?.GenerationSeed ?? Random.Shared.Next();
		var encounter = EngagementBattleFactory.Create(Run.PlayerParty, seed);
		Run.ActiveBattle = new ActiveBattle
		{
			Encounter = encounter,
			InitiatorUnitId = committed.InitiatorUnitId,
			ParticipantUnitIds = committed.ParticipantUnitIds,
		};
		return true;
	}

	public void ResolveEngagement(BattleOutcome outcome)
	{
		if (Run.ActiveBattle is null)
			return;

		Run.StarSystem.ResolveEngagement(State.PlayerFleetUnitId, outcome);
		Run.ActiveBattle = null;
	}

	public void RegenerateMap(int? seed = null)
	{
		if (!IsRunReady())
		{
			StartNewRun();
			return;
		}

		var nextSeed = seed ?? Random.Shared.Next();
		Run.StarSystem.Dispose();
		Run.StarSystem = StarSystemOrchestrator.CreateDevSession(State.PlayerFleetUnitId, nextSeed);
	}
}
