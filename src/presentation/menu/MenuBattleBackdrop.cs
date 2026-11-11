using Godot;
using GrimSpace.Application;
using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Battle.Presentation.Replay;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.Units.Maneuvering;
using GrimSpace.World.Factions;

namespace GrimSpace.Presentation.Menu;

/// <summary>
/// Scripted skirmish for the main menu: real ships and actions are committed through the battle
/// engine once, then the recorded beats are replayed on a loop. Simultaneous beats replay each
/// actor's slice of history on its own replay player so ships animate in parallel.
/// </summary>
public partial class MenuBattleBackdrop : Node3D
{
	private sealed record ScriptedShip(string Id, EType Chassis, ETeam Team, Coord Position, Coord Fore, EChassisBuffOffer[] Buffs, int? HullPoints = null);

	private sealed record Beat(IAction[] Actions, bool Simultaneous, double PauseAfterSeconds = 0);

	private sealed record BattleScript(
		int GridSize,
		float BackdropRadius,
		ScriptedShip[] Ships,
		BattleHazardSpawn[] Asteroids,
		Beat[] Beats,
		Vector3 CameraOffset,
		Vector3 CameraLookOffset,
		float CameraFov,
		float BattleScreenCenter,
		float SunYawDegrees,
		float SunPitchDegrees,
		double LoopDelaySeconds);

	private const string Fighter = "menu-fighter";
	private const string Gooper = "menu-gooper";
	private const string Stalker = "menu-stalker";
	private const string Hostile = "menu-hostile";

	// Long flights would otherwise push ships into Overdrive and turn the hits into kills.
	private static readonly EChassisBuffOffer[] HeatHeadroom =
		[EChassisBuffOffer.HeatCapacity, EChassisBuffOffer.HeatCapacity, EChassisBuffOffer.HeatCapacity];

	// One battle: allies take turns against the same enemy; idle allies wait in the blacked-out spawn column.
	private static readonly BattleScript Script = new(
		GridSize: 21,
		BackdropRadius: 111f,
		Ships:
		[
			new(Fighter, EType.Fighter, ETeam.Player, new Coord(0, 10, 10), new Coord(1, 0, 0), HeatHeadroom),
			new(Gooper, EType.IndustrialGooper, ETeam.Player, new Coord(0, 8, 10), new Coord(1, 0, 0), HeatHeadroom),
			new(Stalker, EType.Rogue, ETeam.Player, new Coord(0, 13, 10), new Coord(1, 0, 0), HeatHeadroom),
			// The enemy absorbs every ally's shot in the same battle, so it needs hull for all of them.
			new(Hostile, EType.Fighter, ETeam.Enemy, new Coord(20, 10, 10), new Coord(-1, 0, 0), HeatHeadroom, HullPoints: 2000),
		],
		Asteroids:
		[
			Rock(new Coord(7, 12, 13), new Coord(8, 12, 13)),
			Rock(new Coord(13, 8, 12), new Coord(13, 9, 12), new Coord(14, 8, 12)),
			Rock(new Coord(10, 13, 15)),
			Rock(new Coord(15, 12, 11)),
			Rock(new Coord(6, 8, 11), new Coord(6, 8, 12)),
			Rock(new Coord(11, 7, 14), new Coord(12, 7, 14), new Coord(11, 6, 14), new Coord(11, 7, 15)),
			Rock(new Coord(3, 14, 16), new Coord(4, 14, 16)),
			Rock(new Coord(17, 6, 15)),
			Rock(new Coord(18, 13, 13), new Coord(18, 14, 13)),
			Rock(new Coord(2, 6, 13)),
			Rock(new Coord(9, 15, 18), new Coord(10, 15, 18), new Coord(9, 16, 18)),
			Rock(new Coord(15, 4, 17)),
			Rock(new Coord(5, 11, 6)),
			Rock(new Coord(16, 9, 5)),
		],
		Beats:
		[
			new([..Fly(Fighter, 7), ..Fly(Hostile, 7)], Simultaneous: true, PauseAfterSeconds: 1.0),
			new([new LightningCannonAction(Fighter)], Simultaneous: false, PauseAfterSeconds: 1.2),
			new([..TurnAndFly(Fighter, 7), ..TurnAndFly(Hostile, 7)], Simultaneous: true),
			// Off camera: the fighter clears the lane and the gooper climbs into it.
			new([
					new HeadingTurnAction(Fighter, EHeadingTurn.PitchUp),
					..Fly(Fighter, 2),
					new HeadingTurnAction(Gooper, EHeadingTurn.PitchUp),
					..Fly(Gooper, 2),
					new HeadingTurnAction(Gooper, EHeadingTurn.PitchDown),
				],
				Simultaneous: true,
				PauseAfterSeconds: 2),

			new([..Fly(Gooper, 9), ..TurnAndFly(Hostile, 8)], Simultaneous: true, PauseAfterSeconds: 1.0),
			new([new RamAction(Gooper)], Simultaneous: false, PauseAfterSeconds: 1.2),
			new([..TurnAndFly(Gooper, 11), ..TurnAndFly(Hostile, 8)], Simultaneous: true, PauseAfterSeconds: 3),

			new([..Fly(Stalker, 10), ..TurnAndFly(Hostile, 10)], Simultaneous: true, PauseAfterSeconds: 1.0),
			new([new HighVelocityRodAction(Stalker, ESpatialOrientation.Ventral)], Simultaneous: false, PauseAfterSeconds: 1.2),
			new([..TurnAndFly(Stalker, 10), ..TurnAndFly(Hostile, 10)], Simultaneous: true),
		],
		CameraOffset: new Vector3(-7f, 3f, -34f),
		CameraLookOffset: new Vector3(0f, 1.2f, 0f),
		CameraFov: 45f,
		BattleScreenCenter: 0.7f,
		SunYawDegrees: -38f,
		SunPitchDegrees: -9f,
		LoopDelaySeconds: 3);

	private readonly List<TurnReplay> _beats = [];
	private readonly List<TurnReplayPlayer> _players = [];
	private Dictionary<string, ETeam> _teams = null!;
	private BattleView _battleView = null!;
	private int _beatIndex;
	private int _playersRunning;
	private Camera3D _camera = null!;
	private ShaderMaterial _edgeFade = null!;
	private Vector3 _sunPosition;

	// Building takes ~0.4s, so the menu UI paints first over a black cover, then the battle fades in.
	public override async void _Ready()
	{
		var cover = new ColorRect { Color = Colors.Black, AnchorRight = 1f, AnchorBottom = 1f, MouseFilter = Control.MouseFilterEnum.Ignore };
		var coverLayer = new CanvasLayer { Name = "Cover", Layer = -1 };
		coverLayer.AddChild(cover);
		AddChild(coverLayer);
		GetTree().NodeAdded += QuietSfx;
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		if (!IsInsideTree())
			return;

		_teams = Script.Ships.ToDictionary(ship => ship.Id, ship => ship.Team);
		var grid = RecordBeats(out var asteroids);

		var center = WorldMapping.GridCenter(grid);
		var camera = _camera = new Camera3D { Fov = Script.CameraFov, Current = true };
		AddChild(camera);
		camera.Position = center + Script.CameraOffset;
		camera.LookAt(center + Script.CameraLookOffset);

		AddChild(SpaceBackdrop.CreateWorldEnvironment());
		AddSky(center, camera);

		var asteroidView = new BoardAsteroidView { Name = "Asteroids" };
		asteroidView.Build(camera, asteroids);
		AddChild(asteroidView);

		_battleView = new BattleView { Name = "BattleView" };
		_battleView.ShieldTintForActor = id => ShieldFaceMaterials.TeamTint(_teams[id]);
		AddChild(_battleView);
		_battleView.BindInitial(_beats[0].StartStates.Select(pair => (pair.Key, pair.Value, ColorFor(pair.Key))));

		foreach (var _ in Script.Ships)
		{
			var player = new TurnReplayPlayer { LogTimings = false };
			player.Configure(
				_battleView.UnitViews,
				ColorFor,
				(state, color) => _battleView.Ensure(state, color),
				_battleView.Remove,
				SyncViews,
				_ => { },
				_ => { },
				_ => { });
			player.PlaybackComplete += OnPlayerFinished;
			AddChild(player);
			_players.Add(player);
		}

		AddChild(CreateEdgeFade());
		GetViewport().SizeChanged += UpdateEdgeFade;
		UpdateEdgeFade();
		CreateTween().TweenProperty(cover, ColorRect.PropertyName.Modulate.ToString(), Colors.Transparent, 1.2);
		PlayBeat(0);
	}

	public override void _ExitTree() => GetTree().NodeAdded -= QuietSfx;

	// Battle SFX players are created deep inside views; rerouting on tree entry catches them before they Play().
	private void QuietSfx(Node node)
	{
		if (!IsAncestorOf(node))
			return;
		switch (node)
		{
			case AudioStreamPlayer3D player: player.Bus = AudioBuses.MenuSfx; break;
			case AudioStreamPlayer player: player.Bus = AudioBuses.MenuSfx; break;
			case AudioStreamPlayer2D player: player.Bus = AudioBuses.MenuSfx; break;
		}
	}

	// The battle sun direction is fixed, so the sky (sun, carrier, light) is rotated around the grid instead to frame the sun.
	private void AddSky(Vector3 center, Camera3D camera)
	{
		var basis = new Basis(camera.GlobalBasis.X, Mathf.DegToRad(Script.SunPitchDegrees))
			* new Basis(Vector3.Up, Mathf.DegToRad(Script.SunYawDegrees));
		var yaw = new Transform3D(basis, center - basis * center);

		var skyCamera = new Camera3D();
		AddChild(skyCamera);
		skyCamera.GlobalTransform = yaw.AffineInverse() * camera.GlobalTransform;
		var carrier = RuinedCarrierBackdrop.Create(center, Script.BackdropRadius, skyCamera);
		RemoveChild(skyCamera);
		skyCamera.Free();

		var light = new DirectionalLight3D();
		AddChild(light);
		RedDwarfSun.Configure(light, center, Script.BackdropRadius);
		AddChild(carrier);
		var sun = RedDwarfSun.CreateVisual(center, Script.BackdropRadius);
		AddChild(sun);
		_sunPosition = yaw * sun.GlobalPosition;
		foreach (var node in new[] { light, carrier, sun })
			node.GlobalTransform = yaw * node.GlobalTransform;
	}

	private CanvasLayer CreateEdgeFade()
	{
		_edgeFade = new ShaderMaterial
		{
			Shader = new Shader
			{
				Code =
					"""
					shader_type canvas_item;

					uniform vec4 lane;
					uniform vec3 sun;
					uniform float aspect;
					uniform float patchiness = 1.2;
					uniform vec3 smoke_color = vec3(0.16, 0.13, 0.17);
					uniform vec3 haze_dark = vec3(0.05, 0.04, 0.065);
					uniform vec3 haze_light = vec3(0.29, 0.23, 0.33);

					float hash(vec2 p) {
						return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
					}

					float noise(vec2 p) {
						vec2 i = floor(p);
						vec2 f = fract(p);
						vec2 u = f * f * (3.0 - 2.0 * f);
						return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), u.x),
							mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x), u.y);
					}

					float fbm(vec2 p) {
						float value = 0.0;
						float amplitude = 0.5;
						for (int i = 0; i < 5; i++) {
							value += amplitude * noise(p);
							p = p * 2.03 + vec2(17.3, 9.1);
							amplitude *= 0.5;
						}
						return value;
					}

					void fragment() {
						vec2 p = UV * vec2(aspect, 1.0) * 1.8;
						vec2 drift = vec2(TIME * 0.02, -TIME * 0.012);
						// Warping the lookup coordinates makes the fade boundary ragged instead of rectangular.
						vec2 warp = vec2(fbm(p + drift), fbm(p + vec2(5.2, 1.3) - drift)) - 0.5;
						vec2 uv = UV + warp * vec2(0.14, 0.2);

						float sides = max(1.0 - smoothstep(lane.x, lane.y, uv.x), smoothstep(lane.z, lane.w, uv.x));
						float bands = max(1.0 - smoothstep(0.06, 0.34, uv.y), smoothstep(0.68, 0.94, uv.y));
						float sun_distance = length((UV - sun.xy) * vec2(aspect, 1.0));
						float raw = clamp(sides + bands - sides * bands, 0.0, 1.0) * smoothstep(sun.z, sun.z * 1.8, sun_distance);

						float smoke = fbm(p * 1.6 + drift + fbm(p * 0.7 - TIME * 0.01));
						float fade = clamp(raw * (1.0 + patchiness) - smoke * patchiness, 0.0, 1.0);
						fade = fade * fade * (3.0 - 2.0 * fade);
						// Faint lit wisps inside the transition read as smoke rather than a flat black gradient.
						float wisp = clamp(smoothstep(0.45, 0.8, fbm(p * 2.4 - vec2(TIME * 0.03, 0.0))) * raw * (1.0 - fade) * 4.0, 0.0, 1.0);
						float cloud = smoothstep(0.3, 0.85, fbm(p * 1.1 + drift * 1.5 + warp * 3.0));
						vec3 haze = mix(haze_dark, haze_light, cloud) + smoke_color * wisp;
						COLOR = vec4(haze, max(fade, wisp * 0.55));
					}
					""",
			},
		};
		var layer = new CanvasLayer { Name = "EdgeFade", Layer = -2 };
		layer.AddChild(new ColorRect
		{
			AnchorRight = 1f,
			AnchorBottom = 1f,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Material = _edgeFade,
		});
		return layer;
	}

	// An off-axis frustum shifts the whole picture uniformly so the battle lane centres on BattleScreenCenter, leaving the rest for the menu.
	// The sides reach full haze at the spawn columns and clear partway to the innermost positions any beat reaches; the sun stays uncovered.
	private void UpdateEdgeFade()
	{
		var size = GetViewport().GetVisibleRect().Size;
		var aspect = size.X / size.Y;
		var height = 2f * _camera.Near * Mathf.Tan(Mathf.DegToRad(Script.CameraFov) / 2f);
		_camera.SetFrustum(height, Vector2.Zero, _camera.Near, _camera.Far);
		float ScreenX(Coord cell) => _camera.UnprojectPosition(WorldMapping.ToWorld(cell)).X / size.X;
		var spawnXs = _beats[0].StartStates.Values.Select(state => ScreenX(state.Position)).ToArray();
		var shift = Script.BattleScreenCenter - (spawnXs.Min() + spawnXs.Max()) / 2f;
		_camera.SetFrustum(height, new Vector2(-shift * height * aspect, 0f), _camera.Near, _camera.Far);

		var spawns = _beats[0].StartStates.Values.Select(state => ScreenX(state.Position)).ToArray();
		var holds = _beats
			.SelectMany(beat => beat.EndStates.Values)
			.Where(state => state.Position.X > 0 && state.Position.X < Script.GridSize - 1)
			.Select(state => ScreenX(state.Position))
			.ToArray();
		const float shipMargin = 0.04f;
		var blackLeft = spawns.Min() + shipMargin;
		var blackRight = spawns.Max() - shipMargin;
		_edgeFade.SetShaderParameter("lane", new Vector4(
			blackLeft, Mathf.Lerp(blackLeft, holds.Min(), 0.6f), Mathf.Lerp(blackRight, holds.Max(), 0.6f), blackRight));

		var sun = _camera.UnprojectPosition(_sunPosition) / size;
		var rim = _camera.UnprojectPosition(_sunPosition + _camera.GlobalBasis.Y * Script.BackdropRadius * RedDwarfSun.RadiusScale) / size;
		_edgeFade.SetShaderParameter("sun", new Vector3(sun.X, sun.Y, sun.DistanceTo(rim) * 1.3f));
		_edgeFade.SetShaderParameter("aspect", aspect);
	}

	private Math.Grid.Grid RecordBeats(out IReadOnlyList<Asteroid> asteroids)
	{
		var encounter = new BattleEncounter
		{
			Id = "menu-battle",
			Seed = 0,
			Objective = EObjective.EliminateOpponents,
			WorldHazards = Script.Asteroids,
			Spawns = Script.Ships.Select(ship => new BattleSpawn
			{
				Ship = CreateShip(ship),
				Team = ship.Team,
				Faction = ship.Team == ETeam.Player ? EFaction.Player : EFaction.TheOptimality,
				Position = ship.Position,
				Fore = ship.Fore,
				Dorsal = Coord.Up,
				ExecutionAgent = new UserExecutionAgent(),
			}).ToArray(),
		};
		using var battle = BattleOrchestrator.PrepareFromEncounter(encounter, Script.GridSize);
		var engine = battle.Engine;
		asteroids = engine.World.Asteroids.ToList();

		foreach (var beat in Script.Beats)
		{
			var start = Snapshot(engine.World);
			var historyStart = engine.History().Count;
			engine.Commit([..Script.Ships.Select(ship => (IAction)new TurnUpkeepAction(ship.Id))]);
			foreach (var action in beat.Actions)
			{
				var before = engine.History().Count;
				engine.Commit(action);
				if (engine.History().Count == before)
					throw new InvalidOperationException($"Menu battle script action was rejected by the engine: {action}");
			}

			var end = Snapshot(engine.World);
			if (end.Values.FirstOrDefault(state => !state.IsAlive) is { } dead)
				throw new InvalidOperationException($"Menu battle script killed {dead.Id}; every ship must fly off screen.");
			_beats.Add(new TurnReplay(start, engine.History().Skip(historyStart).ToArray(), end));
		}

		return engine.World.Grid;
	}

	private void PlayBeat(int index)
	{
		_beatIndex = index;
		var beat = _beats[index];
		var replays = Script.Beats[index].Simultaneous
			? Script.Beats[index].Actions.Select(action => action.ActorId).Distinct().Select(id => SliceFor(beat, id)).ToArray()
			: [beat];

		_playersRunning = replays.Length;
		for (var i = 0; i < replays.Length; i++)
			_players[i].ResetToLive(replays[i].StartStates, replays[i].EndStates);
		for (var i = 0; i < replays.Length; i++)
			_players[i].Play(replays[i], index + 1, _teams);
	}

	private void OnPlayerFinished()
	{
		if (--_playersRunning > 0)
			return;

		var pause = Script.Beats[_beatIndex].PauseAfterSeconds;
		var next = _beatIndex + 1;
		if (next == _beats.Count)
		{
			next = 0;
			pause += Script.LoopDelaySeconds;
		}

		GetTree().CreateTimer(pause).Timeout += () => PlayBeat(next);
	}

	private void SyncViews(IReadOnlyDictionary<string, ActorState> states)
	{
		foreach (var (id, state) in states)
		{
			if (!state.IsAlive)
			{
				_battleView.Remove(id);
				continue;
			}

			_battleView.Ensure(state, ColorFor(id));
			_battleView.UnitViews[id].Sync(state);
		}
	}

	private Color ColorFor(string actorId) =>
		_teams.TryGetValue(actorId, out var team) && team == ETeam.Enemy
			? new Color(0.9f, 0.25f, 0.2f)
			: new Color(0.25f, 0.85f, 0.35f);

	/// <summary>Records follow the action that produced them, so they are attributed to that action's actor.</summary>
	private static TurnReplay SliceFor(TurnReplay beat, string actorId)
	{
		string? owner = null;
		var history = beat.History.Where(entry =>
		{
			if (entry is IAction action)
				owner = action.ActorId;
			return owner == actorId;
		}).ToArray();
		return new TurnReplay(
			beat.StartStates.Where(pair => pair.Key == actorId).ToDictionary(),
			history,
			beat.EndStates.Where(pair => pair.Key == actorId).ToDictionary());
	}

	private static Dictionary<string, ActorState> Snapshot(BattleWorld world) =>
		world.UnitRegistry.All.ToDictionary(unit => unit.State.Id, unit => unit.State.Clone());

	private static IEnumerable<IAction> Fly(string actorId, int steps) =>
		[new ApDeltaAction(actorId, steps), ..Enumerable.Range(0, steps).Select(_ => new MoveStepAction(actorId))];

	private static IEnumerable<IAction> TurnAndFly(string actorId, int steps) =>
		[new HeadingTurnAction(actorId, EHeadingTurn.Yaw180), ..Fly(actorId, steps)];

	private static ShipInstance CreateShip(ScriptedShip ship)
	{
		var instance = ShipInstance.FromCatalog(ship.Id, ship.Chassis);
		instance.Loadout = instance.Loadout with { ChassisBuffs = ship.Buffs };
		if (ship.HullPoints is { } hull)
		{
			instance.Loadout = instance.Loadout with { BaseHullPoints = hull };
			instance.CurrentHullPoints = hull;
		}
		return instance;
	}

	private static BattleHazardSpawn Rock(Coord origin, params Coord[] extraCells) =>
		new() { Origin = origin, Cells = new HashSet<Coord>([origin, ..extraCells]) };
}
