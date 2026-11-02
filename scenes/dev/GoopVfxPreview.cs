using Godot;
using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Dev;

public partial class GoopVfxPreview : Node3D
{
	private readonly Dictionary<string, UnitView> _views = new();
	private TurnReplayPlayer _replay = null!;
	private GoopBlobFieldView _field = null!;
	private Label _status = null!;
	private Camera3D _camera = null!;
	private State _actor = null!;
	private Coord _center;
	private IReadOnlySet<Coord> _cells = new HashSet<Coord>();
	private MeshInstance3D _guide = null!;
	private ulong _seed = 0x6A09E667F3BCC909UL;
	private int _orientation;
	private float _azimuth = 0.8f;
	private float _elevation = 0.35f;
	private Vector3 _cameraFocus;

	public override void _Ready()
	{
		_camera = GetNode<Camera3D>("Camera3D");
		_status = GetNode<Label>("UI/Status");
		_field = new GoopBlobFieldView { Name = "PersistentGoopField" };
		AddChild(_field);
		_guide = new MeshInstance3D
		{
			Name = "HazardPlaneGuide",
			MaterialOverride = new StandardMaterial3D
			{
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				AlbedoColor = new Color(0.12f, 0.24f, 0.32f, 0.2f),
			},
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		PresentationLayers.MarkWorld(_guide);
		AddChild(_guide);
		_replay = new TurnReplayPlayer();
		AddChild(_replay);
		_replay.Configure(_views, _ => Colors.White,
			(_, _) => throw new InvalidOperationException("Goop preview must not spawn units."),
			_ => throw new InvalidOperationException("Goop preview must not remove units."),
			states =>
			{
				foreach (var state in states.Values)
				{
					if (!_views.TryGetValue(state.Id, out var view))
					{
						view = new UnitView();
						AddChild(view);
						view.Bind(state, Colors.White);
						_views.Add(state.Id, view);
					}
					view.Sync(state);
				}
			},
			_ => { },
			goop => _field.Reveal(goop.Cells, _seed, goop.Center),
			_ => _field.Build(new HashSet<Coord>(), _seed));
		CallDeferred(MethodName.Fire);
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } key)
			return;
		switch (key.Keycode)
		{
			case Key.Space:
				if (!_replay.IsPlaying)
					Fire();
				break;
			case Key.R:
				_seed = unchecked(_seed + 0x9E3779B97F4A7C15UL);
				_field.Reveal(_cells, _seed, _center);
				UpdateStatus();
				break;
			case Key.Key1 or Key.Key2 or Key.Key3:
				if (!_replay.IsPlaying)
					SetOrientation((int)key.Keycode - (int)Key.Key1);
				break;
			case Key.Left:
				_azimuth -= 0.2f;
				UpdateCamera();
				break;
			case Key.Right:
				_azimuth += 0.2f;
				UpdateCamera();
				break;
			case Key.Up:
				_elevation = Mathf.Min(_elevation + 0.15f, 1.2f);
				UpdateCamera();
				break;
			case Key.Down:
				_elevation = Mathf.Max(_elevation - 0.15f, -0.4f);
				UpdateCamera();
				break;
			default:
				return;
		}
		GetViewport().SetInputAsHandled();
	}

	public void SetOrientation(int orientation)
	{
		if (orientation is < 0 or > 2)
			throw new ArgumentOutOfRangeException(nameof(orientation));
		_orientation = orientation;
		Fire();
	}

	public void Fire()
	{
		var (fore, dorsal) = _orientation switch
		{
			1 => (new Coord(1, 0, 0), Coord.Up),
			2 => (Coord.Up, Coord.Forward),
			_ => (Coord.Forward, Coord.Up),
		};
		_actor = State.FromShipInstance(ShipCatalog.CreateInstance("goop-actor", EType.IndustrialGooper),
			new Coord(6, 6, 6), fore, dorsal);
		var installed = _actor.FindInstalled(EAbilityKind.GoopGun, ESpatialOrientation.Retro)
			?? throw new InvalidOperationException("Gooper preview requires its rear Goop Gun.");
		var spec = (GoopGunSpec)installed.Spec;
		var frame = BodyFrame.From(_actor);
		var axis = frame.Step(installed.MountedOn);
		_center = _actor.Position + axis * spec.Range;
		_cells = AbilityArea.CellsInBounds(spec, frame, installed.MountedOn, new Grid(16, 16, 16));
		var states = new Dictionary<string, State> { [_actor.Id] = _actor };
		_replay.ResetToLive(states, states);
		_field.Build(new HashSet<Coord>(), _seed);
		var patch = BodyFrame.PatchAt(_center, axis, dorsal);
		_guide.Position = WorldMapping.ToWorld(_center);
		_guide.Basis = new Basis(ToVector(patch.Starboard), ToVector(patch.Dorsal), ToVector(patch.Fore));
		var width = 3 * WorldMapping.CellSize;
		_guide.Mesh = new BoxMesh { Size = new Vector3(width, width, 0.025f) };
		_cameraFocus = (WorldMapping.ToWorld(_actor.Position) + WorldMapping.ToWorld(_center)) * 0.5f;
		UpdateCamera();
		var hazardId = $"goop-preview-{_seed}";
		_replay.Play(new TurnReplay(states,
			[new GoopGunAction(_actor.Id, installed.MountedOn, hazardId),
				new Record<GoopSpawnedFacts>(new(_actor.Id, hazardId, _center, _cells))],
			states), 1, new Dictionary<string, ETeam> { [_actor.Id] = ETeam.Player });
		UpdateStatus();
	}

	private void UpdateCamera()
	{
		_camera.Position = _cameraFocus + new Vector3(
			Mathf.Cos(_elevation) * Mathf.Sin(_azimuth),
			Mathf.Sin(_elevation),
			Mathf.Cos(_elevation) * Mathf.Cos(_azimuth)) * 11;
		_camera.LookAt(_cameraFocus);
	}

	private void UpdateStatus() =>
		_status.Text = $"Goop Gun: rough 3D sludge streak | Retro mount | Orientation {_orientation + 1}\n"
			+ "Space: fire replay   1/2/3: change ship orientation   Arrows: orbit camera   R: reseed field";

	private static Vector3 ToVector(Coord coord) => new(coord.X, coord.Y, coord.Z);
}
