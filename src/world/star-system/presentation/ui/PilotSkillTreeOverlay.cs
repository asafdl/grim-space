using Godot;
using GrimSpace.Components;
using GrimSpace.Pilots;
using GrimSpace.Run;

namespace GrimSpace.World.StarSystem.Presentation.Ui;

public sealed partial class PilotSkillTreeOverlay : Control
{
	private const string ShieldIconPath = "res://assets/ui/abilities/shields_up.svg";
	private const int SkillNodeSize = 60;

	private static readonly Color AvailableColor = new(0.55f, 0.78f, 1f);
	private static readonly Color LearnedColor = new(0.42f, 0.88f, 0.58f);
	private static readonly Color LockedColor = new(0.32f, 0.38f, 0.46f);

	private readonly ModalShell _shell;

	public event Action<string, EPilotSkillId>? SkillLearnRequested;

	public PilotSkillTreeOverlay()
	{
		AnchorsPreset = (int)LayoutPreset.FullRect;
		AnchorRight = 1f;
		AnchorBottom = 1f;
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;
		MouseFilter = MouseFilterEnum.Ignore;

		_shell = new ModalShell(HudThemeFamily.Informative);
		_shell.SetDismissVisible(true);
		AddChild(_shell);
	}

	public bool IsOpen => _shell.IsOpen;

	public bool TryHandleInput(InputEvent @event) => _shell.TryHandleInput(@event);

	public void Open(Pilot pilot)
	{
		ArgumentNullException.ThrowIfNull(pilot);
		_shell.Open("Pilot skills", $"{pilot.Name} · Level {pilot.ClaimedLevel}");
		_shell.SetHeader(HudHeaderMode.Close);
		_shell.SetBackHandler(null);
		_shell.SetFooter([]);
		_shell.SetBody(BuildTree(pilot));
	}

	private Control BuildTree(Pilot pilot)
	{
		var body = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		body.AddThemeConstantOverride("separation", 8);

		var available = PilotSkillAvailability.AvailableToLearn(pilot).ToHashSet();
		var learned = pilot.LearnedSkills.ToHashSet();
		for (var level = 2; level <= PilotProgression.MaxLevel; level++)
			body.AddChild(BuildLevelRow(pilot, level, learned, available));

		return body;
	}

	private Control BuildLevelRow(
		Pilot pilot,
		int level,
		HashSet<EPilotSkillId> learned,
		HashSet<EPilotSkillId> available)
	{
		var row = new HBoxContainer
		{
			CustomMinimumSize = new Vector2(0, 68),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		row.AddThemeConstantOverride("separation", 10);

		var levelLabel = new Label
		{
			Text = $"LEVEL {level}",
			CustomMinimumSize = new Vector2(72, 0),
			VerticalAlignment = VerticalAlignment.Center,
		};
		HudStyles.ApplyTextRole(levelLabel, HudTextRole.Metadata);
		row.AddChild(levelLabel);

		var definitions = PilotSkillCatalog.All
			.Where(definition => definition.Tier + 1 == level)
			.ToArray();
		if (definitions.Length == 0)
		{
			row.AddChild(CreateLevelLine(expand: true));
			return row;
		}

		row.AddChild(CreateLevelLine(expand: false));
		foreach (var definition in definitions)
		{
			row.AddChild(BuildSkillNode(pilot, definition, learned, available));
			row.AddChild(CreateLevelLine(expand: true));
		}

		return row;
	}

	private static ColorRect CreateLevelLine(bool expand) =>
		new()
		{
			Color = new Color(0.22f, 0.32f, 0.42f, 0.7f),
			CustomMinimumSize = new Vector2(expand ? 0 : 28, 2),
			SizeFlagsHorizontal = expand ? SizeFlags.ExpandFill : SizeFlags.ShrinkBegin,
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
			MouseFilter = MouseFilterEnum.Ignore,
		};

	private Control BuildSkillNode(
		Pilot pilot,
		PilotSkillDefinition definition,
		HashSet<EPilotSkillId> learned,
		HashSet<EPilotSkillId> available)
	{
		var isLearned = learned.Contains(definition.Id);
		var canLearn = available.Contains(definition.Id);
		var color = isLearned ? LearnedColor : canLearn ? AvailableColor : LockedColor;

		var button = new Button
		{
			CustomMinimumSize = new Vector2(SkillNodeSize, SkillNodeSize),
			Disabled = !canLearn,
			Icon = SvgIconLoader.Load(ShieldIconPath, color, 34),
			ExpandIcon = false,
			IconAlignment = HorizontalAlignment.Center,
			VerticalIconAlignment = VerticalAlignment.Center,
			TooltipText = $"{definition.Name}\n{definition.Description}",
		};
		button.AddThemeColorOverride("icon_disabled_color", Colors.White);
		ApplyCircleStyle(button, color, isLearned);
		if (canLearn)
			button.Pressed += () => SkillLearnRequested?.Invoke(pilot.Id, definition.Id);
		return button;
	}

	private static void ApplyCircleStyle(Button button, Color color, bool learned)
	{
		button.AddThemeStyleboxOverride("normal", CircleStyle(color, learned ? 0.28f : 0.12f, 2));
		button.AddThemeStyleboxOverride("hover", CircleStyle(color, 0.22f, 3));
		button.AddThemeStyleboxOverride("pressed", CircleStyle(color, 0.34f, 3));
		button.AddThemeStyleboxOverride("disabled", CircleStyle(color, learned ? 0.24f : 0.08f, 2));
	}

	private static StyleBoxFlat CircleStyle(Color color, float backgroundAlpha, int borderWidth) =>
		new()
		{
			BgColor = new Color(color.R, color.G, color.B, backgroundAlpha),
			BorderColor = color,
			BorderWidthLeft = borderWidth,
			BorderWidthTop = borderWidth,
			BorderWidthRight = borderWidth,
			BorderWidthBottom = borderWidth,
			CornerRadiusTopLeft = SkillNodeSize / 2,
			CornerRadiusTopRight = SkillNodeSize / 2,
			CornerRadiusBottomLeft = SkillNodeSize / 2,
			CornerRadiusBottomRight = SkillNodeSize / 2,
		};
}
