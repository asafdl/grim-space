using GrimSpace.Components;

namespace GrimSpace.Tests.Components;

[BattleTestSuite]
public sealed class ModalShellLayoutTests
{
	[Fact]
	public void BaselinePanelMinimumSize_is_non_zero()
	{
		Assert.True(ModalShellLayout.BaselinePanelMinimumSize.Width > 0f);
		Assert.True(ModalShellLayout.BaselinePanelMinimumSize.Height > 0f);
	}

	[Fact]
	public void ResolveContainerSize_prefers_control_size_when_usable()
	{
		var controlSize = new LayoutSize(1600f, 900f);
		var viewportSize = new LayoutSize(1920f, 1080f);

		var resolved = ModalShellLayout.ResolveContainerSize(controlSize, viewportSize);

		Assert.Equal(controlSize, resolved);
	}

	[Fact]
	public void FirstOpen_uses_viewport_when_control_size_is_near_zero()
	{
		var viewportSize = new LayoutSize(1920f, 1080f);

		var resolved = ModalShellLayout.ResolveContainerSize(LayoutSize.Zero, viewportSize);
		var panelSize = ModalShellLayout.ComputePanelMinimumSize(resolved);

		Assert.Equal(viewportSize, resolved);
		Assert.True(panelSize.Width >= 720f);
		Assert.True(panelSize.Height >= 540f);
	}

	[Fact]
	public void CloseReopen_keeps_non_zero_panel_size_when_control_still_unlaid_out()
	{
		var viewportSize = new LayoutSize(1920f, 1080f);
		var firstOpen = ModalShellLayout.ComputePanelMinimumSize(
			ModalShellLayout.ResolveContainerSize(LayoutSize.Zero, viewportSize));
		var reopen = ModalShellLayout.ComputePanelMinimumSize(
			ModalShellLayout.ResolveContainerSize(LayoutSize.Zero, viewportSize));

		Assert.Equal(firstOpen, reopen);
		Assert.True(reopen.Width > 0f);
		Assert.True(reopen.Height > 0f);
	}

	[Fact]
	public void ViewportResize_updates_panel_minimum_size()
	{
		var compact = ModalShellLayout.ComputePanelMinimumSize(
			ModalShellLayout.ResolveContainerSize(LayoutSize.Zero, new LayoutSize(1280f, 720f)));
		var wide = ModalShellLayout.ComputePanelMinimumSize(
			ModalShellLayout.ResolveContainerSize(
				new LayoutSize(1920f, 1080f),
				new LayoutSize(1920f, 1080f)));

		Assert.True(wide.Width >= compact.Width);
		Assert.True(wide.Height >= compact.Height);
	}

	[Fact]
	public void UltraWideViewport_caps_panel_width_without_throwing()
	{
		var viewportSize = new LayoutSize(2560f, 1440f);
		var resolved = ModalShellLayout.ResolveContainerSize(LayoutSize.Zero, viewportSize);
		var panelSize = ModalShellLayout.ComputePanelMinimumSize(resolved);

		Assert.Equal(ModalShellLayout.MaxPanelWidth, panelSize.Width);
		Assert.True(panelSize.Height >= 540f);
	}

	[Fact]
	public void ResolveContainerSize_uses_fallback_when_control_and_viewport_are_unusable()
	{
		var resolved = ModalShellLayout.ResolveContainerSize(
			LayoutSize.Zero,
			new LayoutSize(32f, 32f));

		Assert.Equal(ModalShellLayout.FallbackViewportSize, resolved);
		Assert.Equal(
			ModalShellLayout.BaselinePanelMinimumSize,
			ModalShellLayout.ComputePanelMinimumSize(resolved));
	}
}
