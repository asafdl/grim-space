namespace GrimSpace.Core.Actions;

public interface IEnqueueable
{
	EnqueuePolicy EnqueuePolicy => EnqueuePolicy.Add;
}
