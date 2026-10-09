using Godot;
using GrimSpace.Battle.Presentation;
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
	private Aabb _hullBounds;
	private bool _hitMarked;
	private Tween? _poseTween;
	private Color _bindColor = Colors.White;
	private ShaderMaterial? _ghostMaterial;
	private readonly Dictionary<GeometryInstance3D, ShaderMaterial> _moveGhostMaterials = [];
	private MovementTrailView? _movementTrail;
	private Vector3? _movementTangent;

	public UnitVisualState VisualState { get; private set; } = UnitVisualState.Hidden;

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
		_bindColor = color;
		Array.Fill(_shieldPoints, -1);

		if (state.Type == EType.VoidBomb)
			BindVoidBomb();
		else if (state.Type == EType.RepurposedMiner)
			BindRepurposedMiner();
		else if (state.Type == EType.Carrier)
			BindCarrier();
		else if (state.Type == EType.Gunship)
			BindGunship();
		else if (state.Type == EType.IndustrialGooper)
			BindIndustrialGooper();
		else
			BindShip();

		BindShieldBubble(state);

		Sync(state);
	}

	/// <summary>Synchronizes authoritative unit state; dead units are hidden.</summary>
	public void Sync(State state) =>
		Present(state, state.IsAlive ? UnitVisualState.Live : UnitVisualState.Hidden);

	public void Present(State state, UnitVisualState visualState)
	{
		_poseTween?.Kill();
		_poseTween = null;
		_movementTangent = null;
		SetMovementTrailEmitting(false);
		VisualState = visualState;
		Visible = visualState != UnitVisualState.Hidden;
		if (!Visible)
			return;

		ApplyPose(state);
		ApplyVisualState();
	}

	public void HideVisual()
	{
		_poseTween?.Kill();
		_poseTween = null;
		_movementTangent = null;
		SetMovementTrailEmitting(false);
		VisualState = UnitVisualState.Hidden;
		Visible = false;
	}

	public void ConfigureMovementTrail(Color color)
	{
		if (_movementTrail is null)
		{
			_movementTrail = new MovementTrailView { Name = "MovementTrail" };
			AddChild(_movementTrail);
		}

		_movementTrail.Configure(
			_hullBounds,
			_type == EType.VoidBomb,
			color);
	}

	public void SetMovementTrailEmitting(bool emitting) =>
		_movementTrail?.SetEmitting(emitting);

	public void ClearMovementTrail() => _movementTrail?.Clear();

	private void ApplyVisualState()
	{
		var selectedGhost = VisualState == UnitVisualState.SelectedGhost;
		var passiveGhost = VisualState == UnitVisualState.Ghost;
		var selectedMoveGhost = VisualState == UnitVisualState.SelectedMoveGhost;
		var passiveMoveGhost = VisualState == UnitVisualState.MoveGhost;
		var anyGhost = selectedGhost || passiveGhost || selectedMoveGhost || passiveMoveGhost;
		foreach (var child in FindChildren("*", "GeometryInstance3D", true, false))
		{
			if (child is not GeometryInstance3D visual)
				continue;

			if (!IsHullVisual(visual))
			{
				if (!anyGhost)
					visual.Transparency = 0f;
				continue;
			}

			visual.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
			visual.Transparency = 0f;
			if (selectedMoveGhost || passiveMoveGhost)
			{
				if (!_moveGhostMaterials.TryGetValue(visual, out var material))
				{
					material = MoveGhostMaterials.Create(
						MeshToUnitTransform(visual),
						_hullBounds);
					_moveGhostMaterials.Add(visual, material);
				}
				MoveGhostMaterials.Apply(material, selectedMoveGhost);
				visual.MaterialOverride = material;
				continue;
			}

			if (selectedGhost || passiveGhost)
			{
				_ghostMaterial ??= WeaponPreviewMaterials.CreateDotted(GhostMarkerTint(selectedGhost));
				var tint = GhostMarkerTint(selectedGhost);
				var strength = selectedGhost ? 1.55f : 0.95f;
				WeaponPreviewMaterials.ApplyAim(_ghostMaterial, tint, strength);
				_ghostMaterial.SetShaderParameter(
					"fill",
					selectedGhost ? GhostGoalFill : GhostPassiveFill);
				visual.MaterialOverride = _ghostMaterial;
				continue;
			}

			visual.MaterialOverride = null;
		}
	}

	private const float GhostGoalFill = 0.14f;
	private const float GhostPassiveFill = 0.06f;

	private bool IsHullVisual(GeometryInstance3D visual) =>
		_hull is not null && (visual == _hull || _hull.IsAncestorOf(visual));

	private Transform3D MeshToUnitTransform(GeometryInstance3D visual)
	{
		var transform = Transform3D.Identity;
		for (Node3D? node = visual; node is not null && node != this; node = node.GetParent() as Node3D)
			transform = node.Transform * transform;
		return transform;
	}

	private Color GhostMarkerTint(bool selected)
	{
		var marker = selected
			? new Color(0.42f, 0.92f, 1f, 0.68f)
			: new Color(0.58f, 0.66f, 0.78f, 0.42f);
		return selected ? marker.Lerp(_bindColor, 0.18f) : marker;
	}

	public void AnimateMoveTo(State state, double duration, Coord? nextPosition = null)
	{
		AnimateMovementTo(state, duration, nextPosition, rotate: false);
	}

	public void AnimatePoseTo(State state, double duration, Coord? nextPosition = null)
	{
		AnimateMovementTo(state, duration, nextPosition, rotate: true);
	}

	private void AnimateMovementTo(
		State state,
		double duration,
		Coord? nextPosition,
		bool rotate)
	{
		_poseTween?.Kill();
		if (!state.IsAlive)
		{
			SetMovementTrailEmitting(false);
			return;
		}

		VisualState = UnitVisualState.Live;
		Visible = true;
		ApplyVisualState();
		SetMovementTrailEmitting(true);
		var startPosition = Position;
		var targetPosition = WorldMapping.ToWorld(state.Position);
		var chord = targetPosition - startPosition;
		var startTangent = _movementTangent ?? chord * 0.65f;
		var endTangent = MovementEndTangent(chord, targetPosition, nextPosition);
		_movementTangent = nextPosition is null ? null : endTangent;
		var startRotation = NormalizeRotationQuaternion(Basis.GetRotationQuaternion());
		var targetRotation = NormalizeRotationQuaternion(BasisFrom(state).GetRotationQuaternion());
		_poseTween = CreateTween();
		_poseTween.TweenMethod(
			Callable.From<float>(weight =>
			{
				if (!IsInstanceValid(this))
					return;

				Position = Hermite(startPosition, targetPosition, startTangent, endTangent, weight);
				if (rotate)
					Basis = new Basis(startRotation.Slerp(targetRotation, weight).Normalized());
			}),
			0f,
			1f,
			duration)
			.SetTrans(Tween.TransitionType.Linear);
		_poseTween.Chain().TweenCallback(Callable.From(() =>
		{
			ApplyPose(state);
			SetMovementTrailEmitting(false);
			_poseTween = null;
		}));
	}

	public void AnimateOrientationTo(State state, double duration, bool emitTrail = true)
	{
		_poseTween?.Kill();
		SetMovementTrailEmitting(false);
		if (!state.IsAlive)
			return;

		VisualState = UnitVisualState.Live;
		Visible = true;
		ApplyVisualState();
		SetMovementTrailEmitting(emitTrail);
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
			SetMovementTrailEmitting(false);
			_poseTween = null;
		}));
	}

	public void PlayHitSparks() =>
		OneShotParticles.PlayHitSparks(this, Vector3.Zero);

	public void PlayLightningHitSparks()
	{
		PlayHitSparks();
		PresentationSfx.PlayWorldOneShot(this, Vector3.Zero, PresentationSfx.LightningCannonHitPath);
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
			FontSize = _type == EType.VoidBomb ? 44 : 56,
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
			EType.VoidBomb => 0.55f,
			EType.RepurposedMiner => 0.72f,
			EType.Carrier => 1.35f,
			_ => 1.1f,
		};

		var host = GetParent();
		if (host is null)
			return;

		HideHullForDeath();

		var worldPosition = GlobalPosition;
		if (_type == EType.VoidBomb)
		{
			OneShotParticles.Play(
				host,
				worldPosition,
				new Color(1f, 0.42f, 0.12f, 0.9f),
				scale,
				worldSpace: true);
			OneShotParticles.PlayExplosionModel(host, worldPosition, scale * 3.3f);
			return;
		}

		OneShotParticles.PlayShipDestruction(host, worldPosition, scale);
		PresentationSfx.PlayWorldOneShot(
			host,
			worldPosition,
			PresentationSfx.ShipDeathPath,
			PresentationSfx.ShipDeathPitchScale);
	}

	void HideHullForDeath()
	{
		foreach (var child in FindChildren("*", "GeometryInstance3D", true, false))
		{
			if (child is GeometryInstance3D visual)
				visual.Visible = false;
		}
	}

	/// <summary>Shows a simulation-dead unit only until its replay death animation finishes.</summary>
	public void ShowPendingDeath(State state) => Present(state, UnitVisualState.PendingDeath);

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

	private void EnsureHitMark()
	{
		if (_hitMark is not null)
			return;

		var radius = _type switch
		{
			EType.VoidBomb => 0.45f,
			EType.RepurposedMiner => 0.58f,
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

	private void BindShip()
	{
		_hull = ShipMesh.CreateFighterHull();
		AddChild(_hull);
	}

	private void BindCarrier()
	{
		_hull = CarrierMesh.CreateHullInstance();
		AddChild(_hull);
	}

	private void BindGunship()
	{
		_hull = GunshipMesh.CreateHullInstance();
		AddChild(_hull);
	}

	private void BindIndustrialGooper()
	{
		_hull = IndustrialGooperMesh.CreateHullInstance();
		AddChild(_hull);
	}

	private void BindRepurposedMiner()
	{
		_hull = RepurposedMinerMesh.CreateHullInstance();
		AddChild(_hull);
	}

	private void BindVoidBomb()
	{
		_hull = VoidBombMesh.CreateHullInstance();
		AddChild(_hull);
	}

	private void BindShieldBubble(State state)
	{
		_hullBounds = LocalVisualBounds();
		var maxProfile = state.Loadout.MaxShieldPoints;
		foreach (var face in Faces)
		{
			if (maxProfile[face] <= 0)
				continue;

			var instance = new MeshInstance3D
			{
				Name = $"Shield{face}",
				Mesh = ShieldBubbleMesh.CreateFace(_hullBounds, face),
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			};
			PresentationLayers.MarkUx(instance);
			_shieldFaces[(int)face] = instance;
			AddChild(instance);
		}
	}

	private void ApplyShields(State state)
	{
		var hideForGhost = VisualState is
			UnitVisualState.Ghost
			or UnitVisualState.SelectedGhost
			or UnitVisualState.MoveGhost
			or UnitVisualState.SelectedMoveGhost;
		var maxProfile = state.Loadout.MaxShieldPoints;
		foreach (var face in Faces)
		{
			var index = (int)face;
			var instance = _shieldFaces[index];
			if (instance is null)
				continue;

			if (hideForGhost)
			{
				instance.Visible = false;
				continue;
			}

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
		foreach (var child in FindChildren("*", "MeshInstance3D", true, false))
		{
			if (child is not MeshInstance3D { Mesh: { } mesh } instance)
				continue;

			var transform = Transform3D.Identity;
			for (Node3D? node = instance; node is not null && node != this; node = node.GetParent() as Node3D)
				transform = node.Transform * transform;
			var meshBounds = transform * mesh.GetAabb();
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

	private void ApplyOrientation(State state) =>
		Basis = BasisFrom(state);

	private static Quaternion NormalizeRotationQuaternion(Quaternion quaternion) =>
		quaternion.LengthSquared() < Mathf.Epsilon ? Quaternion.Identity : quaternion.Normalized();

	private static Vector3 MovementEndTangent(
		Vector3 chord,
		Vector3 targetPosition,
		Coord? nextPosition)
	{
		if (nextPosition is null)
			return chord * 0.65f;

		var outgoing = WorldMapping.ToWorld(nextPosition.Value) - targetPosition;
		if (chord.LengthSquared() < Mathf.Epsilon || outgoing.LengthSquared() < Mathf.Epsilon)
			return chord * 0.65f;

		var direction = chord.Normalized() + outgoing.Normalized();
		if (direction.LengthSquared() < Mathf.Epsilon)
			return chord * 0.35f;

		var length = Mathf.Min(chord.Length(), outgoing.Length()) * 0.65f;
		return direction.Normalized() * length;
	}

	private static Vector3 Hermite(
		Vector3 start,
		Vector3 end,
		Vector3 startTangent,
		Vector3 endTangent,
		float weight)
	{
		var squared = weight * weight;
		var cubed = squared * weight;
		return start * (2f * cubed - 3f * squared + 1f)
			+ startTangent * (cubed - 2f * squared + weight)
			+ end * (-2f * cubed + 3f * squared)
			+ endTangent * (cubed - squared);
	}

	private static Basis BasisFrom(State state) =>
		new(
			ToVector3(state.Starboard),
			ToVector3(state.Dorsal),
			ToVector3(state.Fore));

	private static float PopupLabelHeight(EType type) =>
		type switch
		{
			EType.VoidBomb => 0.55f,
			EType.RepurposedMiner => 0.82f,
			EType.Carrier => 1.35f,
			_ => 1.2f,
		};

	private static Vector3 ToVector3(Coord coord) =>
		new(coord.X, coord.Y, coord.Z);
}
