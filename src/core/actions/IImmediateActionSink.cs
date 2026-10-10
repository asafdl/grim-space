namespace GrimSpace.Core.Actions;

public interface IImmediateActionSink : IActionSink
{
	bool CommitImmediately();
}
