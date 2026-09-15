using GrimSpace.Components;

namespace GrimSpace.Tests.Presentation;

public sealed class InputShortcutTextTests
{
	[Theory]
	[InlineData(true, "Cmd")]
	[InlineData(false, "Ctrl")]
	public void PrimaryModifierUsesOperatingSystemConvention(
		bool isMacOs,
		string expected) =>
		Assert.Equal(expected, InputShortcutText.PrimaryModifierFor(isMacOs));
}
