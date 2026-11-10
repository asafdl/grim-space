using Godot;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Components;
using GrimSpace.Ui.Components;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Presentation.Facilities;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

/// <summary>Pre-engagement pilot selection; UI-local until submitted through EngageAction.</summary>
public sealed partial class DeploymentSlots : HBoxContainer
{
	private const float SlotSize = 80f;
	private const float DropMargin = 12f;
	// Inner opening of deployment_pilot_frame_blue.png at SlotSize.
	private static readonly Rect2 PortraitRect =
		new(new Vector2(DropMargin + 4f, DropMargin + 8f), new Vector2(SlotSize - 8f, SlotSize - 16f));

	private readonly string?[] _selection = new string?[RunPilotRegistry.MaxDeployedPilots];
	private readonly ShipPicker _shipPicker = new();
	private State? _run;
	private string? _slotDragPilotId;

	public event Action? Changed;

	public DeploymentSlots()
	{
		Name = "DeploymentSlots";
		Alignment = AlignmentMode.Center;
		AddThemeConstantOverride("separation", 8);
		AddChild(_shipPicker);
	}

	public IReadOnlyList<string> DeployedPilotIds =>
		_selection
			.Where(id => id is not null && HasShip(id))
			.Select(id => id!)
			.ToArray();

	/// <summary>Defaults to the last deployment that still has ships, else every crewed pilot.</summary>
	public void Reset(State run)
	{
		_run = run;
		var crewed = run.PilotRegistry.All
			.Where(pilot => pilot.AssignedShipId is not null)
			.Select(pilot => pilot.Id)
			.ToArray();
		var defaults = run.PilotRegistry.LastDeployment.Count > 0
			? run.PilotRegistry.LastDeployment.Where(crewed.Contains)
			: crewed;
		Array.Fill(_selection, null);
		foreach (var (pilotId, index) in defaults.Take(_selection.Length).Select((id, index) => (id, index)))
			_selection[index] = pilotId;
		Refresh();
	}

	public bool IsPilot(Variant data) =>
		data.VariantType == Variant.Type.String
		&& _run is { } run && run.PilotRegistry.TryGet(data.AsString(), out _);

	public Variant BeginDrag(Control source, string pilotId, Vector2 grabPosition, Rect2 portraitRect)
	{
		if (_run is not { } run || !run.PilotRegistry.TryGet(pilotId, out var pilot))
			return default;
		var preview = new Control { MouseFilter = MouseFilterEnum.Ignore };
		preview.AddChild(Image(ShipPilotPortraitCatalog.TextureFor(pilot.PortraitId),
			portraitRect.Position - grabPosition, portraitRect.Size));
		source.SetDragPreview(preview);
		return pilotId;
	}

	public void Deploy(string pilotId)
	{
		if (!_selection.Contains(pilotId) && Array.IndexOf(_selection, null) is var open and >= 0)
			Drop(open, pilotId);
	}

	public void Remove(string pilotId)
	{
		var index = Array.IndexOf(_selection, pilotId);
		if (index >= 0)
			_selection[index] = null;
		Refresh();
	}

	public override void _Notification(int what)
	{
		if (what != NotificationDragEnd || _slotDragPilotId is not { } pilotId)
			return;
		_slotDragPilotId = null;
		if (!GetViewport().GuiIsDragSuccessful())
			Remove(pilotId);
	}

	private bool HasShip(string pilotId) =>
		_run is { } run && run.PilotRegistry.TryGet(pilotId, out var pilot) && pilot.AssignedShipId is not null;

	private void Drop(int index, string pilotId)
	{
		var previous = Array.IndexOf(_selection, pilotId);
		if (previous >= 0)
			_selection[previous] = _selection[index];
		_selection[index] = pilotId;
		Refresh();
	}

	public void Refresh()
	{
		foreach (var child in GetChildren().OfType<Control>())
		{
			RemoveChild(child);
			child.QueueFree();
		}
		var run = _run!;
		var palette = HudPalettes.BlueCyan;
		for (var index = 0; index < _selection.Length; index++)
		{
			var slotIndex = index;
			var host = new Control { CustomMinimumSize = Vector2.One * (SlotSize + DropMargin * 2f) };
			host.SetDragForwarding(
				Callable.From<Vector2, Variant>(position =>
				{
					if (_selection[slotIndex] is not { } id)
						return default;
					_slotDragPilotId = id;
					return BeginDrag(host, id, position, PortraitRect);
				}),
				Callable.From<Vector2, Variant, bool>((_, data) => IsPilot(data)),
				Callable.From<Vector2, Variant>((_, data) => Drop(slotIndex, data.AsString())));
			host.AddChild(ShipBox.Frame(Vector2.One * DropMargin, SlotSize,
				_selection[index] is null ? palette.Border : palette.Secondary));
			if (_selection[index] is { } pilotId && run.PilotRegistry.TryGet(pilotId, out var pilot))
			{
				var portrait = Image(ShipPilotPortraitCatalog.TextureFor(pilot.PortraitId),
					PortraitRect.Position, PortraitRect.Size);
				portrait.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
				host.AddChild(portrait);
				host.AddChild(ShipBadge(run, pilot));
			}
			AddChild(host);
		}
		Changed?.Invoke();
	}

	private HudTooltipHost ShipBadge(State run, Pilot pilot)
	{
		var equipped = pilot.AssignedShipId is { } shipId ? run.ItemInventory.Get(shipId) : null;
		var badge = _shipPicker.CreateReticle(
			equipped,
			() => run.ItemInventory.ShipsOwnedBy(State.PlayerFleetUnitId)
				.Where(ship => ship.Id != pilot.AssignedShipId
					&& OperatorClassCompatibility.CanOperate(pilot.ClassId, ship.Spec.ClassId)),
			ship => ShipRecruitmentDisplay.CompareTooltip(ship, equipped),
			ship =>
			{
				if (!run.TryAssignPilotToShip(pilot.Id, ship.Id, out var failure))
					GD.PushWarning($"Ship assignment rejected: {failure}.");
				Refresh();
			});
		badge.Position = Vector2.One * (DropMargin + SlotSize - 46f);
		return badge;
	}

	private static TextureRect Image(Texture2D texture, Vector2 position, Vector2 size) =>
		new()
		{
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			Texture = texture,
			Position = position,
			Size = size,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Ignore,
		};
}
