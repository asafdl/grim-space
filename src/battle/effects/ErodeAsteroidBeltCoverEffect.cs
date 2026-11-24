using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public readonly record struct AsteroidBeltChangedFacts(
	string SourceId,
	string HazardId,
	Coord Cell,
	int BeforeResistancePercent,
	int AfterResistancePercent);

public sealed class ErodeAsteroidBeltCoverEffect(IReadOnlySet<Coord> hitCells)
	: IEffect<BattleWorld, ActorRuntime>
{
	private readonly List<AsteroidBelt> _snapshots = [];

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		_snapshots.Clear();
		_snapshots.AddRange(world.NonUnits.Values
			.OfType<AsteroidBelt>()
			.Where(belt => hitCells.Contains(belt.Center))
			.OrderBy(belt => belt.Id, StringComparer.Ordinal));

		var records = new List<IRecord>(_snapshots.Count);
		foreach (var belt in _snapshots)
		{
			if (!world.RemoveNonUnit(belt.Id))
				throw new InvalidOperationException($"Asteroid belt '{belt.Id}' disappeared before erosion.");

			var afterResistance = belt.DamageResistancePercent - AsteroidBelt.ResistanceLostPerHit;
			if (afterResistance > 0)
				world.AddNonUnit(AsteroidBelt.Create(belt.Id, belt.Center, world.Grid, afterResistance));

			records.Add(new Record<AsteroidBeltChangedFacts>(new(
				actorId,
				belt.Id,
				belt.Center,
				belt.DamageResistancePercent,
				afterResistance)));
		}

		return records;
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var belt in _snapshots)
		{
			if (world.NonUnits.TryGetValue(belt.Id, out var current))
			{
				if (current is not AsteroidBelt)
					throw new InvalidOperationException(
						$"Non-unit '{belt.Id}' changed type before asteroid belt erosion was undone.");
				if (!world.RemoveNonUnit(belt.Id))
					throw new InvalidOperationException($"Asteroid belt '{belt.Id}' could not be removed during undo.");
			}

			world.AddNonUnit(belt);
		}
	}
}
