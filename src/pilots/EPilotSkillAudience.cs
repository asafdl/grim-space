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
	IReadOnlyList<EOperatorClassId>? OperatorClassIds = null,
	EPilotBackgroundId? BackgroundId = null)
{
	public static PilotSkillAudience Global { get; } = new(EPilotSkillAudience.Global);

	public static PilotSkillAudience ForOperatorClass(EOperatorClassId operatorClassId) =>
		ForOperatorClasses(operatorClassId);

	public static PilotSkillAudience ForOperatorClasses(params EOperatorClassId[] operatorClassIds) =>
		new(EPilotSkillAudience.OperatorClass, operatorClassIds);

	public static PilotSkillAudience ForBackground(EPilotBackgroundId backgroundId) =>
		new(EPilotSkillAudience.Background, null, backgroundId);

	public string OriginLabel() =>
		Kind switch
		{
			EPilotSkillAudience.Global => "Global",
			EPilotSkillAudience.OperatorClass => string.Join(
				" / ",
				OperatorClassIds!.Select(id => id.DisplayName())),
			EPilotSkillAudience.Background => PilotBackgroundCatalog.Get(BackgroundId!.Value).Name,
			_ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, null),
		};

	internal void Validate()
	{
		switch (Kind)
		{
		case EPilotSkillAudience.Global:
			if (OperatorClassIds is not null || BackgroundId is not null)
				throw new InvalidOperationException("Global pilot skills must not specify class or background.");
			return;
		case EPilotSkillAudience.OperatorClass:
			if (BackgroundId is not null)
				throw new InvalidOperationException("Class pilot skills must not specify a background.");
			if (OperatorClassIds is not { Count: > 0 } classes)
				throw new InvalidOperationException("Class pilot skills require at least one playable operator class.");
			var seen = new HashSet<EOperatorClassId>();
			foreach (var operatorClassId in classes)
			{
				if (!Enum.IsDefined(operatorClassId) || operatorClassId == EOperatorClassId.Unclassified)
					throw new InvalidOperationException("Class pilot skills require a playable operator class.");
				if (!seen.Add(operatorClassId))
					throw new InvalidOperationException("Class pilot skills must not list a class more than once.");
			}
			return;
		case EPilotSkillAudience.Background:
			if (OperatorClassIds is not null)
				throw new InvalidOperationException("Background pilot skills must not specify a class.");
			if (BackgroundId is not { } backgroundId || !Enum.IsDefined(backgroundId))
				throw new InvalidOperationException("Background pilot skills require a registered background.");
			return;
		default:
			throw new ArgumentOutOfRangeException(nameof(Kind), Kind, null);
		}
	}
}
