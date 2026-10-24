using Godot;
using GrimSpace.Components;

namespace GrimSpace.Ui.Components;

public sealed partial class UiFramedContainer : Container, IUiSized
{
	private readonly UiSizing _sizing;
	public UiSize DefaultSize => _sizing.DefaultSize;
	public UiSizeOverride? SizeOverride
	{
		get => _sizing.SizeOverride;
		set => _sizing.SizeOverride = value;
	}

	private readonly UiContainer _background;
	private readonly MarginContainer _margin;

	public UiFramedContainer()
	{
		_sizing = new UiSizing(this, new UiSize(0f, 0f));
		MouseFilter = MouseFilterEnum.Ignore;

		_background = new UiContainer();
		_background.SetVisual(UiContainerVisual.Framed);
		AddChild(_background);

		_margin = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
		AddChild(_margin);
		ApplyContentPadding();
	}

	public void SetContent(Control content)
	{
		ArgumentNullException.ThrowIfNull(content);
		foreach (var child in _margin.GetChildren())
		{
			_margin.RemoveChild(child);
			child.QueueFree();
		}

		content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_margin.AddChild(content);
		UpdateMinimumSize();
		QueueSort();
	}

	public override Vector2 _GetMinimumSize()
	{
		var contentMin = _margin.GetCombinedMinimumSize();
		return contentMin + new Vector2(
			_background.PatchMarginLeft + _background.PatchMarginRight,
			_background.PatchMarginTop + _background.PatchMarginBottom);
	}

	public override void _Notification(int what)
	{
		if (what != NotificationSortChildren)
			return;

		var rect = new Rect2(Vector2.Zero, Size);
		FitChildInRect(_background, rect);
		FitChildInRect(_margin, ContentRect(rect.Size));
	}

	private void ApplyContentPadding()
	{
		_margin.AddThemeConstantOverride("margin_left", _background.ContentMarginLeft);
		_margin.AddThemeConstantOverride("margin_right", _background.ContentMarginRight);
		_margin.AddThemeConstantOverride("margin_top", _background.ContentMarginTop);
		_margin.AddThemeConstantOverride("margin_bottom", _background.ContentMarginBottom);
	}

	private Rect2 ContentRect(Vector2 size) =>
		new(
			_background.PatchMarginLeft,
			_background.PatchMarginTop,
			Mathf.Max(
				0f,
				size.X - _background.PatchMarginLeft - _background.PatchMarginRight),
			Mathf.Max(
				0f,
				size.Y - _background.PatchMarginTop - _background.PatchMarginBottom));
}
