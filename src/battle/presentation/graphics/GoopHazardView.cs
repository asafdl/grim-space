using Godot;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>
/// Authoritative presentation for persistent goop hazards.
/// </summary>
public partial class GoopHazardView : Node3D
{
	private readonly Dictionary<string, GoopBlobFieldView> _fields = new(StringComparer.Ordinal);

	public void Apply(IEnumerable<GoopHazard> hazards)
	{
		var live = hazards.ToDictionary(hazard => hazard.Id, StringComparer.Ordinal);
		foreach (var hazard in live.Values)
			Show(hazard.Id, hazard.Cells);

		foreach (var id in _fields.Keys.Where(id => !live.ContainsKey(id)).ToArray())
			Remove(id);
	}

	public void Show(GoopSpawnedFacts spawn)
	{
		var field = GetOrCreateField(spawn.HazardId);
		field.Reveal(spawn.Cells, SeedFor(spawn.HazardId), spawn.Center);
	}

	public void Show(string hazardId, IReadOnlySet<Coord> cells)
	{
		var field = GetOrCreateField(hazardId);
		field.Build(cells, SeedFor(hazardId));
	}

	public void Remove(string hazardId)
	{
		if (!_fields.Remove(hazardId, out var field))
			return;

		field.QueueFree();
	}

	public void Clear()
	{
		foreach (var id in _fields.Keys.ToArray())
			Remove(id);
	}

	private GoopBlobFieldView GetOrCreateField(string hazardId)
	{
		if (_fields.TryGetValue(hazardId, out var field))
			return field;

		field = new GoopBlobFieldView { Name = hazardId };
		AddChild(field);
		_fields.Add(hazardId, field);
		return field;
	}

	private static ulong SeedFor(string id)
	{
		const ulong offset = 14695981039346656037UL;
		const ulong prime = 1099511628211UL;
		var hash = offset;
		foreach (var character in id)
		{
			hash ^= character;
			hash *= prime;
		}

		return hash;
	}
}
