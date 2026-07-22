namespace GrimSpace.Core.Engine;

public interface IRuntimeContext<TSelf> where TSelf : IRuntimeContext<TSelf>
{
	void Reset();

	TSelf Fork();
}
