using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>Dim static reticles over units already locked into a targeting session.</summary>
public sealed partial class PickedTargetMarksView : Node3D
{
	private const string ReticleTexturePath = "res://assets/ui/battle/enemy_target_reticle.svg";
	private const float PixelSize = 0.0115f;

	private static readonly Color MarkTint = new(0.6f, 0.72f, 0.82f, 0.5f);

	private readonly List<Sprite3D> _marks = [];
	private readonly List<Node3D> _targets = [];
	private Texture2D? _texture;

	public void Apply(IReadOnlyList<Node3D> targets)
	{
		_targets.Clear();
		_targets.AddRange(targets);
		_texture ??= GD.Load<Texture2D>(ReticleTexturePath)
			?? throw new InvalidOperationException(
				$"Could not load enemy target reticle '{ReticleTexturePath}'.");
		while (_marks.Count < _targets.Count)
		{
			var mark = new Sprite3D
			{
				Texture = _texture,
				Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
				PixelSize = PixelSize,
				Modulate = MarkTint,
				Shaded = false,
				TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear,
			};
			AddChild(mark);
			_marks.Add(mark);
		}

		for (var i = 0; i < _marks.Count; i++)
			_marks[i].Visible = i < _targets.Count;
		FollowTargets();
	}

	public override void _Process(double delta) => FollowTargets();

	private void FollowTargets()
	{
		for (var i = 0; i < _targets.Count; i++)
		{
			if (!IsInstanceValid(_targets[i]))
			{
				_marks[i].Visible = false;
				continue;
			}

			_marks[i].GlobalPosition = _targets[i].GlobalPosition;
		}
	}
}
