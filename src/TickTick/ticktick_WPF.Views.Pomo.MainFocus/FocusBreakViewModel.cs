using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Pomo.MainFocus;

internal class FocusBreakViewModel : INotifyPropertyChanged
{
	private bool _isShowAppendTime;

	private bool _isShowAppendTimeTip;

	private string _baseDurationText;

	private string _appendTimeText;

	private string _relaxTimeText;

	private bool _isDark;

	public bool IsShowAppendTime
	{
		get
		{
			return _isShowAppendTime;
		}
		set
		{
			_isShowAppendTime = value;
			NotifyPropertyChanged("IsShowAppendTime");
		}
	}

	public bool IsShowAppendTimeTip
	{
		get
		{
			return _isShowAppendTimeTip;
		}
		set
		{
			_isShowAppendTimeTip = value;
			NotifyPropertyChanged("IsShowAppendTimeTip");
		}
	}

	public string BaseDurationText
	{
		get
		{
			return _baseDurationText;
		}
		set
		{
			_baseDurationText = value;
			NotifyPropertyChanged("BaseDurationText");
		}
	}

	public string AppendTimeText
	{
		get
		{
			return _appendTimeText;
		}
		set
		{
			_appendTimeText = value;
			NotifyPropertyChanged("AppendTimeText");
		}
	}

	public string RelaxTimeText
	{
		get
		{
			return _relaxTimeText;
		}
		set
		{
			_relaxTimeText = value;
			NotifyPropertyChanged("RelaxTimeText");
		}
	}

	public bool IsDark
	{
		get
		{
			return _isDark;
		}
		set
		{
			_isDark = value;
			NotifyPropertyChanged("IsDark");
		}
	}

	public Action<bool> OnShowAppendTimeAnimate { get; set; }

	private DispatcherTimer timer { get; set; } = new DispatcherTimer
	{
		Interval = TimeSpan.FromSeconds(1.0)
	};

	private DateTime finishTime { get; set; }

	private DateTime startTime { get; set; }

	private double pauseDuration { get; set; }

	private double rawPomoActualDuration { get; set; }

	private int appendMin { get; set; }

	public event PropertyChangedEventHandler PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public FocusBreakViewModel()
	{
		timer.Tick += OnTimerTick;
	}

	public void HandlePageChange(PomoPageChangeArgs e)
	{
		switch (e.EventType)
		{
		case PomoPageEventType.EditConfirm:
			EditConfirm((int)e.Data);
			break;
		case PomoPageEventType.EditFinish:
			FocusTimer.Config.LastEditDurationId = ((e.Source == PomoDurationChangeSource.AppendDuration) ? null : FocusTimer.Config.Id);
			SetText();
			break;
		case PomoPageEventType.EditUndo:
			SetText();
			break;
		}
	}

	private void OnTimerTick(object sender, EventArgs e)
	{
		CheckAppendTime();
	}

	private void CheckAppendTime()
	{
		double totalMinutes = (DateTime.Now - finishTime).TotalMinutes;
		if (totalMinutes > 1.0)
		{
			TimeSpan timeSpan = DateTime.Now - finishTime;
			double num = rawPomoActualDuration / 60.0;
			timeSpan = ((timeSpan.TotalMinutes + num > 180.0) ? TimeSpan.FromMinutes(180.0 - num) : timeSpan);
			AppendTimeText = ((timeSpan.TotalHours >= 1.0) ? $"{(int)timeSpan.TotalHours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}" : timeSpan.ToString("mm\\:ss"));
			if (!IsShowAppendTime)
			{
				IsShowAppendTime = true;
				OnShowAppendTimeAnimate?.Invoke(obj: true);
			}
			if (!LocalSettings.Settings.ExtraSettings.CheckHasFlag(HasSetFlag.PomoAppendHint))
			{
				LocalSettings.Settings.ExtraSettings.SetHasFlag(HasSetFlag.PomoAppendHint);
				LocalSettings.Settings.Save();
				IsShowAppendTimeTip = true;
			}
		}
		if (totalMinutes >= 182.0)
		{
			timer.Stop();
		}
	}

	public async void SetText()
	{
		long relaxMinutes = TickFocusManager.Config.GetRelaxMinutes();
		RelaxTimeText = string.Format(Utils.GetString((relaxMinutes > 1) ? "RelaxNMins" : "Relax1Min"), relaxMinutes);
		PomodoroModel pomodoroModel = await PomoService.GetAdjustPomo(FocusTimer.Config.Id);
		if (pomodoroModel == null)
		{
			UtilLog.Error("last focus id“" + FocusTimer.Config.Id + "” not found");
			return;
		}
		finishTime = pomodoroModel.EndTime;
		startTime = pomodoroModel.StartTime;
		pauseDuration = pomodoroModel.PauseDuration;
		rawPomoActualDuration = (double)Utils.GetTotalSecond(startTime, finishTime) - pauseDuration;
		TimeSpan timeSpan = TimeSpan.FromSeconds(rawPomoActualDuration);
		BaseDurationText = ((timeSpan.TotalHours >= 1.0) ? $"{(int)timeSpan.TotalHours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}" : timeSpan.ToString("mm\\:ss"));
		OnShowAppendTimeAnimate?.Invoke(obj: false);
		IsShowAppendTime = false;
		IsShowAppendTimeTip = false;
		timer.Stop();
		if (!(FocusTimer.Config.LastEditDurationId == FocusTimer.Config.Id) && !(rawPomoActualDuration >= 10800.0))
		{
			timer.Start();
			CheckAppendTime();
		}
	}

	public void EditConfirm(int targetSeconds)
	{
		timer.Stop();
		TimeSpan timeSpan = TimeSpan.FromSeconds(targetSeconds);
		BaseDurationText = ((timeSpan.TotalHours >= 1.0) ? $"{(int)timeSpan.TotalHours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}" : timeSpan.ToString("mm\\:ss"));
		OnShowAppendTimeAnimate?.Invoke(obj: false);
		IsShowAppendTime = false;
		IsShowAppendTimeTip = false;
	}

	public void Dispose()
	{
		timer.Stop();
		timer.Tick -= OnTimerTick;
	}
}
