using System;
using TickTickModels;

namespace ticktick_WPF.Models;

[Serializable]
public class PomoConfigModel : ErrorModel
{
	public bool autoBreak;

	public bool autoPomo;

	public string id;

	public int longBreakDuration;

	public int longBreakInterval;

	public int pomoDuration;

	public int focusDuration;

	public int pomoGoal;

	public int shortBreakDuration;

	public bool lightsOn;
}
