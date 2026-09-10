namespace GrimSpace.Components;

public readonly record struct TypewriterNextPrompt(string ButtonText, bool IsFinalPage);

public sealed class TypewriterPager
{
	public const double DefaultCharIntervalSeconds = 0.048;
	public const double DefaultShowNextDelaySeconds = 0.0;

	private IReadOnlyList<string> _pages = [];
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
	public event Action<int>? PageBegan;
	public event Action<TypewriterNextPrompt>? NextPromptReady;
	public event Action? AutoAdvanceDue;
	public event Action? Completed;

	public bool IsActive => !_completed && _pageIndex < _pages.Count;

	public bool IsNextVisible => _nextVisible;

	public int PageIndex => _pageIndex;

	public void Configure(
		IReadOnlyList<string> pages,
		Func<int, int, string>? nextButtonText = null,
		double charIntervalSeconds = DefaultCharIntervalSeconds,
		double showNextDelaySeconds = DefaultShowNextDelaySeconds,
		double? autoAdvanceSeconds = null)
	{
		_pages = pages;
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

		var pageText = _pages[_pageIndex];
		if (!_typingComplete)
		{
			_elapsed += delta;
			while (_elapsed >= _charIntervalSeconds && _visibleChars < pageText.Length)
			{
				_elapsed -= _charIntervalSeconds;
				_visibleChars++;
				VisibleTextChanged?.Invoke(pageText[.._visibleChars]);
			}

			if (_visibleChars >= pageText.Length)
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
		VisibleTextChanged?.Invoke("");
		PageBegan?.Invoke(_pageIndex);
	}

	private void ShowNext()
	{
		_nextVisible = true;
		NextPromptReady?.Invoke(new TypewriterNextPrompt(_nextButtonText(_pageIndex, _pages.Count), IsFinalPage()));
	}

	private bool IsFinalPage() => _pageIndex >= _pages.Count - 1;

	private static string DefaultNextButtonText(int pageIndex, int pageCount) =>
		pageIndex >= pageCount - 1 ? "Continue" : "Next";
}
