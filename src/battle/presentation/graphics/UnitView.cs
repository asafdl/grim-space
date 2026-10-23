using Godot;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class UnitView : Node3D
{
	private static readonly FontFile DamagePopupFont = GD.Load<FontFile>(
		"res://assets/ui/fonts/AnakManja.ttf")
		?? throw new InvalidOperationException("Could not load the damage popup font.");
	private static readonly Color ShieldDamageColor = new(0.04f, 0.72f, 1f);
	private static readonly Color HullDamageColor = new(1f, 0.06f, 0.015f);
	private static readonly ESpatialOrientation[] Faces = Enum.GetValues<ESpatialOrientation>();

	private MeshInstance3D? _hull;
	private Node3D _visualRoot = null!;
	private EType _type;
	private readonly int[] _shieldPoints = new int[Faces.Length];
	private readonly MeshInstance3D?[] _shieldFaces = new MeshInstance3D?[Faces.Length];
	private Aabb _hullBounds;
	private Tween? _poseTween;
	private Tween? _shakeTween;
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
		_shakeTween?.Kill();
		_shakeTween = null;
		base._ExitTree();
	}

	public void Bind(State state, Color color)
	{
		Name = state.Id;
		_type = state.Type;
		_bindColor = color;
		Array.Fill(_shieldPoints, -1);
		_visualRoot = new Node3D { Name = "VisualRoot" };
		AddChild(_visualRoot);

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

	public void PlayHitSparks()
	{
		OneShotParticles.PlayHitSparks(this, Vector3.Zero);
		PlayHitShake();
	}

	public void PlayImpactSparks(int shieldDamage, int hullDamage, bool lightning)
	{
		var hasShieldDamage = shieldDamage > 0;
		var hasHullDamage = hullDamage > 0;
		if (shieldDamage > 0)
			OneShotParticles.PlayHitSparks(this, Vector3.Zero);
		if (hullDamage > 0)
			OneShotParticles.PlayHullHitSparks(this, Vector3.Zero);
		if (lightning)
			PresentationSfx.PlayWorldOneShot(this, Vector3.Zero, PresentationSfx.LightningCannonHitPath);
		if (hasShieldDamage || hasHullDamage)
			PlayHitShake();
	}

	public void PlayLightningHitSparks()
	{
		PlayImpactSparks(1, 0, lightning: true);
	}

	private void PlayHitShake()
	{
		_shakeTween?.Kill();
		_visualRoot.Position = Vector3.Zero;
		var strength = _type == EType.Carrier ? 0.18f : 0.12f;
		_shakeTween = CreateTween();
		_shakeTween.TweenProperty(
			_visualRoot,
			"position",
			new Vector3(strength, 0f, 0f),
			0.045)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
		_shakeTween.TweenProperty(
			_visualRoot,
			"position",
			new Vector3(-strength * 0.75f, 0f, 0f),
			0.07)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		_shakeTween.TweenProperty(
			_visualRoot,
			"position",
			new Vector3(strength * 0.4f, 0f, 0f),
			0.065)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		_shakeTween.TweenProperty(
			_visualRoot,
			"position",
			Vector3.Zero,
			0.08)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
	}

	public void PlayDamagePopup(int shieldDamage, int hullDamage)
	{
		var hasShieldDamage = shieldDamage > 0;
		var hasHullDamage = hullDamage > 0;
		var splitPopups = hasShieldDamage && hasHullDamage;
		if (hasShieldDamage)
			AddDamagePopup(shieldDamage, ShieldDamageColor, splitPopups ? -0.62f : 0f);
		if (hasHullDamage)
			AddDamagePopup(hullDamage, HullDamageColor, splitPopups ? 0.62f : 0f);
	}

	private void AddDamagePopup(int damage, Color color, float horizontalOffset)
	{
		var tiltDirection = damage % 2 == 0 ? -1f : 1f;
		var popup = new Node3D
		{
			Name = "DamagePopup",
			Position = new Vector3(horizontalOffset, PopupLabelHeight(_type) + 0.45f, 0f),
		};
		var label = new Label3D
		{
			Text = $"-{damage}",
			Rotation = new Vector3(0f, 0f, tiltDirection * 0.16f),
			Scale = Vector3.One * 0.78f,
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			Font = DamagePopupFont,
			FontSize = _type == EType.VoidBomb ? 104 : 132,
			OutlineSize = 20,
			Modulate = color,
			OutlineModulate = new Color(0.025f, 0.035f, 0.075f),
		};
		AddChild(popup);
		popup.AddChild(label);

		var floatTween = CreateTween();
		floatTween.TweenProperty(popup, "position", popup.Position + Vector3.Up * 0.9f, 1.35)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
		floatTween.TweenCallback(Callable.From(popup.QueueFree));

		var animationTween = CreateTween();
		animationTween.SetParallel(true);
		animationTween.TweenProperty(label, "scale", Vector3.One * 1.12f, 0.22)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);
		animationTween.TweenProperty(label, "modulate", Colors.Transparent, 1.1)
			.SetDelay(0.2)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.In);

		var wobbleTween = CreateTween();
		wobbleTween.TweenProperty(label, "position", new Vector3(0.85f, 0f, 0f), 0.05)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);
		wobbleTween.TweenProperty(label, "position", new Vector3(-0.68f, 0f, 0f), 0.07);
		wobbleTween.TweenProperty(label, "position", new Vector3(0.48f, 0f, 0f), 0.09);
		wobbleTween.TweenProperty(label, "position", new Vector3(-0.3f, 0f, 0f), 0.11);
		wobbleTween.TweenProperty(label, "position", new Vector3(0.15f, 0f, 0f), 0.13);
		wobbleTween.TweenProperty(label, "position", Vector3.Zero, 0.18)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
		wobbleTween.Parallel().TweenProperty(label, "rotation", Vector3.Zero, 0.55)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
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
			OneShotParticles.PlaySmokeExplosion(
				host,
				worldPosition,
				scale);
			return;
		}

		OneShotParticles.PlayShipDestruction(host, worldPosition, scale);
		PresentationSfx.PlayWorldOneShot(
			host,
			worldPosition,
			PresentationSfx.ShipDeathPath,
			PresentationSfx.ShipDeathPitchScale);
	}

	void HideHullForDeath() => _visualRoot.Visible = false;

	/// <summary>Shows a simulation-dead unit only until its replay death animation finishes.</summary>
	public void ShowPendingDeath(State state) => Present(state, UnitVisualState.PendingDeath);

	private void ApplyPose(State state)
	{
		Position = WorldMapping.ToWorld(state.Position);
		ApplyOrientation(state);
		ApplyShields(state);
	}

	private void BindShip()
	{
		_hull = ShipMesh.CreateFighterHull();
		_visualRoot.AddChild(_hull);
	}

	private void BindCarrier()
	{
		_hull = CarrierMesh.CreateHullInstance();
		_visualRoot.AddChild(_hull);
	}

	private void BindGunship()
	{
		_hull = GunshipMesh.CreateHullInstance();
		_visualRoot.AddChild(_hull);
	}

	private void BindIndustrialGooper()
	{
		_hull = IndustrialGooperMesh.CreateHullInstance();
		_visualRoot.AddChild(_hull);
	}

	private void BindRepurposedMiner()
	{
		_hull = RepurposedMinerMesh.CreateHullInstance();
		_visualRoot.AddChild(_hull);
	}

	private void BindVoidBomb()
	{
		_hull = VoidBombMesh.CreateHullInstance();
		_visualRoot.AddChild(_hull);
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
			_visualRoot.AddChild(instance);
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
			var temporary = TemporaryShields.TotalOnFace(state, face);
			var points = System.Math.Clamp(state.ShieldPoints[face] + temporary, 0, maxOnFace + temporary);
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
