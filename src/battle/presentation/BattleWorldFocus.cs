using Godot;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Education;
using GrimSpace.Math.Camera;
using GrimSpace.Tutorials;
using GrimSpace.Battle.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation;

public sealed class BattleWorldFocus(
	Controller camera,
	BattleLayout layout,
	BattleView battleView,
	Func<State> playerState) : IWorldFocus
{
	private const float OverviewPitch = 1.2f;
	private readonly Func<State> _playerState =
		playerState ?? throw new ArgumentNullException(nameof(playerState));

	public WorldFocusResult Focus(string objectId)
	{
		var positions = PositionsFor(objectId);
		if (positions.Count == 0)
			return new WorldFocusResult.MissingTarget();

		if (objectId == FirstBattleTutorial.OverviewTargetId)
		{
			var previousPose = camera.CurrentPose;
			SetOverview(positions);
			return new WorldFocusResult.Accepted(
				new RestoreCameraFocus(camera, previousPose));
		}
		if (objectId == FirstBattleTutorial.PlayerTargetId)
		{
			var previousPose = camera.CurrentPose;
			camera.TweenFocusOn(
				positions[0],
				targetDistance: 22f);
			return new WorldFocusResult.Accepted(
				new RestoreCameraFocus(camera, previousPose));
		}
		if (objectId == FirstBattleTutorial.WeaponTrainingTargetId)
		{
			var previousPose = camera.CurrentPose;
			var pose = BattleCameraPoses.PlayerAft(_playerState());
			camera.TweenFocusOn(
				pose.Pivot,
				pose.Distance,
				pose.Yaw,
				pose.Pitch);
			return new WorldFocusResult.Accepted(
				new RestoreCameraFocus(camera, previousPose));
		}

		return new WorldFocusResult.Accepted(new NoOpFocusHandle());
	}

	private IReadOnlyList<Vector3> PositionsFor(string objectId) =>
		objectId switch
		{
			FirstBattleTutorial.OverviewTargetId => battleView.UnitViews.Values
				.Select(view => view.GlobalPosition)
				.ToArray(),
			FirstBattleTutorial.EnemiesTargetId => layout.Participants
				.Where(pair => pair.Value == ETeam.Enemy)
				.Select(pair => pair.Key)
				.Where(battleView.UnitViews.ContainsKey)
				.Select(id => battleView.UnitViews[id].GlobalPosition)
				.ToArray(),
			FirstBattleTutorial.PlayerTargetId => layout.Participants
				.Where(pair => pair.Value == ETeam.Player)
				.Select(pair => pair.Key)
				.Where(battleView.UnitViews.ContainsKey)
				.Select(id => battleView.UnitViews[id].GlobalPosition)
				.ToArray(),
			FirstBattleTutorial.WeaponTrainingTargetId =>
				[WorldMapping.ToWorld(_playerState().Position)],
			_ => [],
		};

	private void SetOverview(IReadOnlyList<Vector3> positions)
	{
		var center = positions.Aggregate(Vector3.Zero, (sum, position) => sum + position)
			/ positions.Count;
		var furthestDistance = positions.Max(position => (position - center).Length());
		var gridSpan = Mathf.Max(layout.Grid.Width, Mathf.Max(layout.Grid.Height, layout.Grid.Depth))
			* WorldMapping.CellSize;
		var distance = Mathf.Max(21f, Mathf.Max(furthestDistance * 2.25f, gridSpan * 1.3f));
		camera.TweenFocusOn(
			center,
			distance,
			targetYaw: 0f,
			targetPitch: OverviewPitch);
	}

	private sealed class RestoreCameraFocus(Controller camera, OrbitPose pose) : IWorldFocusHandle
	{
		private Controller? _camera = camera;

		public void Dispose()
		{
			if (_camera is null)
				return;

			if (GodotObject.IsInstanceValid(_camera))
			{
				_camera.TweenFocusOn(
					pose.Pivot,
					pose.Distance,
					pose.Yaw,
					pose.Pitch);
			}
			_camera = null;
		}
	}

	private sealed class NoOpFocusHandle : IWorldFocusHandle
	{
		public void Dispose()
		{
		}
	}
}
