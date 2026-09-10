namespace GrimSpace.Components;

public readonly record struct TypewriterNextPrompt(string ButtonText, bool IsFinalPage);

public sealed class TypewriterPager
{
	public const double DefaultCharIntervalSeconds = 0.048;
	public const double DefaultShowNextDelaySeconds = 0.0;

	private IReadOnlyList<string>? _pages = [];
	private Func<int, int> _pageCharacterCount = _ => 0;
	private int _pageCount;
	private Func<int, int, string> _nextButtonText = DefaultNextButtonText;
	private double _charIntervalSeconds = DefaultCharIntervalSeconds;
	private double _showNextDelaySeconds = DefaultShowNextDelaySeconds;
	private double? _autoAdvanceSeconds;

	private int _pageIndex;
	private int _visibleChars;
	private double _elapsed;
	private bool _typingComplete;
	private bool _nextVisible;
	private bool _completed;

	public event Action<string>? VisibleTextChanged;
	public event Action<int>? VisibleCharacterCountChanged;
	public event Action<int>? PageBegan;
	public event Action<TypewriterNextPrompt>? NextPromptReady;
	public event Action? AutoAdvanceDue;
	public event Action? Completed;

	public bool IsActive => !_completed && _pageIndex < _pageCount;

	public bool IsNextVisible => _nextVisible;

	public int PageIndex => _pageIndex;

	public void Configure(
		IReadOnlyList<string> pages,
		Func<int, int, string>? nextButtonText = null,
		double charIntervalSeconds = DefaultCharIntervalSeconds,
		double showNextDelaySeconds = DefaultShowNextDelaySeconds,
		double? autoAdvanceSeconds = null)
	{
		ArgumentNullException.ThrowIfNull(pages);
		_pages = pages;
		Configure(
			pages.Count,
			pageIndex => pages[pageIndex].Length,
			nextButtonText,
			charIntervalSeconds,
			showNextDelaySeconds,
			autoAdvanceSeconds);
	}

	public void ConfigureCharacterCounts(
		int pageCount,
		Func<int, int> pageCharacterCount,
		Func<int, int, string>? nextButtonText = null,
		double charIntervalSeconds = DefaultCharIntervalSeconds,
		double showNextDelaySeconds = DefaultShowNextDelaySeconds,
		double? autoAdvanceSeconds = null)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(pageCount);
		ArgumentNullException.ThrowIfNull(pageCharacterCount);
		_pages = null;
		Configure(
			pageCount,
			pageCharacterCount,
			nextButtonText,
			charIntervalSeconds,
			showNextDelaySeconds,
			autoAdvanceSeconds);
	}

	private void Configure(
		int pageCount,
		Func<int, int> pageCharacterCount,
		Func<int, int, string>? nextButtonText,
		double charIntervalSeconds,
		double showNextDelaySeconds,
		double? autoAdvanceSeconds)
	{
		_pageCount = pageCount;
		_pageCharacterCount = pageCharacterCount;
		_nextButtonText = nextButtonText ?? DefaultNextButtonText;
		_charIntervalSeconds = charIntervalSeconds;
		_showNextDelaySeconds = showNextDelaySeconds;
		_autoAdvanceSeconds = autoAdvanceSeconds;
		_pageIndex = 0;
		_completed = false;
		BeginPage();
	}

	public void Tick(double delta)
	{
		if (!IsActive)
			return;

		var characterCount = _pageCharacterCount(_pageIndex);
		if (characterCount < 0)
			throw new InvalidOperationException("Page character count cannot be negative.");

		if (!_typingComplete)
		{
			_elapsed += delta;
			while (_elapsed >= _charIntervalSeconds && _visibleChars < characterCount)
			{
				_elapsed -= _charIntervalSeconds;
				_visibleChars++;
				VisibleCharacterCountChanged?.Invoke(_visibleChars);
				if (_pages is not null)
					VisibleTextChanged?.Invoke(_pages[_pageIndex][.._visibleChars]);
			}

			if (_visibleChars >= characterCount)
			{
				_typingComplete = true;
				_elapsed = 0;
			}
			else
			{
				return;
			}
		}

		_elapsed += delta;
		if (!_nextVisible && _elapsed >= _showNextDelaySeconds)
			ShowNext();

		if (_autoAdvanceSeconds is { } autoAdvance && _elapsed >= autoAdvance)
			AutoAdvanceDue?.Invoke();
	}

	public void Advance()
	{
		if (_completed)
			return;

		if (IsFinalPage())
		{
			_completed = true;
			Completed?.Invoke();
			return;
		}

		_pageIndex++;
		BeginPage();
	}

	private void BeginPage()
	{
		_visibleChars = 0;
		_elapsed = 0;
		_typingComplete = false;
		_nextVisible = false;
		VisibleCharacterCountChanged?.Invoke(0);
		if (_pages is not null)
			VisibleTextChanged?.Invoke("");
		PageBegan?.Invoke(_pageIndex);
	}

	private void ShowNext()
	{
		_nextVisible = true;
		NextPromptReady?.Invoke(new TypewriterNextPrompt(_nextButtonText(_pageIndex, _pageCount), IsFinalPage()));
	}

	private bool IsFinalPage() => _pageIndex >= _pageCount - 1;

	private static string DefaultNextButtonText(int pageIndex, int pageCount) =>
		pageIndex >= pageCount - 1 ? "Continue" : "Next";
}
