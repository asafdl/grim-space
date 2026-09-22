using Godot;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class UnitView : Node3D
{
	private static readonly ESpatialOrientation[] Faces = Enum.GetValues<ESpatialOrientation>();

	private MeshInstance3D? _hull;
	private MeshInstance3D? _hitMark;
	private EType _type;
	private readonly int[] _shieldPoints = new int[Faces.Length];
	private readonly MeshInstance3D?[] _shieldFaces = new MeshInstance3D?[Faces.Length];
	private bool _hitMarked;
	private Tween? _poseTween;

	public override void _ExitTree()
	{
		_poseTween?.Kill();
		_poseTween = null;
		base._ExitTree();
	}

	public void Bind(State state, Color color)
	{
		Name = state.Id;
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

		BindShieldBubble(state);

		Sync(state);
	}

	public void Sync(State state)
	{
		_poseTween?.Kill();
		_poseTween = null;
		Visible = state.IsAlive;
		if (!state.IsAlive)
			return;

		ApplyPose(state);
	}

	public void SetGhost(bool selected)
	{
		foreach (var child in GetChildren())
		{
			if (child is GeometryInstance3D visual)
			{
				visual.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
				visual.Transparency = selected
					? visual == _hull ? 0.45f : 0f
					: 0.75f;
			}
		}
	}

	public void AnimateMoveTo(State state, double duration)
	{
		_poseTween?.Kill();
		Visible = state.IsAlive;
		if (!state.IsAlive)
			return;

		var target = WorldMapping.ToWorld(state.Position);
		_poseTween = CreateTween();
		_poseTween.TweenProperty(this, "position", target, duration)
			.SetTrans(Tween.TransitionType.Linear);
		_poseTween.Chain().TweenCallback(Callable.From(() =>
		{
			ApplyShields(state);
			_poseTween = null;
		}));
	}

	public void AnimatePoseTo(State state, double duration)
	{
		_poseTween?.Kill();
		Visible = state.IsAlive;
		if (!state.IsAlive)
			return;

		var startPosition = Position;
		var targetPosition = WorldMapping.ToWorld(state.Position);
		var startRotation = NormalizeRotationQuaternion(Basis.GetRotationQuaternion());
		var targetRotation = NormalizeRotationQuaternion(BasisFrom(state).GetRotationQuaternion());
		_poseTween = CreateTween();
		_poseTween.TweenMethod(
			Callable.From<float>(weight =>
			{
				if (!IsInstanceValid(this))
					return;

				Position = startPosition.Lerp(targetPosition, weight);
				Basis = new Basis(startRotation.Slerp(targetRotation, weight).Normalized());
			}),
			0f,
			1f,
			duration)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.InOut);
		_poseTween.Chain().TweenCallback(Callable.From(() =>
		{
			ApplyPose(state);
			_poseTween = null;
		}));
	}

	public void AnimateOrientationTo(State state, double duration)
	{
		_poseTween?.Kill();
		Visible = state.IsAlive;
		if (!state.IsAlive)
			return;

		var startQuat = NormalizeRotationQuaternion(Basis.GetRotationQuaternion());
		var endQuat = NormalizeRotationQuaternion(BasisFrom(state).GetRotationQuaternion());
		_poseTween = CreateTween();
		_poseTween.TweenMethod(
			Callable.From<float>(weight =>
			{
				if (!IsInstanceValid(this))
					return;

				Basis = new Basis(startQuat.Slerp(endQuat, weight).Normalized());
			}),
			0f,
			1f,
			duration)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.InOut);
		_poseTween.Chain().TweenCallback(Callable.From(() =>
		{
			ApplyShields(state);
			_poseTween = null;
		}));
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

	public void PlayDamagePopup(int damage)
	{
		if (damage <= 0)
			return;

		var label = new Label3D
		{
			Text = $"-{damage}",
			Position = new Vector3(0f, PopupLabelHeight(_type) + 0.45f, 0f),
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			FontSize = _type == EType.Torpedo ? 44 : 56,
			OutlineSize = 10,
			Modulate = new Color(1f, 0.22f, 0.18f),
		};
		AddChild(label);
		PresentationTween.FloatAndFree(
			this,
			label,
			new Vector3(0f, 0.75f, 0f),
			0.55,
			fadeDelay: 0.2);
	}

	public void PlayDeathExplosion()
	{
		var scale = _type switch
		{
			EType.Torpedo => 0.55f,
			EType.Patrol => 0.72f,
			EType.Carrier => 1.35f,
			_ => 1.1f,
		};

		OneShotParticles.Play(
			this,
			Vector3.Zero,
			new Color(1f, 0.42f, 0.12f, 0.9f),
			scale);
	}

	/// <summary>Apply post-hit state while keeping the mesh visible for the flash window.</summary>
	public void ShowImpactState(State state)
	{
		Visible = true;
		ApplyPose(state);
	}

	public void SetHitMarked(bool marked)
	{
		if (_hitMarked == marked && _hitMark is not null)
			return;

		_hitMarked = marked;
		ApplyHitMarkVisual();
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

	private void ApplyPose(State state)
	{
		Position = WorldMapping.ToWorld(state.Position);
		ApplyOrientation(state);
		ApplyShields(state);
	}

	private void EndHitFlash()
	{
		if (_hitMark is null)
			return;

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
			MaterialOverride = CreateHullMaterial(color),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		AddChild(_hull);

		var nose = new MeshInstance3D
		{
			Mesh = ShipMesh.CreateNoseMarker(),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = CreateForeMarkerMaterial(color),
		};
		AddChild(nose);

		var bridge = new MeshInstance3D
		{
			Mesh = ShipMesh.CreateBridgeMarker(),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = CreateDorsalMarkerMaterial(),
		};
		AddChild(bridge);
	}

	private void BindCarrier(Color color)
	{
		_hull = new MeshInstance3D
		{
			Mesh = CarrierMesh.CreateHull(),
			MaterialOverride = CreateHullMaterial(color),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		AddChild(_hull);

		var island = new MeshInstance3D
		{
			Mesh = CarrierMesh.CreateIslandMarker(),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = CreateDorsalMarkerMaterial(),
		};
		AddChild(island);
	}

	private void BindPatrol(Color color)
	{
		_hull = new MeshInstance3D
		{
			Mesh = PatrolMesh.CreateHull(),
			MaterialOverride = CreateHullMaterial(color),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		AddChild(_hull);

		var nose = new MeshInstance3D
		{
			Mesh = PatrolMesh.CreateNoseMarker(),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = CreateForeMarkerMaterial(color),
		};
		AddChild(nose);

		var bridge = new MeshInstance3D
		{
			Mesh = PatrolMesh.CreateBridgeMarker(),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = CreateDorsalMarkerMaterial(),
		};
		AddChild(bridge);
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

		var fin = new MeshInstance3D
		{
			Mesh = TorpedoMesh.CreateDorsalFin(),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = CreateDorsalMarkerMaterial(),
		};
		AddChild(fin);
	}

	private void BindShieldBubble(State state)
	{
		var bounds = LocalVisualBounds();
		var maxProfile = state.Spec.MaxShieldPoints;
		foreach (var face in Faces)
		{
			if (maxProfile[face] <= 0)
				continue;

			var instance = new MeshInstance3D
			{
				Name = $"Shield{face}",
				Mesh = ShieldBubbleMesh.CreateFace(bounds, face),
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			};
			PresentationLayers.MarkUx(instance);
			_shieldFaces[(int)face] = instance;
			AddChild(instance);
		}
	}

	private void ApplyShields(State state)
	{
		var maxProfile = state.Spec.MaxShieldPoints;
		foreach (var face in Faces)
		{
			var index = (int)face;
			var instance = _shieldFaces[index];
			if (instance is null)
				continue;

			var maxOnFace = maxProfile[face];
			var points = System.Math.Clamp(state.ShieldPoints[face], 0, maxOnFace);
			if (_shieldPoints[index] == points)
				continue;

			_shieldPoints[index] = points;
			instance.Visible = points > 0;
			if (instance.Visible)
				instance.MaterialOverride = ShieldFaceMaterials.For(points, maxOnFace);
		}
	}

	private Aabb LocalVisualBounds()
	{
		var found = false;
		var bounds = default(Aabb);
		foreach (var child in GetChildren())
		{
			if (child is not MeshInstance3D { Mesh: { } mesh })
				continue;

			var meshBounds = mesh.GetAabb();
			bounds = found ? bounds.Merge(meshBounds) : meshBounds;
			found = true;
		}

		return found
			? bounds
			: throw new InvalidOperationException("Cannot build a shield bubble without a unit mesh.");
	}

	private static StandardMaterial3D CreateHullMaterial(Color color) =>
		new()
		{
			AlbedoColor = color.Lightened(0.12f),
			EmissionEnabled = true,
			Emission = color.Lightened(0.35f),
			EmissionEnergyMultiplier = 0.25f,
			Roughness = 0.45f,
			Metallic = 0.1f,
		};

	private static StandardMaterial3D CreateForeMarkerMaterial(Color color) =>
		new()
		{
			AlbedoColor = color.Lightened(0.35f),
			EmissionEnabled = true,
			Emission = color.Lightened(0.5f),
			EmissionEnergyMultiplier = 0.6f,
			Roughness = 0.3f,
		};

	private static StandardMaterial3D CreateDorsalMarkerMaterial() =>
		new()
		{
			AlbedoColor = new Color(0.82f, 0.90f, 1f),
			EmissionEnabled = true,
			Emission = new Color(0.65f, 0.82f, 1f),
			EmissionEnergyMultiplier = 0.9f,
			Roughness = 0.2f,
		};

	private void ApplyOrientation(State state) =>
		Basis = BasisFrom(state);

	private static Quaternion NormalizeRotationQuaternion(Quaternion quaternion) =>
		quaternion.LengthSquared() < Mathf.Epsilon ? Quaternion.Identity : quaternion.Normalized();

	private static Basis BasisFrom(State state) =>
		new(
			ToVector3(state.Starboard),
			ToVector3(state.Dorsal),
			ToVector3(state.Fore));

	private static float PopupLabelHeight(EType type) =>
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
