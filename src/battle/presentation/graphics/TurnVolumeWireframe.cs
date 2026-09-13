using Godot;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

internal sealed class TurnVolumeWireframe
{
	internal static readonly CellVolumeGeometry.Settings GeometrySettings =
		new(2.5, 0.42, 0.85, 12, 0.4);

	private readonly CellVolumeWireframeSlot[] _turns;

	public TurnVolumeWireframe(
		string namePrefix,
		IReadOnlyList<Color> turnTints,
		CellVolumeMeshStore meshes)
	{
		_turns = turnTints
			.Select((tint, index) => new CellVolumeWireframeSlot(
				$"{namePrefix}{index + 1}",
				WeaponPreviewMaterials.CreateWireframe(tint),
				meshes,
				GeometrySettings))
			.ToArray();
	}

	public IEnumerable<MeshInstance3D> Instances => _turns.Select(turn => turn.Instance);

	public void Apply(TurnVolumePreview? preview, int tick)
	{
		for (var i = 0; i < _turns.Length; i++)
		{
			if (preview is null || i >= preview.TurnBands.Count)
			{
				_turns[i].Apply(null, tick);
				continue;
			}

			var band = preview.TurnBands[i];
			IReadOnlySet<Coord> cells = band;
			if (band.Contains(preview.Origin))
				cells = band.Where(cell => cell != preview.Origin).ToHashSet();

			_turns[i].Apply(cells.Count == 0
				? null
				: new CellVolumePreview(preview.Origin, cells),
				tick);
		}
	}
}
