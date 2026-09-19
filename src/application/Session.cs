using System.Threading.Tasks;
using Godot;
using GrimSpace.Battle;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Log;
using GrimSpace.Presentation.Dev;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contact;

namespace GrimSpace.Application;

public partial class Session : Node
{
	private const string BattleScenePath = "res://scenes/battle.tscn";
	private const string MapScenePath = "res://scenes/map.tscn";

	private static Session? _instance;
	private DevMenuOverlay _devMenu = null!;
	private bool _beginningMapFromMenu;
	private bool _mapScenePreloadRequested;
	private PackedScene? _preloadedMapScene;
	private Task<State>? _preparedRunTask;
	private IDisposable? _battleOutcomeSubscription;

	public static Session Instance =>
		_instance ?? throw new InvalidOperationException("Session autoload is not ready.");

	public State Run { get; private set; } = null!;

	public DevMenuOverlay DevMenu => _devMenu;
	public RunTransitionInbox TransitionInbox { get; } = new();

	public override void _EnterTree()
	{
		_instance = this;
		ConfigureLogging();
		GameSettings.ApplySavedVideoConfig();
		GameSettings.ApplySavedAudioConfig();
	}

	public override void _Ready()
	{
		var devMenuLayer = new CanvasLayer { Layer = 20 };
		AddChild(devMenuLayer);
		_devMenu = new DevMenuOverlay();
		devMenuLayer.AddChild(_devMenu);
		_devMenu.StartBattleRequested += StartDevBattle;
	}

	public override void _ExitTree()
	{
		TransitionInbox.Dispose();
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

	public void PrepareFirstScene()
	{
		BeginMapScenePreload();
		BeginPreparedRun();
	}

	public async Task BeginMapFromMenuAsync()
	{
		if (_beginningMapFromMenu)
			return;

		_beginningMapFromMenu = true;
		try
		{
			PrepareFirstScene();
			await WaitForPreparedRunAsync();
			await WaitForMapScenePreloadAsync();

			if (!TryAdoptPreparedRun())
				StartNewRun();

			ChangeToMapScene();
		}
		finally
		{
			_beginningMapFromMenu = false;
		}
	}

	public void StartNewRun()
	{
		Run?.StarSystem?.Dispose();
		Run = State.CreateNewRun(Random.Shared.Next());
		Run.ActiveBattle = null;
		TransitionInbox.Bind(Run.StarSystem);
	}

	private void BeginMapScenePreload()
	{
		if (_preloadedMapScene is not null)
			return;

		var status = ResourceLoader.LoadThreadedGetStatus(MapScenePath);
		if (status == ResourceLoader.ThreadLoadStatus.Loaded)
		{
			_preloadedMapScene = ResourceLoader.LoadThreadedGet(MapScenePath) as PackedScene;
			return;
		}

		if (status == ResourceLoader.ThreadLoadStatus.InvalidResource && !_mapScenePreloadRequested)
		{
			ResourceLoader.LoadThreadedRequest(MapScenePath);
			_mapScenePreloadRequested = true;
		}
	}

	private void BeginPreparedRun()
	{
		if (_preparedRunTask is { IsCompleted: false })
			return;

		if (_preparedRunTask is { IsCompleted: true, IsFaulted: false })
			return;

		_preparedRunTask = Task.Run(() => State.CreateNewRun(Random.Shared.Next()));
	}

	private bool TryAdoptPreparedRun()
	{
		var task = _preparedRunTask;
		if (task is null || !task.IsCompleted)
			return false;

		_preparedRunTask = null;
		if (task.IsFaulted)
		{
			GameLog.LogException(
				task.Exception!.GetBaseException(),
				"Prepared run generation failed; falling back to synchronous generation.");
			return false;
		}

		Run?.StarSystem?.Dispose();
		Run = task.Result;
		Run.ActiveBattle = null;
		TransitionInbox.Bind(Run.StarSystem);
		return true;
	}

	private async Task WaitForPreparedRunAsync()
	{
		BeginPreparedRun();
		var task = _preparedRunTask;
		if (task is null)
			return;

		while (!task.IsCompleted)
			await ToSignal(GetTree().CreateTimer(0), SceneTreeTimer.SignalName.Timeout);
	}

	private async Task WaitForMapScenePreloadAsync()
	{
		BeginMapScenePreload();
		while (_preloadedMapScene is null)
		{
			var status = ResourceLoader.LoadThreadedGetStatus(MapScenePath);
			switch (status)
			{
				case ResourceLoader.ThreadLoadStatus.Loaded:
					_preloadedMapScene = ResourceLoader.LoadThreadedGet(MapScenePath) as PackedScene;
					_mapScenePreloadRequested = false;
					return;
				case ResourceLoader.ThreadLoadStatus.Failed:
					_mapScenePreloadRequested = false;
					return;
				case ResourceLoader.ThreadLoadStatus.InvalidResource when !_mapScenePreloadRequested:
					ResourceLoader.LoadThreadedRequest(MapScenePath);
					_mapScenePreloadRequested = true;
					break;
			}

			await ToSignal(GetTree().CreateTimer(0), SceneTreeTimer.SignalName.Timeout);
		}
	}

	private void ChangeToMapScene()
	{
		var scene = TakePreloadedMapScene();
		if (scene is not null)
			GetTree().ChangeSceneToPacked(scene);
		else
			GetTree().ChangeSceneToFile(MapScenePath);
	}

	private PackedScene? TakePreloadedMapScene()
	{
		if (_preloadedMapScene is not null)
		{
			var scene = _preloadedMapScene;
			_preloadedMapScene = null;
			_mapScenePreloadRequested = false;
			return scene;
		}

		if (!_mapScenePreloadRequested)
			return null;

		var status = ResourceLoader.LoadThreadedGetStatus(MapScenePath);
		if (status != ResourceLoader.ThreadLoadStatus.Loaded)
			return null;

		_mapScenePreloadRequested = false;
		return ResourceLoader.LoadThreadedGet(MapScenePath) as PackedScene;
	}

	public BattleOrchestrator CreateBattleOrchestrator(BattleEncounter encounter)
	{
		var orchestrator = BattleOrchestrator.FromEncounter(encounter);
		ReleaseBattleOutcomeSubscription();
		if (Run.ActiveBattle is not null)
			_battleOutcomeSubscription = orchestrator.Subscribe<Record<BattleOutcome>>(
				Run.OnCommittedBattleOutcome);
		return orchestrator;
	}

	public void ReleaseBattleOutcomeSubscription()
	{
		_battleOutcomeSubscription?.Dispose();
		_battleOutcomeSubscription = null;
	}

	public bool BeginEngagement(string playerId)
	{
		var starSystem = Run.StarSystem;
		if (!EngagementQueries.TryGetCommittedPlayerEngagement(starSystem.Map, playerId, out var committed))
			return false;

		var fleets = committed.ParticipantUnitIds
			.Select(id => starSystem.Map.FleetRegistry.FleetOf(id))
			.ToArray();
		var seed = Random.Shared.Next();
		var encounter = EngagementBattleFactory.Create(fleets, seed, committed.EngagementId);
		Run.ActiveBattle = encounter;

		return true;
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
		Run.StarSystem = StarSystemOrchestrator.CreateSession(
			State.PlayerFleetUnitId,
			Run.PlayerParty.Members,
			nextSeed);
		TransitionInbox.Bind(Run.StarSystem);
	}
}
