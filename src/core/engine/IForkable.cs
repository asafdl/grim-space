namespace GrimSpace.Core.Engine;

public interface IForkable<TSelf> where TSelf : IForkable<TSelf>
{
	TSelf Fork();
}
