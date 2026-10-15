namespace GrimSpace.Components;

public static class CenteredCarouselWindow
{
	public static int ClampIndex(int index, int count) =>
		count == 0 ? 0 : System.Math.Clamp(index, 0, count - 1);

	public static int ItemIndexForSlot(int centerIndex, int slot, int centerSlot) =>
		centerIndex + (slot - centerSlot);

	public static T? ItemAt<T>(IReadOnlyList<T?> items, int index)
		where T : class =>
		index < 0 || index >= items.Count ? null : items[index];
}
