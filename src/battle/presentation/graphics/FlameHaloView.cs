using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class FlameHaloView : Node3D
{
	private const int FrameCount = 6;
	private const float TextureHeight = 384f;
	private const float PixelSize = 0.006f;
	private const float FlameScale = 0.5f;
	private const float FlameWidth = 0.43f;
	private const float FramesPerSecond = 10f;
	private const float WarmupVisualCap = 0.28f;
	private static readonly Color WarmupFlame = new(0.12f, 0.78f, 1f);
	private static readonly Color OverdriveFlame = new(1f, 0.42f, 0.1f);
	private static readonly Color OverheatFlame = new(0.04f, 0.58f, 1f);
	private static Texture2D[]? _frames;

	private Node3D _flameRoot = null!;
	private Sprite3D _flame = null!;
	private Sprite3D _core = null!;
	private Sprite3D _glow = null!;
	private StandardMaterial3D _flameMaterial = null!;
	private StandardMaterial3D _coreMaterial = null!;
	private StandardMaterial3D _glowMaterial = null!;
	private Tween? _heatTween;
	private float _displayedHeat;
	private float _targetHeat;
	private float _elapsed;
	private float _frameElapsed;
	private float _worldUpOffset;
	private int _frameIndex;
	private bool _overdrive;
	private bool _overheat;
	private bool _active;

	public void AttachAbove(float worldUpOffset)
	{
		_worldUpOffset = worldUpOffset;
		TopLevel = true;
	}

	public override void _Ready()
	{
		_frames ??= LoadFrames();
		_flameRoot = new Node3D { Name = "FlameRoot" };
		AddChild(_flameRoot);
		_glowMaterial = CreateFlameMaterial(_frames[0], alpha: 0.2f, emissionEnergy: 5f);
		_glow = CreateSprite(_frames[0]);
		_glow.Name = "FlameGlow";
		_glow.Scale = Vector3.One * 1.8f;
		_glow.MaterialOverride = _glowMaterial;
		_flameRoot.AddChild(_glow);

		_flameMaterial = CreateFlameMaterial(_frames[0], alpha: 0.32f, emissionEnergy: 3.2f);
		_flame = CreateSprite(_frames[0]);
		_flame.Name = "AnimatedFlame";
		_flame.MaterialOverride = _flameMaterial;
		_flameRoot.AddChild(_flame);

		_coreMaterial = CreateFlameMaterial(_frames[0], alpha: 0.24f, emissionEnergy: 2.4f);
		_core = CreateSprite(_frames[0]);
		_core.Name = "FlameCore";
		_core.Scale = new Vector3(0.58f, 0.78f, 1f);
		_core.Position = Vector3.Up * 0.055f;
		_core.MaterialOverride = _coreMaterial;
		_flameRoot.AddChild(_core);
		UpdateFlame();
	}

	public override void _ExitTree()
	{
		_heatTween?.Kill();
		_heatTween = null;
		base._ExitTree();
	}

	public override void _Process(double delta)
	{
		FollowHost();
		_elapsed += (float)delta;
		if (_active)
		{
			_frameElapsed += (float)delta;
			if (_frameElapsed >= 1f / FramesPerSecond)
			{
				var frameSteps = Mathf.FloorToInt(_frameElapsed * FramesPerSecond);
				_frameElapsed -= frameSteps / FramesPerSecond;
				_frameIndex = (_frameIndex + frameSteps) % FrameCount;
				var frames = _frames
					?? throw new InvalidOperationException("Flame frames were not loaded.");
				_flame.Texture = frames[_frameIndex];
				_core.Texture = frames[_frameIndex];
				_glow.Texture = frames[_frameIndex];
				_flameMaterial.AlbedoTexture = frames[_frameIndex];
				_flameMaterial.EmissionTexture = frames[_frameIndex];
				_coreMaterial.AlbedoTexture = frames[_frameIndex];
				_coreMaterial.EmissionTexture = frames[_frameIndex];
				_glowMaterial.AlbedoTexture = frames[_frameIndex];
				_glowMaterial.EmissionTexture = frames[_frameIndex];
			}
		}
		UpdateFlame();
	}

	private void FollowHost()
	{
		if (!IsInsideTree() || GetParent() is not Node3D host || !host.IsInsideTree())
			return;

		GlobalTransform = new Transform3D(
			Basis.Identity,
			host.GlobalPosition + Vector3.Up * _worldUpOffset);
	}

	public void SetHeat(int heat, int capacity, bool overdrive, bool overheat, bool visible)
	{
		var nextActive = visible && capacity > 0 && (heat > 0 || overdrive || overheat);
		var heatRatio = capacity > 0
			? Mathf.Clamp(heat / (float)capacity, 0f, 1f)
			: 0f;
		var nextTarget = overdrive
			? 1f
			: overheat
				? Mathf.Max(heatRatio, 0.55f)
				: heatRatio * WarmupVisualCap;
		if (_active == nextActive
			&& _overdrive == overdrive
			&& _overheat == overheat
			&& Mathf.IsEqualApprox(_targetHeat, nextTarget))
			return;

		var enteredOverdrive = overdrive && !_overdrive;
		_overdrive = overdrive;
		_overheat = overheat;
		_active = nextActive;
		_targetHeat = nextTarget;

		if (!IsInsideTree())
		{
			_displayedHeat = _targetHeat;
			return;
		}

		_heatTween?.Kill();
		_heatTween = CreateTween();
		var tween = _heatTween.TweenMethod(
			Callable.From<float>(value =>
			{
				_displayedHeat = value;
				UpdateFlame();
			}),
			_displayedHeat,
			_targetHeat,
			enteredOverdrive ? 0.48 : overheat ? 0.8 : 0.55);
		if (enteredOverdrive)
			tween.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		else
			tween.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
	}

	private static Sprite3D CreateSprite(Texture2D texture) =>
		new()
		{
			Texture = texture,
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			Shaded = false,
			PixelSize = PixelSize,
			Centered = true,
		};

	private static StandardMaterial3D CreateFlameMaterial(
		Texture2D texture,
		float alpha,
		float emissionEnergy)
	{
		var material = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
			BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
			BillboardKeepScale = true,
			AlbedoTexture = texture,
			AlbedoColor = new Color(1f, 1f, 1f, alpha),
			EmissionEnabled = true,
			EmissionTexture = texture,
			EmissionEnergyMultiplier = emissionEnergy,
		};
		return material;
	}

	private void UpdateFlame()
	{
		if (!_active || _displayedHeat <= 0.005f)
		{
			Visible = false;
			return;
		}

		Visible = true;
		if (_flameRoot is null || _flame is null || _glow is null)
			return;

		var flicker = 0.97f + 0.03f * Mathf.Sin(_elapsed * (_overdrive ? 14f : 8f));
		var height = PixelSize * TextureHeight * FlameScale * _displayedHeat * flicker;
		_flameRoot.Scale = new Vector3(FlameWidth, FlameScale * _displayedHeat * flicker, 1f);
		_flameRoot.Position = Vector3.Up * (height * 0.5f);
		_glow.Scale = Vector3.One * (_overdrive ? 2.15f : 1.55f);

		var color = FlameColor();
		var overdrivePunch = _overdrive
			? Mathf.Clamp(Mathf.InverseLerp(WarmupVisualCap, 1f, _displayedHeat), 0f, 1f)
			: 0f;
		_flameMaterial.AlbedoColor = new Color(color.R, color.G, color.B, 0.28f + 0.1f * overdrivePunch);
		_flameMaterial.Emission = color;
		_flameMaterial.EmissionEnergyMultiplier = 3.2f + 2.4f * overdrivePunch;
		var core = color.Lerp(Colors.White, 0.22f + 0.28f * overdrivePunch);
		_coreMaterial.AlbedoColor = new Color(core.R, core.G, core.B, 0.22f + 0.1f * overdrivePunch);
		_coreMaterial.Emission = core;
		_coreMaterial.EmissionEnergyMultiplier = 2.4f + 2.2f * overdrivePunch;
		_glowMaterial.AlbedoColor = new Color(color.R, color.G, color.B, 0.16f + 0.12f * overdrivePunch);
		_glowMaterial.Emission = color;
		_glowMaterial.EmissionEnergyMultiplier = 5f + 3.5f * overdrivePunch;
	}

	private Color FlameColor()
	{
		if (_overheat)
			return OverheatFlame;
		if (!_overdrive)
			return WarmupFlame;

		var punch = Mathf.Clamp(
			Mathf.InverseLerp(WarmupVisualCap, 1f, _displayedHeat),
			0f,
			1f);
		return WarmupFlame.Lerp(OverdriveFlame, punch);
	}

	private static Texture2D[] LoadFrames()
	{
		var frames = new Texture2D[FrameCount];
		for (var index = 0; index < frames.Length; index++)
		{
			var path = $"res://assets/vfx/flame_halo/flame_{index + 1:00}.png";
			frames[index] = GD.Load<Texture2D>(path)
				?? throw new InvalidOperationException($"Could not load flame animation frame '{path}'.");
		}

		return frames;
	}
}
