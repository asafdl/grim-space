using Godot;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Graphics;

namespace GrimSpace.Dev;

/// <summary>
/// Dev loop of a 4-hop chain lightning path in mixed directions (incl. a dorsal-parallel hop).
/// Open <c>chain_lightning_preview.tscn</c> and run current scene (F6).
/// </summary>
public partial class ChainLightningPreview : Node3D
{
	private static readonly Vector3 Dorsal = Vector3.Up;

	/// <summary>Cell-space waypoints: beam start + 4 hops (fore, starboard, dorsal, aft-port).</summary>
	private static readonly Vector3[] HopCells =
	[
		new(0f, 0f, 0f),
		new(6f, 0f, 0f),
		new(6f, 0f, 4f),
		new(6f, 4f, 4f),
		new(2f, 4f, 6f),
	];

	private float _timeUntilFire;
	private float _zoom = 1f;
	private bool _frozen;
	private Node3D _bolts = null!;
	private Node3D _markers = null!;
	private Label _status = null!;

	public override void _Ready()
	{
		_status = GetNode<Label>("UI/Status");
		_bolts = new Node3D { Name = "Bolts" };
		_markers = new Node3D { Name = "HopMarkers" };
		AddChild(_bolts);
		AddChild(_markers);
		BuildHopMarkers();
		UpdateCamera();
		UpdateStatus();
		Fire();
	}

	public override void _Process(double delta)
	{
		if (_frozen)
			return;

		_timeUntilFire -= (float)delta;
		if (_timeUntilFire <= 0f)
			Fire();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton { Pressed: true } mouse)
			return;

		switch (mouse.ButtonIndex)
		{
			case MouseButton.WheelUp:
				_zoom = Mathf.Max(0.35f, _zoom * 0.85f);
				break;
			case MouseButton.WheelDown:
				_zoom = Mathf.Min(2.5f, _zoom / 0.85f);
				break;
			default:
				return;
		}

		UpdateCamera();
		UpdateStatus();
		GetViewport().SetInputAsHandled();
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } key)
			return;

		switch (key.Keycode)
		{
			case Key.Space:
				Fire();
				break;
			case Key.P:
				_frozen = !_frozen;
				if (_frozen)
					ClearBolts();
				else
					Fire();
				break;
			default:
				return;
		}

		UpdateStatus();
		GetViewport().SetInputAsHandled();
	}

	private void Fire()
	{
		ClearBolts();
		var cellSize = WorldMapping.CellSize;
		var path = BuildPath(cellSize);
		var segmentCount = path.Length - 1;
		for (var segment = 0; segment < segmentCount; segment++)
		{
			var chainHop = segment > 0;
			var effect = LightningCannonEffect.FireAlongPath(
				_bolts,
				[path[segment], path[segment + 1]],
				Dorsal,
				cellSize,
				strikeSeconds: chainHop
					? LightningCannonEffect.ChainHopStrikeSeconds
					: LightningCannonEffect.StrikeSeconds,
				startDelay: LightningCannonEffect.ChainSegmentStartDelay(segment));
			if (_frozen)
				effect.ProcessMode = ProcessModeEnum.Disabled;
		}

		_timeUntilFire = LightningCannonEffect.ChainSequencePauseSeconds(segmentCount) + 0.55f;
	}

	private void ClearBolts()
	{
		foreach (var child in _bolts.GetChildren())
			child.QueueFree();
	}

	private void BuildHopMarkers()
	{
		var cellSize = WorldMapping.CellSize;
		var path = BuildPath(cellSize);
		for (var i = 0; i < path.Length; i++)
		{
			var marker = new MeshInstance3D
			{
				Name = $"Hop{i}",
				Mesh = new SphereMesh
				{
					Radius = cellSize * (i == 0 ? 0.18f : 0.12f),
					Height = cellSize * (i == 0 ? 0.36f : 0.24f),
				},
				MaterialOverride = new StandardMaterial3D
				{
					AlbedoColor = i == 0
						? new Color(0.95f, 0.85f, 0.25f)
						: new Color(0.35f, 0.85f, 1f),
					EmissionEnabled = true,
					Emission = i == 0
						? new Color(0.7f, 0.55f, 0.1f)
						: new Color(0.15f, 0.45f, 0.7f),
					EmissionEnergyMultiplier = 1.4f,
				},
				Position = path[i],
			};
			_markers.AddChild(marker);
		}
	}

	private static Vector3[] BuildPath(float cellSize)
	{
		var path = new Vector3[HopCells.Length];
		for (var i = 0; i < HopCells.Length; i++)
			path[i] = HopCells[i] * cellSize;
		return path;
	}

	private void UpdateCamera()
	{
		var cellSize = WorldMapping.CellSize;
		var path = BuildPath(cellSize);
		var bounds = new Aabb(path[0], Vector3.Zero);
		foreach (var point in path)
			bounds = bounds.Expand(point);

		var center = bounds.GetCenter();
		var extent = Mathf.Max(bounds.Size.X, Mathf.Max(bounds.Size.Y, bounds.Size.Z));
		var camera = GetNode<Camera3D>("Camera3D");
		camera.Position = center + new Vector3(extent * 0.55f, extent * 0.55f, extent * 0.95f) * _zoom;
		camera.LookAt(center, Vector3.Up);
	}

	private void UpdateStatus()
	{
		_status.Text =
			$"Chain lightning preview  |  Hops: {HopCells.Length - 1}  |  {(_frozen ? "Frozen" : "Looping")}\n" +
			"Path: fore → starboard → dorsal (parallel-to-up case) → aft-port\n" +
			"Mouse wheel: zoom   Space: fire   P: freeze/resume";
	}
}
