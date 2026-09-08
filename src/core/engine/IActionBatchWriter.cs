namespace GrimSpace.Core.Engine;

public interface IActionBatchWriter
{
	void Publish(ActionBatch batch);

	void Fail(Exception exception);
}
