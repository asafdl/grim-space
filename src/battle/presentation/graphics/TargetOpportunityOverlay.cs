using Godot;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Components;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class TargetOpportunityOverlay : Node2D
{
	private const float BadgePx = 32f;
	private const float GlyphPx = 28f;
	private const float VerticalOffsetPx = 34f;
	private const float FanSpacingPx = 16f;

	private Camera3D _camera = null!;
	private IReadOnlyDictionary<string, UnitDisplayState> _previewUnits = new Dictionary<string, UnitDisplayState>();
	private IReadOnlyList<PoseHitOpportunity> _opportunities = [];
	private readonly Dictionary<string, Texture2D> _glyphCache = new(StringComparer.Ordinal);
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
			if (!_previewUnits.TryGetValue(group.Key, out var unit) || !unit.IsAlive)
				continue;

			var screenCenter = _camera.UnprojectPosition(WorldMapping.ToWorld(unit.Position));
			var icons = group.ToList();
			var count = icons.Count;
			for (var i = 0; i < count; i++)
			{
				var opportunity = icons[i];
				var fanOffset = count == 1
					? 0f
					: (i - (count - 1) * 0.5f) * FanSpacingPx;
				var badge = AcquireBadge();
				badge.Apply(GetGlyph(opportunity.IconPath), opportunity.IconTint);
				badge.Position = screenCenter
					+ new Vector2(fanOffset - BadgePx * 0.5f, -VerticalOffsetPx - BadgePx * 0.5f);
				badge.Visible = true;
				_activeBadges.Add(badge);
			}
		}
	}

	private Texture2D GetGlyph(string? iconPath)
	{
		var key = iconPath ?? string.Empty;
		if (_glyphCache.TryGetValue(key, out var cached))
			return cached;

		cached = SvgIconLoader.Load(iconPath, Colors.White, (int)GlyphPx);
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

		public IconBadge()
		{
			CustomMinimumSize = new Vector2(BadgePx, BadgePx);
			MouseFilter = Control.MouseFilterEnum.Ignore;

			var pad = new MarginContainer();
			pad.AddThemeConstantOverride("margin_left", 6);
			pad.AddThemeConstantOverride("margin_right", 6);
			pad.AddThemeConstantOverride("margin_top", 6);
			pad.AddThemeConstantOverride("margin_bottom", 6);
			AddChild(pad);

			_glyph = new TextureRect
			{
				CustomMinimumSize = new Vector2(GlyphPx, GlyphPx),
				Size = new Vector2(GlyphPx, GlyphPx),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				MouseFilter = Control.MouseFilterEnum.Ignore,
			};
			pad.AddChild(_glyph);
		}

		public void Apply(Texture2D glyph, Color accent)
		{
			_glyph.Texture = glyph;
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
