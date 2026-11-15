using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.MarkDown;

namespace ticktick_WPF.Views.Calendar;

public class BindICloudWindow : Window, IOkCancelWindow, IComponentConnector
{
	private SubscribeCalendar _parentCal;

	private bool _isReauthorize;

	private readonly BindCalendarAccountModel _account;

	internal Grid GetSepcificPassWordGrid;

	internal Run iCloudDesc;

	internal Run Period;

	internal Grid AccountPanel;

	internal Run UserNameRun;

	internal TextBox UsernameInput;

	internal TextBlock UsernameInputPlaceholder;

	internal PasswordBox PasswordInput;

	internal TextBlock PasswordInputPlaceholder;

	internal TextBox DescInput;

	internal TextBlock ErrorText;

	internal Run ErrorRun;

	internal Run OptionRun;

	internal Run PeriodRun;

	internal Grid Buttons;

	internal Button CancelButton;

	internal Button SaveButton;

	private bool _contentLoaded;

	public BindICloudWindow(SubscribeCalendar parent, BindCalendarAccountModel account = null)
	{
		InitializeComponent();
		_parentCal = parent;
		_account = account;
		_isReauthorize = account != null;
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		string text = Utils.GetString("iCloudDesc");
		if (text.Contains("{0}"))
		{
			List<string> list = text.SplitByStr("{0}");
			iCloudDesc.Text = list[0];
			Period.Text = list[1];
		}
		if (_isReauthorize)
		{
			base.Title = string.Format(Utils.GetString("ReauthorizeAccount"), "iCloud");
			ShowAccountPanel();
			UsernameInput.Text = _account.Account;
			DescInput.Text = _account.Description ?? string.Empty;
			PasswordInput.Focus();
		}
	}

	private void ShowAccountPanel()
	{
		AccountPanel.Visibility = Visibility.Visible;
		Buttons.Visibility = Visibility.Visible;
		GetSepcificPassWordGrid.Visibility = Visibility.Collapsed;
	}

	private void OnTextChanged(object sender, TextChangedEventArgs e)
	{
		ErrorRun.Text = string.Empty;
		PeriodRun.Text = string.Empty;
		OptionRun.Text = string.Empty;
		SaveButton.IsEnabled = !string.IsNullOrWhiteSpace(UsernameInput.Text) && !string.IsNullOrEmpty(PasswordInput.Password);
	}

	private void ClearNameText(object sender, MouseButtonEventArgs e)
	{
		UsernameInput.Text = "";
	}

	private void OnPasswordChanged(object sender, RoutedEventArgs e)
	{
		if (sender is PasswordBox passwordBox)
		{
			PasswordInputPlaceholder.Visibility = ((passwordBox.Password.Length > 0) ? Visibility.Collapsed : Visibility.Visible);
		}
		SaveButton.IsEnabled = !string.IsNullOrWhiteSpace(UsernameInput.Text) && !string.IsNullOrEmpty(PasswordInput.Password);
	}

	private void ClearDomainText(object sender, MouseButtonEventArgs e)
	{
		DescInput.Text = "";
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		Ok();
	}

	private void OnHavePasswordClick(object sender, MouseButtonEventArgs e)
	{
		ShowAccountPanel();
	}

	public void OnCancel()
	{
		Close();
	}

	public void Ok()
	{
		if (!string.IsNullOrWhiteSpace(UsernameInput.Text) && !string.IsNullOrEmpty(PasswordInput.Password))
		{
			ErrorRun.Text = string.Empty;
			PeriodRun.Text = string.Empty;
			OptionRun.Text = string.Empty;
			SetContentIsEnabled(isEnabled: false);
			if (_isReauthorize)
			{
				UpdateAccount();
			}
			else
			{
				BindAccount();
			}
		}
	}

	private async Task UpdateAccount()
	{
		BindAccountModel model = new BindAccountModel
		{
			username = UsernameInput.Text.Trim(),
			password = PasswordInput.Password,
			desc = DescInput.Text.Trim()
		};
		string text = await Communicator.UpdateICloudAccount(_account.Id, model);
		if (text.Contains("caldav_bind_faild") || text.Contains("false"))
		{
			ErrorRun.Text = Utils.GetString("CalendarBindFaild");
			PeriodRun.Text = string.Empty;
			OptionRun.Text = string.Empty;
			SetContentIsEnabled(isEnabled: true);
			return;
		}
		await BindCalendarAccountDao.DeleteCalendarsByAccountId(_account?.Id);
		await CalendarService.DeleteEvent(_account?.Id);
		PullEvent(_account?.Id);
		SetContentIsEnabled(isEnabled: true);
		if (_parentCal != null)
		{
			await _parentCal.LoadData(needPull: false);
		}
		Close();
	}

	private async Task PullEvent(string id)
	{
		await CalendarService.PullAccountCalendarsAndEvents(null, null, id);
		CalendarEventChangeNotifier.NotifyRemoteChanged();
	}

	private async Task BindAccount()
	{
		_ = 2;
		try
		{
			BindCalendarAccountModel calendar = await Communicator.BindICloudAccount(new BindAccountModel
			{
				username = UsernameInput.Text.Trim(),
				password = PasswordInput.Password,
				desc = DescInput.Text.Trim()
			});
			if (calendar == null)
			{
				throw new CustomException.CalendarBindException("calendar_bind_faild");
			}
			await BindCalendarAccountDao.SaveBindCalendarAccount(calendar);
			if (_parentCal != null)
			{
				await _parentCal.LoadData(needPull: false);
			}
			ListViewContainer.ReloadProjectData();
			CalendarService.PullBindCalEvents(calendar.Id, null, null, Constants.BindAccountType.CalDAV);
			SetContentIsEnabled(isEnabled: true);
			Close();
		}
		catch (CustomException.CalendarBindException ex)
		{
			OptionRun.Text = string.Empty;
			PeriodRun.Text = string.Empty;
			if (ex.Message == "calendar_bind_duplicate")
			{
				ErrorRun.Text = Utils.GetString("CalendarBindDuplicate");
			}
			else if (ex.Message == "calendar_bind_faild")
			{
				string text = Utils.GetString("iCloudPasswordError");
				if (text.Contains("{0}"))
				{
					List<string> list = text.SplitByStr("{0}");
					ErrorRun.Text = list[0];
					PeriodRun.Text = list[1];
					OptionRun.Text = Utils.GetString("AppSepcificPassword");
				}
			}
			else if (ex.Message == "calendar_bind_exceed")
			{
				ErrorRun.Text = string.Format(Utils.GetString("CalendarBindExceed"), "iCloud");
			}
			else
			{
				ErrorRun.Text = ex.Message;
			}
		}
		finally
		{
			SetContentIsEnabled(isEnabled: true);
		}
	}

	private void SetContentIsEnabled(bool isEnabled)
	{
		if (isEnabled)
		{
			SaveButton.Content = Utils.GetString("OK");
			SaveButton.IsEnabled = true;
			SaveButton.Opacity = 1.0;
			UsernameInput.IsEnabled = true;
			PasswordInput.IsEnabled = true;
			DescInput.IsEnabled = true;
			CancelButton.IsEnabled = true;
		}
		else
		{
			SaveButton.Content = Utils.GetString("Subscribing");
			SaveButton.IsEnabled = false;
			SaveButton.Opacity = 0.699999988079071;
			CancelButton.IsEnabled = false;
			UsernameInput.IsEnabled = false;
			PasswordInput.IsEnabled = false;
			DescInput.IsEnabled = false;
		}
	}

	private void GetSepcificPassword(object sender, MouseButtonEventArgs e)
	{
		Utils.TryProcessStartUrl("https://appleid.apple.com");
	}

	private void OpenGuide(object sender, MouseButtonEventArgs e)
	{
		OpenGuide();
	}

	private async Task OpenGuide()
	{
		string text = await Communicator.GetICloudGuideUrl();
		if (string.IsNullOrEmpty(text))
		{
			text = (Utils.IsZhCn() ? "https://support.apple.com/zh-cn/HT204397" : "https://support.apple.com/en-us/HT204397");
		}
		Utils.TryProcessStartUrl(text);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/bindicloudwindow.xaml", UriKind.Relative);
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
			((BindICloudWindow)target).Loaded += OnWindowLoaded;
			break;
		case 2:
			GetSepcificPassWordGrid = (Grid)target;
			break;
		case 3:
			iCloudDesc = (Run)target;
			break;
		case 4:
			((Run)target).MouseLeftButtonUp += OpenGuide;
			break;
		case 5:
			Period = (Run)target;
			break;
		case 6:
			((Border)target).MouseLeftButtonUp += OnHavePasswordClick;
			break;
		case 7:
			((Border)target).MouseLeftButtonUp += GetSepcificPassword;
			break;
		case 8:
			AccountPanel = (Grid)target;
			break;
		case 9:
			UserNameRun = (Run)target;
			break;
		case 10:
			UsernameInput = (TextBox)target;
			UsernameInput.TextChanged += OnTextChanged;
			break;
		case 11:
			UsernameInputPlaceholder = (TextBlock)target;
			break;
		case 12:
			((Border)target).MouseLeftButtonUp += ClearNameText;
			break;
		case 13:
			PasswordInput = (PasswordBox)target;
			PasswordInput.PasswordChanged += OnPasswordChanged;
			break;
		case 14:
			PasswordInputPlaceholder = (TextBlock)target;
			break;
		case 15:
			DescInput = (TextBox)target;
			break;
		case 16:
			((Border)target).MouseLeftButtonUp += ClearDomainText;
			break;
		case 17:
			ErrorText = (TextBlock)target;
			break;
		case 18:
			ErrorRun = (Run)target;
			break;
		case 19:
			OptionRun = (Run)target;
			OptionRun.MouseLeftButtonUp += GetSepcificPassword;
			break;
		case 20:
			PeriodRun = (Run)target;
			break;
		case 21:
			Buttons = (Grid)target;
			break;
		case 22:
			CancelButton = (Button)target;
			CancelButton.Click += OnCancelClick;
			break;
		case 23:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
