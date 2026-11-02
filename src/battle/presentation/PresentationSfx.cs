using System;
using Godot;
using GrimSpace.Application;

namespace GrimSpace.Battle.Presentation;

public static class PresentationSfx
{
	public const string LightningCannonFirePath =
		"res://assets/sfx/abilities/lightning_cannon_charge_snap.wav";
	public const string LightningCannonMountChargePath =
		"res://assets/sfx/abilities/lightning_cannon_mount_charge.wav";
	public const string LightningCannonHitPath =
		"res://assets/sfx/abilities/lightning_cannon_hit.wav";
	public const string ScrapDroneFirePath =
		"res://assets/sfx/abilities/scrap_drone_flight_1150ms.wav";
	public const string ScrapDroneMountLatchPath =
		"res://assets/sfx/abilities/latch_release_80ms.wav";
	public const string ScrapDroneMountBeepsPath =
		"res://assets/sfx/abilities/robot_beeps_300ms.wav";
	public const string VoidBombExplosionPath =
		"res://assets/sfx/abilities/void_bomb_explosion.wav";
	public const string ShipDeathPath =
		"res://assets/sfx/ships/ship_death_enhanced_v2.wav";
	public const float ShipDeathPitchScale = 1.6f;
	public const string ShieldChangeHumPath =
		"res://assets/sfx/abilities/shield_static_hum.wav";
	public const string ShieldPowerDownPath =
		"res://assets/sfx/abilities/shield_power_down.wav";

	private static readonly Dictionary<string, AudioStream> Streams = new();

	public static void PlayWorldOneShot(
		Node parent,
		Vector3 localPosition,
		string path,
		float pitchScale = 1f)
	{
		var player = new AudioStreamPlayer3D
		{
			Stream = Load(path),
			Position = localPosition,
			PitchScale = pitchScale,
			Bus = AudioBuses.Sfx,
			MaxDistance = 4096f,
			AttenuationModel = AudioStreamPlayer3D.AttenuationModelEnum.Disabled,
		};
		parent.AddChild(player);
		player.Play();
		player.Finished += player.QueueFree;
	}

	public static void PlayScrapDroneMount(Node parent, Vector3 localPosition)
	{
		PlayWorldOneShot(parent, localPosition, ScrapDroneMountLatchPath);
		var tree = parent.GetTree();
		if (tree is null)
			return;

		tree.CreateTimer(0.08).Timeout += () =>
			PlayWorldOneShot(parent, localPosition, ScrapDroneMountBeepsPath);
	}

	public static AudioStreamPlayer3D PlayShieldChangeHum(Node parent, double duration)
	{
		var player = new AudioStreamPlayer3D
		{
			Stream = Load(ShieldChangeHumPath),
			Bus = AudioBuses.Sfx,
			VolumeDb = -26f,
			MaxDistance = 4096f,
			AttenuationModel = AudioStreamPlayer3D.AttenuationModelEnum.Disabled,
		};
		parent.AddChild(player);
		player.Play();
		var fade = player.CreateTween();
		fade.TweenProperty(player, "volume_db", -12f, 0.08);
		fade.TweenProperty(player, "volume_db", -60f, duration - 0.08)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.In);
		fade.TweenCallback(Callable.From(player.QueueFree));
		return player;
	}

	private static AudioStream Load(string path) =>
		Streams.TryGetValue(path, out var cached)
			? cached
			: Streams[path] = GD.Load<AudioStream>(path)
				?? throw new InvalidOperationException($"Could not load SFX '{path}'.");
}
