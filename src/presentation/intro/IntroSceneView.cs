using Godot;
using GrimSpace.Components;

namespace GrimSpace.Presentation.Intro;

public partial class IntroSceneView : Control
{
	private TextureRect _background = null!;
	private Label _artCredit = null!;
	private FramedActionBar _bar = null!;

	public event Action? NextPressed;

	public override void _Ready()
	{
		_background = GetNode<TextureRect>("Background");
		_artCredit = GetNode<Label>("ArtCredit");
		_artCredit.OffsetTop = HudStyles.HalfMargin;

		_bar = new FramedActionBar();
		_bar.ConfigureWidth(900, 1400, 0.72f);
		_bar.ActionPressed += () => NextPressed?.Invoke();
		AddChild(_bar);
	}

	public void SetBackground(Texture2D? texture) =>
		_background.Texture = texture;

	public void SetBodyText(string text) =>
		_bar.Text = text;

	public void SetNextVisible(bool visible) =>
		_bar.ActionVisible = visible;

	public void SetNextText(string text) =>
		_bar.ActionText = text;

	public void FocusNextButton() =>
		_bar.FocusAction();
}
