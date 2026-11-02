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
	private const string DeliveryBadgePath = "res://assets/ui/map/delivery-target-icon.png";
	private static readonly Color DeliveryBadgeTint = new(0x4a / 255f, 0xd5 / 255f, 0xf0 / 255f);
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
	private Texture2D? _deliveryBadgeTexture;
	private readonly Dictionary<string, RoleBadge> _contractBadges = new(StringComparer.Ordinal);
	private readonly Dictionary<string, RoleBadge> _leadBadges = new(StringComparer.Ordinal);
	private readonly Dictionary<string, RoleBadge> _deliveryBadges = new(StringComparer.Ordinal);
	private readonly Dictionary<string, RoleBadge> _meetingBadges = new(StringComparer.Ordinal);

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
		_deliveryBadgeTexture = SvgIconLoader.Load(DeliveryBadgePath, DeliveryBadgeTint, (int)BadgePx);
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
		var deliveryCounts = DeliveryMapIndicators.CountActiveByPresenterPoi(map);
		var meetings = DeliveryMapIndicators.ActiveMeetings(map);
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
		SyncBadgeLayer(
			deliveryCounts,
			_deliveryBadges,
			_deliveryBadgeTexture!,
			poiId => DeliveryMapIndicators.TooltipForPoi(map, poiId));
		SyncMeetingBadges(meetings);

		var activePoiIds = contractCounts.Keys
			.Concat(leadCounts.Keys)
			.Concat(deliveryCounts.Keys)
			.ToHashSet(StringComparer.Ordinal);
		foreach (var poiId in activePoiIds)
		{
			if (!TryProjectBadgePosition(map, poiId, out var screen))
			{
				HideBadge(_contractBadges, poiId);
				HideBadge(_leadBadges, poiId);
				HideBadge(_deliveryBadges, poiId);
				continue;
			}

			PlacePoiRow(
				screen,
				contractCounts.ContainsKey(poiId) ? _contractBadges[poiId] : null,
				leadCounts.ContainsKey(poiId) ? _leadBadges[poiId] : null,
				deliveryCounts.ContainsKey(poiId) ? _deliveryBadges[poiId] : null);
		}

		foreach (var meeting in meetings)
		{
			if (!TryProjectMeetingPosition(map, meeting, out var screen))
			{
				HideBadge(_meetingBadges, meeting.MeetingId);
				continue;
			}

			PlaceBadge(
				_meetingBadges[meeting.MeetingId],
				screen.X - BadgePx * 0.5f,
				screen.Y - BadgePx - VerticalScreenOffsetPx);
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
			HudTooltipCard.SetTooltip(badge, tooltip(poiId));
		}
	}

	private void SyncMeetingBadges(IReadOnlyList<DeliveryMapIndicators.MeetingMarker> meetings)
	{
		var activeIds = meetings
			.Select(meeting => meeting.MeetingId)
			.ToHashSet(StringComparer.Ordinal);
		foreach (var meetingId in _meetingBadges.Keys.ToArray())
		{
			if (!activeIds.Contains(meetingId))
				RemoveBadge(_meetingBadges, meetingId);
		}

		foreach (var meeting in meetings)
		{
			var badge = GetOrCreateBadge(_meetingBadges, meeting.MeetingId, _deliveryBadgeTexture!);
			HudTooltipCard.SetTooltip(badge, DeliveryMapIndicators.TooltipForCount(1));
		}
	}

	private static void PlacePoiRow(
		Vector2 anchor,
		RoleBadge? contract,
		RoleBadge? lead,
		RoleBadge? delivery)
	{
		var count = (contract is null ? 0 : 1) + (lead is null ? 0 : 1) + (delivery is null ? 0 : 1);
		var rowLeft = anchor.X - (BadgePx * count + BadgeGap * (count - 1)) * 0.5f;
		var topY = anchor.Y - BadgePx - VerticalScreenOffsetPx;
		var index = 0;
		if (contract is not null)
			PlaceBadge(contract, rowLeft + index++ * (BadgePx + BadgeGap), topY);
		if (lead is not null)
			PlaceBadge(lead, rowLeft + index++ * (BadgePx + BadgeGap), topY);
		if (delivery is not null)
			PlaceBadge(delivery, rowLeft + index * (BadgePx + BadgeGap), topY);
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

	private bool TryProjectMeetingPosition(
		StarMap map,
		DeliveryMapIndicators.MeetingMarker meeting,
		out Vector2 screen)
	{
		var clearance = _view.GetIndicatorClearance(meeting.MeetingId, map) + WorldClearancePadding;
		var worldPos = MapMapping.ToWorld(meeting.Position, map.Width, map.Height)
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
		foreach (var badge in _deliveryBadges.Values)
			badge.Visible = false;
		foreach (var badge in _meetingBadges.Values)
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
