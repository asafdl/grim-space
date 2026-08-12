using Godot;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Ui;

/// <summary>Left-side hull + per-face shield point bars (stacked top-to-bottom).</summary>
public sealed partial class HealthBar : HBoxContainer
{
	private const float BlockWidth = 28f;
	private const float BlockHeight = 14f;

	private static readonly Color HullFilled = new(0.85f, 0.15f, 0.18f, 0.95f);
	private static readonly Color HullEmpty = new(0.18f, 0.08f, 0.08f, 0.85f);
	private static readonly Color HullBorder = new(0.02f, 0.02f, 0.02f, 1f);

	private static readonly Color ShieldFilled = new(0.25f, 0.55f, 0.95f, 0.95f);
	private static readonly Color ShieldEmpty = new(0.08f, 0.12f, 0.22f, 0.85f);
	private static readonly Color ShieldBorder = new(0.92f, 0.95f, 1f, 0.95f);

	private static readonly ESpatialOrientation[] Faces =
	[
		ESpatialOrientation.Forward,
		ESpatialOrientation.Retro,
		ESpatialOrientation.Starboard,
		ESpatialOrientation.Port,
		ESpatialOrientation.Dorsal,
		ESpatialOrientation.Ventral,
	];

	private readonly VBoxContainer _hullHost;
	private readonly VBoxContainer _shieldColumn;
	private readonly Control _shieldSection;
	private readonly List<Panel> _hullBlocks = [];
	private readonly List<(VBoxContainer Host, List<Panel> Blocks)> _shieldFaces = [];

	public HealthBar()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		AddThemeConstantOverride("separation", 12);

		var hullSection = new VBoxContainer { MouseFilter = MouseFilterEnum.Stop };
		hullSection.AddThemeConstantOverride("separation", 4);
		hullSection.TooltipText = BattleHudCopy.HullTooltip;
		hullSection.AddChild(CreateTitle(BattleHudCopy.HullTitle, new Color(1f, 0.75f, 0.75f)));
		_hullHost = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		_hullHost.AddThemeConstantOverride("separation", 3);
		hullSection.AddChild(_hullHost);
		AddChild(hullSection);

		_shieldSection = new VBoxContainer { MouseFilter = MouseFilterEnum.Stop };
		_shieldSection.AddThemeConstantOverride("separation", 4);
		_shieldSection.TooltipText = BattleHudCopy.ShieldsTooltip;
		_shieldSection.AddChild(CreateTitle(BattleHudCopy.ShieldsTitle, new Color(0.75f, 0.88f, 1f)));
		_shieldColumn = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		_shieldColumn.AddThemeConstantOverride("separation", 6);
		_shieldSection.AddChild(_shieldColumn);
		AddChild(_shieldSection);
	}

	public void Set(State state) => Set(UnitDisplayState.Capture(state));

	public void Set(UnitDisplayState state)
	{
		SetHull(state.HullPoints, state.MaxHullPoints);
		SetShields(state.ShieldPoints, state.MaxShieldPointsPerFace);
	}

	private void SetShields(FaceShieldPoints shieldPoints, int maxPerFace)
	{
		_shieldSection.Visible = maxPerFace > 0;
		if (maxPerFace <= 0)
			return;

		while (_shieldFaces.Count < Faces.Length)
		{
			var host = new VBoxContainer
			{
				MouseFilter = MouseFilterEnum.Stop,
			};
			host.AddThemeConstantOverride("separation", 2);
			_shieldFaces.Add((host, []));
			_shieldColumn.AddChild(host);
		}

		for (var faceIndex = 0; faceIndex < Faces.Length; faceIndex++)
		{
			var face = Faces[faceIndex];
			var (host, blocks) = _shieldFaces[faceIndex];
			var current = System.Math.Clamp(shieldPoints[face], 0, maxPerFace);
			host.TooltipText = BattleHudCopy.FaceShieldTooltip(
				BattleHudCopy.FaceName(face),
				current,
				maxPerFace);
			SyncBlockCount(host, blocks, maxPerFace);

			for (var i = 0; i < blocks.Count; i++)
			{
				var filled = i < current;
				blocks[i].AddThemeStyleboxOverride(
					"panel",
					MakeStyle(filled ? ShieldFilled : ShieldEmpty, ShieldBorder, borderWidth: 2));
			}
		}
	}

	private void SetHull(int current, int max)
	{
		max = System.Math.Max(0, max);
		current = System.Math.Clamp(current, 0, max);
		SyncBlockCount(_hullHost, _hullBlocks, max);

		for (var i = 0; i < _hullBlocks.Count; i++)
		{
			var filled = i < current;
			_hullBlocks[i].AddThemeStyleboxOverride(
				"panel",
				MakeStyle(filled ? HullFilled : HullEmpty, HullBorder, borderWidth: 2));
		}
	}

	private static Label CreateTitle(string text, Color color)
	{
		var label = new Label
		{
			Text = text,
			MouseFilter = MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		label.AddThemeFontSizeOverride("font_size", 12);
		label.AddThemeColorOverride("font_color", color);
		return label;
	}

	private static void SyncBlockCount(Control host, List<Panel> blocks, int count)
	{
		while (blocks.Count < count)
		{
			var block = new Panel
			{
				CustomMinimumSize = new Vector2(BlockWidth, BlockHeight),
				MouseFilter = MouseFilterEnum.Ignore,
			};
			blocks.Add(block);
			host.AddChild(block);
		}

		while (blocks.Count > count)
		{
			var last = blocks[^1];
			blocks.RemoveAt(blocks.Count - 1);
			last.QueueFree();
		}
	}

	private static StyleBoxFlat MakeStyle(Color bg, Color border, int borderWidth) =>
		new()
		{
			BgColor = bg,
			BorderColor = border,
			BorderWidthLeft = borderWidth,
			BorderWidthTop = borderWidth,
			BorderWidthRight = borderWidth,
			BorderWidthBottom = borderWidth,
			CornerRadiusTopLeft = 2,
			CornerRadiusTopRight = 2,
			CornerRadiusBottomRight = 2,
			CornerRadiusBottomLeft = 2,
		};
}
