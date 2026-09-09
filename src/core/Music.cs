using System.Collections.Generic;
using Godot;

namespace GrimSpace.Core;

public partial class Music : Node
{
	private const float FadeOutSeconds = 4f;
	private const float FadeInSeconds = 3f;
	private const float SilentVolumeDb = -80f;
	private const float NormalVolumeDb = 0f;
	private const string TrackPathPrefix = "res://assets/music/terraforming-mars-tracks/track-";

	private readonly record struct MusicCue(string Path, double StartSeconds = 0);

	private readonly record struct SceneMusic(MusicCue[] Cues, bool Loop = false, bool ShuffleOnLoad = false);

	private const int TrackCount = 39;
	private const string MapScenePath = "res://scenes/map.tscn";

	private static readonly Dictionary<string, SceneMusic> SceneTracks = new()
	{
		["res://scenes/intro.tscn"] = new([
			new($"{TrackPathPrefix}15.mp3", 24),
			new($"{TrackPathPrefix}19.mp3"),
		]),
		["res://scenes/main.tscn"] = LoopingTracks(1, 6),
		["res://scenes/battle.tscn"] = LoopingTracks(7, 14, shuffleOnLoad: true),
		[MapScenePath] = UnassignedTracksLoop(shuffleOnLoad: true),
	};

	private static Music? _instance;
	private AudioStreamPlayer _player = null!;
	private Tween? _volumeTween;
	private string? _currentTrack;
	private SceneMusic? _activeMusic;
	private int _cueIndex;
	private SceneMusic? _pendingMusic;
	private bool _fadingOut;
	private bool _starmapSessionActive;

	public static Music Instance =>
		_instance ?? throw new InvalidOperationException("Music autoload is not ready.");

	public override void _EnterTree()
	{
		_instance = this;
	}

	public override void _Ready()
	{
		_player = new AudioStreamPlayer();
		AddChild(_player);
		_player.Finished += OnPlayerFinished;
		GetTree().SceneChanged += OnSceneChanged;
		CallDeferred(MethodName.OnSceneChanged);
	}

	public override void _ExitTree()
	{
		_player.Finished -= OnPlayerFinished;
		GetTree().SceneChanged -= OnSceneChanged;
		if (_instance == this)
			_instance = null;
	}

	public void Stop()
	{
		_pendingMusic = null;
		_activeMusic = null;
		_cueIndex = 0;
		_starmapSessionActive = false;
		_fadingOut = false;
		_volumeTween?.Kill();
		_volumeTween = null;
		_player.Stop();
		_currentTrack = null;
	}

	private void OnSceneChanged()
	{
		var scenePath = GetTree().CurrentScene?.SceneFilePath ?? "";
		if (ShouldPreserveCurrentMusic(scenePath))
			return;

		_starmapSessionActive = scenePath == MapScenePath;
		_activeMusic = null;
		_cueIndex = 0;
		TransitionTo(ResolveSceneMusic(scenePath));
	}

	private bool ShouldPreserveCurrentMusic(string scenePath) =>
		_starmapSessionActive
		&& (!SceneTracks.ContainsKey(scenePath) || scenePath == MapScenePath);

	private SceneMusic? ResolveSceneMusic(string scenePath)
	{
		if (!SceneTracks.TryGetValue(scenePath, out var music))
			return null;

		if (!music.ShuffleOnLoad || music.Cues.Length <= 1)
			return music;

		var cues = (MusicCue[])music.Cues.Clone();
		Shuffle(cues);
		return new SceneMusic(cues, music.Loop);
	}

	private void TransitionTo(SceneMusic? nextMusic)
	{
		_pendingMusic = nextMusic;

		if (nextMusic is { Cues.Length: > 0 } music
		    && music.Cues[0].Path == _currentTrack
		    && _player.Playing
		    && !_fadingOut)
		{
			_pendingMusic = null;
			return;
		}

		if (!_player.Playing)
		{
			_pendingMusic = null;
			if (nextMusic is not null)
				StartMusic(nextMusic.Value, fadeIn: true);
			return;
		}

		if (_fadingOut)
			return;

		BeginFadeOut();
	}

	private void BeginFadeOut()
	{
		_activeMusic = null;
		_cueIndex = 0;
		_fadingOut = true;
		_volumeTween?.Kill();
		_volumeTween = CreateTween();
		_volumeTween.TweenProperty(_player, "volume_db", SilentVolumeDb, FadeOutSeconds);
		_volumeTween.Finished += OnFadeOutFinished;
	}

	private void OnFadeOutFinished()
	{
		if (_volumeTween is not null)
			_volumeTween.Finished -= OnFadeOutFinished;

		_fadingOut = false;
		_player.Stop();
		_currentTrack = null;

		var nextMusic = _pendingMusic;
		_pendingMusic = null;
		if (nextMusic is not null)
			StartMusic(nextMusic.Value, fadeIn: true);
	}

	private void OnPlayerFinished()
	{
		if (_fadingOut || _activeMusic is null)
			return;

		var music = _activeMusic.Value;
		var nextIndex = _cueIndex + 1;
		if (nextIndex >= music.Cues.Length)
		{
			if (!music.Loop)
			{
				_activeMusic = null;
				return;
			}

			nextIndex = 0;
		}

		_cueIndex = nextIndex;
		StartCue(music.Cues[nextIndex], fadeIn: false);
	}

	private void StartMusic(SceneMusic music, bool fadeIn)
	{
		_activeMusic = music;
		_cueIndex = 0;
		StartCue(music.Cues[0], fadeIn);
	}

	private void StartCue(MusicCue cue, bool fadeIn)
	{
		_volumeTween?.Kill();
		_volumeTween = null;

		_player.Stream = GD.Load<AudioStream>(cue.Path);
		_currentTrack = cue.Path;

		if (fadeIn)
		{
			_player.VolumeDb = SilentVolumeDb;
			_player.Play();
			if (cue.StartSeconds > 0)
				_player.Seek((float)cue.StartSeconds);
			_volumeTween = CreateTween();
			_volumeTween.TweenProperty(_player, "volume_db", NormalVolumeDb, FadeInSeconds);
			return;
		}

		_player.VolumeDb = NormalVolumeDb;
		_player.Play();
		if (cue.StartSeconds > 0)
			_player.Seek((float)cue.StartSeconds);
	}

	private static SceneMusic LoopingTracks(int from, int to, bool shuffleOnLoad = false)
	{
		var cues = new MusicCue[to - from + 1];
		for (var i = from; i <= to; i++)
			cues[i - from] = new($"{TrackPathPrefix}{i:D2}.mp3");
		return new SceneMusic(cues, Loop: true, ShuffleOnLoad: shuffleOnLoad);
	}

	private static SceneMusic UnassignedTracksLoop(bool shuffleOnLoad = false)
	{
		var assigned = new HashSet<int>();
		for (var i = 1; i <= 6; i++)
			assigned.Add(i);
		for (var i = 7; i <= 14; i++)
			assigned.Add(i);
		assigned.Add(15);
		assigned.Add(19);

		var cues = new List<MusicCue>();
		for (var i = 1; i <= TrackCount; i++)
		{
			if (!assigned.Contains(i))
				cues.Add(new($"{TrackPathPrefix}{i:D2}.mp3"));
		}

		return new SceneMusic(cues.ToArray(), Loop: true, ShuffleOnLoad: shuffleOnLoad);
	}

	private static void Shuffle(MusicCue[] cues)
	{
		var rng = new RandomNumberGenerator();
		rng.Randomize();
		for (var i = cues.Length - 1; i > 0; i--)
		{
			var j = rng.RandiRange(0, i);
			(cues[i], cues[j]) = (cues[j], cues[i]);
		}
	}
}
