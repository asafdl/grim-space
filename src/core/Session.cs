using Godot;
using GrimSpace.Run;

namespace GrimSpace.Core;

public partial class Session : Node
{
	private static Session? _instance;

	public static Session Instance =>
		_instance ?? throw new InvalidOperationException("Session autoload is not ready.");

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
		CurrentEncounter = Encounter.DevDefault();
	}
}
