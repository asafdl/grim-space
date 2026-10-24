using Godot;

namespace GrimSpace.Presentation.Graphics;

/// <summary>
/// The casting office of the star's prominences (the S2' law, user-ordered
/// 2026-10-22): there is ONE prominence role on the star at a time — a single
/// active loop, spoken by one of four holder functions.
///
/// <para>The roll itself is generated in the pipeline
/// (<c>scripts/make-sun-audio.py</c>, which mirrors the law into
/// <c>assets/data/sun_events.json</c>'s <c>prominence_roles</c> list: each
/// birth performs one seeded roll of 0..3, weighted toward 1, whose holder
/// owns its own start and death times). This class consumes that schedule:
/// it drives the shader's sole loop through
/// <see cref="ProminenceTurn"/> turns, publishing one gain per frame.</para>
///
/// <para>The envelope dissolves the loop in at birth and, reversed, out at
/// the death — the gain is exactly 0 there, a true disappearance. Between
/// turns the stage may sit all-quiet; that is legal. There is never more
/// than one prominent loop on the star.</para>
/// </summary>
public static class SunProminenceCasting
{
	/// <summary>The four holder functions of the one prominence role.</summary>
	public const int HolderCount = 4;

	/// <summary>
	/// One turn of the role: a holder on stage from <see cref="Start"/> to
	/// <see cref="Death"/> seconds on the star's casting clock.
	/// </summary>
	public readonly record struct ProminenceTurn(int Holder, float Start, float Death);

	/// <summary>
	/// Per-holder gain multipliers, indexed 0..3.  Loop 1's turn is the
	/// heavyweight; loop 0 is strong, loops 2-3 are lighter.
	/// </summary>
	public static readonly float[] HolderGains = [1.00f, 1.42f, 0.72f, 0.58f];

	/// <summary>The dissolve in and out of a life, as a fraction of the life.</summary>
	public const float DissolveFraction = 0.14f;

	/// <summary>
	/// How long (in player-seconds at 1x) the live prominence's GROWTH phase
	/// is designed to take — the rise from nothing to the held arc.  Five
	/// minutes, mid of the user-ordered 3–6 minute band ("the prominence
	/// animation is very fast, it should take minutes to grow!", 2026-10-24).
	/// The whole life (grow, plateau, dissolve back to exactly 0) scales from
	/// the timeline's design curve with this as the anchor; the one-live-at-
	/// a-time roll, the all-quiet gaps and death-at-0 are all unchanged.
	/// </summary>
	public const float GrowthSecondsAt1x = 300f;


	/// <summary>
	/// The holder live at <paramref name="time"/> seconds on the casting
	/// clock, and its gain.  <paramref name="time"/> runs on the casting
	/// clock; <paramref name="castingSecondsPerTimelineSecond"/> is how many
	/// casting seconds one timeline design second lasts (1 = design speed;
	/// the strategic map plays minutes scale — see
	/// <see cref="GrowthSecondsAt1x"/>).  In an all-quiet gap — or past the
	/// schedule's end, before the timeline loop wraps — the gain is 0.
	/// </summary>
	public static (int Holder, float Gain) Resolve(
		ReadOnlySpan<ProminenceTurn> schedule,
		float time,
		float castingSecondsPerTimelineSecond = 1f)
	{
		time /= Mathf.Max(castingSecondsPerTimelineSecond, 0.0001f);
		foreach (var turn in schedule)
		{
			if (time < turn.Start)
				return (turn.Holder, 0f); // the all-quiet gap before this birth
			if (time < turn.Death)
			{
				var life = Mathf.Max(turn.Death - turn.Start, 0.001f);
				return (turn.Holder, HolderGains[Mathf.Clamp(turn.Holder, 0, HolderCount - 1)] * Dissolve((time - turn.Start) / life));
			}
		}

		return (0, 0f);
	}

	/// <summary>
	/// The visual intensity at progress u in [0, 1] of a life.  The same
	/// S-ramp dissolves the loop in at birth and, reversed, dissolves it out
	/// at the death, so the value is exactly 0 at u=0 and at u=1: the death
	/// is a true disappearance, never a hover.
	/// </summary>
	public static float Dissolve(float u)
	{
		u = Mathf.Clamp(u, 0f, 1f);
		var rise = Mathf.Clamp(u / DissolveFraction, 0f, 1f);
		var decay = Mathf.Clamp((1f - u) / DissolveFraction, 0f, 1f);
		return Smooth(rise) * Smooth(decay);
	}

	private static float Smooth(float x)
	{
		x = Mathf.Clamp(x, 0f, 1f);
		return x * x * (3f - 2f * x);
	}
}
