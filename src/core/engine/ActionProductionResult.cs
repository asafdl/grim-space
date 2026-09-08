namespace GrimSpace.Core.Engine;

public sealed class ActionProductionResult
{
	private ActionProductionResult(ActionBatch? batch, Exception? failure)
	{
		Batch = batch;
		Failure = failure;
	}

	public ActionBatch? Batch { get; }

	public Exception? Failure { get; }

	public bool IsSuccess => Failure is null;

	public static ActionProductionResult FromBatch(ActionBatch batch) => new(batch, null);

	public static ActionProductionResult FromFailure(Exception failure) => new(null, failure);
}
