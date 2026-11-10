using Godot;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Components;
using GrimSpace.Ui.Components;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Presentation.Facilities;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

/// <summary>Ship reticle plus the 4×2 tier-sorted picker it opens on right-click.</summary>
public sealed partial class ShipPicker : PopupPanel
{
	private const int Columns = 4;
	private const int Cells = 8;
	private const float CellSize = 48f;
	public const float ReticleSize = 72f;

	private readonly GridContainer _grid = new() { Columns = Columns };
	private readonly Control _previous;
	private readonly Control _next;
	private readonly Label _page = new() { HorizontalAlignment = HorizontalAlignment.Center };
	private ShipInstance[] _ships = [];
	private Func<ShipInstance, HudTooltipContent> _tooltip = _ => HudTooltipContent.Plain("");
	private Action<ShipInstance> _picked = _ => { };
	private int _pageIndex;

	public ShipPicker()
	{
		Exclusive = false;
		Unresizable = true;
		TransparentBg = true;
		AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
		var gridHeight = CellSize * (Cells / Columns);
		_previous = HudCycleArrow.Create(-1, gridHeight, "Previous page", () => ShowPage(_pageIndex - 1));
		_next = HudCycleArrow.Create(1, gridHeight, "Next page", () => ShowPage(_pageIndex + 1));
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 6);
		row.AddChild(_previous);
		row.AddChild(_grid);
		row.AddChild(_next);
		var column = new VBoxContainer();
		column.AddChild(row);
		column.AddChild(_page);
		AddChild(column);
	}

	private int PageCount => System.Math.Max(1, (_ships.Length + Cells - 1) / Cells);

	/// <summary>Reticle showing <paramref name="ship"/>; right-click opens this picker.</summary>
	public HudTooltipHost CreateReticle(
		ShipInstance? ship,
		Func<IEnumerable<ShipInstance>> candidates,
		Func<ShipInstance, HudTooltipContent> tooltip,
		Action<ShipInstance> picked)
	{
		var reticle = new HudTooltipHost
		{
			MouseFilter = Control.MouseFilterEnum.Pass,
			Size = Vector2.One * ReticleSize,
			CustomMinimumSize = Vector2.One * ReticleSize,
		};
		reticle.AddChild(new UiReticle(UiReticleVisual.Cyan)
		{
			SizeOverride = new UiSizeOverride(ReticleSize, ReticleSize),
			Size = Vector2.One * ReticleSize,
		});
		if (ship is not null)
		{
			var backing = new Panel
			{
				Position = Vector2.One * 13f,
				Size = Vector2.One * 46f,
				MouseFilter = Control.MouseFilterEnum.Ignore,
			};
			backing.AddThemeStyleboxOverride("panel", new StyleBoxFlat
			{
				BgColor = HudPalettes.BlueCyan.DeepSurface,
				CornerRadiusTopLeft = 23,
				CornerRadiusTopRight = 23,
				CornerRadiusBottomLeft = 23,
				CornerRadiusBottomRight = 23,
			});
			reticle.AddChild(backing);
			reticle.MoveChild(backing, 0);
			reticle.AddChild(new TextureRect
			{
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				Texture = UnitPortraitCatalog.For(ship.Spec.Chassis)
					?? throw new InvalidOperationException("Missing ship portrait."),
				Position = Vector2.One * 10f,
				Size = Vector2.One * 52f,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				MouseFilter = Control.MouseFilterEnum.Ignore,
			});
			reticle.AddChild(new ShipHealthArcs(ship));
			reticle.SetTooltip(ShipRecruitmentDisplay.ShipTooltip(ship));
		}
		reticle.GuiInput += @event =>
		{
			if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right })
				return;
			reticle.AcceptEvent();
			Open(
				reticle,
				candidates().OrderByDescending(ship => ShipLoadoutGearTier.FromLoadout(ship.Loadout, ship.Spec.Chassis)),
				tooltip,
				picked);
		};
		return reticle;
	}

	/// <summary>Opens above <paramref name="anchor"/>, listing <paramref name="candidates"/> in the given order.</summary>
	public void Open(
		Control anchor,
		IEnumerable<ShipInstance> candidates,
		Func<ShipInstance, HudTooltipContent> tooltip,
		Action<ShipInstance> picked)
	{
		_ships = candidates.ToArray();
		_tooltip = tooltip;
		_picked = picked;
		ShowPage(0);
		var size = GetContentsMinimumSize();
		var anchorRect = anchor.GetGlobalRect();
		Popup(new Rect2I(
			new Vector2I(
				Mathf.RoundToInt(anchorRect.GetCenter().X - size.X / 2f),
				Mathf.RoundToInt(anchorRect.Position.Y - size.Y)),
			new Vector2I(Mathf.CeilToInt(size.X), Mathf.CeilToInt(size.Y))));
	}

	private void ShowPage(int pageIndex)
	{
		_pageIndex = System.Math.Clamp(pageIndex, 0, PageCount - 1);
		_previous.Modulate = _pageIndex == 0 ? new Color(1f, 1f, 1f, 0.25f) : Colors.White;
		_next.Modulate = _pageIndex == PageCount - 1 ? new Color(1f, 1f, 1f, 0.25f) : Colors.White;
		_page.Text = $"{_pageIndex + 1} / {PageCount}";
		foreach (var child in _grid.GetChildren())
		{
			_grid.RemoveChild(child);
			child.QueueFree();
		}
		for (var index = 0; index < Cells; index++)
		{
			var shipIndex = _pageIndex * Cells + index;
			var ship = shipIndex < _ships.Length ? _ships[shipIndex] : null;
			var cell = new ShipBox(ship, CellSize);
			if (ship is not null)
			{
				cell.SetTooltip(_tooltip(ship));
				cell.GuiInput += @event =>
				{
					if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
						return;
					cell.AcceptEvent();
					Hide();
					_picked(ship);
				};
			}
			_grid.AddChild(cell);
		}
	}
}
