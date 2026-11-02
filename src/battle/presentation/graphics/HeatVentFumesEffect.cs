using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class HeatVentFumesEffect : Node3D
{
	public const double DurationSeconds = 0.8;
	private const string TexturePath = "res://assets/vfx/heat_vent_fumes.png";
	private const float VisibleWidthPixels = 113f;
	private const float AnchorOffsetPixels = 55.5f;
	private readonly List<(Sprite3D Sprite, Vector3 Outward)> _puffs = [];

	public void Attach(UnitView ship)
	{
		var texture = GD.Load<Texture2D>(TexturePath)
			?? throw new InvalidOperationException($"Could not load vent fumes '{TexturePath}'.");
		var bounds = ship.HullBounds;
		TopLevel = true;
		GlobalTransform = Transform3D.Identity;
		foreach (var side in new[] { -1f, 1f })
		{
			var localPosition = bounds.GetCenter();
			localPosition.X = side < 0 ? bounds.Position.X : bounds.End.X;
			var sprite = new Sprite3D
			{
				Name = side < 0 ? "PortFumes" : "StarboardFumes",
				Texture = texture,
				Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
				Shaded = false,
				NoDepthTest = false,
				PixelSize = 1.2f / VisibleWidthPixels,
				TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps,
			};
			PresentationLayers.MarkWorld(sprite);
			AddChild(sprite);
			sprite.GlobalPosition = ship.GlobalTransform * localPosition;
			_puffs.Add((sprite, ship.GlobalBasis.X.Normalized() * side));
		}
		UpdateMirroring();
	}

	public void Play()
	{
		foreach (var (sprite, outward) in _puffs)
		{
			var start = sprite.GlobalPosition;
			sprite.Scale = Vector3.One * 0.65f;
			sprite.Modulate = new Color(1, 1, 1, 0);
			var tween = sprite.CreateTween();
			tween.SetParallel();
			tween.TweenProperty(sprite, "global_position",
				start + outward * 0.85f + Vector3.Up * 0.18f, DurationSeconds)
				.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
			tween.TweenProperty(sprite, "scale", Vector3.One * 1.15f, DurationSeconds)
				.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
			tween.TweenProperty(sprite, "modulate:a", 1f, 0.08);
			tween.TweenProperty(sprite, "modulate:a", 0f, 0.48).SetDelay(0.32);
		}
		var sound = PresentationSfx.PlayWorldOneShot(this, Vector3.Zero, PresentationSfx.HeatVentsReleasePath);
		sound.VolumeDb = 0f;
		var lifetime = CreateTween();
		lifetime.TweenInterval(0.6);
		lifetime.TweenProperty(sound, "volume_db", -60f, 0.2);
		lifetime.TweenCallback(Callable.From(QueueFree));
	}

	public override void _Process(double delta) => UpdateMirroring();

	private void UpdateMirroring()
	{
		if (GetViewport().GetCamera3D() is not { } camera)
			return;
		foreach (var (sprite, outward) in _puffs)
		{
			var flipped = outward.Dot(camera.GlobalBasis.X) > 0;
			sprite.FlipH = flipped;
			sprite.Offset = new Vector2(flipped ? AnchorOffsetPixels : -AnchorOffsetPixels, 50f);
		}
	}
}
