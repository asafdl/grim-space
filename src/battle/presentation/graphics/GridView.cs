using Godot;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class GridView : Node3D
{
private const float LocalGridViewDotThreshold = 0.9995f;

	private Camera3D _camera = null!;
	private MeshInstance3D _rangeShell = null!;
	private CellVolumeWireframeSlot _rangeSlot = null!;
	private MeshInstance3D _localGrid = null!;
	private StandardMaterial3D _rangeShellMaterial = null!;

	private Vector3? _ghostWorld;
	private Vector3? _localGridViewDirection;

	internal void Build(Camera3D camera, CellVolumeMeshStore meshes)
	{
		_camera = camera;
		_rangeShellMaterial = CreateRangeShellMaterial();

		_rangeSlot = new CellVolumeWireframeSlot(
			"MovementRangeShell",
			_rangeShellMaterial,
			meshes,
			CellVolumeGeometry.Settings.Default);
		_rangeShell = _rangeSlot.Instance;
		AddChild(_rangeShell);

		_localGrid = CreateVisual(
			"MovementLocalGrid",
			CreateLocalGridMesh(CurrentViewDirection()),
			CellGridGeometry.CreateMaterial(Colors.White));
		_localGridViewDirection = CurrentViewDirection();
		AddChild(_localGrid);

		HideMoveVisuals();
	}

	public override void _Process(double delta)
	{
		if (_localGrid.Visible)
			RefreshLocalGridMesh();
	}

	public void ApplyFrame(PresentationFrame frame)
	{
		if (!frame.ShowMovePreview
			|| frame.ShowOutcomeOverlay
			|| frame.Mode != EPlayerMode.Move)
		{
			HideMoveVisuals();
			PresentationDiagnostics.LogMoveRange(0, 0);
			return;
		}

		ApplyMoveVisuals(frame);
	}

	private void ApplyMoveVisuals(PresentationFrame frame)
	{
		var source = frame.FocusState.Position;
		RefreshRange(source, frame.MovePaths, frame.SimulationTick);

		_ghostWorld = frame.MoveGhostState is { } ghost
			? WorldMapping.ToWorld(ghost.Position)
			: null;
		_localGrid.Visible = _ghostWorld is not null;
		if (_ghostWorld is Vector3 ghostWorld)
		{
			_localGrid.GlobalPosition = ghostWorld;
			RefreshLocalGridMesh();
		}
	}

	private void RefreshRange(
		Coord source,
		IReadOnlyList<MovePathOption> paths,
		int tick)
	{
		var endpoints = paths.Select(option => option.EndPosition).ToHashSet();
		var cells = paths
			.SelectMany(option => option.Cells)
			.Append(source)
			.ToHashSet();
		_rangeSlot.Apply(
			endpoints.Count > 0
				? new CellVolumePreview(source, cells)
				: null,
			tick);
		PresentationDiagnostics.LogMoveRange(paths.Count, endpoints.Count);
		PresentationDiagnostics.LogMoveRange(paths.Count, endpoints.Count);
	}

	private void HideMoveVisuals()
	{
		if (_rangeShell is not null)
			_rangeShell.Visible = false;
		if (_localGrid is not null)
			_localGrid.Visible = false;
		_ghostWorld = null;
	}

	private static ArrayMesh CreateLocalGridMesh(Vector3 viewDirection)
		=> CellGridGeometry.CreateMesh(
			viewDirection,
			CellGridGeometry.NeighborCenters,
			includeHatches: true);

	private void RefreshLocalGridMesh()
	{
		var viewDirection = CurrentViewDirection();
		if (_localGridViewDirection is Vector3 previous
			&& previous.Dot(viewDirection) >= LocalGridViewDotThreshold)
			return;

		_localGrid.Mesh = CreateLocalGridMesh(viewDirection);
		_localGridViewDirection = viewDirection;
	}

	private Vector3 CurrentViewDirection() =>
		_camera.GlobalTransform.Basis.Z.Normalized();

	private static MeshInstance3D CreateVisual(
		string name,
		Mesh mesh,
		Material material)
	{
		var visual = new MeshInstance3D
		{
			Name = name,
			Mesh = mesh,
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		PresentationLayers.MarkUx(visual);
		return visual;
	}

	private static StandardMaterial3D CreateRangeShellMaterial() =>
		new()
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			AlbedoColor = new Color(0.32f, 0.78f, 0.56f, 0.03f),
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
			NoDepthTest = false,
		};

}
