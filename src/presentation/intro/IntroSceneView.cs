using Godot;
using GrimSpace.Presentation.Ui.Hud;

namespace GrimSpace.Presentation.Intro;

public partial class IntroSceneView : Control
{
	private const float TextPanelBottomPadding = 0.14f;

	private TextureRect _background = null!;
	private PanelContainer _textPanel = null!;
	private Label _body = null!;
	private Label _artCredit = null!;
	private Button _next = null!;
	private StyleBoxFlat? _textPanelStyle;

	public override void _Ready()
	{
		_background = GetNode<TextureRect>("Background");
		_textPanel = GetNode<PanelContainer>("TextPanel");
		_body = GetNode<Label>("TextPanel/Body");
		_artCredit = GetNode<Label>("ArtCredit");
		_next = GetNode<Button>("Next");

		GetViewport().SizeChanged += ApplyLayout;
		Callable.From(ApplyLayout).CallDeferred();
	}

	public override void _ExitTree() =>
		GetViewport().SizeChanged -= ApplyLayout;

	public void SetBackground(Texture2D? texture) =>
		_background.Texture = texture;

	public void SetBodyText(string text) =>
		_body.Text = text;

	public void SetNextVisible(bool visible) =>
		_next.Visible = visible;

	public void SetNextText(string text) =>
		_next.Text = text;

	public void FocusNextButton() =>
		_next.GrabFocus();

	private void ApplyLayout()
	{
		var margin = HudStyles.Margin;

		var bottomPadding = Mathf.RoundToInt(GetViewportRect().Size.Y * TextPanelBottomPadding);

		_artCredit.OffsetTop = margin / 2;

		_textPanel.OffsetLeft = margin;
		_textPanel.OffsetRight = -margin;
		_textPanel.OffsetBottom = -bottomPadding;

		_textPanelStyle ??= DuplicateTextPanelStyle();
		if (_textPanelStyle is not null)
		{
			_textPanelStyle.ContentMarginLeft = Mathf.RoundToInt(margin * 3.5f);
			_textPanelStyle.ContentMarginTop = margin;
			_textPanelStyle.ContentMarginRight = margin;
			_textPanelStyle.ContentMarginBottom = margin;
		}

		const int buttonWidth = 140;
		const int buttonHeight = 44;
		_next.CustomMinimumSize = new Vector2(buttonWidth, buttonHeight);
		_next.SetAnchorsPreset(Control.LayoutPreset.TopRight);
		_next.OffsetLeft = -(buttonWidth + margin);
		_next.OffsetTop = margin;
		_next.OffsetRight = -margin;
		_next.OffsetBottom = margin + buttonHeight;
	}

	private StyleBoxFlat? DuplicateTextPanelStyle()
	{
		if (_textPanel.GetThemeStylebox("panel") is not StyleBoxFlat style)
			return null;

		var duplicate = (StyleBoxFlat)style.Duplicate();
		_textPanel.AddThemeStyleboxOverride("panel", duplicate);
		return duplicate;
	}
}
