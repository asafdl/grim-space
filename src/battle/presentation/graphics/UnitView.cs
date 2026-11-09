using Godot;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Statuses;
using GrimSpace.Components;
using GrimSpace.Math.Grid;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class UnitView : Node3D
{
	public const double ShieldChangeSeconds = 0.52;
	private const double ShieldGlowRiseSeconds = 0.12;
	private static readonly FontFile DamagePopupFont = GD.Load<FontFile>(
		"res://assets/ui/fonts/AnakManja.ttf")
		?? throw new InvalidOperationException("Could not load the damage popup font.");
	private static readonly Texture2D HullDamageIcon = GD.Load<Texture2D>(
		"res://assets/ui/battle/hull.png")
		?? throw new InvalidOperationException("Could not load the hull damage icon.");
	private static readonly Texture2D ShieldDamageIcon = GD.Load<Texture2D>(
		"res://assets/ui/battle/shield.png")
		?? throw new InvalidOperationException("Could not load the shield damage icon.");
	private static readonly HudTooltipIconDef HealPopupIcon = HudTooltipIconCatalog.Resolve("Heal");
	private static readonly Texture2D HealPopupIconTexture = GD.Load<Texture2D>(HealPopupIcon.Path)
		?? throw new InvalidOperationException("Could not load the heal popup icon.");
	private static readonly Color ShieldDamageColor = new(0.04f, 0.72f, 1f);
	private static readonly Color HullDamageColor = new(1f, 0.06f, 0.015f);
	private static readonly ESpatialOrientation[] Faces = Enum.GetValues<ESpatialOrientation>();

	private MeshInstance3D? _hull;
	private Node3D _visualRoot = null!;
	private Node3D _idleRoot = null!;
	private double _idleCycleTime;
	private double _idleRollTime = -1;
	private bool _replayActive;
	private bool CanAnimateIdle => VisualState == UnitVisualState.Live && _visualRoot.Visible
		&& _type != EType.VoidBomb && !_replayActive && _poseTween?.IsRunning() != true
		&& _shakeTween?.IsRunning() != true && _overchargeTween?.IsRunning() != true;
	private EType _type;
	private readonly int[] _shieldPoints = new int[Faces.Length];
	private readonly Color[] _shieldTints = new Color[Faces.Length];
	private readonly MeshInstance3D?[] _shieldFaces = new MeshInstance3D?[Faces.Length];
	private readonly ShaderMaterial?[] _shieldLinks = new ShaderMaterial?[Faces.Length];
	private readonly Tween?[] _shieldGlowTweens = new Tween?[Faces.Length];
	private AudioStreamPlayer3D? _shieldHum;
	private Aabb _hullBounds;
	private Tween? _poseTween;
	private PixelTeleportEffect? _teleport;
	private Tween? _shakeTween;
	private Color _bindColor = Colors.White;
	private Color _shieldColor;
	private ShaderMaterial? _ghostMaterial;
	private readonly Dictionary<GeometryInstance3D, ShaderMaterial> _moveGhostMaterials = [];
	private MovementTrailView? _movementTrail;
	private Vector3? _movementTangent;
	private FlameHaloView? _flameHalo;
	private ProtectionDroneView? _protectionDrone;
	private RamChargeEffect? _ramCharge;
	private bool _ramAnimating;
	private Sprite3D? _ramBillboard;
	private AudioStreamPlayer3D? _ramLaunchSound;
	private AudioStreamPlayer3D? _ramImpactSound;
	private RamChargeEffect? _overchargeGlow;
	private Tween? _overchargeTween;
	private AudioStreamPlayer3D? _overchargeSound;
	private AudioStreamPlayer3D? _movementSound;
	private Tween? _movementSoundTween;
	private bool _movementSoundFading;

	public UnitVisualState VisualState { get; private set; } = UnitVisualState.Hidden;
	public Aabb HullBounds => _hullBounds;

	public override void _Process(double delta)
	{
		if (_idleRoot is null)
			return;
		if (VisualState != UnitVisualState.Live || !_visualRoot.Visible || _type == EType.VoidBomb)
		{
			_idleRoot.Transform = Transform3D.Identity;
			_idleRollTime = -1;
			return;
		}

		var idle = CanAnimateIdle;
		var hover = Vector3.Zero;
		var angle = 0f;
		if (idle)
		{
			_idleCycleTime += delta;
			if (_idleCycleTime >= 4)
			{
				_idleCycleTime %= 4;
				if (_idleRollTime < 0 && GD.Randf() < 0.01f)
					_idleRollTime = 0;
			}
			var phase = (float)(_idleCycleTime / 4) * Mathf.Tau;
			var wave = Mathf.Sin(phase);
			hover = new Vector3(wave * 0.06f, wave * wave * 0.04f, 0);
			angle = -Mathf.Cos(phase) * Mathf.DegToRad(2.5f);
			if (_idleRollTime >= 0)
			{
				_idleRollTime += delta;
				var progress = (float)System.Math.Min(_idleRollTime / 3, 1);
				var envelope = Mathf.Sin(Mathf.Pi * progress);
				hover += new Vector3(
					0.1f * Mathf.Sin(Mathf.Tau * progress) * envelope,
					0.05f * envelope * envelope,
					0);
				angle += Mathf.Tau * progress * progress * (3 - 2 * progress);
				if (progress >= 1)
					_idleRollTime = -1;
			}
		}
		else
			_idleRollTime = -1;

		var blend = (float)(1 - System.Math.Exp(-12 * delta));
		var rotation = _idleRoot.Basis.GetRotationQuaternion()
			.Slerp(new Quaternion(Vector3.Back, angle), blend).Normalized();
		var basis = new Basis(rotation);
		var center = _hullBounds.GetCenter();
		var currentHover = _idleRoot.Position - center + _idleRoot.Basis * center;
		var offset = currentHover.Lerp(hover, blend);
		_idleRoot.Transform = new Transform3D(basis, center - basis * center + offset);
	}

	public void SetReplayActive(bool active) => _replayActive = active;

	public bool TryPlayIdleBarrelRoll()
	{
		if (!CanAnimateIdle || _idleRollTime >= 0)
			return false;
		_idleRollTime = 0;
		return true;
	}

	public override void _ExitTree()
	{
		_teleport?.Stop();
		StopMovementSound();
		StopEngineOvercharge();
		StopRamAnimation();
		StopShieldAnimation();
		_poseTween?.Kill();
		_poseTween = null;
		_shakeTween?.Kill();
		_shakeTween = null;
		base._ExitTree();
	}

	public void Bind(State state, Color color, Color? shieldColor = null)
	{
		Name = state.Id;
		_type = state.Type;
		_bindColor = color;
		_shieldColor = shieldColor ?? ShieldFaceMaterials.TeamTint(null);
		Array.Fill(_shieldPoints, -1);
		_idleCycleTime = GD.Randf() * 4;
		_idleRoot = new Node3D { Name = "IdleMotion" };
		AddChild(_idleRoot);
		_visualRoot = new Node3D { Name = "VisualRoot" };
		_idleRoot.AddChild(_visualRoot);

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
			BindShip(state.Type);

		ApplyGearTierHullTint(state);
		BindShieldBubble(state);
		if (state.HeatProfile.Participates)
		{
			_flameHalo = new FlameHaloView { Name = "FlameHalo" };
			AddChild(_flameHalo);
			_flameHalo.AttachAbove(_hullBounds.End.Y + 0.32f);
		}

		Sync(state);
	}

	/// <summary>Synchronizes authoritative unit state; dead units are hidden.</summary>
	public void Sync(State state) =>
		Present(state, state.IsAlive ? UnitVisualState.Live : UnitVisualState.Hidden);

	public void Present(State state, UnitVisualState visualState)
	{
		_teleport?.Stop();
		StopMovementSound(immediate: visualState != UnitVisualState.Live);
		StopEngineOvercharge();
		StopRamAnimation();
		_poseTween?.Kill();
		_poseTween = null;
		_movementTangent = null;
		SetMovementTrailEmitting(false);
		VisualState = visualState;
		if (visualState != UnitVisualState.Live)
		{
			_idleRoot.Transform = Transform3D.Identity;
			_idleRollTime = -1;
		}
		Visible = visualState != UnitVisualState.Hidden;
		UpdateHeatIndicator(state);
		if (!Visible)
		{
			ClearProtectionDrone();
			return;
		}

		ApplyPose(state);
		ApplyVisualState();
		if (visualState == UnitVisualState.PendingDeath)
		{
			if (IsInstanceValid(_protectionDrone))
				_protectionDrone!.FadeOut();
		}
		else if (visualState == UnitVisualState.Live && HasProtectionDrone(state))
			EnsureProtectionDrone().SnapToHover();
		else
			ClearProtectionDrone();
	}

	public void HideVisual()
	{
		_teleport?.Stop();
		StopMovementSound();
		StopEngineOvercharge();
		StopRamAnimation();
		ClearHeatVentFumes();
		ClearProtectionDrone();
		StopShieldAnimation();
		_poseTween?.Kill();
		_poseTween = null;
		_movementTangent = null;
		SetMovementTrailEmitting(false);
		VisualState = UnitVisualState.Hidden;
		_idleRoot.Transform = Transform3D.Identity;
		_idleRollTime = -1;
		Visible = false;
		ClearHeatIndicator();
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

	public void PlayMovementSound(double duration)
	{
		_movementSoundTween?.Kill();
		_movementSoundFading = false;
		if (!IsInstanceValid(_movementSound))
			_movementSound = PresentationSfx.PlayShipMovementLoop(this);

		var sound = _movementSound!;
		_movementSoundTween = CreateTween();
		var attack = System.Math.Min(0.08, duration);
		_movementSoundTween.TweenProperty(sound, "volume_linear", Mathf.DbToLinear(-12f), attack);
		_movementSoundTween.TweenInterval(duration - attack);
		_movementSoundTween.TweenCallback(Callable.From(() => StopMovementSound(immediate: false)));
	}

	public void StopMovementSound(bool immediate = true)
	{
		if (!immediate && _movementSoundFading)
			return;
		_movementSoundTween?.Kill();
		_movementSoundTween = null;
		_movementSoundFading = false;
		if (IsInstanceValid(_movementSound))
		{
			if (!immediate)
			{
				_movementSoundFading = true;
				_movementSoundTween = CreateTween();
				_movementSoundTween.TweenProperty(_movementSound, "volume_linear", 0f, 0.18)
					.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
				_movementSoundTween.TweenCallback(Callable.From(() => StopMovementSound()));
				return;
			}
			_movementSound!.Stop();
			_movementSound.QueueFree();
		}
		_movementSound = null;
	}

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
		UpdateHeatIndicator(state);
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
		UpdateHeatIndicator(state);
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

	public void PlayImpactSparks(
		int shieldDamage,
		int hullDamage,
		bool lightning,
		ESpatialOrientation hitFace)
	{
		var hasShieldDamage = shieldDamage > 0;
		var hasHullDamage = hullDamage > 0;
		var exitFace = UnitFaceLocalGeometry.SparkExitFace(hitFace);
		var emitNormal = UnitFaceLocalGeometry.OutwardNormal(exitFace);
		var emitPoint = UnitFaceLocalGeometry.EmitPointOnFace(_hullBounds, exitFace);
		var emitBasis = UnitFaceLocalGeometry.BasisWithLocalYAxis(emitNormal);
		if (shieldDamage > 0)
			OneShotParticles.PlayHitSparks(this, emitPoint, emitBasis);
		if (hullDamage > 0)
		{
			OneShotParticles.PlayHullHitSparks(this, emitPoint, emitBasis);
			var host = GetParent()
				?? throw new InvalidOperationException("Hull hit effects require a unit scene parent.");
			var hullSize = Mathf.Max(_hullBounds.Size.X, Mathf.Max(_hullBounds.Size.Y, _hullBounds.Size.Z));
			OneShotParticles.PlayHullFragments(
				host,
				GlobalTransform * new Transform3D(emitBasis, emitPoint),
				hullSize,
				hullDamage);
		}
		if (lightning)
			PresentationSfx.PlayWorldOneShot(this, emitPoint, PresentationSfx.LightningCannonHitPath);
		if (hasShieldDamage || hasHullDamage)
			PlayHitShake();
	}

	public void PlayLightningHitSparks(ESpatialOrientation hitFace = ESpatialOrientation.Forward)
	{
		PlayImpactSparks(1, 0, lightning: true, hitFace);
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
			AddFloatingStatPopup(
				$"-{shieldDamage}",
				ShieldDamageColor,
				ShieldDamageIcon,
				tintIcon: true,
				splitPopups ? -0.62f : 0f);
		if (hasHullDamage)
			AddFloatingStatPopup(
				$"-{hullDamage}",
				HullDamageColor,
				HullDamageIcon,
				tintIcon: true,
				splitPopups ? 0.62f : 0f);
	}

	public void PlayHealPopup(int shieldRestored, int hullRestored)
	{
		var hasShieldHeal = shieldRestored > 0;
		var hasHullHeal = hullRestored > 0;
		var splitPopups = hasShieldHeal && hasHullHeal;
		if (hasShieldHeal)
			AddFloatingStatPopup(
				$"+{shieldRestored}",
				HealPopupIcon.Color,
				HealPopupIconTexture,
				HealPopupIcon.TintIcon,
				splitPopups ? -0.62f : 0f);
		if (hasHullHeal)
			AddFloatingStatPopup(
				$"+{hullRestored}",
				HealPopupIcon.Color,
				HealPopupIconTexture,
				HealPopupIcon.TintIcon,
				splitPopups ? 0.62f : 0f);
	}

	private void AddFloatingStatPopup(
		string text,
		Color color,
		Texture2D iconTexture,
		bool tintIcon,
		float horizontalOffset)
	{
		var amountDigits = text.TrimStart('+', '-');
		var tiltDirection = int.TryParse(amountDigits, out var amount) && amount % 2 == 0 ? -1f : 1f;
		var fontSize = _type == EType.VoidBomb ? 104 : 132;
		var popup = new Node3D
		{
			Name = "StatPopup",
			Position = new Vector3(horizontalOffset, PopupLabelHeight(_type) + 0.45f, 0f),
		};
		var viewport = CreateStatPopupViewport(text, color, iconTexture, tintIcon, fontSize);
		var label = new Sprite3D
		{
			Name = "Label",
			Texture = viewport.GetTexture(),
			Rotation = new Vector3(0f, 0f, tiltDirection * 0.16f),
			Scale = Vector3.One * 0.78f,
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			Shaded = false,
			Transparent = true,
			Centered = true,
			PixelSize = 0.005f,
			TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear,
		};
		AddChild(popup);
		popup.AddChild(viewport);
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

	private static SubViewport CreateStatPopupViewport(
		string text,
		Color color,
		Texture2D iconTexture,
		bool tintIcon,
		int fontSize)
	{
		const int outlineSize = 20;
		const int gap = 10;
		var iconPx = Mathf.RoundToInt(fontSize * 0.92f);
		var textSize = DamagePopupFont.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize);
		var pad = outlineSize + 2;
		var width = Mathf.CeilToInt(pad + textSize.X + gap + iconPx + pad);
		var height = Mathf.CeilToInt(pad + Mathf.Max(textSize.Y, iconPx) + pad);

		var viewport = new SubViewport
		{
			Name = "StatPopupViewport",
			Size = new Vector2I(width, height),
			TransparentBg = true,
			Disable3D = true,
			RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
		};
		var row = new HBoxContainer
		{
			Name = "Row",
			Position = Vector2.Zero,
			Size = new Vector2(width, height),
			Alignment = BoxContainer.AlignmentMode.Center,
		};
		row.AddThemeConstantOverride("separation", gap);

		var label = new Label
		{
			Text = text,
			VerticalAlignment = VerticalAlignment.Center,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		label.AddThemeFontOverride("font", DamagePopupFont);
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeColorOverride("font_outline_color", new Color(0.025f, 0.035f, 0.075f));
		label.AddThemeConstantOverride("outline_size", outlineSize);

		var icon = new TextureRect
		{
			Texture = iconTexture,
			CustomMinimumSize = new Vector2(iconPx, iconPx),
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			Modulate = tintIcon ? color : Colors.White,
		};

		row.AddChild(label);
		row.AddChild(icon);
		viewport.AddChild(row);
		return viewport;
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

	void HideHullForDeath()
	{
		StopMovementSound();
		StopEngineOvercharge();
		StopRamAnimation();
		ClearHeatVentFumes();
		StopShieldAnimation();
		_visualRoot.Visible = false;
		_idleRoot.Transform = Transform3D.Identity;
		_idleRollTime = -1;
		ClearHeatIndicator();
	}

	/// <summary>Shows a simulation-dead unit only until its replay death animation finishes.</summary>
	public void ShowPendingDeath(State state) => Present(state, UnitVisualState.PendingDeath);

	private void ApplyPose(State state)
	{
		Position = WorldMapping.ToWorld(state.Position);
		ApplyOrientation(state);
		ApplyShields(state);
	}

	/// <summary>Updates heat VFX without interrupting pose animation.</summary>
	public void RefreshHeat(State state) => UpdateHeatIndicator(state);

	private void ClearHeatIndicator() =>
		_flameHalo?.SetHeat(0, 0, overdrive: false, overheat: false, visible: false);

	private void UpdateHeatIndicator(State state)
	{
		// FlameHalo is TopLevel and lives outside _visualRoot, so it must not stay
		// active through PendingDeath / HideHullForDeath.
		var showOnUnit = VisualState == UnitVisualState.Live;
		_flameHalo?.SetHeat(
			state.Heat,
			state.HeatProfile.EffectiveCapacity,
			state.Overdrive,
			state.Overheat,
			showOnUnit);
	}

	private void BindShip(EType chassis)
	{
		_hull = ShipMesh.CreateHull(chassis);
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

	private void ApplyGearTierHullTint(State state)
	{
		if (_hull is null)
			return;
		if (!ShipGearTierTint.TryGet(
			ShipLoadoutGearTier.FromLoadout(state.Loadout, state.Type),
			out var tint))
			return;

		foreach (var child in _hull.FindChildren("*", "MeshInstance3D", true, false))
		{
			if (child is not MeshInstance3D { Mesh: { } mesh } instance)
				continue;

			for (var surface = 0; surface < mesh.GetSurfaceCount(); surface++)
			{
				if (instance.GetActiveMaterial(surface) is not BaseMaterial3D source)
					continue;

				var material = (BaseMaterial3D)source.Duplicate();
				material.AlbedoColor *= tint;
				instance.SetSurfaceOverrideMaterial(surface, material);
			}
		}
	}

	private void BindShieldBubble(State state)
	{
		_hullBounds = LocalVisualBounds();
		var maxProfile = state.Loadout.BaseShieldPoints;
		foreach (var face in Faces)
		{
			if (maxProfile[face] <= 0)
				continue;

			var instance = new MeshInstance3D
			{
				Name = $"Shield{face}",
				Mesh = ShieldBubbleMesh.CreateFace(_hullBounds, face),
				MaterialOverride = ShieldFaceMaterials.Create(_hullBounds, _shieldColor, face),
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			};
			PresentationLayers.MarkUx(instance);
			var link = ShieldBubbleMesh.CreatePowerLink(_hullBounds, face, _shieldColor);
			PresentationLayers.MarkUx(link);
			instance.AddChild(link);
			_shieldLinks[(int)face] = (ShaderMaterial)link.MaterialOverride;
			_shieldFaces[(int)face] = instance;
			_visualRoot.AddChild(instance);
		}
	}

	public const double EngineOverchargeSeconds = 0.8;

	public void AnimateEngineOvercharge()
	{
		StopEngineOvercharge();
		var glow = new RamChargeEffect { Name = "EngineOverchargeGlow" };
		_visualRoot.AddChild(glow);
		glow.BuildEngineGlow(_hullBounds);
		_overchargeGlow = glow;
		var sound = PresentationSfx.PlayWorldOneShot(this, Vector3.Zero, PresentationSfx.RamLaunchPath);
		sound.VolumeDb = -6f;
		_overchargeSound = sound;
		_overchargeTween = CreateTween();
		_overchargeTween.TweenMethod(Callable.From<float>(seconds =>
			glow.SetCharge(seconds)), 0f, (float)RamChargeEffect.RevealSeconds, 0.45);
		_overchargeTween.TweenInterval(0.1);
		_overchargeTween.TweenMethod(Callable.From<float>(fade =>
		{
			foreach (var mesh in glow.GetChildren().OfType<MeshInstance3D>())
				mesh.Transparency = fade;
		}), 0f, 1f, 0.25)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		_overchargeTween.TweenCallback(Callable.From(() =>
		{
			glow.QueueFree();
			_overchargeGlow = null;
			_overchargeTween = null;
		}));
		var fade = sound.CreateTween();
		fade.TweenInterval(0.35);
		fade.TweenMethod(Callable.From<float>(gain =>
			sound.VolumeDb = -6f + Mathf.LinearToDb(Mathf.Max(gain, 0.0001f))), 1f, 0f, 0.8)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		fade.TweenCallback(Callable.From(sound.QueueFree));
	}

	public void StopEngineOvercharge()
	{
		_overchargeTween?.Kill();
		_overchargeTween = null;
		if (IsInstanceValid(_overchargeGlow))
		{
			_overchargeGlow!.Visible = false;
			_overchargeGlow.QueueFree();
		}
		_overchargeGlow = null;
		if (IsInstanceValid(_overchargeSound))
		{
			_overchargeSound!.Stop();
			_overchargeSound.QueueFree();
		}
		_overchargeSound = null;
	}

	public void AnimateTeleport(State destination)
	{
		var departure = Position;
		Sync(destination);
		Position = departure;
		_idleRoot.Transform = Transform3D.Identity;
		if (_hull is null)
			throw new InvalidOperationException("Teleport requires a bound hull.");
		if (_teleport is null)
		{
			_teleport = new PixelTeleportEffect { Name = "PixelTeleport" };
			AddChild(_teleport);
		}
		var arrival = destination.Clone();
		_teleport.Play(
			_hull,
			() => ApplyPose(arrival),
			_shieldFaces.OfType<MeshInstance3D>()
				.Where(face => face.Visible)
				.SelectMany(face => face.GetChildren().OfType<MeshInstance3D>().Prepend(face)));
	}

	public void AnimateRam(State destination)
	{
		StopMovementSound();
		StopRamAnimation();
		_poseTween?.Kill();
		_shakeTween?.Kill();
		_visualRoot.Position = Vector3.Zero;
		_movementTangent = null;
		SetMovementTrailEmitting(false);
		var charge = new RamChargeEffect { Name = "RamCharge" };
		_visualRoot.AddChild(charge);
		charge.Build(_hullBounds);
		_ramCharge = charge;
		_ramAnimating = true;
		var start = Position;
		var end = WorldMapping.ToWorld(destination.Position);
		var buildup = RamChargeEffect.RevealSeconds + RamChargeEffect.HoldSeconds;
		_ramLaunchSound = PresentationSfx.PlayWorldOneShot(this, Vector3.Zero, PresentationSfx.RamLaunchPath);
		var engine = _ramLaunchSound;
		var engineFade = engine.CreateTween();
		engineFade.TweenInterval(buildup - 0.3);
		engineFade.TweenMethod(Callable.From<float>(gain =>
			engine.VolumeDb = Mathf.LinearToDb(Mathf.Max(gain, 0.0001f))), 1f, 0f, 0.8)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		engineFade.TweenCallback(Callable.From(engine.QueueFree));
		_poseTween = CreateTween();
		_poseTween.TweenMethod(Callable.From<float>(seconds =>
		{
			charge.SetCharge(seconds);
			var strength = Mathf.Min(1, seconds / (float)RamChargeEffect.RevealSeconds);
			_visualRoot.Position = Vector3.Right * (0.055f * strength * Mathf.Sin(seconds * Mathf.Tau * 18f));
		}), 0f, (float)buildup, buildup);
		_poseTween.TweenCallback(Callable.From(() =>
		{
			charge.Visible = false;
			charge.QueueFree();
			_ramCharge = null;
			_visualRoot.Position = Vector3.Zero;
			SetMovementTrailEmitting(true);
		}));
		_poseTween.TweenProperty(this, "position", end, RamChargeEffect.DashSeconds)
			.From(start).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
		_poseTween.TweenCallback(Callable.From(() =>
		{
			Position = end;
			PlayRamBillboard();
			_ramImpactSound = PresentationSfx.PlayWorldOneShot(this, Vector3.Zero, PresentationSfx.RamImpactPath);
			_ramImpactSound.VolumeDb = 3f;
			SetMovementTrailEmitting(false);
			_ramAnimating = false;
			_poseTween = null;
		}));
	}

	public void StopRamAnimation()
	{
		if (IsInstanceValid(_ramImpactSound))
		{
			_ramImpactSound!.Stop();
			_ramImpactSound.QueueFree();
		}
		_ramImpactSound = null;
		if (IsInstanceValid(_ramLaunchSound))
		{
			_ramLaunchSound!.Stop();
			_ramLaunchSound.QueueFree();
		}
		_ramLaunchSound = null;
		if (IsInstanceValid(_ramBillboard))
		{
			_ramBillboard!.Visible = false;
			_ramBillboard.QueueFree();
		}
		_ramBillboard = null;
		if (!_ramAnimating)
			return;
		_poseTween?.Kill();
		_poseTween = null;
		if (IsInstanceValid(_ramCharge))
		{
			_ramCharge!.Visible = false;
			_ramCharge.QueueFree();
		}
		_ramCharge = null;
		_ramAnimating = false;
		_visualRoot.Position = Vector3.Zero;
		SetMovementTrailEmitting(false);
	}

	private void PlayRamBillboard()
	{
		const string path = "res://assets/vfx/ram_baam.png";
		var sprite = new Sprite3D
		{
			Name = "RamBaam",
			Texture = GD.Load<Texture2D>(path)
				?? throw new InvalidOperationException($"Could not load Ram artwork '{path}'."),
			Position = new Vector3(_hullBounds.GetCenter().X, _hullBounds.End.Y + 0.85f,
				_hullBounds.GetCenter().Z),
			PixelSize = 2.4f / 714f,
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			Shaded = false,
			NoDepthTest = false,
			TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps,
			Scale = Vector3.One * 0.75f,
		};
		PresentationLayers.MarkWorld(sprite);
		AddChild(sprite);
		_ramBillboard = sprite;
		var tween = sprite.CreateTween();
		tween.TweenProperty(sprite, "scale", Vector3.One, 0.08)
			.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		tween.TweenInterval(0.12);
		tween.TweenProperty(sprite, "modulate:a", 0f, 0.2);
		tween.TweenCallback(Callable.From(() =>
		{
			if (_ramBillboard == sprite)
				_ramBillboard = null;
			sprite.QueueFree();
		}));
	}

	public void PlayHeatVentFumes()
	{
		var effect = new HeatVentFumesEffect { Name = "HeatVentFumes" };
		AddChild(effect);
		effect.Attach(this);
		effect.Play();
	}

	public void ClearHeatVentFumes()
	{
		foreach (var effect in GetChildren().OfType<HeatVentFumesEffect>())
		{
			effect.Visible = false;
			effect.ProcessMode = ProcessModeEnum.Disabled;
			effect.QueueFree();
		}
	}

	public double AnimateProtectionDrone(State state, Vector3? launchOrigin)
	{
		if (state.IsAlive && HasProtectionDrone(state))
		{
			var drone = EnsureProtectionDrone();
			if (launchOrigin is { } origin)
			{
				drone.LaunchFrom(origin);
				return ProtectionDroneView.FlightSeconds;
			}
			drone.SnapToHover();
			return 0;
		}
		if (!IsInstanceValid(_protectionDrone))
			return 0;
		_protectionDrone!.FadeOut();
		return ProtectionDroneView.FadeSeconds;
	}

	private static bool HasProtectionDrone(State state) =>
		state.Statuses.Exists(status => status.InstanceId ==
			BattleStatuses.BuffInstanceId(nameof(EAbilityKind.ProtectionDrone), state.Id));

	private ProtectionDroneView EnsureProtectionDrone()
	{
		if (IsInstanceValid(_protectionDrone) && !_protectionDrone!.IsQueuedForDeletion())
			return _protectionDrone;
		_protectionDrone = new ProtectionDroneView { Name = "ProtectionDrone" };
		AddChild(_protectionDrone);
		_protectionDrone.Attach(this, new Vector3(
			_hullBounds.End.X + 0.6f, _hullBounds.End.Y + 0.3f, _hullBounds.GetCenter().Z));
		return _protectionDrone;
	}

	private void ClearProtectionDrone()
	{
		if (IsInstanceValid(_protectionDrone))
		{
			_protectionDrone!.Visible = false;
			_protectionDrone.QueueFree();
		}
		_protectionDrone = null;
	}

	public double AnimateShields(State state)
	{
		if (!state.IsAlive || VisualState != UnitVisualState.Live)
			return 0;
		if (!ApplyShields(state, animate: true))
			return 0;

		if (IsInstanceValid(_shieldHum))
			_shieldHum!.QueueFree();
		_shieldHum = PresentationSfx.PlayShieldChangeHum(this, ShieldChangeSeconds);
		return ShieldChangeSeconds;
	}

	private void StopShieldAnimation()
	{
		for (var i = 0; i < _shieldGlowTweens.Length; i++)
		{
			_shieldGlowTweens[i]?.Kill();
			_shieldGlowTweens[i] = null;
			if (_shieldFaces[i] is { } face)
				face.Scale = Vector3.One;
		}
		if (IsInstanceValid(_shieldHum))
			_shieldHum!.QueueFree();
		_shieldHum = null;
	}

	private bool ApplyShields(State state, bool animate = false)
	{
		if (!animate)
			StopShieldAnimation();
		var hideForGhost = VisualState is
			UnitVisualState.Ghost
			or UnitVisualState.SelectedGhost
			or UnitVisualState.MoveGhost
			or UnitVisualState.SelectedMoveGhost;
		var maxProfile = state.Loadout.BaseShieldPoints;
		var changed = false;
		var depleted = false;
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
			var points = System.Math.Max(0, state.CurrentShieldPoints[face] + temporary);
			var previous = _shieldPoints[index];
			var material = instance.MaterialOverride as ShaderMaterial
				?? throw new InvalidOperationException($"Shield {face} requires its energy shader material.");
			var link = _shieldLinks[index]!;
			var tint = ShieldFaceMaterials.TintFor(_shieldColor, state.CurrentShieldPoints[face], maxOnFace, temporary);
			if (previous == points && _shieldTints[index] == tint && animate)
				continue;

			_shieldGlowTweens[index]?.Kill();
			_shieldGlowTweens[index] = null;
			instance.Scale = Vector3.One;
			_shieldPoints[index] = points;
			_shieldTints[index] = tint;
			if (!animate || previous < 0 || previous == points)
			{
				instance.Visible = points > 0;
				material.SetShaderParameter("team_color", tint);
				material.SetShaderParameter("opacity", points > 0 ? 1f : 0f);
				material.SetShaderParameter("pulse_strength", 0f);
				link.SetShaderParameter("team_color", tint);
				link.SetShaderParameter("opacity", points > 0 ? 1f : 0f);
				continue;
			}

			changed = true;
			depleted |= previous > 0 && points == 0;
			instance.Visible = true;
			var pulse = points > previous
				? new Color(0.32f, 0.85f, 1f)
				: new Color(1f, 0.5f, 0.12f);
			material.SetShaderParameter("team_color", tint);
			material.SetShaderParameter("pulse_color", pulse);
			material.SetShaderParameter("pulse_strength", 0f);
			link.SetShaderParameter("team_color", tint);
			if (previous == 0)
				material.SetShaderParameter("opacity", 0f);
			var initialOpacity = (float)material.GetShaderParameter("opacity");
			var finalOpacity = points > 0 ? 1f : 0f;
			var changeFraction = Mathf.Clamp(
				(float)System.Math.Abs(points - previous) / System.Math.Max(1, maxOnFace), 0f, 1f);
			var amplitude = Mathf.Lerp(0.012f, 0.06f, Mathf.Sqrt(changeFraction));
			var vibrate = Callable.From<float>(seconds =>
			{
				var progress = seconds / (float)ShieldChangeSeconds;
				var envelope = Mathf.Sin(Mathf.Pi * progress) * (1f - progress);
				instance.Scale = Vector3.One * (
					1f + amplitude * envelope * Mathf.Sin(Mathf.Tau * 14f * seconds));
			});
			var tween = CreateTween();
			_shieldGlowTweens[index] = tween;
			tween.SetTrans(Tween.TransitionType.Sine);
			tween.SetEase(Tween.EaseType.InOut);
			var setPulse = Callable.From<float>(value => material.SetShaderParameter("pulse_strength", value));
			var setOpacity = Callable.From<float>(value =>
			{
				material.SetShaderParameter("opacity", value);
				link.SetShaderParameter("opacity", value);
			});
			tween.TweenMethod(setPulse, 0f, 4f, ShieldGlowRiseSeconds);
			tween.Parallel().TweenMethod(setOpacity, initialOpacity, 1f, ShieldGlowRiseSeconds);
			tween.Parallel().TweenMethod(vibrate, 0f, (float)ShieldGlowRiseSeconds, ShieldGlowRiseSeconds);
			tween.TweenMethod(setPulse, 4f, 0f, ShieldChangeSeconds - ShieldGlowRiseSeconds);
			tween.Parallel().TweenMethod(setOpacity, 1f, finalOpacity, ShieldChangeSeconds - ShieldGlowRiseSeconds);
			tween.Parallel().TweenMethod(vibrate, (float)ShieldGlowRiseSeconds, (float)ShieldChangeSeconds,
				ShieldChangeSeconds - ShieldGlowRiseSeconds);
			tween.TweenCallback(Callable.From(() =>
			{
				instance.Scale = Vector3.One;
				instance.Visible = points > 0;
				_shieldGlowTweens[index] = null;
			}));
		}
		if (depleted)
			PresentationSfx.PlayWorldOneShot(this, Vector3.Zero, PresentationSfx.ShieldPowerDownPath);
		return changed;
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
