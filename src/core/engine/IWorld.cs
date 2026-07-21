namespace GrimSpace.Core.Engine;

public interface IWorld<TSelf> where TSelf : IWorld<TSelf>
{
	TSelf Fork();

	Timeline Timeline { get; }
}
