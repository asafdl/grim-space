using Godot;

public partial class TurnTimelineDemo : Control
{
    private const float BoxSize = 64.0f;
    private const float Spacing = 12.0f;
    private const float ScrollDuration = 0.3f;

    private HBoxContainer _items;
    private Control _timeline;
    private int _currentSlot = 0;

    public override void _Ready()
    {
        _timeline = GetNode<Control>("Timeline");
        _items = GetNode<HBoxContainer>("Timeline/Items");

        CreateBoxes();

        GetNode<Button>("AdvanceButton").Pressed += AdvanceTurn;

        // Start with the first box in the center.
        PositionItems(false);
    }

    private void CreateBoxes()
    {
        // Create enough boxes that the strip can keep moving
        // without immediately reaching its end.
        for (int i = 0; i < 20; i++)
        {
            var box = new Panel
            {
                CustomMinimumSize = new Vector2(BoxSize, BoxSize),
                MouseFilter = Control.MouseFilterEnum.Ignore
            };

            var label = new Label
            {
                Text = (i + 1).ToString(),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };

            label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

            box.AddChild(label);
            _items.AddChild(box);
        }
    }

    private void AdvanceTurn()
    {
        _currentSlot++;

        PositionItems(true);
    }

    private void PositionItems(bool animate)
    {
        float step = BoxSize + Spacing;

        // Position the center of the first item at the center
        // of the timeline.
        float timelineCenter = _timeline.Size.X * 0.5f;

        float targetX =
            timelineCenter
            - (BoxSize * 0.5f)
            - (_currentSlot * step);

        if (!animate)
        {
            _items.Position = new Vector2(
                targetX,
                _items.Position.Y
            );

            UpdateBoxAppearance();
            return;
        }

        var tween = CreateTween();

        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);

        tween.TweenProperty(
            _items,
            "position:x",
            targetX,
            ScrollDuration
        );

        tween.TweenCallback(Callable.From(UpdateBoxAppearance));
    }

    private void UpdateBoxAppearance()
    {
        float center = _timeline.Size.X * 0.5f;

        for (int i = 0; i < _items.GetChildCount(); i++)
        {
            var box = _items.GetChild<Control>(i);

            float boxCenter =
                box.Position.X +
                (box.Size.X * 0.5f);

            float distance = Mathf.Abs(boxCenter - center);

            // 0 = center, 1 = far away.
            float t = Mathf.Clamp(
                distance / 180.0f,
                0.0f,
                1.0f
            );

            float scale = Mathf.Lerp(1.15f, 0.75f, t);
            float alpha = Mathf.Lerp(1.0f, 0.35f, t);

            box.Scale = Vector2.One * scale;
            box.Modulate = new Color(
                1.0f,
                1.0f,
                1.0f,
                alpha
            );
        }
    }
}