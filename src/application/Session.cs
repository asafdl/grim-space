using System.Text.Json;
using System.Threading.Tasks;
using GrimSpace.Battle.World;
using Godot;
using GrimSpace.Core.Log;
using GrimSpace.Education;
using GrimSpace.Presentation.Dev;
using GrimSpace.Run;
using GrimSpace.Run.Persistence;
using GrimSpace.World.StarSystem;

namespace GrimSpace.Application;

public partial class Session : Node
{
	public const double DefaultAutosaveIntervalSeconds = 60;

	private const string BattleScenePath = "res://scenes/battle.tscn";
	private const string MapScenePath = "res://scenes/map.tscn";

	private static Session? _instance;
	private AutosaveScheduler _autosaveScheduler = null!;
	private DevMenuOverlay _devMenu = null!;
	private Label _fpsLabel = null!;
	private bool _beginningMapFromMenu;
	private bool _mapScenePreloadRequested;
	private PackedScene? _preloadedMapScene;
	private Task<State>? _preparedRunTask;
	private TutorialDialogHost? _tutorialDialogHost;
	private readonly ISaveGameStorage _saveStorage;
	private readonly PersistenceRegistry _persistenceRegistry;
	private Func<BattleWorld?>? _autosaveBattleWorldProvider;
	private Func<SaveGateState>? _autosaveGateProvider;

	[Export(PropertyHint.Range, "5,3600,5")]
	public double AutosaveIntervalSeconds { get; set; } = DefaultAutosaveIntervalSeconds;

	public static Session Instance =>
		_instance ?? throw new InvalidOperationException("Session autoload is not ready.");

	public State Run { get; private set; } = null!;

	public Session()
		: this(new SaveGameStore(), PersistenceRegistry.CreateDefault())
	{
	}

	internal Session(
		ISaveGameStorage saveStorage,
		PersistenceRegistry persistenceRegistry)
	{
		_saveStorage = saveStorage
			?? throw new ArgumentNullException(nameof(saveStorage));
		_persistenceRegistry = persistenceRegistry
			?? throw new ArgumentNullException(nameof(persistenceRegistry));
	}

	public bool HasSaveGame => _saveStorage.Exists();

	public SaveStorageResult TrySaveGame(BattleWorld? battleWorld = null)
	{
		if (!IsRunReady())
			return SaveStorageResult.Failed;

		var activeScene = GetTree().CurrentScene?.SceneFilePath
			?? MapScenePath;
		var snapshot = Run.CaptureSnapshot(_persistenceRegistry, battleWorld);
		var document = SaveGameDocument.Create(
			GameVersion.Display,
			activeScene,
			snapshot,
			options: _persistenceRegistry.Options);
		return _saveStorage.TryWrite(document);
	}

	public SaveStorageResult TrySaveBeforeLeavingRun(BattleWorld? battleWorld = null)
	{
		if (_autosaveGateProvider is not null
			&& SaveLoadPolicy.CanSave(_autosaveGateProvider()) != SaveBlockReason.None)
			return SaveStorageResult.Failed;

		return TrySaveGame(battleWorld ?? _autosaveBattleWorldProvider?.Invoke());
	}

	public LoadResult TryLoadGame()
	{
		var storageResult = _saveStorage.TryRead(out var document);
		var result = SaveLoadResult.Classify(storageResult, document);
		if (result != LoadResult.Success)
			return result;

		State? restored = null;
		try
		{
			var snapshot = ReflectionJson.Read<RunStateSnapshotDto>(
				document!.Payload,
				_persistenceRegistry.Options);
			restored = State.FromSnapshot(snapshot, _persistenceRegistry);
			AdoptRun(restored);
			restored = null;
			NavigateToRunScene();
			return LoadResult.Success;
		}
		catch (JsonException)
		{
			return LoadResult.Corrupt;
		}
		catch (InvalidDataException)
		{
			return LoadResult.Corrupt;
		}
		catch (Exception ex)
		{
			GameLog.LogException(ex, "Failed to restore save game.");
			return LoadResult.Failed;
		}
		finally
		{
			restored?.Dispose();
		}
	}

	public DevMenuOverlay DevMenu => _devMenu;

	public TutorialDialogHost TutorialDialogHost =>
		_tutorialDialogHost ?? throw new InvalidOperationException("Session tutorial host is not ready.");

	public override void _EnterTree()
	{
		_instance = this;
		ConfigureLogging();
		GameSettings.ApplySavedVideoConfig();
		GameSettings.ApplySavedAudioConfig();
		GameSettings.ApplyMouseCameraPan(GameSettings.ReadMouseCameraPan());
		GameInputBindings.Apply(GameSettings.ReadKeyBindings());
	}

	public override void _Ready()
	{
		_autosaveScheduler = new AutosaveScheduler(AutosaveIntervalSeconds);
		var devMenuLayer = new CanvasLayer { Layer = 20 };
		AddChild(devMenuLayer);
		_devMenu = new DevMenuOverlay();
		devMenuLayer.AddChild(_devMenu);
		_devMenu.StartBattleRequested += StartDevBattle;

		var performanceLayer = new CanvasLayer { Layer = 30 };
		AddChild(performanceLayer);
		_fpsLabel = new Label
		{
			AnchorLeft = 1f,
			AnchorRight = 1f,
			OffsetLeft = -120f,
			OffsetTop = 16f,
			OffsetRight = -16f,
			OffsetBottom = 44f,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Right,
		};
		performanceLayer.AddChild(_fpsLabel);
		_fpsLabel.ProcessMode = Node.ProcessModeEnum.Always;

		_tutorialDialogHost = new TutorialDialogHost();
		AddChild(_tutorialDialogHost);
	}

	public override void _Process(double delta)
	{
		_fpsLabel.Text = $"FPS: {Engine.GetFramesPerSecond()}";
		if (_autosaveScheduler.Advance(delta)
			&& _autosaveGateProvider is not null
			&& SaveLoadPolicy.CanSave(_autosaveGateProvider()) == SaveBlockReason.None)
		{
			var result = TrySaveGame(_autosaveBattleWorldProvider?.Invoke());
			if (result != SaveStorageResult.Success)
				GameLog.Log($"Autosave failed: {result}");
		}
	}

	internal void RegisterAutosaveContext(
		Func<BattleWorld?> battleWorldProvider,
		Func<SaveGateState> gateProvider)
	{
		ArgumentNullException.ThrowIfNull(battleWorldProvider);
		ArgumentNullException.ThrowIfNull(gateProvider);
		_autosaveBattleWorldProvider = battleWorldProvider;
		_autosaveGateProvider = gateProvider;
		_autosaveScheduler?.Reset();
	}

	internal void ClearAutosaveContext()
	{
		_autosaveBattleWorldProvider = null;
		_autosaveGateProvider = null;
		_autosaveScheduler?.Reset();
	}

	public override void _ExitTree()
	{
		if (Run is not null)
		{
			Run.BattleReady -= OnBattleReady;
			Run.Dispose();
		}

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

	/// <summary>
	/// Drops a background-prepared run so the next prepare uses current settings (e.g. tutorial toggle).
	/// </summary>
	public void DiscardPreparedRun()
	{
		var task = _preparedRunTask;
		_preparedRunTask = null;
		if (task is { IsCompleted: true, IsFaulted: false })
			task.Result.Dispose();
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

			NavigateToRunScene();
		}
		finally
		{
			_beginningMapFromMenu = false;
		}
	}

	public void StartNewRun()
	{
		AdoptRun(State.CreateNewRun(Random.Shared.Next(), GameSettings.ReadShowTutorials()));
	}

	private void AdoptRun(State next)
	{
		if (Run is not null)
		{
			Run.BattleReady -= OnBattleReady;
			Run.Dispose();
		}

		Run = next;
		Run.BattleReady += OnBattleReady;
	}

	private void OnBattleReady()
	{
		Callable.From(() => GetTree().ChangeSceneToFile(BattleScenePath)).CallDeferred();
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

		_preparedRunTask = Task.Run(() =>
			State.CreateNewRun(Random.Shared.Next(), GameSettings.ReadShowTutorials()));
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

		AdoptRun(task.Result);
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

	private void NavigateToRunScene()
	{
		if (Run.ActiveBattle is not null)
		{
			GetTree().ChangeSceneToFile(BattleScenePath);
			return;
		}

		ChangeToMapScene();
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
}
