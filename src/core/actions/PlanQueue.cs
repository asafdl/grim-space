namespace GrimSpace.Core.Actions;

public sealed class PlanQueue
{
	private readonly List<PlanBatch> _batches = [];

	public IReadOnlyList<IAction> Actions =>
		_batches.SelectMany(batch => batch.Actions).ToList();

	public IReadOnlyList<PlanBatch> Batches => _batches;

	public void Clear() => _batches.Clear();

	public bool TryPopLastBatch(out PlanBatch batch)
	{
		if (_batches.Count == 0)
		{
			batch = default;
			return false;
		}

		var index = _batches.Count - 1;
		batch = _batches[index];
		_batches.RemoveAt(index);
		return true;
	}

	public void Enqueue(PlanBatch batch) => _batches.Add(batch);

	public void Enqueue(IAction action) => Enqueue(new PlanBatch([action]));
}
