using Godot;
using GrimSpace.Components;
using GrimSpace.World.StarSystem.Contracts;

namespace GrimSpace.World.StarSystem.Presentation;

public sealed partial class PoiContractOfferOverlay : Control
{
	private const string BadgePath = "res://assets/ui/map/contract-offer-icon.svg";
	private const float BadgePx = 32f;
	private const float VerticalScreenOffsetPx = 10f;
	private const float WorldClearancePadding = 0.2f;

	private MapCamera _camera = null!;
	private MapView _view = null!;
	private Func<StarMap> _world = null!;
	private Func<bool> _isVisible = null!;
	private Texture2D? _badgeTexture;
	private readonly Dictionary<string, ContractBadge> _badges = new(StringComparer.Ordinal);

	public void Configure(
		MapCamera camera,
		MapView view,
		Func<StarMap> world,
		Func<bool> isVisible)
	{
		_camera = camera;
		_view = view;
		_world = world;
		_isVisible = isVisible;
		_badgeTexture = SvgIconLoader.LoadRaw(BadgePath, (int)BadgePx);
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
	}

	public override void _Process(double _)
	{
		if (_world is null || !_isVisible())
		{
			HideAllBadges();
			return;
		}

		var map = _world();
		var counts = ContractOfferMapIndicators.CountOfferedByIssuerPoi(map);
		var activePoiIds = new HashSet<string>(counts.Keys, StringComparer.Ordinal);

		foreach (var poiId in _badges.Keys.ToArray())
		{
			if (!activePoiIds.Contains(poiId))
				RemoveBadge(poiId);
		}

		foreach (var (poiId, count) in counts)
		{
			var badge = GetOrCreateBadge(poiId);
			var label = $"{count} Contracts available";
			badge.TooltipText = label;
			if (!TryProjectBadgePosition(map, poiId, out var screen))
			{
				badge.Visible = false;
				continue;
			}

			badge.Position = screen - new Vector2(BadgePx * 0.5f, BadgePx + VerticalScreenOffsetPx);
			badge.Visible = true;
		}
	}

	private bool TryProjectBadgePosition(StarMap map, string poiId, out Vector2 screen)
	{
		var clearance = _view.GetIndicatorClearance(poiId, map) + WorldClearancePadding;
		var worldPos = _view.GetPoiWorldPosition(poiId, map.Width, map.Height)
			+ Vector3.Up * clearance;
		return MapScreenAnchor.TryProject(_camera, worldPos, out screen);
	}

	private ContractBadge GetOrCreateBadge(string poiId)
	{
		if (_badges.TryGetValue(poiId, out var badge))
			return badge;

		badge = new ContractBadge(_badgeTexture!);
		AddChild(badge);
		_badges[poiId] = badge;
		return badge;
	}

	private void RemoveBadge(string poiId)
	{
		if (!_badges.Remove(poiId, out var badge))
			return;

		badge.QueueFree();
	}

	private void HideAllBadges()
	{
		foreach (var badge in _badges.Values)
			badge.Visible = false;
	}

	private sealed partial class ContractBadge : TextureRect
	{
		public ContractBadge(Texture2D texture)
		{
			Texture = texture;
			CustomMinimumSize = new Vector2(BadgePx, BadgePx);
			Size = new Vector2(BadgePx, BadgePx);
			ExpandMode = ExpandModeEnum.IgnoreSize;
			StretchMode = StretchModeEnum.KeepAspectCentered;
			MouseFilter = MouseFilterEnum.Stop;
		}
	}
}
