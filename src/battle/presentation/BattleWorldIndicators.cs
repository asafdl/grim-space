using Godot;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Education;
using GrimSpace.Tutorials;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation;

public sealed partial class BattleWorldIndicators : Node3D, IWorldIndicator
{
	private BattleLayout _layout = null!;
	private BattleView _battleView = null!;
	private bool _configured;

	public void Configure(BattleLayout layout, BattleView battleView)
	{
		_layout = layout ?? throw new ArgumentNullException(nameof(layout));
		_battleView = battleView ?? throw new ArgumentNullException(nameof(battleView));
		_configured = true;
	}

	public WorldIndicatorResult Show(string objectId)
	{
		if (!_configured)
			throw new InvalidOperationException("Battle world indicators must be configured before use.");

		var views = ViewsFor(objectId);
		if (views.Count == 0)
			return new WorldIndicatorResult.MissingTarget();

		var arrows = new List<WorldArrowIndicator>(views.Count);
		foreach (var view in views)
		{
			var arrow = new WorldArrowIndicator { Name = "EducationArrow" };
			view.AddChild(arrow);
			arrows.Add(arrow);
		}

		return new WorldIndicatorResult.Shown(new IndicatorHandle(arrows));
	}

	private IReadOnlyList<UnitView> ViewsFor(string objectId)
	{
		var ids = objectId switch
		{
			FirstBattleTutorial.EnemiesTargetId => _layout.Participants
				.Where(pair => pair.Value == ETeam.Enemy)
				.Select(pair => pair.Key),
			FirstBattleTutorial.PlayerTargetId => _layout.Participants
				.Where(pair => pair.Value == ETeam.Player)
				.Select(pair => pair.Key),
			_ => [],
		};

		return ids
			.Where(_battleView.UnitViews.ContainsKey)
			.Select(id => _battleView.UnitViews[id])
			.ToArray();
	}

	private sealed class IndicatorHandle(IReadOnlyList<WorldArrowIndicator> arrows)
		: IWorldIndicatorHandle
	{
		private IReadOnlyList<WorldArrowIndicator>? _arrows = arrows;

		public void Dispose()
		{
			if (_arrows is null)
				return;

			foreach (var arrow in _arrows)
			{
				if (GodotObject.IsInstanceValid(arrow))
					arrow.QueueFree();
			}
			_arrows = null;
		}
	}
}
