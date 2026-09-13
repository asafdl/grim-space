using Godot;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Picking;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class AbilitySourcePickerView : Node3D
{
	private const float ViewDotThreshold = 0.9995f;
	private static readonly Color CellHoverTint = new(0.55f, 0.82f, 1f);

	private readonly List<SourceView> _sources = [];
	private IReadOnlyList<AbilityActivationChoice> _choices = [];
	private Camera3D _camera = null!;
	private ArrayMesh _cellMesh = null!;
	private Vector3? _cellMeshViewDirection;
	private StandardMaterial3D _cellMaterial = null!;
	private StandardMaterial3D _hoverCellMaterial = null!;

	public void Configure(Camera3D camera)
	{
		_camera = camera;
		_cellMaterial = CellGridGeometry.CreateMaterial(Colors.White);
		_hoverCellMaterial = CellGridGeometry.CreateMaterial(CellHoverTint);
		RefreshCellMesh();
		Visible = false;
	}

	public override void _Process(double delta)
	{
		if (!Visible || !IsInstanceValid(_camera))
			return;

		RefreshCellMesh();
	}

	public void Apply(
		IReadOnlyList<AbilityActivationChoice> choices,
		int? hoveredIndex)
	{
		_choices = choices;
		EnsureSourceCount(choices.Count);
		for (var i = 0; i < _sources.Count; i++)
		{
			var active = i < choices.Count;
			_sources[i].Root.Visible = active;
			if (active)
			{
				_sources[i].Apply(
					choices[i],
					_cellMesh,
					i == hoveredIndex,
					_cellMaterial,
					_hoverCellMaterial);
			}
		}

		Visible = choices.Count > 0;
	}

	public int? PickIndex(Vector2 screenPosition)
	{
		if (!Visible || _choices.Count == 0)
			return null;

		var cells = _choices.Select(choice => choice.Position).ToHashSet();
		if (GridPick.PickFromSet(_camera, screenPosition, cells) is not { } cell)
			return null;

		for (var i = 0; i < _choices.Count; i++)
		{
			if (_choices[i].Position == cell)
				return i;
		}

		return null;
	}

	private void EnsureSourceCount(int count)
	{
		while (_sources.Count < count)
		{
			var source = new SourceView(_sources.Count);
			_sources.Add(source);
			AddChild(source.Root);
		}
	}

	private void RefreshCellMesh()
	{
		var viewDirection = _camera.GlobalTransform.Basis.Z.Normalized();
		if (_cellMeshViewDirection is Vector3 previous
			&& previous.Dot(viewDirection) >= ViewDotThreshold)
		{
			return;
		}

		_cellMesh = CellGridGeometry.CreateMesh(
			viewDirection,
			[Vector3.Zero],
			includeHatches: true);
		_cellMeshViewDirection = viewDirection;
		foreach (var source in _sources)
			source.Cell.Mesh = _cellMesh;
	}

	private sealed class SourceView
	{
		private AbilityTargetingSpec? _targeting;
		private MeshInstance3D? _ghost;
		private ShaderMaterial? _ghostMaterial;

		public SourceView(int index)
		{
			Root = new Node3D
			{
				Name = $"AbilitySource{index}",
				Visible = false,
			};
			Cell = new MeshInstance3D
			{
				Name = "Cell",
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			};
			PresentationLayers.MarkUx(Cell);
			Root.AddChild(Cell);
		}

		public Node3D Root { get; }
		public MeshInstance3D Cell { get; }

		public void Apply(
			AbilityActivationChoice choice,
			ArrayMesh cellMesh,
			bool hovered,
			Material cellMaterial,
			Material hoverCellMaterial)
		{
			Root.Position = WorldMapping.ToWorld(choice.Position);
			Cell.Mesh = cellMesh;
			Cell.MaterialOverride = hovered ? hoverCellMaterial : cellMaterial;
			EnsureGhost(choice.Targeting);
			_ghost!.Basis = BasisFrom(choice);
			WeaponPreviewMaterials.ApplyAim(
				_ghostMaterial!,
				choice.Targeting.Tint,
				hovered ? 1.45f : 0.85f);
		}

		private void EnsureGhost(AbilityTargetingSpec targeting)
		{
			if (_ghost is not null && _targeting == targeting)
				return;

			_ghost?.QueueFree();
			_ghostMaterial = WeaponPreviewMaterials.CreateDotted(targeting.Tint);
			_ghost = new MeshInstance3D
			{
				Name = "AbilityGhost",
				Mesh = targeting.CreateGhostMesh(),
				MaterialOverride = _ghostMaterial,
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			};
			PresentationLayers.MarkUx(_ghost);
			Root.AddChild(_ghost);
			_targeting = targeting;
		}

		private static Basis BasisFrom(AbilityActivationChoice choice)
		{
			var starboard = Coord.Cross(choice.Dorsal, choice.Fore);
			return new Basis(
				ToVector(starboard),
				ToVector(choice.Dorsal),
				ToVector(choice.Fore));
		}

		private static Vector3 ToVector(Coord coord) =>
			new(coord.X, coord.Y, coord.Z);
	}
}
