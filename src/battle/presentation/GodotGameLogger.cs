using Godot;
using GrimSpace.Core;

namespace GrimSpace.Battle.Presentation;

public sealed class GodotGameLogger : IGameLogger
{
	public void Log(string message) => GD.Print(message);
}
