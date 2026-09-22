using GrimSpace.Education;

namespace GrimSpace.Tutorials;

public abstract record TutorialStartResult
{
	public sealed record Started : TutorialStartResult;

	public sealed record AlreadyCompleted : TutorialStartResult;

	public sealed record Busy(string ActiveTutorialId) : TutorialStartResult;

	public sealed record FocusFailed(WorldFocusResult Result) : TutorialStartResult;

	public sealed record IndicatorFailed(WorldIndicatorResult Result) : TutorialStartResult;
}

public abstract record TutorialAdvanceResult
{
	public sealed record Advanced : TutorialAdvanceResult;

	public sealed record Completed : TutorialAdvanceResult;

	public sealed record NoActiveFlow : TutorialAdvanceResult;

	public sealed record FocusFailed(WorldFocusResult Result) : TutorialAdvanceResult;

	public sealed record IndicatorFailed(WorldIndicatorResult Result) : TutorialAdvanceResult;
}
