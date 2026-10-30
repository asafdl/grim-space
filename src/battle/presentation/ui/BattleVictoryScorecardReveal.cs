using Godot;
using System.Linq;

namespace GrimSpace.Battle.Presentation.Ui;

internal static class BattleVictoryScorecardReveal
{
	private const float InitialDelaySeconds = 1.2f;
	private const float RowBeatSeconds = 3.5f;
	private const float ExperienceFillSeconds = 2.8f;

	public static Tween Begin(
		Node host,
		AudioStreamPlayer doorLatchPlayer,
		IReadOnlyList<Control> rows,
		Action onComplete)
	{
		foreach (var row in rows)
			row.Visible = false;

		var tween = host.CreateTween();
		if (rows.Count == 0)
		{
			tween.TweenCallback(Callable.From(onComplete));
			return tween;
		}

		tween.TweenInterval(InitialDelaySeconds);
		for (var index = 0; index < rows.Count; index++)
		{
			var row = rows[index];
			if (index > 0)
				tween.TweenInterval(RowBeatSeconds);

			tween.TweenCallback(Callable.From(() => RevealRow(row, doorLatchPlayer)));
		}

		tween.TweenCallback(Callable.From(() => WaitForExperienceFills(host, rows, onComplete)));
		return tween;
	}

	private static void WaitForExperienceFills(
		Node host,
		IReadOnlyList<Control> rows,
		Action onComplete)
	{
		var pilotRows = rows.OfType<BattleVictoryPilotRowView>().ToArray();
		if (pilotRows.Length == 0)
		{
			onComplete();
			return;
		}

		var pending = pilotRows.Length;
		void OnRowFillFinished()
		{
			pending--;
			if (pending > 0)
				return;

			foreach (var row in pilotRows)
				row.ExperienceFillFinished -= OnRowFillFinished;
			onComplete();
		}

		foreach (var row in pilotRows)
		{
			if (row.IsExperienceFillIdle)
			{
				pending--;
				continue;
			}

			row.ExperienceFillFinished += OnRowFillFinished;
		}

		if (pending <= 0)
			onComplete();
	}

	private static void RevealRow(Control row, AudioStreamPlayer doorLatchPlayer)
	{
		row.Visible = true;
		doorLatchPlayer.Stop();
		doorLatchPlayer.Play();

		if (row is BattleVictoryPilotRowView pilotRow)
			pilotRow.BeginRowReveal(ExperienceFillSeconds);
	}
}
