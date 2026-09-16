namespace GrimSpace.World.StarSystem.Presentation;

public enum PresentationTransitionFailure
{
	AlreadyTransitioning,
	UnknownMode,
	WrongCurrentMode,
	NotAllowed,
	CanEnterRejected,
	InvalidPayload,
	ExitTargetMissing,
	ModeBusy,
}

public readonly record struct PresentationTransitionResult
{
	public bool Succeeded { get; }
	public PresentationTransitionFailure? Failure { get; }

	private PresentationTransitionResult(bool succeeded, PresentationTransitionFailure? failure)
	{
		Succeeded = succeeded;
		Failure = failure;
	}

	public static PresentationTransitionResult Ok() => new(true, null);

	public static PresentationTransitionResult Fail(PresentationTransitionFailure failure) =>
		new(false, failure);
}
