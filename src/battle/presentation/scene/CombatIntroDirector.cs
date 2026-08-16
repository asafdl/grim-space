using Godot;
using GrimSpace.Battle.Presentation.Camera;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Scene;

/// <summary>
/// Brief combat-start presentation: overview, opponent focus, then behind-player framing.
/// </summary>
public sealed partial class CombatIntroDirector : Node
{
	private const float PanToOpponentsDelay = 1.7f;
	private const float PanDuration = 1.4f;
	private const float PanBehindPlayerDelay = 4.4f;
	private const float PanBehindPlayerDuration = 1.4f;
	private const float MinIntroDistance = 18f;
	private const float MaxIntroDistance = 30f;
	private const float OverviewPitch = 0.92f;
	private const float BehindPlayerPitch = 0.38f;
	private const float BehindPlayerDistance = 22f;
	private const float BehindPlayerSideBias = 0.32f;

	private BattleOrchestrator _battle = null!;
	private BattleView _battleView = null!;
	private OpponentIntroMarkersView _markers = null!;
	private Controller _camera = null!;
	private Func<Vector3> _playerPosition = () => Vector3.Zero;

	public void Configure(
		BattleOrchestrator battle,
		BattleView battleView,
		OpponentIntroMarkersView markers,
		Controller camera,
		Func<Vector3> playerPosition)
	{
		_battle = battle;
		_battleView = battleView;
		_markers = markers;
		_camera = camera;
		_playerPosition = playerPosition;
	}

	public void Play(Action onBannerDismiss, Action onComplete)
	{
		var opponents = CollectOpponents();
		var enemyPoints = OpponentWorldPositions(opponents.Keys);
		var enemyCentroid = enemyPoints.Count == 0 ? _playerPosition() : ComputeCentroid(enemyPoints);

		SetOverviewPose(enemyCentroid, enemyPoints);
		_markers.Show(opponents.Values.Select(pair => pair.Position).ToList());

		GetTree().CreateTimer(PanToOpponentsDelay).Timeout += () => FocusOnOpponents(opponents);
		GetTree().CreateTimer(PanBehindPlayerDelay).Timeout += () =>
		{
			onBannerDismiss();
			ClearHighlights();
			FocusBehindPlayerTowardEnemies(enemyCentroid);
			GetTree().CreateTimer(PanBehindPlayerDuration).Timeout += onComplete;
		};
	}

	private void SetOverviewPose(Vector3 enemyCentroid, IReadOnlyList<Vector3> enemyPoints)
	{
		var playerPos = _playerPosition();
		var pivot = (playerPos + enemyCentroid) * 0.5f;
		var battleAxisYaw = BattleAxisYaw(playerPos, enemyCentroid);
		var distance = OverviewDistance(pivot, playerPos, enemyCentroid, enemyPoints);

		_camera.SetFocus(pivot, distance, battleAxisYaw, OverviewPitch);
	}

	private void ClearHighlights() => _markers.Clear();

	private void FocusOnOpponents(IReadOnlyDictionary<string, OpponentSnapshot> opponents)
	{
		var points = OpponentWorldPositions(opponents.Keys);
		if (points.Count == 0)
			return;

		var focusPivot = ComputeCentroid(points);
		var viewDirection = CameraDirectionForOpponents(opponents, focusPivot);
		var (targetYaw, targetPitch) = Controller.OrbitAnglesForDirection(viewDirection);
		_camera.TweenFocusOn(
			focusPivot,
			IntroFocusDistance(points, focusPivot),
			targetYaw,
			targetPitch,
			PanDuration);
	}

	private void FocusBehindPlayerTowardEnemies(Vector3 enemyCentroid)
	{
		var playerPos = _playerPosition();
		var behindDirection = BehindPlayerViewDirection(playerPos, enemyCentroid);
		var targetYaw = Mathf.Atan2(behindDirection.X, behindDirection.Z);
		_camera.TweenFocusOn(
			playerPos,
			BehindPlayerDistance,
			targetYaw,
			BehindPlayerPitch,
			PanBehindPlayerDuration);
	}

	private static Vector3 BehindPlayerViewDirection(Vector3 playerPos, Vector3 enemyCentroid)
	{
		var toEnemies = enemyCentroid - playerPos;
		toEnemies.Y = 0f;
		if (toEnemies.LengthSquared() < 0.001f)
			return Vector3.Back;

		toEnemies = toEnemies.Normalized();
		var side = toEnemies.Cross(Vector3.Up).Normalized();
		return (-toEnemies + side * BehindPlayerSideBias).Normalized();
	}

	private Vector3 CameraDirectionForOpponents(
		IReadOnlyDictionary<string, OpponentSnapshot> opponents,
		Vector3 focusPivot)
	{
		var foreSum = Vector3.Zero;
		foreach (var opponent in opponents.Values)
			foreSum += ToWorldDirection(opponent.Fore);

		if (foreSum.LengthSquared() > 0.2f)
			return foreSum.Normalized();

		var fromPlayer = focusPivot - _playerPosition();
		fromPlayer.Y *= 0.4f;
		if (fromPlayer.LengthSquared() > 0.001f)
			return fromPlayer.Normalized();

		return Vector3.Forward;
	}

	private float IntroFocusDistance(IReadOnlyList<Vector3> points, Vector3 focusPivot)
	{
		var maxSpread = 0f;
		foreach (var point in points)
		{
			var offset = point - focusPivot;
			var horizontal = Mathf.Sqrt(offset.X * offset.X + offset.Z * offset.Z);
			maxSpread = Mathf.Max(maxSpread, horizontal);
		}

		return Mathf.Clamp(16f + maxSpread * 0.85f, MinIntroDistance, MaxIntroDistance);
	}

	private static float OverviewDistance(
		Vector3 pivot,
		Vector3 playerPos,
		Vector3 enemyCentroid,
		IReadOnlyList<Vector3> enemyPoints)
	{
		var maxSpread = new[]
			{
				(playerPos - pivot).Length(),
				(enemyCentroid - pivot).Length(),
			}
			.Concat(enemyPoints.Select(point => (point - pivot).Length()))
			.DefaultIfEmpty(12f)
			.Max();

		return Mathf.Clamp(maxSpread * 2.4f + 14f, 34f, 80f);
	}

	private static float BattleAxisYaw(Vector3 playerPos, Vector3 enemyCentroid)
	{
		var axis = enemyCentroid - playerPos;
		axis.Y = 0f;
		if (axis.LengthSquared() < 0.001f)
			return 0f;

		return Mathf.Atan2(axis.X, axis.Z);
	}

	private Dictionary<string, OpponentSnapshot> CollectOpponents()
	{
		var player = UnitRegistry.For(_battle.Engine.World).UnitOf(_battle.PlayerId);
		return UnitRegistry.For(_battle.Engine.World).All
			.Where(unit => unit.State.IsAlive && player.RelationTo(unit) == EUnitRelation.Opponent)
			.ToDictionary(
				unit => unit.State.Id,
				unit => new OpponentSnapshot(unit.State.Position, unit.State.Fore));
	}

	private List<Vector3> OpponentWorldPositions(IEnumerable<string> opponentIds)
	{
		var points = new List<Vector3>();
		foreach (var opponentId in opponentIds)
		{
			if (_battleView.UnitViews.TryGetValue(opponentId, out var view))
				points.Add(view.GlobalPosition);
			else
				points.Add(WorldMapping.ToWorld(_battle.Engine.World.StateOf(opponentId).Position));
		}

		return points;
	}

	private static Vector3 ComputeCentroid(IReadOnlyList<Vector3> points)
	{
		var sum = Vector3.Zero;
		foreach (var point in points)
			sum += point;
		return sum / points.Count;
	}

	private static Vector3 ToWorldDirection(Coord direction) =>
		new(direction.X, direction.Y, direction.Z);

	private readonly record struct OpponentSnapshot(Coord Position, Coord Fore);
}
