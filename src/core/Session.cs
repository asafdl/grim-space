using Godot;
using GrimSpace.Domain.Run;

namespace GrimSpace.Core;

public partial class Session : Node
{
	private static Session? _instance;

	public static Session Instance =>
		_instance ?? throw new InvalidOperationException("Session autoload is not ready.");

	public Domain.Run.State Run { get; private set; } = null!;
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
		Run = Domain.Run.State.CreateDevDefault();
		CurrentEncounter = Encounter.DevDefault();
	}
}
