using GrimSpace.Components;

namespace GrimSpace.Tests.Presentation;

public sealed class TypewriterPagerTests
{
	[Fact]
	public void Tick_RevealsTextOneCharacterAtATime()
	{
		var pager = new TypewriterPager();
		var visible = "";
		pager.VisibleTextChanged += text => visible = text;
		pager.Configure(["ab"], charIntervalSeconds: 1.0, showNextDelaySeconds: 10.0);

		pager.Tick(0.5);
		Assert.Equal("", visible);

		pager.Tick(0.5);
		Assert.Equal("a", visible);

		pager.Tick(1.0);
		Assert.Equal("ab", visible);
	}

	[Fact]
	public void Tick_RevealsProviderCharacterCountWithoutRebuildingText()
	{
		var pager = new TypewriterPager();
		var visibleCharacters = -1;
		pager.VisibleCharacterCountChanged += count => visibleCharacters = count;
		pager.ConfigureCharacterCounts(
			1,
			_ => 2,
			charIntervalSeconds: 1.0,
			showNextDelaySeconds: 10.0);

		Assert.Equal(0, visibleCharacters);
		pager.Tick(1.0);
		Assert.Equal(1, visibleCharacters);
		pager.Tick(1.0);
		Assert.Equal(2, visibleCharacters);
	}

	[Fact]
	public void Tick_ShowsNextPromptAfterTypingCompletes()
	{
		var pager = new TypewriterPager();
		pager.Configure(["x"], charIntervalSeconds: 0.0, showNextDelaySeconds: 0.5);
		TypewriterNextPrompt? prompt = null;
		pager.NextPromptReady += value => prompt = value;

		pager.Tick(0.0);
		pager.Tick(0.5);

		Assert.NotNull(prompt);
		Assert.Equal("Continue", prompt.Value.ButtonText);
		Assert.True(prompt.Value.IsFinalPage);
	}

	[Fact]
	public void Tick_DefaultShowsNextPromptAsTypingCompletes()
	{
		var pager = new TypewriterPager();
		TypewriterNextPrompt? prompt = null;
		pager.NextPromptReady += value => prompt = value;
		pager.Configure(["x"], charIntervalSeconds: 0.0);

		pager.Tick(0.0);

		Assert.NotNull(prompt);
		Assert.Equal("Continue", prompt.Value.ButtonText);
	}

	[Fact]
	public void Advance_MovesThroughPagesBeforeCompleting()
	{
		var pager = new TypewriterPager();
		pager.Configure(["one", "two"], charIntervalSeconds: 0.0, showNextDelaySeconds: 0.0);
		var completed = false;
		pager.Completed += () => completed = true;

		pager.Tick(0.0);
		pager.Advance();
		Assert.Equal(1, pager.PageIndex);
		Assert.False(completed);

		pager.Tick(0.0);
		pager.Advance();
		Assert.True(completed);
	}
}
