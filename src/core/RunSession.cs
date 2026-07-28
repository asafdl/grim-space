using Godot;
using GrimSpace.Run;

namespace GrimSpace.Core;

public partial class RunSession : Node
{
	private static RunSession? _instance;

	public static RunSession Instance =>
		_instance ?? throw new InvalidOperationException("RunSession autoload is not ready.");

	public State Run { get; private set; } = null!;
	public Encounter CurrentEncounter { get; private set; } = null!;

	public override void _EnterTree() => _instance = this;

	public override void _ExitTree()
	{
		if (_instance == this)
			_instance = null;
	}

	public override void _Ready() => StartNewRun();

	public void StartNewRun()
	{
		Run = State.CreateDevDefault();
		CurrentEncounter = Encounter.DevDefault(Random.Shared.Next());
	}
}
