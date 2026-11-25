using Godot;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Components;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>Screen-space corner brackets, range labels, and hover tooltips for hazards near the focused unit.</summary>
public sealed partial class HazardProximityOverlay : Node2D
{
	public const int RangeCells = 4;

	private const float MinHalfPx = 14f;
	private const float MaxHalfPx = 60f;
	private const float CornerFraction = 0.35f;
	private const float LineWidthPx = 2f;
	private const int LabelFontSize = 14;
	private static readonly Color BracketColor = new(0.72f, 0.9f, 1f, 0.9f);
	private static readonly Vector2 TooltipCursorOffset = new(18f, 18f);

	private readonly List<(Hazard Hazard, Rect2 Rect, string Range)> _marks = [];
	private Camera3D _camera = null!;
	private Node _hud = null!;
	private IReadOnlyList<(Hazard Hazard, string Range)> _inRange = [];
	private Control? _tooltip;
	private (string Id, int? Resistance)? _tooltipKey;

	public void Configure(Camera3D camera, Node hud)
	{
		_camera = camera;
		_hud = hud;
	}

	public void Apply(Coord origin, IEnumerable<Hazard> hazards) =>
		_inRange = hazards
			.Select(hazard => (Hazard: hazard, Meters: BattleDistance.SeparationMeters(origin, hazard.Center)))
			.Where(entry => entry.Meters <= RangeCells * BattleDistance.MetersPerCell)
			.Select(entry => (entry.Hazard, BattleDistance.Format(entry.Meters)))
			.ToArray();

	public override void _Process(double delta)
	{
		_marks.Clear();
		if (IsInstanceValid(_camera))
		{
			foreach (var (hazard, range) in _inRange)
			{
				var center = WorldMapping.ToWorld(hazard.Center);
				if (_camera.IsPositionBehind(center))
					continue;

				var screen = _camera.UnprojectPosition(center);
				var edge = _camera.UnprojectPosition(center + _camera.GlobalBasis.X * WorldMapping.CellSize * 0.5f);
				var half = Mathf.Clamp(screen.DistanceTo(edge), MinHalfPx, MaxHalfPx);
				_marks.Add((hazard, new Rect2(screen - Vector2.One * half, Vector2.One * half * 2f), range));
			}
		}

		UpdateTooltip();
		QueueRedraw();
	}

	public override void _Draw()
	{
		var font = ThemeDB.FallbackFont;
		foreach (var (_, rect, range) in _marks)
		{
			var corner = rect.Size.X * CornerFraction;
			var (min, max) = (rect.Position, rect.End);
			DrawPolyline([min + new Vector2(0f, corner), min, min + new Vector2(corner, 0f)], BracketColor, LineWidthPx);
			DrawPolyline([max - new Vector2(corner, 0f), max, max - new Vector2(0f, corner)], BracketColor, LineWidthPx);
			DrawString(font, new Vector2(min.X, min.Y - 4f), range, HorizontalAlignment.Left, -1, LabelFontSize, BracketColor);
		}
	}

	private void UpdateTooltip()
	{
		if (_tooltip is not null && !IsInstanceValid(_tooltip))
			_tooltip = null;
		if (HudTooltipPin.IsAnyPinned(GetTree()))
		{
			_tooltip?.QueueFree();
			_tooltip = null;
			_tooltipKey = null;
			return;
		}

		var viewport = GetViewport();
		var pointer = viewport.GetMousePosition();
		var overHud = viewport.GuiGetHoveredControl() is { } control && _hud.IsAncestorOf(control);
		var hovered = overHud ? null : _marks.FirstOrDefault(mark => mark.Rect.HasPoint(pointer)).Hazard;

		// Cover keeps its id while eroding, so resistance must refresh an already hovered tooltip.
		var key = hovered is null ? ((string, int?)?)null : (hovered.Id, (hovered as AsteroidBelt)?.DamageResistancePercent);
		if (key != _tooltipKey || (hovered is not null && _tooltip is null))
		{
			_tooltip?.QueueFree();
			_tooltip = null;
			_tooltipKey = key;
			if (hovered is not null)
			{
				_tooltip = BattleHudTooltipCard.Create(BattleHudCopy.HazardTooltipFor(hovered));
				_tooltip.MouseFilter = Control.MouseFilterEnum.Ignore;
				AddChild(_tooltip);
			}
		}

		if (_tooltip is not null)
			_tooltip.Position = pointer + TooltipCursorOffset;
	}
}
