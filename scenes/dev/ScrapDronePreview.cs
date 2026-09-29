using Godot;
using System;
using GrimSpace.Battle.Presentation.Graphics;

public partial class ScrapDronePreview : Node3D
{
    [Export] public PackedScene AttackScene { get; set; } = null!;
    // Cycles through miss, one target, two targets. Edit the attack scene to tune VFX.
    private readonly Vector3[] _positions = { new(-2.3f, 0, -3), new(2.3f, 0.7f, -4) };
    private readonly MeshInstance3D[] _targets = new MeshInstance3D[2];
    private int _count;
    private double _delay = 0.8;
    private Camera3D _camera = null!;
    private Button _cameraButton = null!;
    private Label _label = null!;
    private bool _showFiringShipSide;

    public override void _Ready()
    {
        _camera = new Camera3D { Current = true, Fov = 48 };
        AddChild(_camera);
        SetCameraSide();
        AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-55, -25, 0), LightEnergy = 2 });
        AddChild(new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = new Color(0.012f, 0.017f, 0.025f),
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new Color(0.65f, 0.7f, 0.8f),
                AmbientLightEnergy = 0.7f
            }
        });
        AddChild(Box(new Vector3(0, 0, 4.6f), new Color(0.1f, 0.65f, 0.8f)));
        for (int i = 0; i < 2; i++)
        {
            _targets[i] = Box(_positions[i], new Color(0.7f, 0.24f, 0.12f));
            AddChild(_targets[i]);
            _targets[i].Visible = false;
        }
        var canvas = new CanvasLayer();
        AddChild(canvas);
        _label = new Label { Position = new Vector2(20, 20) };
        canvas.AddChild(_label);
        _label.Text = "Scrap drones: preparing preview";
        _cameraButton = new Button
        {
            Text = "View firing ship",
            Position = new Vector2(20, 70),
            Size = new Vector2(180, 36)
        };
        _cameraButton.Pressed += ToggleCameraSide;
        canvas.AddChild(_cameraButton);
    }

    public override void _Process(double delta)
    {
        _delay -= delta;
        if (_delay > 0) return;
        for (int i = 0; i < 2; i++) _targets[i].Visible = i < _count;
        var hits = new Vector3[_count];
        Array.Copy(_positions, hits, _count);
        var effect = AttackScene.Instantiate<ScrapDroneAttack>();
        AddChild(effect);
        effect.TargetReached += (index, position) => Flash(position);
        effect.Play(new Vector3(0, 0, 4), Vector3.Forward, Vector3.Up, hits, new Vector3(0, 0, -4));
        _label.Text = $"{_count} targets | {Math.Max(1, _count) * 4} drones\n" +
            "Cycles: miss / one target / two targets. Tune scrap_drone_attack.tscn in Inspector.";
        _delay = effect.DurationSeconds + 1.3;
        _count = (_count + 1) % 3;
    }

    private void Flash(Vector3 position)
    {
        var flash = new MeshInstance3D
        {
            Position = position,
            Mesh = new SphereMesh { Radius = 0.3f, Height = 0.6f },
            MaterialOverride = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                AlbedoColor = new Color(1, 0.75f, 0.2f)
            }
        };
        AddChild(flash);
        var tween = CreateTween();
        tween.TweenProperty(flash, "scale", Vector3.One * 0.01f, 0.15);
        tween.TweenCallback(Callable.From(flash.QueueFree));
    }

    private void ToggleCameraSide()
    {
        _showFiringShipSide = !_showFiringShipSide;
        SetCameraSide();
        _cameraButton.Text = _showFiringShipSide ? "View targets" : "View firing ship";
    }

    private void SetCameraSide()
    {
        _camera.Position = _showFiringShipSide
            ? new Vector3(0, 9, -13)
            : new Vector3(0, 9, 13);
        _camera.LookAt(
            _showFiringShipSide ? new Vector3(0, 0, 4.6f) : Vector3.Zero,
            Vector3.Up);
    }

    private static MeshInstance3D Box(Vector3 position, Color color) => new()
    {
        Position = position,
        Mesh = new BoxMesh { Size = new Vector3(0.8f, 0.4f, 1.1f) },
        MaterialOverride = new StandardMaterial3D { AlbedoColor = color }
    };
}
