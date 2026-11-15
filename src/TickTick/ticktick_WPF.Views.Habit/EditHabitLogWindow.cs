using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Emoji.Wpf;
using ticktick_WPF.Dal;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Habit;

public class EditHabitLogWindow : MyWindow, IComponentConnector
{
	private readonly bool _isEdit;

	private CheckInLogViewModel _source;

	internal Image IconImage;

	internal Border IconTextBorder;

	internal EmjTextBlock IconText;

	internal new TextBlock Title;

	internal TextBlock CheckInDateText;

	internal TextBox TitleTextBox;

	internal TextBlock HintText;

	internal TextBlock DoNotShowText;

	internal Button OkButton;

	private bool _contentLoaded;

	public EditHabitLogWindow()
	{
		InitializeComponent();
	}

	public EditHabitLogWindow(CheckInLogViewModel model, bool isEdit = true, bool isNew = false)
	{
		InitializeComponent();
		if (isEdit)
		{
			DoNotShowText.Visibility = Visibility.Collapsed;
		}
		if (isNew)
		{
			model.Score = (model.UnCompleted ? 10 : 50);
		}
		_source = model;
		base.DataContext = model.Copy();
		_isEdit = isEdit;
		Title.Text = model.HabitName;
		CheckInDateText.Text = GetFormatedDateStr(model.Date);
		HintText.Text = (model.UnCompleted ? Utils.GetString("UnCompleteLogHint") : (model.IsBoolHabit ? Utils.GetString("CheckInLogHint") : Utils.GetString("CheckInLogHintReal")));
		SetIcon(model.IconRes, model.Color);
		base.Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		Activate();
	}

	private string GetFormatedDateStr(DateTime fullDate)
	{
		string text = DateUtils.FormatShortDate(fullDate);
		string text2 = DateUtils.FormatWeekDayName(fullDate, withSmart: true);
		if (!Utils.IsEn())
		{
			return text + " " + text2;
		}
		return text2 + ", " + text;
	}

	private void SetIcon(string iconRes, string color)
	{
		iconRes = (string.IsNullOrWhiteSpace(iconRes) ? "habit_daily_check_in" : iconRes);
		if (iconRes.StartsWith("txt"))
		{
			IconTextBorder.Background = (string.IsNullOrEmpty(color) ? ThemeUtil.GetPrimaryColor(0.1) : ThemeUtil.GetColorInString(color));
			IconText.Text = iconRes.Split('_').LastOrDefault() ?? "";
			IconText.Foreground = (string.IsNullOrEmpty(color) ? ThemeUtil.GetColor("PrimaryColor") : Brushes.White);
			IconImage.Visibility = Visibility.Collapsed;
			return;
		}
		try
		{
			IconImage.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/Habits/" + iconRes.ToLower() + ".png"));
		}
		catch (Exception)
		{
		}
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void OnEscKeyDown(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
		case Key.Escape:
			if (AskCancel() != true)
			{
				TitleTextBox.Focus();
			}
			else
			{
				Close();
			}
			break;
		case Key.Return:
			if (Utils.IfCtrlPressed())
			{
				OnSaveClick(this, e);
			}
			break;
		}
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		CheckInLogViewModel data = GetData();
		if (data != null)
		{
			if (!_isEdit || string.IsNullOrEmpty(data.Id))
			{
				if (!string.IsNullOrEmpty(TitleTextBox.Text.Trim()) || data.Score > 0)
				{
					data.Content = TitleTextBox.Text.Trim();
					data.Score = data.Score;
					await HabitService.AddCheckInRecord(data);
				}
			}
			else if (!string.IsNullOrEmpty(TitleTextBox.Text.Trim()) || data.Score > 0)
			{
				data.Content = TitleTextBox.Text.Trim();
				await HabitService.SaveHabitCheckinRecord(data.Id, TitleTextBox.Text, data.Score);
				_source.Content = data.Content;
				_source.Score = data.Score;
			}
			else
			{
				_source.IsHide = true;
				await HabitService.DeleteCheckInRecord(data.Id);
			}
		}
		Close();
	}

	private CheckInLogViewModel GetData()
	{
		if (base.DataContext != null && base.DataContext is CheckInLogViewModel result)
		{
			return result;
		}
		return null;
	}

	private bool? AskCancel()
	{
		if (string.IsNullOrEmpty(TitleTextBox.Text.Trim()))
		{
			return true;
		}
		return new CustomerDialog(Utils.GetString("ConfirmDiscardChanges"), Utils.GetString("DiscardHabitLogConfirmHint"), Utils.GetString("Discard"), Utils.GetString("KeepEditing")).ShowDialog();
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		if (AskCancel() != true)
		{
			TitleTextBox.Focus();
		}
		else
		{
			Close();
		}
	}

	private async void OnLogWindowLoaded(object sender, RoutedEventArgs e)
	{
		CheckInLogViewModel data = GetData();
		if (data != null)
		{
			TitleTextBox.Text = data.Content;
			TitleTextBox.Focus();
			TitleTextBox.CaretIndex = data.Content.Length;
		}
	}

	private async void OnDoNotShowClick(object sender, MouseButtonEventArgs e)
	{
		CheckInLogViewModel data = GetData();
		if (data != null)
		{
			CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("DoNotShowHabitRecord"), Utils.GetString("DoNotShowHabitRecordHint"), Utils.GetString("GotIt"), string.Empty);
			customerDialog.Owner = Window.GetWindow(this);
			customerDialog.ShowDialog();
			await HabitDao.HabitUnableRecord(data.HabitId);
			Close();
		}
	}

	private void OnTitleTextChanged(object sender, TextChangedEventArgs e)
	{
		OkButton.IsEnabled = TitleTextBox.Text.Length <= 1024;
	}

	private void OnScoreClick(object sender, MouseButtonEventArgs e)
	{
		CheckInLogViewModel data = GetData();
		if (data != null && sender is Grid grid && int.TryParse(grid.Tag as string, out var result))
		{
			data.Score = ((data.Score != result) ? result : 0);
		}
	}

	private void TryDargMove(object sender, MouseButtonEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			DragMove();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/habit/edithabitlogwindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((Border)target).PreviewMouseLeftButtonDown += TryDargMove;
			break;
		case 2:
			IconImage = (Image)target;
			break;
		case 3:
			IconTextBorder = (Border)target;
			break;
		case 4:
			IconText = (EmjTextBlock)target;
			break;
		case 5:
			Title = (TextBlock)target;
			break;
		case 6:
			CheckInDateText = (TextBlock)target;
			break;
		case 7:
			((Grid)target).MouseLeftButtonUp += OnScoreClick;
			break;
		case 8:
			((Grid)target).MouseLeftButtonUp += OnScoreClick;
			break;
		case 9:
			((Grid)target).MouseLeftButtonUp += OnScoreClick;
			break;
		case 10:
			((Grid)target).MouseLeftButtonUp += OnScoreClick;
			break;
		case 11:
			((Grid)target).MouseLeftButtonUp += OnScoreClick;
			break;
		case 12:
			TitleTextBox = (TextBox)target;
			TitleTextBox.KeyDown += OnEscKeyDown;
			TitleTextBox.TextChanged += OnTitleTextChanged;
			break;
		case 13:
			HintText = (TextBlock)target;
			break;
		case 14:
			DoNotShowText = (TextBlock)target;
			DoNotShowText.MouseLeftButtonUp += OnDoNotShowClick;
			break;
		case 15:
			OkButton = (Button)target;
			OkButton.Click += OnSaveClick;
			break;
		case 16:
			((Button)target).Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
