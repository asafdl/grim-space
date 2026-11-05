using Godot;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Components;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class TargetOpportunityOverlay : Node2D
{
	public const string LethalIconPath = "res://assets/ui/battle/lethal_hit.svg";
	public static readonly Color LethalIconTint = new(0.96f, 0.94f, 0.86f);

	private const float BadgePx = 32f;
	private const float GlyphPx = 28f;
	private const float VerticalOffsetPx = 34f;
	private const float FanSpacingPx = 16f;

	private Camera3D _camera = null!;
	private IReadOnlyDictionary<string, UnitDisplayState> _previewUnits = new Dictionary<string, UnitDisplayState>();
	private IReadOnlyList<PoseHitOpportunity> _opportunities = [];
	private readonly Dictionary<(string Path, Color Tint, bool TintIcon), Texture2D> _glyphCache = new();
	private readonly List<IconBadge> _activeBadges = [];
	private readonly Stack<IconBadge> _freeBadges = [];

	public void Configure(Camera3D camera) => _camera = camera;

	public void Apply(
		IReadOnlyDictionary<string, UnitDisplayState> previewUnits,
		IReadOnlyList<PoseHitOpportunity> opportunities)
	{
		_previewUnits = previewUnits;
		_opportunities = opportunities;
		Visible = opportunities.Count > 0;
		ReleaseActiveBadges();
		if (Visible)
			UpdateBadges();
	}

	public override void _Process(double delta)
	{
		if (Visible)
			UpdateBadges();
	}

	private void UpdateBadges()
	{
		ReleaseActiveBadges();
		if (!IsInstanceValid(_camera))
			return;

		foreach (var group in _opportunities.GroupBy(opportunity => opportunity.TargetId))
		{
			if (!_previewUnits.TryGetValue(group.Key, out var unit)
				|| (!unit.IsAlive && group.All(opportunity => opportunity.IconPath != LethalIconPath)))
				continue;

			var screenCenter = _camera.UnprojectPosition(WorldMapping.ToWorld(unit.Position));
			var icons = group.Where(opportunity => opportunity.IconPath != LethalIconPath).ToList();
			var count = icons.Count;
			for (var i = 0; i < count; i++)
			{
				var fanOffset = count == 1
					? 0f
					: (i - (count - 1) * 0.5f) * FanSpacingPx;
				ShowBadge(icons[i], screenCenter + new Vector2(fanOffset, -VerticalOffsetPx));
			}

			if (group.FirstOrDefault(opportunity => opportunity.IconPath == LethalIconPath) is { } lethal)
				ShowBadge(lethal, _camera.UnprojectPosition(
					EnemyRangeTetherView.ReticleCenter(WorldMapping.ToWorld(unit.Position), _camera)));
		}
	}

	private void ShowBadge(PoseHitOpportunity opportunity, Vector2 center)
	{
		var badge = AcquireBadge();
		badge.Apply(
			GetGlyph(opportunity.IconPath, opportunity.IconTint, opportunity.TintIcon),
			opportunity.IconTint,
			glowingSkull: opportunity.IconPath == LethalIconPath);
		badge.Position = center - Vector2.One * BadgePx * 0.5f;
		badge.Visible = true;
		_activeBadges.Add(badge);
	}

	private Texture2D GetGlyph(string? iconPath, Color tint, bool tintIcon)
	{
		var key = (iconPath ?? string.Empty, tint, tintIcon);
		if (_glyphCache.TryGetValue(key, out var cached))
			return cached;

		cached = tintIcon
			? SvgIconLoader.Load(iconPath, tint, (int)GlyphPx)
			: SvgIconLoader.LoadRaw(iconPath, (int)GlyphPx);
		_glyphCache[key] = cached;
		return cached;
	}

	private IconBadge AcquireBadge()
	{
		if (_freeBadges.TryPop(out var badge))
			return badge;

		badge = new IconBadge();
		AddChild(badge);
		return badge;
	}

	private void ReleaseActiveBadges()
	{
		foreach (var badge in _activeBadges)
		{
			badge.Visible = false;
			_freeBadges.Push(badge);
		}
		_activeBadges.Clear();
	}

	private sealed partial class IconBadge : PanelContainer
	{
		private readonly TextureRect _glyph;
		private readonly MarginContainer _pad;
		private readonly ShaderMaterial _glowMaterial = new()
		{
			Shader = new Shader
			{
				Code =
					"""
					shader_type canvas_item;

					void fragment() {
						vec2 uv = (UV - vec2(0.5)) * 1.4 + vec2(0.5);
						vec4 glyph = texture(TEXTURE, uv);
						float halo = 0.0;
						for (int i = 0; i < 16; i++) {
							float angle = float(i) * 6.283185 / 16.0;
							vec2 direction = vec2(cos(angle), sin(angle));
							halo += texture(TEXTURE, uv + direction * TEXTURE_PIXEL_SIZE * 2.0).a;
							halo += texture(TEXTURE, uv + direction * TEXTURE_PIXEL_SIZE * 4.0).a * 0.8;
							halo += texture(TEXTURE, uv + direction * TEXTURE_PIXEL_SIZE * 6.0).a * 0.4;
						}
						halo /= 35.2;
						COLOR = vec4(glyph.a > 0.0 ? glyph.rgb : vec3(0.96, 0.94, 0.86),
							glyph.a + (1.0 - glyph.a) * min(halo * 1.4, 0.6));
					}
					""",
			},
		};

		public IconBadge()
		{
			CustomMinimumSize = new Vector2(BadgePx, BadgePx);
			MouseFilter = Control.MouseFilterEnum.Ignore;

			_pad = new MarginContainer();
			AddChild(_pad);

			_glyph = new TextureRect
			{
				CustomMinimumSize = new Vector2(GlyphPx, GlyphPx),
				Size = new Vector2(GlyphPx, GlyphPx),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				MouseFilter = Control.MouseFilterEnum.Ignore,
			};
			_pad.AddChild(_glyph);
		}

		public void Apply(Texture2D glyph, Color accent, bool glowingSkull)
		{
			_glyph.Texture = glyph;
			_glyph.Material = glowingSkull ? _glowMaterial : null;
			_glyph.CustomMinimumSize = Vector2.One * (glowingSkull ? 36f : GlyphPx);
			var padding = glowingSkull ? 2 : 6;
			_pad.AddThemeConstantOverride("margin_left", padding);
			_pad.AddThemeConstantOverride("margin_right", padding);
			_pad.AddThemeConstantOverride("margin_top", padding);
			_pad.AddThemeConstantOverride("margin_bottom", padding);
			if (glowingSkull)
			{
				AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
				return;
			}

			var border = Brighten(accent, 0.2f);
			AddThemeStyleboxOverride("panel", new StyleBoxFlat
			{
				BgColor = new Color(0.04f, 0.06f, 0.1f, 0.95f),
				BorderColor = border,
				BorderWidthLeft = 2,
				BorderWidthTop = 2,
				BorderWidthRight = 2,
				BorderWidthBottom = 2,
				CornerRadiusTopLeft = 8,
				CornerRadiusTopRight = 8,
				CornerRadiusBottomRight = 8,
				CornerRadiusBottomLeft = 8,
				ShadowColor = new Color(0f, 0f, 0f, 0.7f),
				ShadowSize = 4,
			});
		}

		private static Color Brighten(Color color, float amount) =>
			new(
				Mathf.Lerp(color.R, 1f, amount),
				Mathf.Lerp(color.G, 1f, amount),
				Mathf.Lerp(color.B, 1f, amount),
				1f);
	}
}
