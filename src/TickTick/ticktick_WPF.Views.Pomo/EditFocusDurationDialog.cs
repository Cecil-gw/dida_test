using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Mapster;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Pomo;

public class EditFocusDurationDialog : Window, IComponentConnector
{
	internal ComboBox DurationCombo;

	internal TextBlock DescText;

	internal Button SaveButton;

	internal Button CancelButton;

	private bool _contentLoaded;

	public Rect? OwnerControlRect { get; set; }

	private ObservableCollection<int> durations { get; set; } = new ObservableCollection<int>();

	private PomodoroModel pomo { get; set; }

	private long rawSeconds { get; set; }

	public int TargetSeconds { get; set; } = -1;

	public EditFocusDurationDialog(bool isDark = false)
	{
		InitializeComponent();
		DurationCombo.ItemsSource = durations;
		if (isDark)
		{
			ThemeUtil.SetTheme("dark", this);
		}
	}

	protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
	{
		if (e.Source == this)
		{
			base.OnMouseLeftButtonDown(e);
			DragMove();
		}
	}

	public async Task LoadItem(PomodoroModel model)
	{
		pomo = model;
		if (pomo != null)
		{
			rawSeconds = Math.Max(0L, Utils.GetTotalSecond(pomo.StartTime, pomo.EndTime) - pomo.PauseDuration);
			int rawMinutes = (int)Math.Round((double)rawSeconds / 60.0);
			double num = Math.Min(180.0, (double)rawMinutes + (DateTime.Now - pomo.EndTime).TotalMinutes);
			for (int i = 5; (double)i <= num; i++)
			{
				durations.Add(i);
			}
			PomodoroModel pomodoroModel = await PomoService.GetAdjustPomo(pomo);
			DurationCombo.SelectedValue = (int)(pomodoroModel.IsAdjust ? (pomodoroModel.AdjustTime / 60000).Value : rawMinutes);
			await UpdateText();
		}
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		if (OwnerControlRect.HasValue)
		{
			Rect value = OwnerControlRect.Value;
			base.Left = value.X + 0.5 * (value.Width - base.ActualWidth);
			base.Top = value.Y + 0.5 * (value.Height - base.ActualHeight);
		}
		if (base.WindowStartupLocation == WindowStartupLocation.CenterOwner && base.Owner != null)
		{
			base.WindowStartupLocation = WindowStartupLocation.Manual;
			base.Left = base.Owner.Left + (base.Owner.Width - base.Width) / 2.0;
			base.Top = base.Owner.Top + (base.Owner.Height - base.Height) / 2.0 + 65.0;
		}
		if (base.Left < 0.0)
		{
			base.Left = 0.0;
		}
		else if (base.Left + base.ActualWidth > SystemParameters.PrimaryScreenWidth)
		{
			base.Left = SystemParameters.PrimaryScreenWidth - base.ActualWidth;
		}
		if (base.Top < 0.0)
		{
			base.Top = 0.0;
		}
		else if (base.Top + base.ActualHeight > SystemParameters.PrimaryScreenHeight)
		{
			base.Top = SystemParameters.PrimaryScreenHeight - base.ActualHeight;
		}
	}

	private void CancelButton_Click(object sender, RoutedEventArgs e)
	{
		TargetSeconds = -1;
		Close();
	}

	private async void SaveButton_Click(object sender, RoutedEventArgs e)
	{
		if (pomo != null && DurationCombo.SelectedItem != null)
		{
			TargetSeconds = (int)DurationCombo.SelectedItem * 60;
			Close();
		}
	}

	private async void DurationCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		await UpdateText();
	}

	private async Task UpdateText()
	{
		if (pomo != null && DurationCombo.SelectedItem != null)
		{
			int num = (int)DurationCombo.SelectedItem;
			PomodoroModel pomodoroModel = pomo.Adapt<PomodoroModel>();
			pomodoroModel.AdjustTime = num * 60 * 1000;
			pomodoroModel = await PomoService.GetAdjustPomo(pomodoroModel);
			DateTime startTime = pomodoroModel.StartTime;
			DateTime endTime = pomodoroModel.EndTime;
			string text = startTime.ToString("HH:mm");
			string text2 = endTime.ToString("HH:mm");
			if (startTime.Date != endTime.Date)
			{
				text = DateUtils.FormatMonthDay(startTime, DateFormatLen.Long) + " " + text;
				text2 = DateUtils.FormatMonthDay(endTime, DateFormatLen.Long) + " " + text2;
			}
			string text3 = Utils.GetString("DurationDescHeader") + text + " - " + text2;
			if (pomodoroModel.PauseDuration > 0)
			{
				TimeSpan timeSpan = TimeSpan.FromSeconds(pomodoroModel.PauseDuration);
				string arg = ((timeSpan.TotalHours >= 1.0) ? $"{(int)timeSpan.TotalHours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}" : timeSpan.ToString("mm\\:ss"));
				text3 = text3 + Environment.NewLine + string.Format(Utils.GetString("PauseDurationDesc"), arg);
			}
			DescText.Text = text3;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/editfocusdurationdialog.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((EditFocusDurationDialog)target).Loaded += Window_Loaded;
			break;
		case 2:
			DurationCombo = (ComboBox)target;
			DurationCombo.SelectionChanged += DurationCombo_SelectionChanged;
			break;
		case 3:
			DescText = (TextBlock)target;
			break;
		case 4:
			SaveButton = (Button)target;
			SaveButton.Click += SaveButton_Click;
			break;
		case 5:
			CancelButton = (Button)target;
			CancelButton.Click += CancelButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
