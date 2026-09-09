using Godot;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Log;
using GrimSpace.Presentation.Dev;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contact;

namespace GrimSpace.Core;

public partial class RunSession : Node
{
	private const string BattleScenePath = "res://scenes/battle.tscn";

	private static RunSession? _instance;
	private DevMenuOverlay _devMenu = null!;

	public static RunSession Instance =>
		_instance ?? throw new InvalidOperationException("RunSession autoload is not ready.");

	public State Run { get; private set; } = null!;

	public override void _EnterTree()
	{
		_instance = this;
		ConfigureLogging();
		GameSettings.ApplySavedVideoConfig();
	}

	public override void _Ready()
	{
		_devMenu = new DevMenuOverlay();
		AddChild(_devMenu);
		_devMenu.StartBattleRequested += StartDevBattle;
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

		ToggleDevMenu();
		GetViewport().SetInputAsHandled();
	}

	private void ToggleDevMenu()
	{
		if (_devMenu.IsOpen)
			_devMenu.Close();
		else
			_devMenu.Open();
	}

	public void StartDevBattle()
	{
		if (!IsRunReady())
			StartNewRun();

		Run.ActiveBattle = null;
		_devMenu.Close();
		GetTree().ChangeSceneToFile(BattleScenePath);
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
