using Godot;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class UnitView : Node3D
{
	private static readonly ESpatialOrientation[] Faces = Enum.GetValues<ESpatialOrientation>();

	private Label3D? _momentumLabel;
	private MeshInstance3D? _hull;
	private MeshInstance3D? _hitMark;
	private Color _hullColor;
	private EType _type;
	private readonly int[] _shieldPoints = new int[Faces.Length];
	private bool _hitMarked;
	private bool _introMarked;
	private Tween? _introTween;

	public void Bind(State state, Color color)
	{
		Name = state.Id;
		_hullColor = color;
		_type = state.Type;
		Array.Fill(_shieldPoints, -1);

		if (state.Type == EType.Torpedo)
			BindTorpedo(color);
		else if (state.Type == EType.Patrol)
			BindPatrol(color);
		else if (state.Type == EType.Carrier)
			BindCarrier(color);
		else
			BindShip(color);

		_momentumLabel = new Label3D
		{
			Position = new Vector3(0f, StatusLabelHeight(state.Type), 0f),
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			FontSize = state.Type == EType.Torpedo ? 36 : state.Type == EType.Patrol ? 40 : 48,
			OutlineSize = 8,
			Modulate = Colors.White,
		};
		AddChild(_momentumLabel);

		Sync(state);
	}

	public void Sync(State state)
	{
		Visible = state.IsAlive;
		if (!state.IsAlive)
			return;

		ApplyPose(state);
	}

	/// <summary>Brief red pulse so replay impacts read as hits, not silent state changes.</summary>
	public void PlayHitFlash()
	{
		EnsureHitMark();
		Visible = true;
		_hitMark!.Visible = true;
		_hitMark.Scale = Vector3.One * 0.45f;

		var mat = (StandardMaterial3D)_hitMark.MaterialOverride!;
		mat.EmissionEnergyMultiplier = 2.4f;
		mat.AlbedoColor = new Color(1f, 0.28f, 0.22f, 0.55f);

		var tween = CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(_hitMark, "scale", Vector3.One * 1.75f, 0.16)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(mat, "emission_energy_multiplier", 0.55f, 0.28);
		tween.TweenProperty(mat, "albedo_color", new Color(1f, 0.28f, 0.22f, 0.12f), 0.28);
		tween.Chain().TweenCallback(Callable.From(EndHitFlash));
	}

	/// <summary>Apply post-hit state while keeping the mesh visible for the flash window.</summary>
	public void ShowImpactState(State state)
	{
		Visible = true;
		ApplyPose(state);
	}

	public void SetHitMarked(bool marked)
	{
		if (_hitMarked == marked && _hitMark is not null && !_introMarked)
			return;

		_hitMarked = marked;
		if (_introMarked)
			return;

		ApplyHitMarkVisual();
	}

	public void SetIntroMarked(bool marked)
	{
		if (_introMarked == marked)
			return;

		_introMarked = marked;
		_introTween?.Kill();
		_introTween = null;

		if (!marked)
		{
			ApplyHitMarkVisual();
			return;
		}

		EnsureHitMark();
		_hitMark!.Visible = true;
		ApplyIntroVisual();

		_introTween = CreateTween().SetLoops();
		_introTween.TweenProperty(_hitMark, "scale", Vector3.One * 1.42f, 0.55)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
		_introTween.TweenProperty(_hitMark, "scale", Vector3.One * 1.08f, 0.55)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.In);
	}

	private void ApplyHitMarkVisual()
	{
		EnsureHitMark();
		_hitMark!.Visible = _hitMarked;
		if (!_hitMarked)
			return;

		_hitMark.Scale = Vector3.One;
		var mat = (StandardMaterial3D)_hitMark.MaterialOverride!;
		mat.EmissionEnergyMultiplier = 0.7f;
		mat.AlbedoColor = new Color(1f, 0.28f, 0.22f, 0.28f);
	}

	private void ApplyIntroVisual()
	{
		_hitMark!.Scale = Vector3.One * 1.15f;
		var mat = (StandardMaterial3D)_hitMark.MaterialOverride!;
		mat.EmissionEnergyMultiplier = 2.4f;
		mat.AlbedoColor = new Color(1f, 0.15f, 0.1f, 0.68f);
	}

	private void ApplyPose(State state)
	{
		Position = WorldMapping.ToWorld(state.Position);
		ApplyOrientation(state);
		if (_type != EType.Torpedo)
			ApplyShieldColors(state);
		ApplyStatus(state);
	}

	private void EndHitFlash()
	{
		if (_hitMark is null)
			return;

		if (_introMarked)
		{
			ApplyIntroVisual();
			return;
		}

		_hitMark.Scale = Vector3.One;
		var mat = (StandardMaterial3D)_hitMark.MaterialOverride!;
		mat.EmissionEnergyMultiplier = 0.7f;
		mat.AlbedoColor = new Color(1f, 0.28f, 0.22f, 0.28f);
		if (!_hitMarked)
			_hitMark.Visible = false;
	}

	private void EnsureHitMark()
	{
		if (_hitMark is not null)
			return;

		var radius = _type switch
		{
			EType.Torpedo => 0.45f,
			EType.Patrol => 0.58f,
			EType.Carrier => 1.15f,
			_ => 0.95f,
		};
		_hitMark = new MeshInstance3D
		{
			Name = "HitMark",
			Mesh = new SphereMesh { Radius = radius, Height = radius * 2f },
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			Visible = false,
			MaterialOverride = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				AlbedoColor = new Color(1f, 0.28f, 0.22f, 0.28f),
				EmissionEnabled = true,
				Emission = new Color(1f, 0.2f, 0.15f),
				EmissionEnergyMultiplier = 0.7f,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
			},
		};
		PresentationLayers.MarkUx(_hitMark);
		AddChild(_hitMark);
	}

	private void BindShip(Color color)
	{
		_hull = new MeshInstance3D
		{
			Mesh = ShipMesh.CreateHull(),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		AddChild(_hull);

		var nose = new MeshInstance3D
		{
			Mesh = ShipMesh.CreateNoseMarker(),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = color.Lightened(0.35f),
				EmissionEnabled = true,
				Emission = color.Lightened(0.5f),
				EmissionEnergyMultiplier = 0.6f,
				Roughness = 0.3f,
			},
		};
		AddChild(nose);
	}

	private void BindCarrier(Color color)
	{
		_hull = new MeshInstance3D
		{
			Mesh = CarrierMesh.CreateHull(),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		AddChild(_hull);

		var island = new MeshInstance3D
		{
			Mesh = CarrierMesh.CreateIslandMarker(),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = color.Lightened(0.28f),
				EmissionEnabled = true,
				Emission = color.Lightened(0.42f),
				EmissionEnergyMultiplier = 0.55f,
				Roughness = 0.35f,
			},
		};
		AddChild(island);
	}

	private void BindPatrol(Color color)
	{
		_hull = new MeshInstance3D
		{
			Mesh = PatrolMesh.CreateHull(),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		AddChild(_hull);

		var nose = new MeshInstance3D
		{
			Mesh = PatrolMesh.CreateNoseMarker(),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = color.Lightened(0.35f),
				EmissionEnabled = true,
				Emission = color.Lightened(0.5f),
				EmissionEnergyMultiplier = 0.6f,
				Roughness = 0.3f,
			},
		};
		AddChild(nose);
	}

	private void BindTorpedo(Color color)
	{
		_hull = new MeshInstance3D
		{
			Mesh = TorpedoMesh.CreateHull(),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = color.Lightened(0.08f),
				EmissionEnabled = true,
				Emission = color.Lightened(0.25f),
				EmissionEnergyMultiplier = 0.4f,
				Roughness = 0.35f,
				Metallic = 0.35f,
			},
		};
		AddChild(_hull);
	}

	private void ApplyShieldColors(State state)
	{
		if (_hull is null)
			return;

		var maxProfile = state.Stats.MaxShieldPoints;
		foreach (var face in Faces)
		{
			var index = _type switch
			{
				EType.Patrol => PatrolMesh.SurfaceIndex(face),
				EType.Carrier => CarrierMesh.SurfaceIndex(face),
				_ => ShipMesh.SurfaceIndex(face),
			};
			var maxOnFace = maxProfile[face];
			var points = state.ShieldPoints[face];
			if (_shieldPoints[index] == points)
				continue;

			_shieldPoints[index] = points;
			_hull.SetSurfaceOverrideMaterial(
				index,
				ShieldFaceMaterials.For(_hullColor, points, maxOnFace));
		}
	}

	private void ApplyStatus(State state)
	{
		if (_momentumLabel is null)
			return;

		var text = state.Type == EType.Torpedo
			? $"H{state.HullPoints} F{state.FuelRemaining} M{state.MomentumLevel}"
			: $"H{state.HullPoints} M{state.MomentumLevel}";
		if (_momentumLabel.Text == text)
			return;

		_momentumLabel.Text = text;
	}

	private void ApplyOrientation(State state)
	{
		var fore = ToVector3(state.Fore);
		var dorsal = ToVector3(state.Dorsal);
		var starboard = ToVector3(state.Starboard);
		Basis = new Basis(starboard, dorsal, fore);
	}

	private static float StatusLabelHeight(EType type) =>
		type switch
		{
			EType.Torpedo => 0.55f,
			EType.Patrol => 0.82f,
			EType.Carrier => 1.35f,
			_ => 1.2f,
		};

	private static Vector3 ToVector3(Coord coord) =>
		new(coord.X, coord.Y, coord.Z);
}
