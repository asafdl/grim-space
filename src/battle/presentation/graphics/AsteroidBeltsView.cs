using Godot;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>
/// Battle presentation for asteroid-belt cover, one view per cover id.
/// </summary>
public partial class AsteroidBeltsView : Node3D
{
	private readonly Dictionary<string, AsteroidBeltCoverView> _covers = new(StringComparer.Ordinal);

	public int EncounterSeed { private get; init; }

	public void Apply(IEnumerable<AsteroidBelt> belts)
	{
		var present = new HashSet<string>(StringComparer.Ordinal);
		foreach (var belt in belts)
		{
			present.Add(belt.Id);
			Show(belt.Id, belt.Center, belt.DamageResistancePercent);
		}

		// Destroyed covers keep their node so an in-flight shatter animation can finish.
		foreach (var (id, cover) in _covers)
			if (!present.Contains(id) && cover.ResistancePercent is not null)
				cover.Apply(null, 0);
	}

	public void ApplyChange(AsteroidBeltChangedFacts change, Vector3 sourcePosition)
	{
		var cover = Show(change.HazardId, change.Cell, change.BeforeResistancePercent);
		var direction = cover.GlobalBasis.Inverse() * (cover.GlobalPosition - sourcePosition);
		cover.AnimateHit(
			change.AfterResistancePercent == 0 ? null : change.AfterResistancePercent,
			direction.IsZeroApprox() ? Vector3.Forward : direction);
	}

	private AsteroidBeltCoverView Show(string id, Coord cell, int resistancePercent)
	{
		if (!_covers.TryGetValue(id, out var cover))
		{
			cover = new AsteroidBeltCoverView { Name = id, Position = WorldMapping.ToWorld(cell) };
			AddChild(cover);
			_covers.Add(id, cover);
		}

		if (cover.ResistancePercent != resistancePercent)
			cover.Apply(resistancePercent, CloudPattern.SeedFor(EncounterSeed, cell));
		return cover;
	}
}
