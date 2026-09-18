using Godot;
using GrimSpace.Components;

namespace GrimSpace.Presentation.Intro;

public partial class IntroSceneView : Control
{
	private TextureRect _background = null!;
	private Label _artCredit = null!;
	private RichTextLabel _body = null!;
	private FramedActionBar _bar = null!;

	public event Action? NextPressed;

	public event Action? BodyPressed;

	public override void _Ready()
	{
		_background = GetNode<TextureRect>("Background");
		_artCredit = GetNode<Label>("ArtCredit");
		_artCredit.OffsetTop = HudStyles.HalfMargin;

		_body = new RichTextLabel
		{
			BbcodeEnabled = true,
			FitContent = true,
			ScrollActive = false,
			SelectionEnabled = false,
			ContextMenuEnabled = false,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
			ThemeTypeVariation = "NarrativeRichTextLabel",
			VisibleCharactersBehavior = TextServer.VisibleCharactersBehavior.CharsAfterShaping,
		};
		_body.GuiInput += @event =>
		{
			if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
				BodyPressed?.Invoke();
		};

		_bar = new FramedActionBar(_body);
		_bar.ConfigureWidth(900, 1400, 0.72f);
		_bar.ActionPressed += () => NextPressed?.Invoke();
		AddChild(_bar);
	}

	public void SetBackground(Texture2D? texture) =>
		_background.Texture = texture;

	public void SetBodyText(string text) =>
		_body.Text = text;

	public void SetBodyVisibleCharacters(int count) =>
		_body.VisibleCharacters = count;

	public void SetNextVisible(bool visible) =>
		_bar.ActionVisible = visible;

	public void SetNextText(string text) =>
		_bar.ActionText = text;

	public void FocusNextButton() =>
		_bar.FocusAction();
}
