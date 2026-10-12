using Godot;
using GrimSpace.World.StarSystem.Presentation.Picking;
using GrimSpace.Components;
using GrimSpace.World.StarSystem.Contracts;

using GrimSpace.World.StarSystem.Presentation.Camera;

namespace GrimSpace.World.StarSystem.Presentation.Map;

public sealed partial class PoiContractOverlay : Control
{
	private const string ContractBadgePath = "res://assets/ui/map/contract-offer-icon.svg";
	private const string LeadBadgePath = "res://assets/ui/map/lead-contact-icon.svg";
	private const float BadgePx = 32f;
	private const float BadgeGap = 4f;
	private const float VerticalScreenOffsetPx = 10f;
	private const float WorldClearancePadding = 0.2f;

	private MapCamera _camera = null!;
	private MapView _view = null!;
	private Func<StarMap> _world = null!;
	private Func<bool> _isVisible = null!;
	private Texture2D? _contractBadgeTexture;
	private Texture2D? _leadBadgeTexture;
	private readonly Dictionary<string, RoleBadge> _contractBadges = new(StringComparer.Ordinal);
	private readonly Dictionary<string, RoleBadge> _leadBadges = new(StringComparer.Ordinal);

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
		_contractBadgeTexture = SvgIconLoader.LoadRaw(ContractBadgePath, (int)BadgePx);
		_leadBadgeTexture = SvgIconLoader.LoadRaw(LeadBadgePath, (int)BadgePx);
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
		var contractCounts = ContractMapIndicators.CountPendingByPresenterPoi(map);
		var leadCounts = LeadMapIndicators.CountPendingByPresenterPoi(map);
		SyncBadgeLayer(
			contractCounts,
			_contractBadges,
			_contractBadgeTexture!,
			poiId => ContractMapIndicators.TooltipForPoi(map, poiId));
		SyncBadgeLayer(
			leadCounts,
			_leadBadges,
			_leadBadgeTexture!,
			poiId => LeadMapIndicators.TooltipForPoi(map, poiId));

		var activePoiIds = contractCounts.Keys
			.Concat(leadCounts.Keys)
			.ToHashSet(StringComparer.Ordinal);
		foreach (var poiId in activePoiIds)
		{
			if (!TryProjectBadgePosition(map, poiId, out var screen))
			{
				HideBadge(_contractBadges, poiId);
				HideBadge(_leadBadges, poiId);
				continue;
			}

			var hasContract = contractCounts.ContainsKey(poiId);
			var hasLead = leadCounts.ContainsKey(poiId);
			var centerX = screen.X;
			var topY = screen.Y - BadgePx - VerticalScreenOffsetPx;
			if (hasContract && hasLead)
			{
				var rowLeft = centerX - (BadgePx * 2f + BadgeGap) * 0.5f;
				PlaceBadge(_contractBadges[poiId], rowLeft, topY);
				PlaceBadge(_leadBadges[poiId], rowLeft + BadgePx + BadgeGap, topY);
			}
			else if (hasContract)
				PlaceBadge(_contractBadges[poiId], centerX - BadgePx * 0.5f, topY);
			else
				PlaceBadge(_leadBadges[poiId], centerX - BadgePx * 0.5f, topY);
		}
	}

	private void SyncBadgeLayer(
		Dictionary<string, int> counts,
		Dictionary<string, RoleBadge> badges,
		Texture2D texture,
		Func<string, string> tooltip)
	{
		foreach (var poiId in badges.Keys.ToArray())
		{
			if (!counts.ContainsKey(poiId))
				RemoveBadge(badges, poiId);
		}

		foreach (var poiId in counts.Keys)
		{
			var badge = GetOrCreateBadge(badges, poiId, texture);
			badge.TooltipText = tooltip(poiId);
		}
	}

	private static void PlaceBadge(RoleBadge badge, float leftX, float topY)
	{
		badge.Position = new Vector2(leftX, topY);
		badge.Visible = true;
	}

	private static void HideBadge(Dictionary<string, RoleBadge> badges, string poiId)
	{
		if (badges.TryGetValue(poiId, out var badge))
			badge.Visible = false;
	}

	private bool TryProjectBadgePosition(StarMap map, string poiId, out Vector2 screen)
	{
		var clearance = _view.GetIndicatorClearance(poiId, map) + WorldClearancePadding;
		var worldPos = _view.GetPoiWorldPosition(poiId, map.Width, map.Height)
			+ Vector3.Up * clearance;
		return MapScreenAnchor.TryProject(_camera, worldPos, out screen);
	}

	private RoleBadge GetOrCreateBadge(
		Dictionary<string, RoleBadge> badges,
		string poiId,
		Texture2D texture)
	{
		if (badges.TryGetValue(poiId, out var badge))
			return badge;

		badge = new RoleBadge(texture);
		AddChild(badge);
		badges[poiId] = badge;
		return badge;
	}

	private static void RemoveBadge(Dictionary<string, RoleBadge> badges, string poiId)
	{
		if (!badges.Remove(poiId, out var badge))
			return;

		badge.QueueFree();
	}

	private void HideAllBadges()
	{
		foreach (var badge in _contractBadges.Values)
			badge.Visible = false;
		foreach (var badge in _leadBadges.Values)
			badge.Visible = false;
	}

	private sealed partial class RoleBadge : TextureRect
	{
		public RoleBadge(Texture2D texture)
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
