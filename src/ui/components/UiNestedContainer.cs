using Godot;
using GrimSpace.Components;

namespace GrimSpace.Ui.Components;

/// <summary>
/// Shrink-wrapped panel: <see cref="UiContainer"/> background plus padded content host.
/// </summary>
public sealed partial class UiNestedContainer : Container, IUiSized
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
	public UiNestedContainer(UiContainerVisual visual = UiContainerVisual.Simple)
	{
		_sizing = new UiSizing(this, new UiSize(0f, 0f));
		MouseFilter = MouseFilterEnum.Ignore;

		_background = new UiContainer();
		_background.SetVisual(visual);
		AddChild(_background);

		_margin = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
		AddChild(_margin);
		ApplyContentPadding();
	}

	public void SetPalette(HudPalette palette) => _background.SetPalette(palette);

	public void SetContentPadding(int left, int top, int right, int bottom)
	{
		_margin.AddThemeConstantOverride("margin_left", left);
		_margin.AddThemeConstantOverride("margin_top", top);
		_margin.AddThemeConstantOverride("margin_right", right);
		_margin.AddThemeConstantOverride("margin_bottom", bottom);
		UpdateMinimumSize();
		QueueSort();
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
		content.SizeFlagsVertical = SizeFlags.ShrinkBegin;
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

	private void ApplyContentPadding()
	{
		_margin.AddThemeConstantOverride("margin_left", _background.ContentMarginLeft);
		_margin.AddThemeConstantOverride("margin_right", _background.ContentMarginRight);
		_margin.AddThemeConstantOverride("margin_top", _background.ContentMarginTop);
		_margin.AddThemeConstantOverride("margin_bottom", _background.ContentMarginBottom);
	}
}
