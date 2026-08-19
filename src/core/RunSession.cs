using Godot;
using GrimSpace.Battle.Encounter;
using GrimSpace.Core.Log;
using GrimSpace.Run;

namespace GrimSpace.Core;

public partial class RunSession : Node
{
	private static RunSession? _instance;

	public static RunSession Instance =>
		_instance ?? throw new InvalidOperationException("RunSession autoload is not ready.");

	public State Run { get; private set; } = null!;
	public BattleEncounter CurrentEncounter { get; private set; } = null!;

	public override void _EnterTree()
	{
		_instance = this;
		ConfigureLogging();
	}

	public override void _ExitTree()
	{
		if (_instance == this)
			_instance = null;
	}

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
		Run = State.CreateDevDefault();
		CurrentEncounter = BattleEncounter.DevDefault(Random.Shared.Next());
	}
}
