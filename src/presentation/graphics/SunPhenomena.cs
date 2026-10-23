using Godot;

namespace GrimSpace.Presentation.Graphics;

/// <summary>
/// The state of the star's transient phenomena at one instant: the sunspots on
/// the surface, a flare, the coronal rain running back down the prominence
/// legs, and a coronal mass ejection lifting off the limb.
///
/// The schedule itself lives in <c>assets/data/sun_events.json</c> so that the
/// renderer and the sound generator read the same timeline and stay in sync.
/// </summary>
public sealed class SunPhenomena
{
	/// <summary>Sunspots: (direction x, direction y, radius, strength).</summary>
	public Vector4[] Spots { get; } = new Vector4[4];

	/// <summary>A flare: (direction x, direction y, strength, size).</summary>
	public Vector4 Flare { get; set; }

	/// <summary>How much coronal rain is running back down the loop legs.</summary>
	public float Rain { get; set; }

	/// <summary>A CME: (angle, progress 0..1, strength, radius).</summary>
	public Vector4 Cme { get; set; }

	public bool IsEmpty =>
		Flare.Z <= 0f && Rain <= 0f && Cme.Z <= 0f && !HasSpots;

	private bool HasSpots
	{
		get
		{
			foreach (var spot in Spots)
			{
				if (spot.W > 0f)
					return true;
			}
			return false;
		}
	}

	/// <summary>Loads the event timeline.  A missing file simply means no events.</summary>
	public static Godot.Collections.Array LoadEvents(string path)
	{
		return LoadTimeline(path).Events;
	}

	private static (Godot.Collections.Array Events, Godot.Collections.Array Spots, float Duration) LoadTimeline(string path)
	{
		if (!Godot.FileAccess.FileExists(path))
			return ([], [], 14f);
		var text = Godot.FileAccess.GetFileAsString(path);
		var parsed = Json.ParseString(text);
		if (parsed.VariantType != Variant.Type.Dictionary)
			return ([], [], 14f);
		var dictionary = parsed.AsGodotDictionary();
		return (
			dictionary.ContainsKey("events") ? dictionary["events"].AsGodotArray() : [],
			dictionary.ContainsKey("spots") ? dictionary["spots"].AsGodotArray() : [],
			dictionary.ContainsKey("duration") ? dictionary["duration"].AsSingle() : 14f);
	}

	/// <summary>
	/// Loads the persistent sunspot set (the "spots" array of the timeline).
	/// </summary>
	public static Godot.Collections.Array LoadSpots(string path)
	{
		return LoadTimeline(path).Spots;
	}

	/// <summary>
	/// The timeline's full period in seconds — the loop length the star map
	/// plays (and the rumble soundtrack's length), so the sun's picture and
	/// sound stay event-synced (the S4 law).
	/// </summary>
	public static float ReadTimelineDuration(string path)
	{
		return LoadTimeline(path).Duration;
	}

	/// <summary>
	/// Loads the timeline's prominence casting schedule — the
	/// <c>prominence_roles</c> list the pipeline writes (make-sun-audio.py,
	/// the mirror of the S2' roll law).  One loop at a time; each turn is one
	/// rolled holder's birth...death window on the casting clock.
	/// </summary>
	public static List<(int Holder, float Start, float Death)> LoadRoles(string path)
	{
		if (!Godot.FileAccess.FileExists(path))
			return [];
		var text = Godot.FileAccess.GetFileAsString(path);
		var parsed = Json.ParseString(text);
		if (parsed.VariantType != Variant.Type.Dictionary)
			return [];
		var dictionary = parsed.AsGodotDictionary();
		var roles = new List<(int Holder, float Start, float Death)>();
		if (!dictionary.ContainsKey("prominence_roles"))
			return roles;
		foreach (var element in dictionary["prominence_roles"].AsGodotArray())
		{
			if (element.VariantType != Variant.Type.Dictionary)
				continue;
			var entry = element.AsGodotDictionary();
			roles.Add((
				entry["holder"].AsInt32(),
				entry["start"].AsSingle(),
				entry["death"].AsSingle()));
		}

		return roles;
	}

	/// <summary>
	/// Evaluates the timeline at <paramref name="time"/> seconds.  Sunspots are
	/// persistent and drift slowly; the transient phenomena ramp in fast and
	/// decay slowly, which is how a real flare and a real CME behave.
	/// </summary>
	public static void Evaluate(
		Godot.Collections.Array events,
		Godot.Collections.Array spots,
		float time,
		SunPhenomena into)
	{
		for (var i = 0; i < 4; i++)
		{
			into.Spots[i] = Vector4.Zero;
		}
		into.Flare = Vector4.Zero;
		into.Rain = 0f;
		into.Cme = Vector4.Zero;

		for (var i = 0; i < spots.Count && i < 4; i++)
		{
			if (spots[i].VariantType != Variant.Type.Dictionary)
				continue;
			var spot = spots[i].AsGodotDictionary();
			var dir = spot["dir"].AsVector2();
			var radius = spot["radius"].AsSingle();
			var strength = spot["strength"].AsSingle();
			var drift = spot["drift"].AsSingle();
			// The spots ride the disc: they drift in longitude as the star turns.
			// The spot rides the disc: its longitude drifts as the star turns.
			var longitude = dir.X + drift * time;
			into.Spots[i] = new Vector4(longitude, dir.Y, radius, strength);
		}

		foreach (var element in events)
		{
			if (element.VariantType != Variant.Type.Dictionary)
				continue;
			var entry = element.AsGodotDictionary();
			var start = entry["t"].AsSingle();
			var duration = Mathf.Max(entry["duration"].AsSingle(), 0.01f);
			var strength = entry["strength"].AsSingle();
			var type = entry["type"].AsString();
			var elapsed = time - start;
			if (elapsed < 0f || elapsed > duration)
				continue;

			// Fast attack, slow decay.
			var attack = Mathf.Clamp(elapsed / Mathf.Min(duration * 0.12f, 0.25f), 0f, 1f);
			var decay = 1f - Mathf.Clamp((elapsed - duration * 0.12f) / (duration * 0.88f), 0f, 1f);
			var envelope = attack * (1f - decay) * strength;

			switch (type)
			{
				case "flare":
				{
					var dir = entry["dir"].AsVector2();
					into.Flare = new Vector4(dir.X, dir.Y, envelope, entry["size"].AsSingle());
					break;
				}
				case "cme":
				{
					var progress = Mathf.Clamp(elapsed / duration, 0f, 1f);
					into.Cme = new Vector4(
						entry["angle"].AsSingle(),
						progress,
						envelope,
						entry["radius"].AsSingle());
					break;
				}
				case "rain":
				{
					into.Rain = Mathf.Max(into.Rain, envelope);
					break;
				}
			}
		}
	}
}
