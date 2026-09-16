namespace GrimSpace.Components;

internal readonly record struct LayoutSize(float Width, float Height)
{
	public static LayoutSize Zero => new(0f, 0f);
}

internal static class ModalShellLayout
{
	public const float MinLayoutDimension = 64f;
	public static readonly LayoutSize FallbackViewportSize = new(1280f, 720f);
	public static readonly LayoutSize BaselinePanelMinimumSize =
		ComputePanelMinimumSize(FallbackViewportSize);

	public static LayoutSize ResolveContainerSize(LayoutSize controlSize, LayoutSize viewportSize)
	{
		if (IsUsableLayoutSize(controlSize))
			return controlSize;

		if (IsUsableLayoutSize(viewportSize))
			return viewportSize;

		return FallbackViewportSize;
	}

	public static LayoutSize ComputePanelMinimumSize(LayoutSize containerSize)
	{
		var width = (int)MathF.Round(System.Math.Clamp(
			720f,
			containerSize.Width * 0.38f,
			MathF.Min(containerSize.Width * 0.58f, 800f)));
		var height = (int)MathF.Round(System.Math.Clamp(
			540f,
			containerSize.Height * 0.55f,
			containerSize.Height * 0.85f));

		return new LayoutSize(width, height);
	}

	private static bool IsUsableLayoutSize(LayoutSize size) =>
		size.Width >= MinLayoutDimension && size.Height >= MinLayoutDimension;
}
