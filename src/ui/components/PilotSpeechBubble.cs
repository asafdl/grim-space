using Godot;
using GrimSpace.Components;

namespace GrimSpace.Ui.Components;

public sealed partial class PilotSpeechBubble : Control
{
	public const float TailWidth = 16f;
	private const float TailHalfHeight = 8f;
	private const float NodeRadius = 3.5f;
	// Technical frame art has 150/100 px patch corners; scale it down so short lines stay compact.
	private const float FrameScale = 0.4f;
	// Frame edge line sits at texture x≈16; the tail opens at that edge so its outline continues the border.
	private const float FrameEdgeX = 16f * FrameScale;
	private const float TailEdgeWidth = 2f;
	private const int FontSize = 32;
	private const float MaxTextWidth = 420f;
	private const double CharIntervalSeconds = 0.03;
	private const double HoldBaseSeconds = 0.4;
	private const double HoldSecondsPerCharacter = 0.015;
	private const double MaxHoldSeconds = 2.0;
	private const double FadeSeconds = 0.25;

	private static readonly HudPalette Palette = HudPalettes.BlueCyan;

	private readonly UiContainer _frame;
	private readonly Control _tail;
	private readonly MarginContainer _margin;
	private readonly RichTextLabel _body;
	private readonly TypewriterPager _pager = new();
	private float _tailY;
	private bool _leaving;

	public event Action? Finished;
	public bool ShowTail { set => _tail.Visible = value; }

	public PilotSpeechBubble(string text)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(text);
		MouseFilter = MouseFilterEnum.Ignore;
		Modulate = Colors.Transparent;

		_frame = new UiContainer(Palette) { Scale = new Vector2(FrameScale, FrameScale) };
		_frame.SetVisual(UiContainerVisual.Technical);
		AddChild(_frame);

		_tail = new Control { MouseFilter = MouseFilterEnum.Ignore };
		_tail.Draw += DrawTail;
		AddChild(_tail);

		_margin = new MarginContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			CustomMinimumSize = new Vector2(
				(_frame.PatchMarginLeft + _frame.PatchMarginRight) * FrameScale,
				(_frame.PatchMarginTop + _frame.PatchMarginBottom) * FrameScale),
		};
		_margin.AddThemeConstantOverride("margin_left", Mathf.RoundToInt(_frame.ContentMarginLeft * FrameScale) + 8);
		_margin.AddThemeConstantOverride("margin_top", Mathf.RoundToInt(_frame.ContentMarginTop * FrameScale));
		_margin.AddThemeConstantOverride("margin_right", Mathf.RoundToInt(_frame.ContentMarginRight * FrameScale) + 4);
		_margin.AddThemeConstantOverride("margin_bottom", Mathf.RoundToInt(_frame.ContentMarginBottom * FrameScale) + 2);
		_margin.MinimumSizeChanged += UpdateMinimumSize;
		AddChild(_margin);

		_body = new RichTextLabel
		{
			BbcodeEnabled = true,
			FitContent = true,
			ScrollActive = false,
			SelectionEnabled = false,
			ContextMenuEnabled = false,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			ThemeTypeVariation = "NarrativeRichTextLabel",
			VisibleCharactersBehavior = TextServer.VisibleCharactersBehavior.CharsAfterShaping,
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
			MouseFilter = MouseFilterEnum.Ignore,
			Text = text,
		};
		_body.AddThemeFontSizeOverride("normal_font_size", FontSize);
		_margin.AddChild(_body);

		_pager.VisibleCharacterCountChanged += count => _body.VisibleCharacters = count;
		_pager.AutoAdvanceDue += Leave;
		_pager.ConfigureCharacterCounts(
			1,
			_ => _body.GetTotalCharacterCount(),
			charIntervalSeconds: CharIntervalSeconds,
			autoAdvanceSeconds: System.Math.Min(MaxHoldSeconds, HoldBaseSeconds + HoldSecondsPerCharacter * text.Length));
	}

	/// <summary>Vertical position, relative to the bubble, that the tail points at.</summary>
	public float TailY
	{
		set
		{
			if (Mathf.IsEqualApprox(_tailY, value))
				return;

			_tailY = value;
			_tail.QueueRedraw();
		}
	}

	public override Vector2 _GetMinimumSize() => _margin.GetCombinedMinimumSize();

	public override void _Ready()
	{
		_body.CustomMinimumSize = new Vector2(MeasureWrappedWidth(), 0f);
		CreateTween().TweenProperty(this, "modulate:a", 1f, FadeSeconds);
	}

	private float MeasureWrappedWidth()
	{
		var paragraph = new TextParagraph
		{
			Width = MaxTextWidth,
			BreakFlags = TextServer.LineBreakFlag.Mandatory
				| TextServer.LineBreakFlag.WordBound
				| TextServer.LineBreakFlag.Adaptive,
		};
		paragraph.AddString(_body.GetParsedText(), _body.GetThemeFont("normal_font"), FontSize);
		var width = 0f;
		for (var i = 0; i < paragraph.GetLineCount(); i++)
			width = Mathf.Max(width, paragraph.GetLineSize(i).X);
		return Mathf.Ceil(width) + 2f;
	}

	public override void _Notification(int what)
	{
		if (what != NotificationResized)
			return;

		_frame.Size = Size / FrameScale;
		_tail.Size = Size;
		_margin.Size = Size;
		_tail.QueueRedraw();
	}

	public override void _Process(double delta) => _pager.Tick(delta);

	private void DrawTail()
	{
		var inset = TailHalfHeight + 8f;
		var y = Size.Y <= inset * 2f ? Size.Y / 2f : Mathf.Clamp(_tailY, inset, Size.Y - inset);
		var glow = HudPalettes.WithAlpha(Palette.Highlight, 0.35f);

		// Port: a thickened stretch of the frame edge where the connector plugs in.
		var portTop = new Vector2(FrameEdgeX, y - TailHalfHeight);
		var portBottom = new Vector2(FrameEdgeX, y + TailHalfHeight);
		_tail.DrawLine(portTop, portBottom, glow, TailEdgeWidth + 5f, antialiased: true);
		_tail.DrawLine(portTop, portBottom, Palette.Primary, TailEdgeWidth + 1f, antialiased: true);

		// Wire with a node at the portrait end, echoing the frame's own line-and-circle detail.
		var node = new Vector2(-TailWidth + NodeRadius, y);
		var wireStart = new Vector2(FrameEdgeX, y);
		var wireEnd = node + new Vector2(NodeRadius, 0f);
		_tail.DrawLine(wireStart, wireEnd, glow, TailEdgeWidth + 3f, antialiased: true);
		_tail.DrawLine(wireStart, wireEnd, Palette.Primary, TailEdgeWidth, antialiased: true);
		_tail.DrawCircle(node, NodeRadius + 1.5f, glow, filled: false, width: 3f, antialiased: true);
		_tail.DrawCircle(node, NodeRadius, HudPalettes.WithAlpha(Palette.DeepSurface, 1f));
		_tail.DrawCircle(node, NodeRadius, Palette.Primary, filled: false, width: TailEdgeWidth, antialiased: true);
	}

	private void Leave()
	{
		if (_leaving)
			return;

		_leaving = true;
		var tween = CreateTween();
		tween.TweenProperty(this, "modulate:a", 0f, FadeSeconds);
		tween.TweenCallback(Callable.From(() => Finished?.Invoke()));
	}
}
