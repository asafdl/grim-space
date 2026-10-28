using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Pilots;

public enum EPilotSkillAudience
{
	Global,
	OperatorClass,
	Background,
}

public readonly record struct PilotSkillAudience(
	EPilotSkillAudience Kind,
	EOperatorClassId? OperatorClassId = null,
	EPilotBackgroundId? BackgroundId = null)
{
	public static PilotSkillAudience Global { get; } = new(EPilotSkillAudience.Global);

	public static PilotSkillAudience ForOperatorClass(EOperatorClassId operatorClassId) =>
		new(EPilotSkillAudience.OperatorClass, operatorClassId);

	public static PilotSkillAudience ForBackground(EPilotBackgroundId backgroundId) =>
		new(EPilotSkillAudience.Background, null, backgroundId);

	public string OriginLabel() =>
		Kind switch
		{
			EPilotSkillAudience.Global => "Global",
			EPilotSkillAudience.OperatorClass => OperatorClassId!.Value.DisplayName(),
			EPilotSkillAudience.Background => PilotBackgroundCatalog.Get(BackgroundId!.Value).Name,
			_ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, null),
		};

	internal void Validate()
	{
		switch (Kind)
		{
		case EPilotSkillAudience.Global:
			if (OperatorClassId is not null || BackgroundId is not null)
				throw new InvalidOperationException("Global pilot skills must not specify class or background.");
			return;
		case EPilotSkillAudience.OperatorClass:
			if (BackgroundId is not null)
				throw new InvalidOperationException("Class pilot skills must not specify a background.");
			if (OperatorClassId is not { } operatorClassId
				|| !Enum.IsDefined(operatorClassId)
				|| operatorClassId == EOperatorClassId.Unclassified)
				throw new InvalidOperationException("Class pilot skills require a playable operator class.");
			return;
		case EPilotSkillAudience.Background:
			if (OperatorClassId is not null)
				throw new InvalidOperationException("Background pilot skills must not specify a class.");
			if (BackgroundId is not { } backgroundId || !Enum.IsDefined(backgroundId))
				throw new InvalidOperationException("Background pilot skills require a registered background.");
			return;
		default:
			throw new ArgumentOutOfRangeException(nameof(Kind), Kind, null);
		}
	}
}
