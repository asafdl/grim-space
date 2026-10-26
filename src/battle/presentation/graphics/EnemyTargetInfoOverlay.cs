using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class EnemyTargetInfoOverlay : Control
{
	private const float ReticleScale = 0.5f;
	private const float ReticleRotationRadiansPerSecond = 0.08f;
	private const float RailRadius = 55f;
	private const float RailWidth = 10f;
	private const float RailSpan = Mathf.Pi * 0.5f;
	private const int RailPointCount = 24;
	private const string ReticleTexturePath = "res://assets/ui/battle/enemy_target_reticle.svg";

	private static readonly Vector2 RailCenter = new(120f, 92f);
	private static readonly Color HeatBarColor = new(0.78f, 0.3f, 0.08f);
	private static readonly Color RailUnderlayColor = new(0.01f, 0.03f, 0.05f, 0.96f);
	private static readonly Color EmptyRailColor = new(0.36f, 0.48f, 0.58f, 0.92f);
	private static readonly Color HullColor = new(1f, 0.5f, 0.42f);
	private static readonly Color ShieldColor = new(0.38f, 0.86f, 1f);

	private readonly Sprite2D _reticle;
	private float _hullRatio;
	private float _shieldRatio;
	private float _heatRatio;
	private bool _showsHeat;

	public EnemyTargetInfoOverlay()
	{
		Size = new Vector2(240f, 160f);
		MouseFilter = MouseFilterEnum.Ignore;

		_reticle = new Sprite2D
		{
			Name = "Reticle",
			Texture = GD.Load<Texture2D>(ReticleTexturePath)
				?? throw new InvalidOperationException(
					$"Could not load enemy target reticle '{ReticleTexturePath}'."),
			Position = RailCenter,
			Scale = Vector2.One * ReticleScale,
			Modulate = new Color(0.82f, 0.9f, 0.98f),
		};
		AddChild(_reticle);

		var identity = new Label
		{
			Name = "Identity",
			Position = Vector2.Zero,
			Size = new Vector2(Size.X, 28f),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		identity.AddThemeFontSizeOverride("font_size", 18);
		identity.AddThemeColorOverride(
			"font_color",
			new Color(0.48f, 0.82f, 0.82f, 0.86f));
		identity.AddThemeColorOverride(
			"font_outline_color",
			new Color(0.02f, 0.05f, 0.07f, 0.9f));
		identity.AddThemeConstantOverride("outline_size", 4);
		AddChild(identity);
	}

	public void Set(
		string identity,
		float hullRatio,
		float shieldRatio,
		float heatRatio,
		bool showsHeat)
	{
		GetNode<Label>("Identity").Text = identity;
		_hullRatio = Mathf.Clamp(hullRatio, 0f, 1f);
		_shieldRatio = Mathf.Clamp(shieldRatio, 0f, 1f);
		_heatRatio = Mathf.Clamp(heatRatio, 0f, 1f);
		_showsHeat = showsHeat;
		QueueRedraw();
	}

	public override void _Process(double delta) =>
		_reticle.Rotate(ReticleRotationRadiansPerSecond * (float)delta);

	public override void _Draw()
	{
		const float shieldStart = -Mathf.Pi * 0.5f;
		const float hullStart = Mathf.Pi * 0.5f;
		DrawRail(shieldStart, _shieldRatio, ShieldColor);
		DrawRail(hullStart, _hullRatio, HullColor);
		if (_showsHeat)
			DrawRail(0f, _heatRatio, HeatBarColor);
	}

	private void DrawRail(float start, float ratio, Color color)
	{
		var end = start + RailSpan;
		DrawArc(
			RailCenter,
			RailRadius,
			start,
			end,
			RailPointCount,
			RailUnderlayColor,
			RailWidth + 5f,
			true);
		DrawArc(
			RailCenter,
			RailRadius,
			start,
			end,
			RailPointCount,
			EmptyRailColor,
			RailWidth,
			true);
		if (ratio <= 0f)
			return;

		DrawArc(
			RailCenter,
			RailRadius,
			start,
			Mathf.Lerp(start, end, ratio),
			RailPointCount,
			color,
			RailWidth,
			true);
	}
}
